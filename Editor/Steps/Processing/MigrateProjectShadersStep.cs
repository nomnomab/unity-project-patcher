using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This lets you migrate shaders from one Shader name to another.
    /// <br/><br/>
    /// Such as:
    /// <br/>- GAME/Legacy Shaders/Particles/Multiply (Double) to Legacy Shaders/Particles/Multiply (Double)
    /// <br/><br/>
    /// Restarts the editor.
    /// </summary>
    public readonly struct MigrateProjectShadersStep: IPatcherStep {
        private readonly (string from, string to)[] _shadersToMigrate;
        
        public MigrateProjectShadersStep(params (string from, string to)[] shadersToMigrate) {
            _shadersToMigrate = shadersToMigrate;
        }
        
        public UniTask<StepResult> Run() {
            EditorUtility.DisplayProgressBar("Migrating Shaders", "Migrating Shaders", 0);
            var allMaterials = AssetDatabase.FindAssets("t:Material").Select(x => AssetDatabase.GUIDToAssetPath(x));
            foreach (var assetPath in allMaterials) {
                EditorUtility.DisplayProgressBar("Migrating Shaders", assetPath, 0);
                var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                var shader = material.shader;
                foreach (var (from, to) in _shadersToMigrate) {
                    var shaderFrom = Shader.Find(from);
                    if (!shaderFrom) {
                        Debug.LogWarning($"Could not find shader \"{from}\". Skipping migration.");
                        continue;
                    }
                    
                    if (shader != shaderFrom) {
                        continue;
                    }
                    
                    var shaderTo = Shader.Find(to);
                    if (!shaderTo) {
                        Debug.LogWarning($"Could not find shader \"{to}\". Skipping migration.");
                        continue;
                    }
                    
                    material.shader = shaderTo;
                    
                    Debug.Log($"Migrated \"{assetPath}\" from \"{from}\" to \"{to}\"");
                    break;
                }

                if (shader != material.shader) {
                    // save material
                    EditorUtility.SetDirty(material);
                }
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            // AssetDatabase.Refresh();

            return UniTask.FromResult(StepResult.RestartEditor);
        }

        public void OnComplete(bool failed) { }
    }
}