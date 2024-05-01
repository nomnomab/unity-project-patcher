using System;
using System.IO;
using System.Text;
using Nomnom.UnityProjectPatcher.AssetRipper;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nomnom.UnityProjectPatcher.Editor {
    public static class PatcherUtility {
        public static UPPatcherSettings GetSettings(this IPatcherStep step) {
            return GetSettings();
        }
        
        public static UPPatcherSettings GetSettings() {
            var assets = AssetDatabase.FindAssets($"t:{nameof(UPPatcherSettings)}");
            if (assets.Length == 0) {
                CreateSettings();
                Debug.LogWarning("Created UPPatcherSettings asset since it was missing");
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
            return AssetDatabase.LoadAssetAtPath<UPPatcherSettings>(assetPath);
        }
        
        private static void CreateSettings() {
            // create one at root
            var settings = ScriptableObject.CreateInstance<UPPatcherSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/UPPatcherSettings.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        public static AssetRipperSettings GetAssetRipperSettings(this IPatcherStep step) {
            return GetAssetRipperSettings();
        }
        
        public static AssetRipperSettings GetAssetRipperSettings() {
            var assets = AssetDatabase.FindAssets($"t:{nameof(AssetRipperSettings)}");
            if (assets.Length == 0) {
                CreateAssetRipperSettings();
                Debug.LogWarning("Created AssetRipperSettings asset since it was missing");
            }
            
            var assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
            return AssetDatabase.LoadAssetAtPath<AssetRipperSettings>(assetPath);
        }

        private static void CreateAssetRipperSettings() {
            // create one at root
            var settings = ScriptableObject.CreateInstance<AssetRipperSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/AssetRipperSettings.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        public static bool TryToCreatePath(string path) {
            if (Directory.Exists(path)) {
                return true;
            }

            try {
                Directory.CreateDirectory(path);
                return true;
            } catch(Exception e) {
                Debug.LogError($"Failed to create path at \"{path}\" with error:\n{e}");
                return false;
            }

            return false;
        }
        
        public static Object GetGraphicsSettings() {
            return AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        }
        
        public static Object GetQualitySettings() {
            return AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/QualitySettings.asset");
        }

        public static SerializedProperty? GetCustomRenderPipelineProperty() {
            var qualitySettings = PatcherUtility.GetQualitySettings();
            var serializedObject = new SerializedObject(qualitySettings);
            
            return serializedObject.FindProperty("m_QualitySettings.Array.data[0].customRenderPipeline");
        }

        private const string ExcludeFromAllPlatformsMetaDummyString = @"
fileFormatVersion: 2
guid: e467066723a20064d8f96fc222107589";
        
        private const string ExcludeFromAllPlatformsMetaString = @"
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      : Any
    second:
      enabled: 0
      settings:
        Exclude Editor: 1
        Exclude Linux64: 1
        Exclude OSXUniversal: 1
        Exclude Win: 1
        Exclude Win64: 1
  - first:
      Any: 
    second:
      enabled: 0
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 0
      settings:
        DefaultValueInitialized: true
  - first:
      Standalone: Linux64
    second:
      enabled: 0
      settings:
        CPU: None
  - first:
      Standalone: OSXUniversal
    second:
      enabled: 0
      settings:
        CPU: None
  - first:
      Standalone: Win
    second:
      enabled: 0
      settings:
        CPU: None
  - first:
      Standalone: Win64
    second:
      enabled: 0
      settings:
        CPU: None
  - first:
      Windows Store Apps: WindowsStoreApps
    second:
      enabled: 0
      settings:
        CPU: AnyCPU
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";
        
        public static void ExcludeDllFromLoading(string path) {
            var metaPath = path + ".meta";
            if (File.Exists(metaPath)) {
                var metaLines = File.ReadAllLines(metaPath);
                // keep the first two lines
                var stringBuilder = new StringBuilder();
                for (var i = 0; i < metaLines.Length; i++) {
                    if (i < 2) {
                        stringBuilder.AppendLine(metaLines[i]);
                    }
                }
                stringBuilder.Append(ExcludeFromAllPlatformsMetaString);
                File.WriteAllText(metaPath, stringBuilder.ToString());
            } else {
                File.WriteAllText(metaPath, $"{ExcludeFromAllPlatformsMetaDummyString}\n{ExcludeFromAllPlatformsMetaString}");
            }
        }

        [MenuItem("CONTEXT/Object/Debug Guid")]
        public static void DebugGuid() {
            var selection = Selection.activeObject;
            if (!selection) return;
            
            var path = AssetDatabase.GetAssetPath(selection);
            var guid = AssetDatabase.AssetPathToGUID(path);
            Debug.Log(guid);
            
            var instance = AssetDatabase.LoadMainAssetAtPath(path);
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instance, out guid, out long fileId)) {
                Debug.Log($"{guid} {fileId}");
            }
        }
        
        // [MenuItem("CONTEXT/Object/Debug Guid2")]
        // public static void DebugGuid2() {
        //     var selection = Selection.activeObject;
        //     if (!selection) return;
        //
        //     Debug.Log(FileIDUtil.Compute(typeof(ES3Defaults)));
        // }
        
        [MenuItem("CONTEXT/MonoScript/Debug FullTypeName")]
        [MenuItem("Assets/Debug FullTypeName")]
        public static void DebugFullTypeName() {
            var selection = Selection.activeObject;
            if (!selection) return;
            if (selection is not MonoScript monoScript) return;
            
            var assetPath = AssetDatabase.GetAssetPath(monoScript);
            foreach (var entry in AssetScrubber.ScrubNonMonoData(monoScript, assetPath, 0)) {
                Debug.Log(entry); 
            } 
        }
    }
}