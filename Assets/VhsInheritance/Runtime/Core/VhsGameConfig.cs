using System;
using System.Collections.Generic;
using UnityEngine;

namespace VhsInheritance
{
    [Serializable]
    public sealed class UpgradeTrack
    {
        public UpgradeType type;
        public string title;
        [TextArea] public string description;
        public List<float> values = new List<float>();
        public List<int> nextLevelCosts = new List<int>();
        public List<string> levelEffectLabels = new List<string>();
        public Color iconColor = Color.white;

        public int ClampLevel(int level)
        {
            return Mathf.Clamp(level, 0, Mathf.Max(0, values.Count - 1));
        }

        public float ValueAt(int level)
        {
            if (values == null || values.Count == 0)
            {
                return 0f;
            }

            return values[ClampLevel(level)];
        }

        public int CostForNextLevel(int currentLevel)
        {
            if (nextLevelCosts == null || currentLevel < 0 || currentLevel >= nextLevelCosts.Count)
            {
                return -1;
            }

            return nextLevelCosts[currentLevel];
        }

        public string EffectAt(int level)
        {
            if (levelEffectLabels == null || levelEffectLabels.Count == 0)
            {
                return ValueAt(level).ToString("0.##");
            }

            return levelEffectLabels[Mathf.Clamp(level, 0, levelEffectLabels.Count - 1)];
        }
    }

    [CreateAssetMenu(fileName = "VhsGameConfig", menuName = "VHS Inheritance/Game Balance Config")]
    public sealed class VhsGameConfig : ScriptableObject
    {
        [Header("Day Loop")]
        [Min(1f)] public float dayDurationSeconds = 480f;
        [Min(0f)] public float duskFadeDurationSeconds = 90f;
        [Min(0.1f)] public float machineStartIntervalSeconds = 9f;
        [Min(0f)] public float machineIntervalDecreasePerDay = 0.5f;
        [Min(0.1f)] public float machineMinimumIntervalSeconds = 4f;
        [Min(1)] public int initialPileCount = 12;
        [Min(1)] public int basePileCapacity = 40;
        [Min(0.01f)] public float overflowDrainIntervalSeconds = 0.2f;

        [Header("Player and Sorting")]
        [Min(0.1f)] public float baseWalkSpeed = 3f;
        [Min(0.1f)] public float interactionRadius = 1.15f;
        [Min(0)] public int correctShelfReward = 1;

        [Header("Visitors and Orders")]
        [Min(1)] public int maxVisibleVisitors = 8;
        [Min(1f)] public float baseVisitorSpawnInterval = 24f;
        [Min(0.1f)] public float baseOrderPayout = 2f;
        [Min(0f)] public float uniqueGenrePayoutBonus = 0.1f;
        public int[] orderMaximumByDay = { 3, 5, 7, 10 };

        [Header("Assistant")]
        [Min(0.1f)] public float assistantMoveSpeed = 2.4f;
        [Min(0.1f)] public float assistantActionCooldown = 5f;
        [Min(0.1f)] public float pathGridCellSize = 0.5f;

        [Header("Upgrade Tracks")]
        public List<UpgradeTrack> upgrades = new List<UpgradeTrack>();

        [Header("Night Lore")]
        [TextArea] public string loreAfterLastFragment = "Запись {0}. Салон снят снаружи. В окне отражается день, которого ещё не было. На двери — новый номер архива: {0}.";
        [TextArea(2, 5)] public string[] loreFragments =
        {
            "1994. На записи пустой зал. Камера медленно поворачивается к двери подсобки, хотя держать её некому.",
            "1996. В отражении экрана дед спорит с человеком без лица: «Салон сам выбирает, что сохранить».",
            "1998. Аппарат печатает наклейку с завтрашней датой. Под ней — ваше имя, написанное знакомым почерком.",
            "2001. Последняя запись. Дед выключает свет и оставляет ключ под телевизором: «Теперь очередь наследника»."
        };

        public UpgradeTrack GetTrack(UpgradeType type)
        {
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i].type == type)
                {
                    return upgrades[i];
                }
            }

            return null;
        }

        public float UpgradeValue(UpgradeType type, int level)
        {
            UpgradeTrack track = GetTrack(type);
            return track == null ? 0f : track.ValueAt(level);
        }

        public int UpgradeLevel(int[] levels, UpgradeType type)
        {
            int index = (int)type;
            if (levels == null || index < 0 || index >= levels.Length)
            {
                return 0;
            }

            UpgradeTrack track = GetTrack(type);
            return track == null ? 0 : track.ClampLevel(levels[index]);
        }

        public void ResetToPrototypeDefaults()
        {
            dayDurationSeconds = 480f;
            duskFadeDurationSeconds = 90f;
            machineStartIntervalSeconds = 9f;
            machineIntervalDecreasePerDay = 0.5f;
            machineMinimumIntervalSeconds = 4f;
            initialPileCount = 12;
            basePileCapacity = 40;
            overflowDrainIntervalSeconds = 0.2f;
            baseWalkSpeed = 3f;
            interactionRadius = 1.15f;
            correctShelfReward = 1;
            maxVisibleVisitors = 8;
            baseVisitorSpawnInterval = 24f;
            baseOrderPayout = 2f;
            uniqueGenrePayoutBonus = 0.1f;
            orderMaximumByDay = new[] { 3, 5, 7, 10 };
            assistantMoveSpeed = 2.4f;
            assistantActionCooldown = 5f;
            pathGridCellSize = 0.5f;
            loreFragments = new[]
            {
                "1994. На записи пустой зал. Камера медленно поворачивается к двери подсобки, хотя держать её некому.",
                "1996. В отражении экрана дед спорит с человеком без лица: «Салон сам выбирает, что сохранить».",
                "1998. Аппарат печатает наклейку с завтрашней датой. Под ней — ваше имя, написанное знакомым почерком.",
                "2001. Последняя запись. Дед выключает свет и оставляет ключ под телевизором: «Теперь очередь наследника»."
            };

            upgrades = new List<UpgradeTrack>
            {
                Track(UpgradeType.CarryCapacity, "Вместимость", "Кассет в руках", new[] { 1f, 2f, 4f }, new[] { 20, 50 }, new[] { "1 кассета", "2 кассеты", "4 кассеты" }, "#E15A97"),
                Track(UpgradeType.WalkSpeed, "Скорость ходьбы", "Скорость персонажа", new[] { 1f, 1.15f, 1.30f }, new[] { 25, 60 }, new[] { "Базовая", "+15%", "+30%" }, "#52C8C4"),
                Track(UpgradeType.BasketCapacity, "Большая корзина", "Лимит кучи", new[] { 40f, 55f, 70f, 85f }, new[] { 30, 60, 100 }, new[] { "40 кассет", "55 кассет", "70 кассет", "85 кассет" }, "#F0A44B"),
                Track(UpgradeType.ExtraGenre, "Доп. полка / жанр", "Открывает драму", new[] { 0f, 1f }, new[] { 80 }, new[] { "Закрыто", "Драма открыта" }, "#936FD1"),
                Track(UpgradeType.ShelfCapacity, "Больше места", "Слотов на каждой полке", new[] { 20f, 30f, 45f, 60f }, new[] { 40, 80, 140 }, new[] { "20 слотов", "30 слотов", "45 слотов", "60 слотов" }, "#6BCB77"),
                Track(UpgradeType.OrderPayout, "Доход с заказов", "Множитель оплаты", new[] { 1f, 1.25f, 1.5f, 1.8f }, new[] { 50, 100, 180 }, new[] { "x1.00", "x1.25", "x1.50", "x1.80" }, "#FFD166"),
                Track(UpgradeType.Signage, "Вывеска", "Посетители приходят чаще", new[] { 1f, 0.85f, 0.70f, 0.55f }, new[] { 35, 75, 130 }, new[] { "Обычная", "+15% чаще", "+30% чаще", "+45% чаще" }, "#EF476F"),
                Track(UpgradeType.VisitorPatience, "Время ожидания", "Терпение посетителей", new[] { 45f, 55f, 65f, 75f }, new[] { 30, 70, 120 }, new[] { "45 секунд", "55 секунд", "65 секунд", "75 секунд" }, "#118AB2"),
                Track(UpgradeType.CounterAssistant, "Помощник", "Сам носит часть заказов", new[] { 0f, 1f }, new[] { 180 }, new[] { "Нет помощника", "Помощник нанят" }, "#8BD3DD"),
                Track(UpgradeType.AudioCassettes, "Аудиокассеты", "Новый тип и полка", new[] { 0f, 1f }, new[] { 150 }, new[] { "Закрыто", "Аудио открыто" }, "#F78C6B")
            };
        }

        private static UpgradeTrack Track(UpgradeType type, string title, string description, float[] values, int[] costs, string[] effects, string htmlColor)
        {
            Color color = Color.white;
            ColorUtility.TryParseHtmlString(htmlColor, out color);
            return new UpgradeTrack
            {
                type = type,
                title = title,
                description = description,
                values = new List<float>(values),
                nextLevelCosts = new List<int>(costs),
                levelEffectLabels = new List<string>(effects),
                iconColor = color
            };
        }
    }
}
