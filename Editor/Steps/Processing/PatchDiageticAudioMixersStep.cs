using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This fixes AudioMixers that default to Echo being enabled.
    /// </summary>
    public readonly struct PatchDiageticAudioMixersStep: IPatcherStep {
        private readonly string[] _mixerNames;
        
        public PatchDiageticAudioMixersStep(params string[] mixerNames) {
            _mixerNames = mixerNames;
        }
        
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var arSettings = this.GetAssetRipperSettings();
            
            if (!arSettings.TryGetFolderMapping("AudioMixerController", out var finalFolder, out var exclude) || exclude) {
                Debug.LogWarning("Could not find AudioMixerController folder");
                return UniTask.FromResult(StepResult.Success);
            }

            for (var m = 0; m < _mixerNames.Length; m++) {
                EditorUtility.DisplayProgressBar("Patching AudioMixerController", $"Patching {_mixerNames[m]}", (float)m / _mixerNames.Length);
                
                var mixerName = _mixerNames[m];
                var diageticPath = Path.Combine(settings.ProjectGameAssetsFullPath, finalFolder, mixerName).ToAssetDatabaseSafePath();
                try {
                    var text = File.ReadAllText(diageticPath);
                    var lines = text.Split('\n');
                    var finalText = new StringBuilder();
                    for (var i = 0; i < lines.Length; i++) {
                        finalText.AppendLine(lines[i].TrimEnd());

                        if (!lines[i].Contains("m_EffectName: Echo")) {
                            continue;
                        }

                        for (var j = i + 1; j < lines.Length; j++) {
                            if (lines[j].Contains("m_Bypass:")) {
                                finalText.AppendLine("  m_Bypass: 1");
                                i = j;
                                break;
                            }

                            finalText.AppendLine(lines[j].TrimEnd());
                        }
                    }
                    File.WriteAllText(diageticPath, finalText.ToString());
                } catch (System.Exception e) {
                    Debug.LogError(e);
                }
                
                EditorUtility.ClearProgressBar();
            }
            
            return UniTask.FromResult(StepResult.Success);
        }
        
        public void OnComplete(bool failed) { }
    }
}