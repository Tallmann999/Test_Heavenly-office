using System;
using System.Collections.Generic;
using System.Linq;

namespace HeavenOffice
{
    public sealed class SoulQueueController
    {
        private readonly List<Soul> souls = new List<Soul>();
        private int nextIndex;

        public int QueueCount => souls.Count;
        public int CurrentIndex => nextIndex;
        public int ProcessedCount => nextIndex;
        public bool HasNext => nextIndex < souls.Count;

        public void BuildQueue(HeavenOfficeSessionConfig config, DifficultyController difficultyController, int seed)
        {
            souls.Clear();
            nextIndex = 0;
            var random = new Random(seed);

            for (var i = 0; i < config.sessionSoulCount; i++)
            {
                var templates = difficultyController.GetAvailableDocumentTemplates(i);
                var availableStampIds = new HashSet<string>(difficultyController.GetAvailableStamps(i).Select(stamp => stamp.stampId));
                templates = templates.Where(template => availableStampIds.Contains(template.expectedStampId)).ToList();
                if (templates.Count == 0)
                {
                    throw new InvalidOperationException($"Heaven Office config has no document templates for queue index {i}.");
                }

                var template = PickWeighted(templates, random);
                var document = CreateDocument(template, i);
                souls.Add(new Soul($"soul-{i + 1:00}", document));
            }
        }

        public Soul TakeNext()
        {
            if (!HasNext)
            {
                return null;
            }

            var soul = souls[nextIndex];
            nextIndex++;
            return soul;
        }

        private static DocumentTemplate PickWeighted(List<DocumentTemplate> templates, Random random)
        {
            var totalWeight = templates.Sum(template => Math.Max(1, template.weight));
            var roll = random.Next(0, totalWeight);
            foreach (var template in templates)
            {
                roll -= Math.Max(1, template.weight);
                if (roll < 0)
                {
                    return template;
                }
            }

            return templates[templates.Count - 1];
        }

        private static SoulDocument CreateDocument(DocumentTemplate template, int queueIndex)
        {
            return new SoulDocument
            {
                DocumentId = $"{template.templateId}-{queueIndex + 1:00}",
                SoulName = template.soulName,
                LifeDescription = template.lifeDescription,
                GoodActs = new List<string>(template.goodActs),
                BadActs = new List<string>(template.badActs),
                SpecialNotes = new List<string>(template.specialNotes),
                CaseType = template.caseType,
                Tags = new List<string>(template.tags),
                ExpectedStampId = template.expectedStampId,
                Difficulty = template.difficulty
            };
        }
    }
}
