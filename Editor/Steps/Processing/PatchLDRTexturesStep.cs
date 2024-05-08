using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This checks if there are any LDR textures in the project that need
    /// to be converted to <see cref="TextureImporterFormat.RGB24"/>.
    /// </summary>
    public readonly struct PatchLDRTexturesStep: IPatcherStep {
        private readonly string _prefix;
        
        // [MenuItem("Tools/UPP/Patch LDR Textures")]
        // public static void Patch() {
        //     new PatchLDRTexturesStep("LDR_RGB1_").Run().Forget();
        // }
        
        public PatchLDRTexturesStep(string prefix) {
            _prefix = prefix;
        }
        
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var arSettings = this.GetAssetRipperSettings();
            
            if (!arSettings.TryGetFolderMapping("Texture2D", out var texturesFolder,  out var exclude) || exclude) {
                Debug.LogError("Could not find \"Texture2D\" folder mapping");
                return UniTask.FromResult(StepResult.Success);
            }
            
            Debug.Log(Path.Combine(settings.ProjectGameAssetsPath, texturesFolder).ToAssetDatabaseSafePath());

            var prefix = _prefix;
            var assets = AssetDatabase.FindAssets($"t:Texture2D", new string[] {
                    Path.Combine(settings.ProjectGameAssetsPath, texturesFolder).ToAssetDatabaseSafePath()
                })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => !string.IsNullOrEmpty(x) && Path.GetFileNameWithoutExtension(x).StartsWith(prefix))
                .ToArray();

            // change all to RGB24
            for (var i = 0; i < assets.Length; i++) {
                var asset = AssetDatabase.LoadMainAssetAtPath(assets[i]);
                EditorUtility.DisplayProgressBar("Changing texture format", $"Changing {assets[i]}", (float)i / assets.Length);
                try {
                    // var asset = assets[i];
                    // var path = AssetDatabase.GetAssetPath(asset);
                    if (AssetImporter.GetAtPath(assets[i]) is TextureImporter importer) {
                        var defaultPlatform = importer.GetDefaultPlatformTextureSettings();
                        defaultPlatform.format = TextureImporterFormat.RGB24;
                        importer.SetPlatformTextureSettings(defaultPlatform);
                        importer.SaveAndReimport();
                    } else {
                        Debug.LogWarning($"Could not find TextureImporter for {asset.name}");
                    }
                } catch (Exception e) {
                    Debug.LogError(e);
                }
            }
            
            EditorUtility.ClearProgressBar();
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}