using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This lets you replace a material with another.
    /// </summary>
    public readonly struct MigrateProjectMaterialsStep: IPatcherStep {
        // [MenuItem("Tools/Unity Project Patcher/Other/Migrate Project Materials")]
        // private static void Foo() {
        //     try {
        //         var waterMaterialPath = "Packages/com.nomnom.unity-lc-project-patcher/Runtime/Water/WaterMaterial.mat";
        //         new MigrateProjectMaterialsStep(
        //             ("VowWater", "Packages/com.nomnom.unity-lc-project-patcher/Runtime/Water/VowWater_REPLACEMENT.mat"),
        //             ("Water_mat_04", "Packages/com.nomnom.unity-lc-project-patcher/Runtime/Water/Water_mat_04_REPLACEMENT.mat")
        //         ).Run().Forget();
        //     } catch (Exception e) {
        //         Debug.LogException(e);
        //         EditorUtility.ClearProgressBar();
        //     }
        // }
        
        private readonly (string materialName, string newMaterialPath)[] _materialsToMigrate;
        
        public MigrateProjectMaterialsStep(params (string materialName, string newMaterialPath)[] materialsToMigrate) {
            _materialsToMigrate = materialsToMigrate;
        }
        
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            EditorUtility.DisplayProgressBar("Migrating Materials", "Migrating Materials", 0);
            var allMaterials = AssetDatabase.FindAssets("t:Material", new string[] {
                settings.ProjectGameAssetsPath
            }).Select(x => AssetDatabase.GUIDToAssetPath(x));
            
            foreach (var assetPath in allMaterials) {
                EditorUtility.DisplayProgressBar("Migrating Materials", assetPath, 0);
                var name = Path.GetFileNameWithoutExtension(assetPath);
                var pair = _materialsToMigrate.FirstOrDefault(x => x.materialName == name);
                if (pair.newMaterialPath == null) continue;
                
                var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (!material) {
                    Debug.LogWarning($"Could not find material \"{assetPath}\". Skipping migration.");
                    continue;
                }
                
                var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(pair.newMaterialPath);
                if (!newMaterial) {
                    Debug.LogWarning($"Could not find material \"{pair.newMaterialPath}\". Skipping migration.");
                    continue;
                }
                
                material.shader = newMaterial.shader;
                material.CopyPropertiesFromMaterial(newMaterial);
                EditorUtility.SetDirty(material);
                
                Debug.Log($"Migrated \"{assetPath}\" from \"{pair.materialName}\" to \"{pair.newMaterialPath}\"");
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            // AssetDatabase.Refresh();

            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}