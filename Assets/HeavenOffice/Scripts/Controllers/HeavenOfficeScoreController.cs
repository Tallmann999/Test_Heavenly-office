using UnityEngine;

namespace HeavenOffice
{
    public sealed class HeavenOfficeScoreController
    {
        private readonly HeavenOfficeSessionConfig config;

        public HeavenOfficeScoreController(HeavenOfficeSessionConfig config)
        {
            this.config = config;
        }

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }

        public int ApplyCorrect(float reactionTime, float timeLimit)
        {
            Combo++;
            MaxCombo = Mathf.Max(MaxCombo, Combo);
            var speed01 = timeLimit <= 0f ? 0f : Mathf.Clamp01((timeLimit - reactionTime) / timeLimit);
            var fastBonus = Mathf.RoundToInt(config.fastDecisionBonus * speed01);
            var comboBonus = Mathf.RoundToInt(config.baseScoreReward * config.comboScoreMultiplier * Mathf.Max(0, Combo - 1));
            var delta = config.baseScoreReward + fastBonus + comboBonus;
            Score += delta;
            return delta;
        }

        public int ApplyMistake()
        {
            var previousCombo = Combo;
            if (config.comboResetOnMistake)
            {
                Combo = 0;
            }

            Score -= config.mistakePenalty;
            return previousCombo;
        }
    }
}
