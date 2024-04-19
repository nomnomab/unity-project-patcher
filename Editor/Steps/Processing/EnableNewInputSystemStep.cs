using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct EnableNewInputSystemStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            try {
                // do we even have the new input system installed?
                var request = Client.List(false, false);
                while (true) {
                    if (request.IsCompleted) break;
                    if (request.Status == StatusCode.Failure && request.Error is { } error) {
                        EditorUtility.ClearProgressBar();
                        throw new Exception($"Failed to list packages! [{error.errorCode}] {error.message}");
                    }
                }
                
                if (!request.Result.Any(x => x.name == "com.unity.inputsystem")) {
                    Debug.LogWarning("Input System is not installed");
                    return UniTask.FromResult(StepResult.Success);
                }
                
                var playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>()[0];
                var playerSettingsSo = new SerializedObject(playerSettings);
                var activeInputHandlerProp = playerSettingsSo.FindProperty("activeInputHandler");
                if (activeInputHandlerProp.intValue == 1) {
                    Debug.LogWarning("Input System is already enabled");
                    return UniTask.FromResult(StepResult.Success);
                }
                
                // 0: Input Manager (Old)
                // 1: Input System Package (New)
                // 2: Both
                activeInputHandlerProp.intValue = 1;
                playerSettingsSo.ApplyModifiedPropertiesWithoutUndo();
                
                EditorUtility.DisplayDialog("Input System Enabled", "Input System is now enabled.", "OK");
            } catch {
                Debug.LogError("Could not find or modify PlayerSettings");
                return UniTask.FromResult(StepResult.Failure);
            }
            
            return UniTask.FromResult(StepResult.RestartEditor);
        }
    }
}