using UnityEngine;

namespace HeavenOffice
{
    public interface IHeavenOfficeAnalytics
    {
        void SessionStarted(HeavenOfficeSnapshot snapshot, string sessionId, string[] enabledStampIds);
        void DocumentShown(string sessionId, Soul soul, HeavenOfficeSnapshot snapshot, string[] availableStampIds);
        void StampPlaced(string sessionId, Soul soul, string selectedStampId, float reactionTime, HeavenOfficeSnapshot snapshot);
        void CorrectDecision(DecisionResult result);
        void Mistake(DecisionResult result);
        void TimeExpired(DecisionResult result);
        void ComboEnded(string sessionId, int previousComboValue, string reason, string documentId, int difficultyLevel);
        void SessionCompleted(HeavenOfficeSessionResult result);
        void SessionExited(string sessionId, int processedSoulCount, int finalScore, string exitReason, int currentDifficultyLevel);
    }

    public sealed class DebugHeavenOfficeAnalytics : IHeavenOfficeAnalytics
    {
        public void SessionStarted(HeavenOfficeSnapshot snapshot, string sessionId, string[] enabledStampIds)
        {
            Debug.Log($"[HeavenOffice] SessionStarted sessionId={sessionId} souls={snapshot.QueueCount} difficulty={snapshot.DifficultyLevel} stamps={string.Join(",", enabledStampIds)} time={snapshot.DocumentTimeLimit:0.0}");
        }

        public void DocumentShown(string sessionId, Soul soul, HeavenOfficeSnapshot snapshot, string[] availableStampIds)
        {
            Debug.Log($"[HeavenOffice] DocumentShown sessionId={sessionId} documentId={soul.Document.DocumentId} soulId={soul.SoulId} caseType={soul.Document.CaseType} difficulty={snapshot.DifficultyLevel} expected={soul.Document.ExpectedStampId} queueIndex={snapshot.QueueIndex}");
        }

        public void StampPlaced(string sessionId, Soul soul, string selectedStampId, float reactionTime, HeavenOfficeSnapshot snapshot)
        {
            Debug.Log($"[HeavenOffice] StampPlaced sessionId={sessionId} documentId={soul.Document.DocumentId} soulId={soul.SoulId} selected={selectedStampId} reaction={reactionTime:0.00} difficulty={snapshot.DifficultyLevel} queueIndex={snapshot.QueueIndex}");
        }

        public void CorrectDecision(DecisionResult result)
        {
            Debug.Log($"[HeavenOffice] CorrectDecision documentId={result.DocumentId} selected={result.SelectedStampId} expected={result.ExpectedStampId} scoreDelta={result.ScoreDelta} combo={result.ComboValue}");
        }

        public void Mistake(DecisionResult result)
        {
            Debug.Log($"[HeavenOffice] Mistake documentId={result.DocumentId} selected={result.SelectedStampId} expected={result.ExpectedStampId} reason={result.MistakeReason} penalty={result.Penalty}");
        }

        public void TimeExpired(DecisionResult result)
        {
            Debug.Log($"[HeavenOffice] TimeExpired documentId={result.DocumentId} reaction={result.ReactionTime:0.00} mistakeReason={result.MistakeReason}");
        }

        public void ComboEnded(string sessionId, int previousComboValue, string reason, string documentId, int difficultyLevel)
        {
            Debug.Log($"[HeavenOffice] ComboEnded sessionId={sessionId} previousCombo={previousComboValue} reason={reason} documentId={documentId} difficulty={difficultyLevel}");
        }

        public void SessionCompleted(HeavenOfficeSessionResult result)
        {
            Debug.Log($"[HeavenOffice] SessionCompleted sessionId={result.SessionId} score={result.FinalScore} processed={result.ProcessedSoulCount} correct={result.CorrectDecisionCount} mistakes={result.MistakeCount} reason={result.CompletionReason}");
        }

        public void SessionExited(string sessionId, int processedSoulCount, int finalScore, string exitReason, int currentDifficultyLevel)
        {
            Debug.Log($"[HeavenOffice] SessionExited sessionId={sessionId} processed={processedSoulCount} score={finalScore} reason={exitReason} difficulty={currentDifficultyLevel}");
        }
    }
}
