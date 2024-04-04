using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor {
    public readonly struct AssetRipper: IPatcherStep {
        public async UniTask<StepResult> Run() {
            var assetRipperExePath = ""; // where ar exe is
            var inputPath = ""; // where game is
            var outputPath = ""; // where the files will end up

            // clear the previous output
            if (Directory.Exists(outputPath)) {
                Directory.Delete(outputPath, recursive: true);
            }

            Directory.CreateDirectory(outputPath);

            try {
                await DownloadRequiredDLL();
            } catch (Exception e) {
                Debug.LogException(e);
                return StepResult.Failure;
            }

            try {
                await RunAssetRipper(assetRipperExePath, inputPath, outputPath, "");
            } catch (Exception e) {
                Debug.LogException(e);
                return StepResult.Failure;
            }
            
            return StepResult.Success;
        }

        private async UniTask DownloadRequiredDLL() {
            const string dllUrl = "https://github.com/nomnomab/AssetRipper/releases/download/v1.0.0/AssetRipper.SourceGenerated.dll.zip";

            var dllLocation = "";
            
            if (File.Exists(dllLocation)) {
                return;
            }

            var zipOutput = "";
            using (var client = new System.Net.WebClient()) {
                client.DownloadProgressChanged += (_, args) => {
                    EditorUtility.DisplayProgressBar("Downloading AssetRipper DLL", $"Downloading from {dllUrl}", args.ProgressPercentage / 100f);
                };
                
                await client.DownloadFileTaskAsync(dllUrl, zipOutput);
            }
            
            EditorUtility.ClearProgressBar();
            
            // if the zip doesn't exist, something went wrong
            if (!File.Exists(zipOutput)) {
                throw new FileNotFoundException("Failed to download AssetRipper DLL");
            }
            
            // extract the zip to where we need it
            try {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipOutput, Path.GetDirectoryName(dllLocation));
            } catch (Exception e) {
                Debug.LogError(e);
                throw;
            }

            // clean up the files
            try {
                File.Delete(zipOutput);
            } catch (Exception e) {
                Debug.LogError(e);
                throw;
            }
            
            // if the dll doesn't exist, the zip may have been incomplete?
            if (!File.Exists(dllLocation)) {
                throw new Exception("Failed to extract AssetRipper DLL");
            }
        }
        
        private async UniTask RunAssetRipper(string assetRipperExePath, string inputPath, string outputPath, string settingsPath) {
            Debug.Log($"Running AssetRipper at \"{assetRipperExePath}\" with \"{inputPath}\" and outputting into \"{outputPath}\"");
            Debug.Log($"Using data folder at \"{inputPath}\"");
            Debug.Log($"Outputting ripped assets at \"{outputPath}\"");
            Debug.Log($"Using settings from \"{settingsPath}\"");
            
            var process = new System.Diagnostics.Process {
                StartInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = assetRipperExePath,
                    Arguments = $"\"{settingsPath}\" \"{inputPath}\" \"{outputPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            
            // run the process
            try {
                process.Start();

                var elapsed = 0f;
                while (!process.StandardOutput.EndOfStream) {
                    var line = process.StandardOutput.ReadLine();
                    //? time estimation of three minutes
                    elapsed += Time.deltaTime / (60f * 3);
                    EditorUtility.DisplayProgressBar("Running AssetRipper", line, elapsed);
                }
                
                EditorUtility.ClearProgressBar();
                process.WaitForExit();
                
                // check for any errors
                var errorOutput = process.StandardError.ReadToEnd();
                if (process.ExitCode != 0) {
                    throw new Exception($"AssetRipper failed to run with exit code {process.ExitCode}. Error: {errorOutput}");
                }
            } catch (Exception e) {
                Debug.LogError($"Error running AssetRipper: {e}");
                throw;
            }
        }
    }
}