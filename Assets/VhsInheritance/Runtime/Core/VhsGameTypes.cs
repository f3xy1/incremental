using System;
using System.Collections.Generic;
using UnityEngine;

namespace VhsInheritance
{
    public enum GamePhase
    {
        MainMenu,
        MorningPrep,
        DayPlay,
        DayResults,
        EveningPrep,
        NightPlay,
        NightCutscene
    }

    public enum CassetteGenre
    {
        Action,
        Comedy,
        Drama,
        Audio
    }

    public enum UpgradeType
    {
        CarryCapacity,
        WalkSpeed,
        BasketCapacity,
        ExtraGenre,
        ShelfCapacity,
        OrderPayout,
        Signage,
        VisitorPatience,
        CounterAssistant,
        AudioCassettes
    }

    [Serializable]
    public sealed class CassetteData
    {
        public string id;
        public CassetteGenre genre;
        public bool isForeign;
        public bool isMisplaced;
        public bool placementRewardClaimed;

        public CassetteData Clone()
        {
            return new CassetteData
            {
                id = id,
                genre = genre,
                isForeign = isForeign,
                isMisplaced = isMisplaced,
                placementRewardClaimed = placementRewardClaimed
            };
        }
    }

    [Serializable]
    public sealed class ShelfSlotData
    {
        public bool occupied;
        public CassetteData cassette;
    }

    [Serializable]
    public sealed class ShelfData
    {
        public CassetteGenre genre;
        public List<ShelfSlotData> slots = new List<ShelfSlotData>();
    }

    [Serializable]
    public sealed class VisitorData
    {
        public string id;
        public int[] totalByGenre = new int[4];
        public int[] remainingByGenre = new int[4];
        public float patienceRemaining;
        public bool hasReadOrder;
        public int visualVariant;

        public int TotalCount()
        {
            int total = 0;
            for (int i = 0; i < totalByGenre.Length; i++)
            {
                total += totalByGenre[i];
            }

            return total;
        }

        public int RemainingCount()
        {
            int total = 0;
            for (int i = 0; i < remainingByGenre.Length; i++)
            {
                total += remainingByGenre[i];
            }

            return total;
        }

        public int UniqueGenreCount()
        {
            int total = 0;
            for (int i = 0; i < totalByGenre.Length; i++)
            {
                if (totalByGenre[i] > 0)
                {
                    total++;
                }
            }

            return total;
        }
    }

    [Serializable]
    public sealed class DayStatsData
    {
        public int completedOrders;
        public int failedOrders;
        public int moneyEarned;
        public int correctlyShelved;
        public int misplacedShelved;

        public void Reset()
        {
            completedOrders = 0;
            failedOrders = 0;
            moneyEarned = 0;
            correctlyShelved = 0;
            misplacedShelved = 0;
        }
    }

    [Serializable]
    public sealed class VhsSaveData
    {
        public int saveVersion = 1;
        public int totalMoney;
        public int currentDayIndex = 1;
        public GamePhase phase = GamePhase.MorningPrep;
        public int[] upgradeLevels = new int[10];
        public List<ShelfData> shelves = new List<ShelfData>();
        public List<CassetteData> pile = new List<CassetteData>();
        public List<CassetteData> hands = new List<CassetteData>();
        public int machineOverflow;
        // Already existing cassettes must keep their identity when returned to a full basket.
        public List<CassetteData> machineStoredCassettes = new List<CassetteData>();
        public CassetteData assistantHeldCassette;
        public float machineTimer;
        public float overflowDrainTimer;
        public float dayTimeRemaining;
        public float visitorSpawnTimer;
        public List<VisitorData> visitors = new List<VisitorData>();
        public DayStatsData dayStats = new DayStatsData();
        public List<string> tutorialFlagsShown = new List<string>();
        public bool foreignCassetteReceivedToday;
        public bool foreignCassetteWatchedToday;
        public float playerX;
        public float playerY = -0.8f;
    }

    public static class VhsNames
    {
        public static string Genre(CassetteGenre genre)
        {
            switch (genre)
            {
                case CassetteGenre.Action: return "Боевик";
                case CassetteGenre.Comedy: return "Комедия";
                case CassetteGenre.Drama: return "Драма";
                case CassetteGenre.Audio: return "Аудио";
                default: return genre.ToString();
            }
        }

        public static string Phase(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.MorningPrep: return "Подготовка";
                case GamePhase.DayPlay: return "Дневная смена";
                case GamePhase.DayResults: return "Итоги дня";
                case GamePhase.EveningPrep: return "Вечер";
                case GamePhase.NightPlay:
                case GamePhase.NightCutscene: return "Ночь";
                default: return string.Empty;
            }
        }
    }
}
