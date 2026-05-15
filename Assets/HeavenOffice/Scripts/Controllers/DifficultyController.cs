using System.Collections.Generic;
using System.Linq;

namespace HeavenOffice
{
    public sealed class DifficultyController
    {
        private readonly HeavenOfficeSessionConfig config;

        public DifficultyController(HeavenOfficeSessionConfig config)
        {
            this.config = config;
        }

        public int GetDifficultyLevel(int queueIndex)
        {
            var level = 0;
            foreach (var entry in config.difficultyRampConfig.OrderBy(item => item.startsAtQueueIndex))
            {
                if (queueIndex >= entry.startsAtQueueIndex)
                {
                    level = entry.difficultyLevel;
                }
            }

            return level;
        }

        public float GetDocumentTimeLimit(int queueIndex)
        {
            var modifier = 0f;
            var difficulty = GetDifficultyLevel(queueIndex);
            foreach (var entry in config.difficultyRampConfig)
            {
                if (entry.difficultyLevel == difficulty)
                {
                    modifier = entry.timeLimitModifier;
                    break;
                }
            }

            var timeLimit = config.documentReadTimeLimit + modifier;
            return timeLimit < config.minDocumentReadTimeLimit ? config.minDocumentReadTimeLimit : timeLimit;
        }

        public string GetRuleHint(int queueIndex)
        {
            var difficulty = GetDifficultyLevel(queueIndex);
            foreach (var entry in config.difficultyRampConfig)
            {
                if (entry.difficultyLevel == difficulty && !string.IsNullOrWhiteSpace(entry.currentRuleHint))
                {
                    return entry.currentRuleHint;
                }
            }

            return "Правило: сравните добрые и дурные поступки, затем выберите печать.";
        }

        public List<StampDefinition> GetAvailableStamps(int queueIndex)
        {
            var difficulty = GetDifficultyLevel(queueIndex);
            return config.stampSetConfig
                .Where(stamp => stamp.availableFromDifficulty <= difficulty)
                .Take(config.maxAvailableStampCount)
                .ToList();
        }

        public List<DocumentTemplate> GetAvailableDocumentTemplates(int queueIndex)
        {
            var difficulty = GetDifficultyLevel(queueIndex);
            return config.documentTemplatePool
                .Where(template => template.minDifficulty <= difficulty && template.difficulty <= difficulty)
                .ToList();
        }
    }
}
