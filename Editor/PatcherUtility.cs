using System;
using System.IO;
using Nomnom.UnityProjectPatcher.AssetRipper;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

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