using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using VhsInheritance;

// Play-mode regression checks. These use a separate save directory and real input frames.
public static class VhsCartoonAcceptance
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string ReportPath = "Temp/verification/cartoon-acceptance.json";
    [Serializable] private sealed class Report { public bool complete; public int passed; public string error; public string[] checks; }
    private static object Field(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void KeyState(params Key[] keys) => InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));

    public static object Run()
    {
        Check(UnityEditor.EditorApplication.isPlaying, "Play mode required");
        var game = UnityEngine.Object.FindAnyObjectByType<VhsGameController>();
        VhsSaveSystem.EditorSaveDirectory = Path.GetFullPath("Temp/verification/cartoon-regression-save");
        Directory.CreateDirectory("Temp/verification");
        File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report()));
        game.NewGame();
        game.StartCoroutine(Guard(Verify(game)));
        return new { started = true, report = ReportPath };
    }

    private static IEnumerator Guard(IEnumerator inner)
    {
        while (true)
        {
            bool more;
            object current = null;
            try { more = inner.MoveNext(); if (more) current = inner.Current; }
            catch (Exception ex)
            {
                KeyState();
                File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { complete = true, error = ex.ToString() }, true));
                yield break;
            }
            if (!more) yield break;
            yield return current;
        }
    }

    private static IEnumerator Verify(VhsGameController game)
    {
        var checks = new List<string>();
        var player = (Transform)Field(game, "player");
        var panel = (GameObject)Field(game, "upgradePanel");
        player.position = ((Transform)Field(game, "upgradePoint")).position;
        KeyState(); yield return new WaitForSecondsRealtime(0.08f);
        KeyState(Key.E); yield return new WaitForSecondsRealtime(0.12f);
        Check(panel.activeSelf, "E at stand opens menu");
        KeyState(); yield return new WaitForSecondsRealtime(0.08f);
        KeyState(Key.E); yield return new WaitForSecondsRealtime(0.16f);
        Check(!panel.activeSelf, "E closes menu and held E does not immediately reopen it");
        KeyState(); yield return new WaitForSecondsRealtime(0.08f);
        KeyState(Key.E); yield return new WaitForSecondsRealtime(0.08f);
        Check(panel.activeSelf, "Next E press opens again");
        KeyState(); yield return new WaitForSecondsRealtime(0.08f);
        KeyState(Key.Escape); yield return new WaitForSecondsRealtime(0.08f);
        Check(!panel.activeSelf && !(bool)Field(game, "paused"), "Esc closes menu without opening pause");
        KeyState(); yield return new WaitForSecondsRealtime(0.08f);
        checks.Add("Actual keyboard E opens/closes/reopens; Esc closes without pause");

        Physics2D.SyncTransforms();
        var fixtures = (List<BoxCollider2D>)Field(game, "fixtureColliders");
        int active = 0;
        foreach (var collider in fixtures.Where(x => x.gameObject.activeInHierarchy))
        {
            active++;
            Check(!collider.isTrigger && collider.bounds.size.y < 0.7f, "Furniture has a solid shallow footprint: " + collider.name);
            Vector2 blocked = (Vector2)collider.bounds.center - Vector2.up * 0.1f;
            Check(!(bool)Call(game, "CanPlayerStand", blocked), "Player blocked at footprint: " + collider.name);
        }
        Check(active >= 9, "All active furniture/decor collider components exist");
        Check((bool)Call(game, "CanPlayerStand", new Vector2(-5.72f, 1.9f)), "Player can walk behind tall shelf facade");
        Check((bool)Call(game, "CanPlayerStand", new Vector2(-6.25f, -1.8f)), "Locked shelf leaves no invisible collider");
        checks.Add("Solid footprints block feet; upper facades and locked shelves remain passable");

        var bounds = (Rect)Field(game, "walkableBounds");
        var obstacles = (List<Rect>)Field(game, "navigationObstacles");
        foreach (var interaction in (IEnumerable)Field(game, "interactions"))
        {
            var point = (Transform)interaction.GetType().GetField("point").GetValue(interaction);
            if (!point.gameObject.activeInHierarchy) continue;
            Check((bool)Call(game, "CanPlayerStand", (Vector2)point.position), "Interaction point outside furniture: " + point.name);
            Check(VhsGridPathfinder.FindPath(new Vector2(0, -0.85f), point.position, bounds, 0.2f, obstacles).Count > 0,
                "Interaction reachable around fixtures: " + point.name);
        }
        checks.Add("Every active interaction is reachable with navigation using the same collider bounds");

        // Walk into the upper side of the action shelf using actual WASD input.
        player.position = new Vector2(-5.72f, 1.8f);
        KeyState(Key.S); yield return new WaitForSecondsRealtime(0.6f);
        KeyState(); yield return new WaitForSecondsRealtime(0.08f);
        Check(player.position.y > 1.2f && player.position.y < 1.65f, "Actual WASD movement stops at shelf feet");
        checks.Add("Actual WASD movement stops at solid furniture");
        player.position = new Vector2(0, -0.85f);

        var data = game.CurrentSave;
        data.pile.Clear();
        Call(game, "RefreshPileVisual");
        yield return null;
        var pileRoot = (Transform)Field(game, "pileVisualRoot");
        Check(pileRoot.childCount == 0, "Empty pile has no tray or cassette object");
        var initial = new CassetteData { id = "first-cartoon-tape", genre = CassetteGenre.Action };
        data.pile.Add(initial);
        Call(game, "RefreshPileVisual");
        yield return new WaitForSecondsRealtime(0.3f);
        var first = pileRoot.GetChild(0).GetComponent<VhsPileCassette>();
        float singleHeight = first.transform.position.y;
        foreach (int count in new[] { 12, 40, 85 })
        {
            while (data.pile.Count < count) data.pile.Add(new CassetteData { id = Guid.NewGuid().ToString("N"), genre = (CassetteGenre)(data.pile.Count % 2) });
            Call(game, "RefreshPileVisual");
            yield return new WaitForSecondsRealtime(0.3f);
            var views = pileRoot.GetComponentsInChildren<VhsPileCassette>();
            Check(views.Length == count && views.Select(x => x.CassetteId).Distinct().Count() == count, "Exactly one object per tape at count " + count);
            Check(ReferenceEquals(first, views.Single(x => x.CassetteId == initial.id)), "Existing tape objects persist across growth");
            Check(views.All(x => x.GetComponent<BoxCollider2D>().isTrigger), "Tape objects have individual pickable colliders");
        }
        Check(pileRoot.GetComponentsInChildren<VhsPileCassette>().Max(x => x.transform.position.y) > singleHeight + 1f, "Full upgraded pile is visibly taller");
        data.pile.RemoveRange(1, 84);
        Call(game, "RefreshPileVisual");
        yield return new WaitForSecondsRealtime(0.3f);
        Check(pileRoot.childCount == 1 && ReferenceEquals(first, pileRoot.GetChild(0).GetComponent<VhsPileCassette>()), "Pile shrinks by removing only missing tape objects");
        checks.Add("Pile grows 1/12/40/85 and shrinks to 1; saved IDs preserve individual objects");

        foreach (var text in game.GetComponentsInChildren<TextMeshPro>(true))
        {
            if (text.transform.parent.name.EndsWith(" Shelf"))
                Check(!text.text.Any(char.IsDigit), "World shelf label has no occupancy count");
            Check(!text.name.Contains("PileCount"), "No world pile counter exists");
        }
        Check(pileRoot.GetComponentsInChildren<TMP_Text>(true).Length == 0, "Pile has no in-game count text");
        checks.Add("Pile and world shelf labels expose no counts in game");

        game.StartDay();
        game.CurrentSave.machineTimer = 999;
        game.CurrentSave.visitorSpawnTimer = 999;
        Call(game, "ProduceCassette");
        var producedId = data.pile.Last().id;
        var produced = pileRoot.GetComponentsInChildren<VhsPileCassette>().Single(x => x.CassetteId == producedId);
        Check(Vector2.Distance(produced.transform.position, new Vector2(6.9f, -1.75f)) < 0.1f, "New tape starts at machine output");
        yield return new WaitForSecondsRealtime(0.8f);
        Check(produced.transform.position.x < 6.5f && produced.transform.position.y < -3f, "New tape drops from machine into pile over actual frames");
        checks.Add("Actual animated machine output adds an individual cassette to the pile");

        Call(game, "SaveNow", false);
        game.ContinueGame();
        yield return new WaitForSecondsRealtime(0.3f);
        Check(game.CurrentSave.pile.Count == 2 && game.CurrentSave.pile[0].id == initial.id && pileRoot.childCount == 2,
            "Reloaded pile keeps saved identity and matching scene objects; empty assistant adds no ghost tape");
        checks.Add("JSON save/reload restores individual pile objects");

        File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { complete = true, passed = checks.Count, checks = checks.ToArray() }, true));
    }

    public static object ReportResult() => JsonUtility.FromJson<Report>(File.ReadAllText(ReportPath));
}
