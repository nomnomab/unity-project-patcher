using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.AssetRipper;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor {
    public readonly struct AssetRipper: IPatcherStep {
        public async UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var arSettings = this.GetAssetRipperSettings();

            // where asset ripper exe is
            var assetRipperExePath = arSettings.ExePath;
            // where game is
            var inputPath = settings.GameFolderPath;
            // where the files will end up
            var outputPath = arSettings.OutputFolderPath;
            // where the config is to determine what ar will do
            var configPath = arSettings.ConfigPath;

            arSettings.SaveToConfig();

            // clear the previous output
            if (Directory.Exists(outputPath)) {
                Directory.Delete(outputPath, recursive: true);
            }

            Directory.CreateDirectory(outputPath);

            EditorUtility.DisplayProgressBar("Checking AssetRipper Dependencies", "", 0);
            await UniTask.Yield();

            // download asset ripper if we don't already have it
            try {
                await DownloadAssetRipper(arSettings);
            } catch (Exception e) {
                Debug.LogException(e);
                return StepResult.Failure;
            }
            finally {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayProgressBar("Running AssetRipper", "", 0);
            await UniTask.Yield();

            // run asset ripper to extract assets
            try {
                await RunAssetRipper(assetRipperExePath, inputPath, outputPath, configPath);
            } catch (Exception e) {
                Debug.LogException(e);
                return StepResult.Failure;
            }
            finally {
                EditorUtility.ClearProgressBar();
            }

            return StepResult.Success;
        }

        private async UniTask DownloadAssetRipper(AssetRipperSettings arSettings) {
            // const string dllUrl = "https://github.com/nomnomab/AssetRipper/releases/download/v1.0.0/AssetRipper.SourceGenerated.dll.zip";
            const string buildUrl = @"";

            var finalPath = arSettings.FolderPath;
            var exePath = arSettings.ExePath;
            if (Directory.Exists(finalPath) && File.Exists(exePath)) {
                return;
            }

            EditorUtility.DisplayProgressBar("Downloading AssetRipper", $"Downloading from {buildUrl}", 0);
            
            var zipOutputPath = Path.Combine(Application.dataPath, "..", "AssetRipper.temp.zip");
            using (var client = new System.Net.WebClient()) {
                client.DownloadProgressChanged += (_, args) => {
                    EditorUtility.DisplayProgressBar("Downloading AssetRipper", $"Downloading from {buildUrl}", args.ProgressPercentage / 100f);
                };
                
                EditorUtility.DisplayProgressBar("Downloading AssetRipper", $"Downloading from {buildUrl}", 0);
                await client.DownloadFileTaskAsync(buildUrl, zipOutputPath);
            }
            
            EditorUtility.ClearProgressBar();
            
            // if the zip doesn't exist, something went wrong
            if (!File.Exists(zipOutputPath)) {
                throw new FileNotFoundException("Failed to download AssetRipper");
            }
            
            if (!Directory.Exists(finalPath)) {
                Directory.CreateDirectory(finalPath);
            }
            
            // extract the zip to where we need it
            try {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipOutputPath, finalPath);
            } catch (Exception e) {
                Debug.LogError($"Failed to extract \"{zipOutputPath}\" to \"{finalPath}\"");
                throw;
            }
            finally {
                // clean up the files
                try {
                    File.Delete(zipOutputPath);
                } catch (Exception e) {
                    Debug.LogError($"Failed to delete \"{zipOutputPath}\"");
                    throw;
                }
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
                    Arguments = $"\"{settingsPath}\" \"{outputPath}\" \"{inputPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            
            // run the process
            try {
                EditorUtility.DisplayCancelableProgressBar("Running AssetRipper", "", 0);
                process.Start();

                var exportCount = 0;
                var totalExports = 5624f;
                while (!process.StandardOutput.EndOfStream) {
                    var line = process.StandardOutput.ReadLine();
                    Debug.Log($"[AssetRipper] <i>{line}</i>");
                    
                    //? time estimation of three minutes
                    if (line.Contains("Exporting", StringComparison.OrdinalIgnoreCase)) {
                        exportCount++;
                    }
                    if (EditorUtility.DisplayCancelableProgressBar("Running AssetRipper", line, exportCount / totalExports)) {
                        process.Kill();
                        Debug.LogWarning("AssetRipper manually cancelled!");
                        break;
                    }
                }
                
                EditorUtility.ClearProgressBar();
                process.WaitForExit();
                
                // check for any errors
                var errorOutput = process.StandardError.ReadToEnd();
                if (process.ExitCode != 0) {
                    throw new Exception($"AssetRipper failed to run with exit code {process.ExitCode}. Error: {errorOutput}");
                }
            } catch (Exception e) {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"Error running AssetRipper: {e}");
                throw;
            }
        }
    }
}