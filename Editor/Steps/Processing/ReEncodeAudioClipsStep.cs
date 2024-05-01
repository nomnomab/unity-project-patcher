using System;
using System.Diagnostics;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct ReEncodeAudioClipsStep: IPatcherStep {
        [MenuItem("Tools/UPP/Test Re-encode audio clips")]
        public static void Test() { 
            new ReEncodeAudioClipsStep().Run().Forget();
        }
        
        public UniTask<StepResult> Run() {
            var arSettings = PatcherUtility.GetAssetRipperSettings();
            
            // todo: add linux support
            var ffmpegExe = Path.GetFullPath("Packages/com.nomnom.unity-project-patcher/Libs/ffmpeg~/ffmpeg.exe");
            if (!File.Exists(ffmpegExe)) {
                throw new Exception($"ffmpeg.exe not found at \"{ffmpegExe}\"");
            }
            
            var audioClips = Directory.GetFiles(arSettings.OutputExportAssetsFolderPath, "*.wav", SearchOption.AllDirectories);

            for (var i = 0; i < audioClips.Length; i++) {
                var audioClipPath = audioClips[i];

                if (EditorUtility.DisplayCancelableProgressBar("Re-encoding audio clips", $"Re-encoding {audioClipPath}", i / (float)audioClips.Length)) {
                    throw new OperationCanceledException();
                }
                
                var tmpFileName = $"{Path.GetFileNameWithoutExtension(audioClipPath)}_temp.wav";
                var tmpPath = Path.Combine(Path.GetDirectoryName(audioClipPath)!, tmpFileName);

                var task = Process.Start(new ProcessStartInfo {
                    FileName = ffmpegExe,
                    Arguments = $"-i \"{audioClipPath}\" \"{tmpPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                task!.WaitForExit();
                if (task.ExitCode != 0) {
                    throw new Exception($"ffmpeg exited with code {task.ExitCode}");
                }

                File.Delete(audioClipPath);
                File.Move(tmpPath, audioClipPath);

                Debug.Log($"Re-encoded \"{audioClipPath}\"");
            }
            
            EditorUtility.ClearProgressBar();

            return UniTask.FromResult(StepResult.Success);
        }
    }
}