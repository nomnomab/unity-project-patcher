using System.IO;
using Nomnom.UnityProjectPatcher.AssetRipper;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Nomnom.UnityProjectPatcher {
    public static class PatcherUtility {
        public static void SetDirty(UnityEngine.Object obj) {
#if UNITY_EDITOR
            EditorUtility.SetDirty(obj);
#endif
        }
        
        public static string ToAssetDatabaseSafePath(this string path) {
            return path.Replace(Path.DirectorySeparatorChar, '/');
        }
        
        public static string ToOSPath(this string path) {
            return path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }
        
#if UNITY_EDITOR
        public static UPPatcherUserSettings GetUserSettings() {
            var assets = AssetDatabase.FindAssets($"t:{nameof(UPPatcherUserSettings)}");
            if (assets.Length == 0) {
                CreateUserSettings();
                Debug.LogWarning("Created UPPatcherUserSettings asset since it was missing");
                assets = AssetDatabase.FindAssets($"t:{nameof(UPPatcherUserSettings)}");
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
            return AssetDatabase.LoadAssetAtPath<UPPatcherUserSettings>(assetPath);
        }
        
        public static void CreateUserSettings() {
            // create one at root
            var settings = ScriptableObject.CreateInstance<UPPatcherUserSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/UPPatcherUserSettings.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        public static UPPatcherSettings GetSettings() {
            var assets = AssetDatabase.FindAssets($"t:{nameof(UPPatcherSettings)}");
            if (assets.Length == 0) {
                CreateSettings();
                Debug.LogWarning("Created UPPatcherSettings asset since it was missing");
                assets = AssetDatabase.FindAssets($"t:{nameof(UPPatcherSettings)}");
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
        
        public static AssetRipperSettings GetAssetRipperSettings() {
            var assets = AssetDatabase.FindAssets($"t:{nameof(AssetRipperSettings)}");
            if (assets.Length == 0) {
                CreateAssetRipperSettings();
                Debug.LogWarning("Created AssetRipperSettings asset since it was missing");
                assets = AssetDatabase.FindAssets($"t:{nameof(AssetRipperSettings)}");
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
#endif
    }
}