using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This lets you enable or disable the New Input System, the old input system, or enable both at the same time.
    /// <br/><br/>
    /// Restarts the editor if the setting is changed.
    /// </summary>
    public readonly struct EnableNewInputSystemStep: IPatcherStep {
        private readonly InputSystemType _inputSystemType;
        
        public EnableNewInputSystemStep(InputSystemType inputSystemType) {
            _inputSystemType = inputSystemType;
        }

        // [MenuItem("Tools/UPP/Input System Test - Old")]
        // private static void Old() {
        //     var step = new EnableNewInputSystemStep(InputSystemType.InputManager_Old);
        //     step.Run().Forget();
        // }
        //
        // [MenuItem("Tools/UPP/Input System Test - New")]
        // private static void New() {
        //     var step = new EnableNewInputSystemStep(InputSystemType.InputSystem_New);
        //     step.Run().Forget();
        // }
        //
        // [MenuItem("Tools/UPP/Input System Test - Both")]
        // private static void Both() {
        //     var step = new EnableNewInputSystemStep(InputSystemType.Both);
        //     step.Run().Forget();
        // }
        
        public UniTask<StepResult> Run() {
            try {
                // do we even have the new input system installed?
                var request = Client.List(false, false);
                while (true) {
                    if (request.IsCompleted) break;
                    if (request.Status == StatusCode.Failure && request.Error != null) {
                        EditorUtility.ClearProgressBar();
                        throw new Exception($"Failed to list packages! [{request.Error.errorCode}] {request.Error.message}");
                    }
                }
                
                if (!request.Result.Any(x => x.name == "com.unity.inputsystem")) {
                    Debug.LogWarning("Input System is not installed");
                    return UniTask.FromResult(StepResult.Success);
                }

                if (!Assign()) {
                    return UniTask.FromResult(StepResult.Success);
                }
                
                EditorUtility.DisplayDialog("Input System Enabled", "Input System is now enabled.", "OK");
            } catch {
                Debug.LogError("Could not find or modify PlayerSettings");
                return UniTask.FromResult(StepResult.Failure);
            }
            
            return UniTask.FromResult(StepResult.RestartEditor);
        }

        public void OnComplete(bool failed) { }

        public bool Assign() {
#if UNITY_2020_3_OR_NEWER
            var playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>()[0];
            var playerSettingsSo = new SerializedObject(playerSettings);
            var activeInputHandlerProp = playerSettingsSo.FindProperty("activeInputHandler");
            if (activeInputHandlerProp.intValue == (int)_inputSystemType) {
                Debug.LogWarning($"Input System is already enabled! ({activeInputHandlerProp.intValue} == {(int)_inputSystemType})");
                return false;
            }
                
            // 0: Input Manager (Old)
            // 1: Input System Package (New)
            // 2: Both
            activeInputHandlerProp.intValue = (int)_inputSystemType;
            playerSettingsSo.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("Input System is now enabled");
            return true;
#else
            var projectSettings = PatcherUtility.GetProjectSettings();
            var playerSettingsSo = new SerializedObject(projectSettings);
            var enableNativePlatformBackendsForNewInputSystem = playerSettingsSo.FindProperty("enableNativePlatformBackendsForNewInputSystem");
            var disableOldInputManagerSupport = playerSettingsSo.FindProperty("disableOldInputManagerSupport");

            // 0: not toggled
            // 1: toggled
            switch (_inputSystemType) {
                case InputSystemType.InputManager_Old:
                    if (!enableNativePlatformBackendsForNewInputSystem.boolValue && !disableOldInputManagerSupport.boolValue) {
                        Debug.LogWarning("New Input System is already disabled");
                        return false;
                    }
                    enableNativePlatformBackendsForNewInputSystem.boolValue = false;
                    disableOldInputManagerSupport.boolValue = false;
                    playerSettingsSo.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("Input System is now enabled");
                    return true;
                case InputSystemType.InputSystem_New:
                    if (enableNativePlatformBackendsForNewInputSystem.boolValue && disableOldInputManagerSupport.boolValue) {
                        Debug.LogWarning("New Input System is already enabled");
                        return false;
                    }
                    enableNativePlatformBackendsForNewInputSystem.boolValue = true;
                    disableOldInputManagerSupport.boolValue = true;
                    playerSettingsSo.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("Input System is now enabled");
                    return true;
                case InputSystemType.Both:
                    if (enableNativePlatformBackendsForNewInputSystem.boolValue && !disableOldInputManagerSupport.boolValue) {
                        Debug.LogWarning("New Input System is already enabled");
                        return false;
                    }
                    enableNativePlatformBackendsForNewInputSystem.boolValue = true;
                    disableOldInputManagerSupport.boolValue = false;
                    playerSettingsSo.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("Input System is now enabled");
                    return true;
            }
            
            return false;
#endif
        }
    }

    public enum InputSystemType {
        InputManager_Old,
        InputSystem_New,
        Both
    }
}