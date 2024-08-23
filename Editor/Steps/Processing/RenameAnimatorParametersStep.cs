using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct RenameAnimatorParametersStep: IPatcherStep {
        // [MenuItem("Tools/Unity Project Patcher/Other/Rename Animator Parameters")]
        // private static void Foo() {
        //     try {
        //         new RenameAnimatorParametersStep(
        //             new RenameAnimatorParametersStep.Replacement("SunAnimContainer", ("eclipsed", "eclipse")),
        //             new RenameAnimatorParametersStep.Replacement("SunAnimContainer 1", ("eclipsed", "eclipse"))
        //         ).Run().Forget();
        //     } catch (Exception e) {
        //         Debug.LogException(e);
        //         EditorUtility.ClearProgressBar();
        //     }
        // }
        
        private readonly Replacement[] _toRename;

        public RenameAnimatorParametersStep(params Replacement[] toRename) {
            _toRename = toRename;
        }
        
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var animatorControllers = AssetDatabase.FindAssets($"t:{nameof(AnimatorController)}", new string[] {
                settings.ProjectGameAssetsPath
            }).Select(AssetDatabase.GUIDToAssetPath);
            
            foreach (var rename in _toRename) {
                var controller = animatorControllers.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(rename.name, StringComparison.InvariantCultureIgnoreCase));
                if (string.IsNullOrEmpty(controller)) continue;
                
                try {
                    var path = Path.GetFullPath(controller);
                    var contents = File.ReadAllText(path.ToValidPath());

                    foreach (var pair in rename.replace) {
                        contents = contents.Replace($"m_Name: {pair.from}", $"m_Name: {pair.to}");
                        contents = contents.Replace($"m_ConditionEvent: {pair.from}", $"m_ConditionEvent: {pair.to}");
                    }

                    File.WriteAllText(path.ToValidPath(), contents);
                } catch (Exception e) {
                    Debug.LogError($"Failed to replace animator controller parameters for \"{controller}\":\n{e}");
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }

        public readonly struct Replacement {
            public readonly string name;
            public readonly (string from, string to)[] replace;
            
            public Replacement(string name, params (string from, string to)[] replace) {
                this.name = name;
                this.replace = replace;
            }
        }
    }
}