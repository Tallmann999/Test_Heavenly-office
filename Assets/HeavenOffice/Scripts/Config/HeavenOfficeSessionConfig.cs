using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeavenOffice
{
    [CreateAssetMenu(menuName = "Heaven Office/Session Config", fileName = "HeavenOfficeSessionConfig")]
    public sealed class HeavenOfficeSessionConfig : ScriptableObject
    {
        [Min(1)] public int sessionSoulCount = 8;
        [Min(1f)] public float documentReadTimeLimit = 14f;
        public int baseScoreReward = 100;
        public int fastDecisionBonus = 40;
        public int mistakePenalty = 50;
        [Min(1)] public int maxMistakeCount = 3;
        [Min(1)] public int difficultyRampStep = 3;
        [Min(1f)] public float comboScoreMultiplier = 0.15f;
        public bool enableSessionTimer;
        [Min(1f)] public float sessionTimeLimit = 120f;
        public bool comboResetOnMistake = true;
        public bool timeExpiredCountsAsMistake = true;
        [Min(1f)] public float minDocumentReadTimeLimit = 7f;
        [Min(1)] public int maxAvailableStampCount = 2;
        [Min(0f)] public float decisionResolveDelay = 0.75f;
        [Min(0f)] public float nextSoulDelay = 0.35f;
        public List<StampDefinition> stampSetConfig = new List<StampDefinition>();
        public List<DocumentTemplate> documentTemplatePool = new List<DocumentTemplate>();
        public List<DifficultyRampEntry> difficultyRampConfig = new List<DifficultyRampEntry>();
        public List<SoulReactionDefinition> soulReactionConfig = new List<SoulReactionDefinition>();
        public QueuePressureConfig queuePressureConfig = new QueuePressureConfig();

        public static HeavenOfficeSessionConfig CreateRuntimeDefault()
        {
            var config = CreateInstance<HeavenOfficeSessionConfig>();
            config.name = "Runtime Heaven Office MVP Config";
            config.stampSetConfig.Add(new StampDefinition
            {
                stampId = "Heaven",
                displayName = "Рай",
                color = new Color(0.25f, 0.58f, 1f),
                symbol = "R",
                panelSide = StampPanelSide.Left,
                availableFromDifficulty = 0
            });
            config.stampSetConfig.Add(new StampDefinition
            {
                stampId = "Hell",
                displayName = "Ад",
                color = new Color(0.95f, 0.22f, 0.16f),
                symbol = "A",
                panelSide = StampPanelSide.Right,
                availableFromDifficulty = 0
            });

            config.difficultyRampConfig.Add(new DifficultyRampEntry
            {
                difficultyLevel = 0,
                startsAtQueueIndex = 0,
                timeLimitModifier = 0f,
                currentRuleHint = "Правило: больше добрых поступков - Рай, больше дурных - Ад."
            });
            config.difficultyRampConfig.Add(new DifficultyRampEntry
            {
                difficultyLevel = 1,
                startsAtQueueIndex = 3,
                timeLimitModifier = -2f,
                currentRuleHint = "Правило: спорные дела требуют читать пометки, но печати пока две."
            });
            config.difficultyRampConfig.Add(new DifficultyRampEntry
            {
                difficultyLevel = 2,
                startsAtQueueIndex = 6,
                timeLimitModifier = -4f,
                currentRuleHint = "Правило: особые пометки могут объяснять итог, проверьте документ целиком."
            });

            AddTemplate(config, "archive-001", "Аврора Снегова", "Пекарь, державшая лавку у вокзала.", "Heaven", 0,
                new[] { "Кормила детей без оплаты", "Каждую зиму чинила чужие печи" },
                new[] { "Трижды ругалась с поставщиками" },
                new[] { "Дело чистое, подписи совпадают" }, "Common");
            AddTemplate(config, "archive-002", "Марк Пепельный", "Сборщик долгов с безупречной бухгалтерией.", "Hell", 0,
                new[] { "Один раз вернул лишнюю монету" },
                new[] { "Запугивал должников", "Подделывал расписки" },
                new[] { "Жалоба архива подтверждена" }, "Common");
            AddTemplate(config, "archive-003", "Лина Радужная", "Фельдшер ночной смены.", "Heaven", 1,
                new[] { "Спасала пациентов после смены", "Собирала лекарства для соседей" },
                new[] { "Скрыла разбитый термометр" },
                new[] { "Раскаяние приложено" }, "Medical");
            AddTemplate(config, "archive-004", "Гектор Серебро", "Меценат с громкими портретами в каждом зале.", "Hell", 1,
                new[] { "Жертвовал на библиотеки" },
                new[] { "Покупал молчание свидетелей", "Разорил приют ради земли" },
                new[] { "Добро ради выгоды: подтверждено" }, "Disputed");
            AddTemplate(config, "archive-005", "София Лист", "Учительница, забытая всеми отчётами.", "Heaven", 2,
                new[] { "Научила читать целый посёлок", "Укрывала людей во время бури" },
                new[] { "Крала мел у управления" },
                new[] { "Мел списан как чрезвычайный расход" }, "Exception");
            AddTemplate(config, "archive-006", "Виктор Чернильный", "Архивариус, знавший все лазейки.", "Hell", 2,
                new[] { "Сортировал дела без ошибок" },
                new[] { "Прятал оправдательные пометки", "Менял очереди за подарки" },
                new[] { "Служебное злоупотребление" }, "Archive");
            return config;
        }

        private static void AddTemplate(HeavenOfficeSessionConfig config, string id, string name, string description, string expectedStampId, int difficulty, string[] goodActs, string[] badActs, string[] notes, string caseType)
        {
            config.documentTemplatePool.Add(new DocumentTemplate
            {
                templateId = id,
                soulName = name,
                lifeDescription = description,
                expectedStampId = expectedStampId,
                difficulty = difficulty,
                minDifficulty = difficulty,
                weight = 1,
                caseType = caseType,
                goodActs = new List<string>(goodActs),
                badActs = new List<string>(badActs),
                specialNotes = new List<string>(notes),
                tags = new List<string> { caseType }
            });
        }
    }

    public enum StampPanelSide
    {
        Left,
        Right
    }

    [Serializable]
    public sealed class StampDefinition
    {
        public string stampId;
        public string displayName;
        public Color color = Color.white;
        public string symbol;
        public StampPanelSide panelSide;
        public int availableFromDifficulty;
    }

    [Serializable]
    public sealed class DocumentTemplate
    {
        public string templateId;
        public int difficulty;
        public int minDifficulty;
        public string caseType;
        [TextArea] public string lifeDescription;
        public string soulName;
        public List<string> goodActs = new List<string>();
        public List<string> badActs = new List<string>();
        public List<string> specialNotes = new List<string>();
        public List<string> tags = new List<string>();
        public string expectedStampId;
        [Min(1)] public int weight = 1;
    }

    [Serializable]
    public sealed class DifficultyRampEntry
    {
        public int difficultyLevel;
        public int startsAtQueueIndex;
        public float timeLimitModifier;
        public string currentRuleHint;
    }

    [Serializable]
    public sealed class SoulReactionDefinition
    {
        public string resultKey;
        public string reactionText;
    }

    [Serializable]
    public sealed class QueuePressureConfig
    {
        [Range(0f, 1f)] public float basePressure = 0.2f;
        [Range(0f, 1f)] public float pressurePerMistake = 0.15f;
    }
}
