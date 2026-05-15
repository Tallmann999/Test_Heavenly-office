using System;
using System.Collections.Generic;

namespace HeavenOffice
{
    public enum HeavenOfficeSessionState
    {
        NotStarted,
        Initializing,
        SoulEntering,
        DocumentActive,
        StampInteraction,
        DecisionResolving,
        TransitionToNextSoul,
        Paused,
        Completed,
        Failed,
        Exited
    }

    public enum SoulState
    {
        WaitingInQueue,
        Entering,
        PresentingDocument,
        ResolvedCorrectly,
        ResolvedIncorrectly,
        Leaving,
        Expired
    }

    public enum HeavenOfficeCompletionReason
    {
        QueueCompleted,
        MaxMistakesReached,
        SessionTimerExpired,
        PlayerExited
    }

    public enum HeavenOfficeMistakeReason
    {
        None,
        WrongStamp,
        TimeExpired
    }

    [Serializable]
    public sealed class SoulDocument
    {
        public string DocumentId;
        public string SoulName;
        public string LifeDescription;
        public List<string> GoodActs = new List<string>();
        public List<string> BadActs = new List<string>();
        public List<string> SpecialNotes = new List<string>();
        public string CaseType;
        public List<string> Tags = new List<string>();
        public string ExpectedStampId;
        public int Difficulty;
    }

    public sealed class Soul
    {
        public Soul(string soulId, SoulDocument document)
        {
            SoulId = soulId;
            Document = document;
            State = SoulState.WaitingInQueue;
        }

        public string SoulId { get; }
        public SoulDocument Document { get; }
        public SoulState State { get; set; }
    }

    public sealed class DecisionResult
    {
        public string SessionId;
        public string SoulId;
        public string DocumentId;
        public string SelectedStampId;
        public string ExpectedStampId;
        public bool IsCorrect;
        public float ReactionTime;
        public int ScoreDelta;
        public int Penalty;
        public int ComboValue;
        public HeavenOfficeMistakeReason MistakeReason;
        public string FinalDirection;
        public int DifficultyLevel;
        public string CaseType;
    }

    public sealed class HeavenOfficeSnapshot
    {
        public HeavenOfficeSessionState State;
        public int Score;
        public int MistakeCount;
        public int Combo;
        public int MaxCombo;
        public int QueueIndex;
        public int QueueCount;
        public float DocumentTimeRemaining;
        public float DocumentTimeLimit;
        public int DifficultyLevel;
        public string CurrentRuleHint;
        public string LastDecisionFeedback;
    }

    public sealed class HeavenOfficeSessionResult
    {
        public string SessionId;
        public int FinalScore;
        public int ProcessedSoulCount;
        public int CorrectDecisionCount;
        public int MistakeCount;
        public int MaxCombo;
        public float AverageReactionTime;
        public HeavenOfficeCompletionReason CompletionReason;
        public int FinalDifficultyLevel;
    }
}
