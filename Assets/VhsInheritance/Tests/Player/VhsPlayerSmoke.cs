#if VHS_PLAYER_SMOKE
using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace VhsInheritance.Tests
{
    // Compiled only into an isolated smoke-test player, never into the game shipped to players.
    public sealed class VhsPlayerSmoke : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed;
            public string error;
            public int frame;
            public int sprites;
            public int pileObjects;
            public string phase;
            public string font;
        }
        private const string ReportDirectory = "D:/Games/Unity/Projects/incremental/Temp/verification";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            Application.runInBackground = true;
            VhsSaveSystem.EditorSaveDirectory = Path.Combine(ReportDirectory, "standalone-save");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartSmoke() => new GameObject("Isolated Player Smoke").AddComponent<VhsPlayerSmoke>();

        private IEnumerator Start()
        {
            yield return null;
            var report = new Report();
            try
            {
                var game = UnityEngine.Object.FindAnyObjectByType<VhsGameController>();
                if (game == null) throw new Exception("Controller missing");
                game.NewGame();
                var sprites = game.GetComponentsInChildren<SpriteRenderer>(true);
                if (sprites.Any(x => x.sprite == null)) throw new Exception("Sprite asset missing in player");
                var labels = game.GetComponentsInChildren<TMP_Text>(true);
                var roundedFont = Resources.Load<TMP_FontAsset>("Fonts/SalonRounded");
                if (roundedFont == null || labels.Any(x => x.font != roundedFont))
                    throw new Exception("Rounded Cyrillic font missing: " + string.Join(", ", labels.Select(x => x.name + "=" + (x.font == null ? "null" : x.font.name)).Distinct()));
                var tapes = game.GetComponentsInChildren<VhsPileCassette>();
                if (tapes.Length != game.CurrentSave.pile.Count) throw new Exception("Pile object count differs from data");
                report.sprites = sprites.Length;
                report.pileObjects = tapes.Length;
                report.font = labels.First().font.name;
                report.phase = game.CurrentPhase.ToString();
                report.passed = true;
            }
            catch (Exception ex) { report.error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(2);
            report.frame = Time.frameCount;
            Directory.CreateDirectory(ReportDirectory);
            if (report.passed)
            {
                try { CaptureSalon(); }
                catch (Exception ex) { report.passed = false; report.error = ex.ToString(); }
            }
            File.WriteAllText(Path.Combine(ReportDirectory, "standalone-smoke.json"), JsonUtility.ToJson(report, true));
            yield return new WaitForSecondsRealtime(2);
            Application.Quit(report.passed ? 0 : 2);
        }

        private static void CaptureSalon()
        {
            var camera = Camera.main;
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            var target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            var original = RenderTexture.active;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(ReportDirectory, "standalone-salon.png"), image.EncodeToPNG());
                var pixels = image.GetPixels32();
                if (pixels.All(x => x.r == pixels[0].r && x.g == pixels[0].g && x.b == pixels[0].b))
                    throw new Exception("Player camera produced a flat frame");
            }
            finally
            {
                RenderTexture.active = original;
                RenderTexture.ReleaseTemporary(target);
                Destroy(image);
            }
        }
    }
}
#endif
