using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This attempts to find URP related assets in the project and inject them into
    /// the SRP pipeline.
    /// </summary>
    public readonly struct InjectURPAssetsStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var arSettings = this.GetAssetRipperSettings();
            
            if (!arSettings.TryGetFolderMapping("MonoBehaviour", out var soFolder, out var exclude) || exclude) {
                Debug.LogError("Could not find \"MonoBehaviour\" folder mapping");
                return UniTask.FromResult(StepResult.Success);
            }
            
            var urpPipelineAssets = AssetDatabase.FindAssets("t:UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset", new string[] {
                    Path.Combine(settings.ProjectGameAssetsPath, soFolder).ToAssetDatabaseSafePath()
                })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

            if (urpPipelineAssets.Length == 0) {
                Debug.LogError("Could not find UniversalRenderPipelineAsset asset");
                return UniTask.FromResult(StepResult.Success);
            }
            
            var urpSettingsPath = urpPipelineAssets.FirstOrDefault();
            if (urpSettingsPath is null) {
                Debug.LogError("Could not find UniversalRenderPipelineAsset asset");
                return UniTask.FromResult(StepResult.Success);
            }
            
            var urpGlobalAssets = AssetDatabase.FindAssets("t:UnityEngine.Rendering.Universal.UniversalRenderPipelineGlobalSettings", new string[] {
                    Path.Combine(settings.ProjectGameAssetsPath, soFolder).ToAssetDatabaseSafePath()
                })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            
            if (urpGlobalAssets.Length == 0) {
                Debug.LogError("Could not find UniversalRenderPipelineGlobalSettings asset");
                return UniTask.FromResult(StepResult.Success);
            }
            
            var urpGlobalSettingsPath = urpGlobalAssets.FirstOrDefault();
            if (urpGlobalSettingsPath is null) {
                Debug.LogError("Could not find UniversalRenderPipelineGlobalSettings asset");
                return UniTask.FromResult(StepResult.Success);
            }
            
            var urpSettings = AssetDatabase.LoadMainAssetAtPath(urpSettingsPath);
            var urpGlobalSettings = AssetDatabase.LoadMainAssetAtPath(urpGlobalSettingsPath);
            var projectGraphicsAsset = PatcherUtility.GetGraphicsSettings();
            var serializedObject = new SerializedObject(projectGraphicsAsset);
            
            serializedObject.FindProperty("m_CustomRenderPipeline").objectReferenceValue = urpSettings;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            
            var iter = serializedObject.GetIterator();
            iter.Next(true);
            while (iter.Next(true)) {
                if (iter.propertyPath == "m_SRPDefaultSettings.Array.data[0].second") {
                    iter.objectReferenceValue = urpGlobalSettings;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"Set m_SRPDefaultSettings.Array.data[0].second to \"{urpGlobalSettingsPath}\"");
                    break;
                }
            }

            var customRenderPipelineProperty = PatcherUtility.GetCustomRenderPipelineProperty();
            if (customRenderPipelineProperty is null) {
                Debug.LogError("Could not find m_QualitySettings.customRenderPipeline");
                return UniTask.FromResult(StepResult.Success);
            }
            
            customRenderPipelineProperty.objectReferenceValue = urpSettings;
            customRenderPipelineProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            
            Debug.Log($"Set m_QualitySettings.customRenderPipeline to \"{urpSettingsPath}\"");

            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}