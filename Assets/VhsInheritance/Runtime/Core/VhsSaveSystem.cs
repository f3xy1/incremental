using System;
using System.IO;
using UnityEngine;

namespace VhsInheritance
{
    public static class VhsSaveSystem
    {
        private const string FileName = "vhs_inheritance_save.json";

        public static string SavePath
        {
            get
            {
#if UNITY_EDITOR || VHS_PLAYER_SMOKE
                if (!string.IsNullOrEmpty(EditorSaveDirectory))
                    return Path.Combine(EditorSaveDirectory, FileName);
#endif
                return Path.Combine(Application.persistentDataPath, FileName);
            }
        }
#if UNITY_EDITOR || VHS_PLAYER_SMOKE
        // Editor acceptance runs use a separate slot; the player's save is never touched.
        public static string EditorSaveDirectory { get; set; }
#endif

        public static bool HasSave()
        {
            return File.Exists(SavePath) || File.Exists(SavePath + ".bak");
        }

        public static void Save(VhsSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = SavePath + ".tmp";
            string backupPath = SavePath + ".bak";
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(SavePath))
            {
                try
                {
                    File.Replace(temporaryPath, SavePath, backupPath, true);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Copy(temporaryPath, SavePath, true);
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                    File.Copy(temporaryPath, SavePath, true);
                    File.Delete(temporaryPath);
                }
            }
            else
            {
                File.Move(temporaryPath, SavePath);
            }
        }

        public static VhsSaveData Load()
        {
            if (!HasSave())
            {
                return null;
            }

            VhsSaveData primary = TryLoad(SavePath);
            if (primary != null) return primary;
            VhsSaveData backup = TryLoad(SavePath + ".bak");
            if (backup != null) Debug.LogWarning("[VHS] Сохранение восстановлено из резервной копии.");
            return backup;
        }

        private static VhsSaveData TryLoad(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path);
                if (!json.Contains("\"currentDayIndex\"") || !json.Contains("\"saveVersion\"")) return null;
                return JsonUtility.FromJson<VhsSaveData>(json);
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException)
            {
                Debug.LogWarning($"[VHS] Не удалось прочитать слот: {exception.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            DeleteIfExists(SavePath);
            DeleteIfExists(SavePath + ".tmp");
            DeleteIfExists(SavePath + ".bak");
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
