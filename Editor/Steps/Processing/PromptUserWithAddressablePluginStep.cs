using System.IO;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This prompts the user to run the Addressables plugin manually, as that's the only
    /// way to get the ripped ids from the actual game.
    /// </summary>
    public readonly struct PromptUserWithAddressablePluginStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var keysPath = Path.Combine(settings.GameDataPath, "Addressables_Rip", "0_ResourceLocationMap_AddressablesMainContentCatalog_keys.txt");
            
            if (File.Exists(keysPath)) {
                return UniTask.FromResult(StepResult.Success);
            }
            
            var pluginPath = Path.GetFullPath("Packages/com.nomnom.unity-project-patcher/Editor/Plugin~/AddressablesScanner/AddressablesScanner.dll");
            if (!File.Exists(pluginPath)) {
                Debug.LogError($"Cannot find Addressables plugin at \"{pluginPath}\"");
                return UniTask.FromResult(StepResult.Failure);
            }
            
            EditorUtility.RevealInFinder(pluginPath);
            
            if (!EditorUtility.DisplayDialog("Manual Addressables Fetching", @"
In order to properly convert all AssetBundle ids to Addressable ids, you will have to manually run a BepInEx plugin for the game.

If Explorer didn't open, the plugin is located at ""[UNITY_PROJECT]/Packages/com.nomnom.unity-project-patcher/Editor/Plugin~/AddressablesScanner/AddressablesScanner.dll""

Once the tool is ran, press ""Continue"".", "Continue", "Abort")) {
                return UniTask.FromResult(StepResult.Failure);
            }

            if (!File.Exists(keysPath)) {
                throw new FileNotFoundException("Could not find keys file. Did you run the plugin??", keysPath);
            }
            
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}