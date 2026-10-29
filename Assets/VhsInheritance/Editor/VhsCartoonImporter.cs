using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace VhsInheritance.Editor
{
    public static class VhsCartoonImporter
    {
        [Serializable] private class Row { public string name; public float ppu; public float pivotX; public float pivotY; }
        [Serializable] private class Rows { public Row[] items; }
        [MenuItem("Tools/VHS Prototype/Import Cartoon Art")]
        public static void Import()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string json = File.ReadAllText("Assets/VhsInheritance/Art/Cartoon/import-settings.json");
            Rows rows = JsonUtility.FromJson<Rows>("{\"items\":" + json + "}");
            foreach (Row row in rows.items)
            {
                string path = "Assets/VhsInheritance/Resources/Art/Cartoon/" + row.name + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = row.ppu;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(row.pivotX, row.pivotY);
                importer.SetTextureSettings(settings);
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            string fontPath = "Assets/VhsInheritance/Resources/Fonts/SalonRounded.asset";
            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/VhsInheritance/Art/Fonts/NunitoSemiBold.ttf");
            if (source != null && AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath) == null)
            {
                var font = TMP_FontAsset.CreateFontAsset(source, 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                font.name = "Salon Rounded";
                AssetDatabase.CreateAsset(font, fontPath);
                font.atlasTextures[0].name = "Salon Rounded Atlas";
                AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
                font.material.name = "Salon Rounded Material";
                AssetDatabase.AddObjectToAsset(font.material, font);
                font.TryAddCharacters("АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюяABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 $.,!?—·:/%+→«»()[]", out _);
                EditorUtility.SetDirty(font);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[VHS] Cartoon sprites imported: " + rows.items.Length);
        }
    }
}
