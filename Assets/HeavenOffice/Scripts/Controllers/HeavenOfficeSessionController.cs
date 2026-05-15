using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HeavenOffice
{
    public sealed class HeavenOfficeSessionController
    {
        private readonly HeavenOfficeSessionConfig config;
        private readonly DifficultyController difficultyController;
        private readonly SoulQueueController queueController;
        private readonly DocumentDecisionEvaluator decisionEvaluator;
        private readonly HeavenOfficeScoreController scoreController;
        private readonly IHeavenOfficeAnalytics analytics;
        private readonly List<float> reactionTimes = new List<float>();

        private Soul activeSoul;
        private float documentTimer;
        private float currentTimeLimit;
        private float sessionTimer;
        private float resolveDelayRemaining;
        private float nextSoulDelayRemaining;
        private int mistakeCount;
        private int correctDecisionCount;
        private bool activeDocumentResolved;
        private string lastDecisionFeedback = string.Empty;
        private HeavenOfficeSessionState state = HeavenOfficeSessionState.NotStarted;

        public HeavenOfficeSessionController(HeavenOfficeSessionConfig config, IHeavenOfficeAnalytics analytics)
        {
            this.config = config;
            this.analytics = analytics;
            difficultyController = new DifficultyController(config);
            queueController = new SoulQueueController();
            decisionEvaluator = new DocumentDecisionEvaluator();
            scoreController = new HeavenOfficeScoreController(config);
            SessionId = Guid.NewGuid().ToString("N");
        }

        public event Action<HeavenOfficeSnapshot> SnapshotChanged;
        public event Action<Soul, HeavenOfficeSnapshot, IReadOnlyList<StampDefinition>> SoulPresented;
        public event Action<DecisionResult> DecisionResolved;
        public event Action<HeavenOfficeSessionResult> SessionFinished;

        public string SessionId { get; }
        public HeavenOfficeSessionState State => state;
        public Soul ActiveSoul => activeSoul;

        public void StartSession()
        {
            state = HeavenOfficeSessionState.Initializing;
            sessionTimer = config.sessionTimeLimit;
            queueController.BuildQueue(config, difficultyController, Environment.TickCount);
            currentTimeLimit = difficultyController.GetDocumentTimeLimit(0);
            analytics.SessionStarted(CreateSnapshot(), SessionId, difficultyController.GetAvailableStamps(0).Select(stamp => stamp.stampId).ToArray());
            PresentNextSoul();
        }

        public void Tick(float deltaTime)
        {
            if (config.enableSessionTimer && state != HeavenOfficeSessionState.Completed && state != HeavenOfficeSessionState.Failed && state != HeavenOfficeSessionState.Exited)
            {
                sessionTimer -= deltaTime;
                if (sessionTimer <= 0f)
                {
                    CompleteSession(HeavenOfficeCompletionReason.SessionTimerExpired);
                    return;
                }
            }

            if (state == HeavenOfficeSessionState.DocumentActive || state == HeavenOfficeSessionState.StampInteraction)
            {
                documentTimer -= deltaTime;
                if (documentTimer <= 0f)
                {
                    HandleTimeExpired();
                    return;
                }

                PublishSnapshot();
                return;
            }

            if (state == HeavenOfficeSessionState.DecisionResolving)
            {
                resolveDelayRemaining -= deltaTime;
                if (resolveDelayRemaining <= 0f)
                {
                    state = HeavenOfficeSessionState.TransitionToNextSoul;
                    nextSoulDelayRemaining = config.nextSoulDelay;
                    PublishSnapshot();
                }
            }
            else if (state == HeavenOfficeSessionState.TransitionToNextSoul)
            {
                nextSoulDelayRemaining -= deltaTime;
                if (nextSoulDelayRemaining <= 0f)
                {
                    PresentNextSoul();
                }
            }
        }

        public bool CanAcceptInput(string documentId)
        {
            return activeSoul != null
                && activeSoul.Document.DocumentId == documentId
                && !activeDocumentResolved
                && (state == HeavenOfficeSessionState.DocumentActive || state == HeavenOfficeSessionState.StampInteraction);
        }

        public bool BeginStampInteraction(string documentId)
        {
            if (!CanAcceptInput(documentId))
            {
                return false;
            }

            if (state == HeavenOfficeSessionState.DocumentActive)
            {
                state = HeavenOfficeSessionState.StampInteraction;
                PublishSnapshot();
            }

            return true;
        }

        public bool PlaceStamp(string documentId, string stampId)
        {
            if (!CanAcceptInput(documentId))
            {
                return false;
            }

            state = HeavenOfficeSessionState.DecisionResolving;
            activeDocumentResolved = true;
            var reactionTime = currentTimeLimit - documentTimer;
            analytics.StampPlaced(SessionId, activeSoul, stampId, reactionTime, CreateSnapshot());

            var expectedStampId = decisionEvaluator.GetExpectedStampId(activeSoul.Document);
            var mistakeReason = decisionEvaluator.GetMistakeReason(activeSoul.Document, stampId);
            var result = CreateDecisionResult(stampId, expectedStampId, reactionTime, mistakeReason);
            ApplyDecisionResult(result);
            return true;
        }

        public void ExitSession()
        {
            if (state == HeavenOfficeSessionState.Completed || state == HeavenOfficeSessionState.Failed || state == HeavenOfficeSessionState.Exited)
            {
                return;
            }

            state = HeavenOfficeSessionState.Exited;
            analytics.SessionExited(SessionId, queueController.ProcessedCount, scoreController.Score, HeavenOfficeCompletionReason.PlayerExited.ToString(), GetCurrentDifficulty());
            PublishSnapshot();
        }

        public IReadOnlyList<StampDefinition> GetAvailableStamps()
        {
            return difficultyController.GetAvailableStamps(Mathf.Max(0, queueController.CurrentIndex - 1));
        }

        private void PresentNextSoul()
        {
            if (!queueController.HasNext)
            {
                CompleteSession(HeavenOfficeCompletionReason.QueueCompleted);
                return;
            }

            activeSoul = queueController.TakeNext();
            activeSoul.State = SoulState.PresentingDocument;
            activeDocumentResolved = false;
            currentTimeLimit = difficultyController.GetDocumentTimeLimit(queueController.CurrentIndex - 1);
            documentTimer = currentTimeLimit;
            state = HeavenOfficeSessionState.DocumentActive;
            var snapshot = CreateSnapshot();
            var availableStamps = difficultyController.GetAvailableStamps(queueController.CurrentIndex - 1);
            analytics.DocumentShown(SessionId, activeSoul, snapshot, availableStamps.Select(stamp => stamp.stampId).ToArray());
            SoulPresented?.Invoke(activeSoul, snapshot, availableStamps);
            SnapshotChanged?.Invoke(snapshot);
        }

        private void HandleTimeExpired()
        {
            if (activeSoul == null || activeDocumentResolved)
            {
                return;
            }

            state = HeavenOfficeSessionState.DecisionResolving;
            activeDocumentResolved = true;
            activeSoul.State = SoulState.Expired;
            var reactionTime = currentTimeLimit;
            var expectedStampId = decisionEvaluator.GetExpectedStampId(activeSoul.Document);
            var result = CreateDecisionResult(string.Empty, expectedStampId, reactionTime, HeavenOfficeMistakeReason.TimeExpired);
            ApplyDecisionResult(result);
            analytics.TimeExpired(result);
        }

        private DecisionResult CreateDecisionResult(string selectedStampId, string expectedStampId, float reactionTime, HeavenOfficeMistakeReason mistakeReason)
        {
            return new DecisionResult
            {
                SessionId = SessionId,
                SoulId = activeSoul.SoulId,
                DocumentId = activeSoul.Document.DocumentId,
                SelectedStampId = selectedStampId,
                ExpectedStampId = expectedStampId,
                IsCorrect = mistakeReason == HeavenOfficeMistakeReason.None,
                ReactionTime = reactionTime,
                MistakeReason = mistakeReason,
                FinalDirection = string.IsNullOrEmpty(selectedStampId) ? expectedStampId : selectedStampId,
                DifficultyLevel = GetCurrentDifficulty(),
                CaseType = activeSoul.Document.CaseType
            };
        }

        private void ApplyDecisionResult(DecisionResult result)
        {
            reactionTimes.Add(result.ReactionTime);
            if (result.IsCorrect)
            {
                activeSoul.State = SoulState.ResolvedCorrectly;
                correctDecisionCount++;
                result.ScoreDelta = scoreController.ApplyCorrect(result.ReactionTime, currentTimeLimit);
                result.ComboValue = scoreController.Combo;
                lastDecisionFeedback = $"Верно: {GetStampDisplayName(result.ExpectedStampId)}. +{result.ScoreDelta}";
                analytics.CorrectDecision(result);
            }
            else
            {
                activeSoul.State = result.MistakeReason == HeavenOfficeMistakeReason.TimeExpired ? SoulState.Expired : SoulState.ResolvedIncorrectly;
                var previousCombo = scoreController.Combo;
                var endedCombo = scoreController.ApplyMistake();
                result.Penalty = config.mistakePenalty;
                result.ComboValue = scoreController.Combo;
                if (result.MistakeReason != HeavenOfficeMistakeReason.TimeExpired || config.timeExpiredCountsAsMistake)
                {
                    mistakeCount++;
                }

                if (previousCombo > 0 && config.comboResetOnMistake)
                {
                    analytics.ComboEnded(SessionId, endedCombo, result.MistakeReason.ToString(), result.DocumentId, result.DifficultyLevel);
                }

                lastDecisionFeedback = result.MistakeReason == HeavenOfficeMistakeReason.TimeExpired
                    ? $"Время истекло. Нужно было: {GetStampDisplayName(result.ExpectedStampId)}."
                    : $"Ошибка: выбрано {GetStampDisplayName(result.SelectedStampId)}, нужно {GetStampDisplayName(result.ExpectedStampId)}.";
                analytics.Mistake(result);
            }

            resolveDelayRemaining = config.decisionResolveDelay;
            DecisionResolved?.Invoke(result);
            PublishSnapshot();

            if (mistakeCount >= config.maxMistakeCount)
            {
                CompleteSession(HeavenOfficeCompletionReason.MaxMistakesReached);
            }
        }

        private void CompleteSession(HeavenOfficeCompletionReason reason)
        {
            state = reason == HeavenOfficeCompletionReason.MaxMistakesReached ? HeavenOfficeSessionState.Failed : HeavenOfficeSessionState.Completed;
            var result = new HeavenOfficeSessionResult
            {
                SessionId = SessionId,
                FinalScore = scoreController.Score,
                ProcessedSoulCount = queueController.ProcessedCount,
                CorrectDecisionCount = correctDecisionCount,
                MistakeCount = mistakeCount,
                MaxCombo = scoreController.MaxCombo,
                AverageReactionTime = reactionTimes.Count == 0 ? 0f : reactionTimes.Average(),
                CompletionReason = reason,
                FinalDifficultyLevel = GetCurrentDifficulty()
            };
            analytics.SessionCompleted(result);
            PublishSnapshot();
            SessionFinished?.Invoke(result);
        }

        private HeavenOfficeSnapshot CreateSnapshot()
        {
            return new HeavenOfficeSnapshot
            {
                State = state,
                Score = scoreController.Score,
                MistakeCount = mistakeCount,
                Combo = scoreController.Combo,
                MaxCombo = scoreController.MaxCombo,
                QueueIndex = queueController.CurrentIndex,
                QueueCount = queueController.QueueCount,
                DocumentTimeRemaining = Mathf.Max(0f, documentTimer),
                DocumentTimeLimit = currentTimeLimit,
                DifficultyLevel = GetCurrentDifficulty(),
                CurrentRuleHint = difficultyController.GetRuleHint(Mathf.Max(0, queueController.CurrentIndex - 1)),
                LastDecisionFeedback = lastDecisionFeedback
            };
        }

        private void PublishSnapshot()
        {
            SnapshotChanged?.Invoke(CreateSnapshot());
        }

        private int GetCurrentDifficulty()
        {
            return difficultyController.GetDifficultyLevel(Mathf.Max(0, queueController.CurrentIndex - 1));
        }

        private string GetStampDisplayName(string stampId)
        {
            if (string.IsNullOrEmpty(stampId))
            {
                return "нет печати";
            }

            foreach (var stamp in config.stampSetConfig)
            {
                if (stamp.stampId == stampId)
                {
                    return stamp.displayName;
                }
            }

            return stampId;
        }
    }
}
