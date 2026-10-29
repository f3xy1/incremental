using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VhsInheritance;

// Run with: unity command script run --caller plugin --skill unity-cli -- --file AgentScripts/VhsAcceptance.cs --entry VhsAcceptance.Run
public static class VhsAcceptance
{
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object Call(VhsGameController game, string method, params object[] args)
        => typeof(VhsGameController).GetMethod(method, Private).Invoke(game, args);
    private static object Field(VhsGameController game, string name)
        => typeof(VhsGameController).GetField(name, Private).GetValue(game);
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception("Acceptance failed: " + message);
    }
    private static CassetteData Tape(CassetteGenre genre) => new CassetteData { id = Guid.NewGuid().ToString("N"), genre = genre };
    private static VisitorData Visitor(params CassetteGenre[] genres)
    {
        var visitor = new VisitorData { id = Guid.NewGuid().ToString("N"), patienceRemaining = 45, hasReadOrder = true };
        foreach (var genre in genres) { visitor.totalByGenre[(int)genre]++; visitor.remainingByGenre[(int)genre]++; }
        return visitor;
    }
    private static object Shelf(VhsGameController game, CassetteGenre genre)
        => ((IEnumerable)Field(game, "shelves")).Cast<object>().First(x => (CassetteGenre)x.GetType().GetField("genre").GetValue(x) == genre);
    private static void ResetOrder(VhsGameController game, params CassetteGenre[] genres)
    {
        game.CurrentSave.visitors.Clear();
        game.CurrentSave.visitors.Add(Visitor(genres));
    }

    public static object Run()
    {
        Check(Application.isPlaying, "Run in Play Mode");
        VhsSaveSystem.EditorSaveDirectory = Path.GetFullPath("Temp/verification/acceptance-save");
        var game = UnityEngine.Object.FindAnyObjectByType<VhsGameController>();
        game.NewGame();
        var checks = new List<string>();
        var data = game.CurrentSave;
        Check(game.CurrentPhase == GamePhase.MorningPrep, "New game starts in morning");
        game.StartDay();
        Check(game.CurrentPhase == GamePhase.DayPlay && data.dayTimeRemaining == 480, "Every shift starts at 480 seconds");
        checks.Add("Morning -> 480 second shift");

        data.upgradeLevels[(int)UpgradeType.CarryCapacity] = 2;
        data.pile.Clear();
        var ordered = new[] { Tape(CassetteGenre.Action), Tape(CassetteGenre.Comedy), Tape(CassetteGenre.Action), Tape(CassetteGenre.Comedy) };
        data.pile.AddRange(ordered);
        Call(game, "TakeFromPile");
        Check(data.hands.Select(x => x.id).SequenceEqual(ordered.Select(x => x.id)), "Pile preserves pickup order up to capacity");
        checks.Add("FIFO pickup and capacity 4");

        var actionShelf = Shelf(game, CassetteGenre.Action);
        int money = data.totalMoney;
        Call(game, "InteractWithShelf", actionShelf);
        Check(data.totalMoney == money + 1 && data.shelves[0].slots[0].cassette.id == ordered[0].id, "Correct placement uses first slot and pays");
        Call(game, "InteractWithShelf", actionShelf);
        Check(data.totalMoney == money + 1 && data.shelves[0].slots[1].cassette.isMisplaced, "Wrong genre stays on shelf without payment");
        checks.Add("Correct and misplaced sorting");
        data.hands.Clear();
        Call(game, "OpenShelfBrowser", actionShelf);
        Call(game, "TakeShelfSlot", 1);
        Call(game, "TakeShelfSlot", 0);
        Check(data.hands.Count == 2 && data.hands[0].genre == CassetteGenre.Comedy, "Can take multiple selected tapes in order");
        Call(game, "InteractWithShelf", actionShelf); // Wrong comedy.
        Call(game, "InteractWithShelf", actionShelf); // Correct action, already rewarded.
        Check(data.totalMoney == money + 1, "Placing the same tape again cannot farm money");
        checks.Add("Shelf selection, multiple pickups, no reward farming");

        ResetOrder(game, CassetteGenre.Action, CassetteGenre.Comedy);
        var activeId = data.visitors[0].id;
        money = data.totalMoney;
        Call(game, "DeliverCassette", Tape(CassetteGenre.Action), activeId, false);
        Check(data.totalMoney == money, "Partial orders are unpaid");
        Call(game, "DeliverCassette", Tape(CassetteGenre.Comedy), activeId, false);
        Check(data.totalMoney == money + 5 && data.dayStats.completedOrders == 1, "Completed diverse order pays rounded config formula");
        checks.Add("Correct multi-genre order and payout");
        ResetOrder(game, CassetteGenre.Action);
        data.visitors.Add(Visitor(CassetteGenre.Comedy));
        string queuedId = data.visitors[1].id;
        money = data.totalMoney;
        int failures = data.dayStats.failedOrders;
        Call(game, "DeliverCassette", Tape(CassetteGenre.Comedy), data.visitors[0].id, false);
        Check(data.visitors.Count == 1 && data.visitors[0].id == queuedId && data.dayStats.failedOrders == failures + 1 && data.totalMoney == money,
            "Wrong delivery fails immediately, promotes queue, no payment");
        checks.Add("Wrong tape failure and queue promotion");
        data.visitors[0].patienceRemaining = 0.01f;
        Call(game, "UpdateVisitors", 0.02f);
        Check(data.visitors.Count == 0 && data.totalMoney == money, "Impatient customer leaves without payment");
        checks.Add("Visitor timeout without payment");
        for (int i = 0; i < 12; i++) Call(game, "AddVisitor");
        Check(data.visitors.Count == 8 && data.visitors.All(x => x.TotalCount() <= 3), "Eight visible visitors and day 1 small orders");
        checks.Add("Queue cap and initial order difficulty");

        data.pile.Clear();
        for (int i = 0; i < 40; i++) data.pile.Add(Tape(CassetteGenre.Action));
        data.machineOverflow = 0;
        Call(game, "ProduceCassette");
        Check(data.pile.Count == 40 && data.machineOverflow == 1, "Machine retains overflow");
        var identity = Tape(CassetteGenre.Comedy);
        identity.placementRewardClaimed = true;
        Call(game, "ReturnCassetteToPile", identity);
        Check(data.machineStoredCassettes[0].id == identity.id, "Returned cassette identity is preserved when basket is full");
        data.pile.RemoveAt(0);
        data.machineTimer = 9;
        data.overflowDrainTimer = 0;
        Call(game, "UpdateMachine", 0.1f);
        Check(data.pile.Last().id == identity.id && data.machineOverflow == 1, "Identity cassette drains before unseen generated backlog");
        checks.Add("Full basket, machine backlog, preserved identity");

        var assistantTape = Tape(CassetteGenre.Action);
        data.assistantHeldCassette = assistantTape;
        typeof(VhsGameController).GetField("assistantHeldCassette", Private).SetValue(game, assistantTape);
        data.hands.Add(Tape(CassetteGenre.Comedy));
        Call(game, "SaveNow", false);
        game.ContinueGame();
        data = game.CurrentSave;
        Check(data.hands.Count == 1 && data.machineStoredCassettes.Any(x => x.id == assistantTape.id), "Save/resume preserves player and assistant inventory");
        checks.Add("Pause-save-resume retains inventories");
        Call(game, "UpdateDay", 481f);
        Check(game.CurrentPhase == GamePhase.DayResults && data.hands.Count == 0, "Shift immediately ends and returns hands");
        Check(VhsSaveSystem.Load().phase == GamePhase.DayResults, "Results autosaved");
        checks.Add("Immediate timer end, hand return, results autosave");
        Call(game, "ProceedToEvening");
        Call(game, "ReceiveForeignCassette");
        Call(game, "ReceiveForeignCassette");
        Check(data.hands.Count(x => x.isForeign) == 1 && VhsSaveSystem.Load().foreignCassetteReceivedToday, "Exactly one guaranteed foreign cassette and evening autosave");
        Call(game, "BeginNight");
        Call(game, "InteractWithDoor");
        Check(data.currentDayIndex == 1 && game.CurrentPhase == GamePhase.NightPlay, "Door locked until viewing");
        Call(game, "InteractWithTelevision");
        Check(game.CurrentPhase == GamePhase.NightCutscene, "Television starts cutscene");
        checks.Add("Evening cassette, night transition, locked door, TV cutscene");
        // Completing the playback itself is tested by FinishPlayback after the real coroutine finishes.
        File.WriteAllText("Temp/verification/acceptance.json", JsonUtility.ToJson(new Report { checks = checks.ToArray(), passed = checks.Count }, true));
        return new { passed = checks.Count, checks, phase = game.CurrentPhase.ToString(), frame = Time.frameCount };
    }

    [Serializable] private class Report { public int passed; public string[] checks; }

    public static object FinishPlayback()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<VhsGameController>();
        var button = (UnityEngine.UI.Button)Field(game, "cutsceneContinueButton");
        Check(button.gameObject.activeSelf, "Actual playback coroutine has finished revealing lore");
        button.onClick.Invoke();
        Check(game.CurrentSave.foreignCassetteWatchedToday && game.CurrentSave.hands.All(x => !x.isForeign), "Viewed tape consumed");
        Call(game, "InteractWithTelevision");
        Check(game.CurrentPhase == GamePhase.NightPlay, "Event does not repeat");
        int money = game.CurrentSave.totalMoney;
        int pile = game.CurrentSave.pile.Count;
        int machine = game.CurrentSave.machineOverflow;
        Call(game, "InteractWithDoor");
        Check(game.CurrentSave.currentDayIndex == 2 && game.CurrentPhase == GamePhase.MorningPrep && game.CurrentSave.totalMoney == money &&
            game.CurrentSave.pile.Count == pile && game.CurrentSave.machineOverflow == machine, "Door advances to day two with persistent economy and backlog");
        Check(game.CurrentSave.upgradeLevels[(int)UpgradeType.CarryCapacity] == 2, "Upgrade retained between days");
        return new { passed = 4, checks = new[] { "Real playback finishes", "Tape consumed and event cannot repeat", "Door -> morning day 2", "Economy, upgrades, basket and machine persisted" } };
    }

    public static object ShowUpgrades()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<VhsGameController>();
        game.CurrentSave.totalMoney = 300;
        Call(game, "OpenUpgradeMenu");
        return new { phase = game.CurrentPhase.ToString() };
    }

    public static object UpgradesAssistantAndFourDays()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<VhsGameController>();
        VhsSaveSystem.EditorSaveDirectory = Path.GetFullPath("Temp/verification/upgrade-save");
        game.NewGame();
        var data = game.CurrentSave;
        data.totalMoney = 10000;
        for (int i = 0; i < 10; i++)
        {
            UpgradeType upgrade = (UpgradeType)i;
            var track = game.Config.GetTrack(upgrade);
            for (int level = 1; level < track.values.Count; level++) Call(game, "PurchaseUpgrade", upgrade);
            Check(data.upgradeLevels[i] == track.values.Count - 1, "Purchase max level " + upgrade);
        }
        Check(data.shelves.All(x => x.slots.Count == 60), "All four shelves expanded to 60 slots");
        Check(((IEnumerable)Call(game, "UnlockedGenres")).Cast<object>().Count() == 4, "Drama and audio both unlocked");
        int money = data.totalMoney;
        Call(game, "PurchaseUpgrade", UpgradeType.CarryCapacity);
        Check(data.totalMoney == money, "Maxed upgrade cannot be bought again");
        game.StartDay();
        Call(game, "PurchaseUpgrade", UpgradeType.WalkSpeed);
        Check(data.totalMoney == money, "Upgrades inaccessible during shift");
        data.shelves[0].slots[0] = new ShelfSlotData { occupied = true, cassette = Tape(CassetteGenre.Action) };
        ResetOrder(game, CassetteGenre.Action);
        typeof(VhsGameController).GetField("assistantCooldown", Private).SetValue(game, 0f);
        for (int i = 0; i < 800 && data.dayStats.completedOrders == 0; i++) Call(game, "UpdateAssistant", 0.05f);
        Check(data.dayStats.completedOrders == 1 && data.assistantHeldCassette == null, "Assistant walks around furniture, picks up at shelf, delivers order");
        for (int day = 1; day <= 4; day++)
        {
            Check(data.dayTimeRemaining == 480, "Shift duration fixed on day " + day);
            data.visitors.Clear();
            for (int i = 0; i < 8; i++) Call(game, "AddVisitor");
            int max = game.Config.orderMaximumByDay[day - 1];
            Check(data.visitors.All(x => x.TotalCount() >= 1 && x.TotalCount() <= max), "Order size progression day " + day);
            Check(Mathf.Approximately((float)Call(game, "MachineInterval"), 9f - 0.5f * (day - 1)), "Machine accelerates between days");
            Call(game, "EndDay");
            Call(game, "ProceedToEvening");
            Call(game, "ReceiveForeignCassette");
            Call(game, "BeginNight");
            Call(game, "InteractWithTelevision");
            Call(game, "FinishNightCutscene");
            Call(game, "InteractWithDoor");
            if (day < 4) game.StartDay();
        }
        Check(data.currentDayIndex == 5 && game.CurrentPhase == GamePhase.MorningPrep, "Four complete day-evening-night cycles");
        Call(game, "SaveNow", false);
        var reloaded = VhsSaveSystem.Load();
        Check(reloaded.currentDayIndex == 5 && reloaded.upgradeLevels.SequenceEqual(data.upgradeLevels), "Four-day progress persisted on disk");
        return new { passed = 8, checks = new[] { "All ten tracks purchased to maximum", "Four genres and 60 shelf slots", "Upgrade phase and max-level guards",
            "Actual assistant path, pickup, delivery", "Four full game cycles", "480-second days", "Order and machine progression", "JSON persistence after day four" } };
    }
}
