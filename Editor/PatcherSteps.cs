using System.Reflection;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;

namespace Nomnom.UnityProjectPatcher.Editor {
    /// <summary>
    /// The core of the patcher. Executes various steps in sequence.
    /// </summary>
    public static class PatcherSteps {
        [InitializeOnLoadMethod]
        private static void OnLoad() {
            // locate game patcher
            foreach (var type in TypeCache.GetTypesWithAttribute<UPPatcherAttribute>()) {
                // does it have a Run function?
                var runFunction = type.GetMethod("Run", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                if (runFunction is null) continue;
                if (runFunction.GetParameters().Length > 0) continue;
                
                // Debug.Log($"Found Patcher: {type.Name}");
                
                EditorApplication.delayCall += () => {
                    var progress = StepsProgress.FromPath(StepsProgress.SavePath);
                    if (progress is null) return;

                    runFunction.Invoke(null, null);
                };

                //! only run one patcher
                break;
            }
        }
    }
}