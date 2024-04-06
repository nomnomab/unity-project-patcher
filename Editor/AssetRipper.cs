using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor {
    public readonly struct AssetRipper: IPatcherStep {
        public async UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var arSettings = this.GetAssetRipperSettings();
            
            // where asset ripper exe is
            var assetRipperExePath = arSettings.AssetRipperPath; 
            // where game is
            var inputPath = settings.GamePath;
            // where the files will end up
            var outputPath = arSettings.AssetRipperOutputPath;

            // clear the previous output
            if (Directory.Exists(outputPath)) {
                Directory.Delete(outputPath, recursive: true);
            }

            Directory.CreateDirectory(outputPath);

            // download asset ripper if we don't already have it
            try {
                await DownloadAssetRipper();
            } catch (Exception e) {
                Debug.LogException(e);
                return StepResult.Failure;
            }

            // run asset ripper to extract assets
            try {
                await RunAssetRipper(assetRipperExePath, inputPath, outputPath, "");
            } catch (Exception e) {
                Debug.LogException(e);
                return StepResult.Failure;
            }
            
            return StepResult.Success;
        }

        private async UniTask DownloadAssetRipper() {
            // const string dllUrl = "https://github.com/nomnomab/AssetRipper/releases/download/v1.0.0/AssetRipper.SourceGenerated.dll.zip";
            const string buildUrl = "";

            var finalPath = "";
            if (Directory.Exists(finalPath)) {
                return;
            }

            var zipOutputPath = "";
            using (var client = new System.Net.WebClient()) {
                client.DownloadProgressChanged += (_, args) => {
                    EditorUtility.DisplayProgressBar("Downloading AssetRipper", $"Downloading from {buildUrl}", args.ProgressPercentage / 100f);
                };
                
                await client.DownloadFileTaskAsync(buildUrl, zipOutputPath);
            }
            
            EditorUtility.ClearProgressBar();
            
            // if the zip doesn't exist, something went wrong
            if (!File.Exists(zipOutputPath)) {
                throw new FileNotFoundException("Failed to download AssetRipper");
            }
            
            // extract the zip to where we need it
            try {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipOutputPath, finalPath);
            } catch (Exception e) {
                Debug.LogError(e);
                throw;
            }

            // clean up the files
            try {
                File.Delete(zipOutputPath);
            } catch (Exception e) {
                Debug.LogError(e);
                throw;
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