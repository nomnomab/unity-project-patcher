using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct EnableNewInputSystemStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            try {
                var playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>()[0];
                var playerSettingsSo = new SerializedObject(playerSettings);
                var activeInputHandlerProp = playerSettingsSo.FindProperty("activeInputHandler");
                
                // 0: Input Manager (Old)
                // 1: Input System Package (New)
                // 2: Both
                activeInputHandlerProp.intValue = 1;
                playerSettingsSo.ApplyModifiedPropertiesWithoutUndo();
            } catch {
                Debug.LogError("Could not find or modify PlayerSettings");
                return UniTask.FromResult(StepResult.Failure);
            }
            
            return UniTask.FromResult(StepResult.Success);
        }
    }
}