using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct InjectHDRPAssetsStep: IPatcherStep {
        [MenuItem("Tools/UPP/Inject HDRP Assets")]
        public static void InjectHDRPAssets() {
            var step = new InjectHDRPAssetsStep();
            step.Run().Forget();
        }
        
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var arSettings = this.GetAssetRipperSettings();
            
            if (!arSettings.TryGetFolderMapping("MonoBehaviour", out var soFolder, out var exclude) || exclude) {
                Debug.LogError("Could not find \"MonoBehaviour\" folder mapping");
                return UniTask.FromResult(StepResult.Success);
            }

            var hdrpSettingAssets = AssetDatabase.FindAssets("t:UnityEngine.Rendering.HighDefinition.HDRenderPipelineGlobalSettings", new string[] {
                Path.Combine(settings.ProjectGameAssetsPath, soFolder).ToAssetDatabaseSafePath()
            })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            
            if (hdrpSettingAssets.Length == 0) {
                Debug.LogError("Could not find HDRenderPipelineGlobalSettings asset");
                return UniTask.FromResult(StepResult.Success);
            }
            
            var hdrpSettingsPath = hdrpSettingAssets.First();
            if (hdrpSettingsPath is null) {
                Debug.LogError("Could not find HDRenderPipelineGlobalSettings asset");
                return UniTask.FromResult(StepResult.Success);
            }
            
            var hdrpSettings = AssetDatabase.LoadMainAssetAtPath(hdrpSettingsPath);
            var projectGraphicsAsset = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            var serializedObject = new SerializedObject(projectGraphicsAsset);
            var iter = serializedObject.GetIterator();
            iter.Next(true);
            while (iter.Next(true)) {
                // Debug.Log($"{iter.propertyPath} -> {iter.type}");
                if (iter.propertyPath == "m_CustomRenderPipeline") {
                    iter.objectReferenceValue = hdrpSettings;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"Set m_CustomRenderPipeline to \"{hdrpSettingsPath}\"");
                    break;
                }
            }
            
            serializedObject = new SerializedObject(hdrpSettings);
            var volumeProfile = serializedObject.FindProperty("m_DefaultVolumeProfile");
            if (volumeProfile != null) {
                // var newSettingsPath = Path.Combine(settings.ProjectGameAssetsPath, soFolder, "UnityEngine", "VolumeProfile", "DefaultSettingsVolumeProfile.asset").ToAssetDatabaseSafePath();
                var newSettingsPath = Path.Combine(settings.ProjectGameAssetsPath, soFolder, "DefaultSettingsVolumeProfile.asset").ToAssetDatabaseSafePath();
                var newSettings = AssetDatabase.LoadAssetAtPath<Object>(newSettingsPath);
                if (newSettings) {
                    volumeProfile.objectReferenceValue = newSettings;
                    serializedObject.ApplyModifiedProperties();
                    Debug.Log($"Set m_DefaultVolumeProfile to one found at \"{newSettingsPath}\"");

                    // this is so jank
                    try {
                        var graphicsSettingsAsset = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");

                        serializedObject = new SerializedObject(graphicsSettingsAsset);
                        var iterator = serializedObject.GetIterator();
                        iterator.Next(true);

                        while (iterator.Next(true)) {
                            if (iterator.propertyPath == "m_SRPDefaultSettings.Array.data[0].second") {
                                iterator.objectReferenceValue = hdrpSettings;
                                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                                Debug.Log($"Set m_SRPDefaultSettings to \"{hdrpSettingsPath}\"");
                                break;
                            }
                        }
                    } catch (Exception e) {
                        Debug.LogError($"Failed to set m_SRPDefaultSettings to \"{hdrpSettingsPath}\": {e}");
                    }
                } else {
                    Debug.LogWarning($"Could not find DefaultSettingsVolumeProfile at \"{newSettingsPath}\"");
                }
            }
            
            return UniTask.FromResult(StepResult.RestartEditor);
        }
    }
}