using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.AssetRipper;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// The core to run AssetRipper through.
    /// <br/><br/>
    /// Automatically handles downloading and running AssetRipper.
    /// </summary>
    public readonly struct AssetRipperStep: IPatcherStep {
        [MenuItem("Tools/UPP/Run AssetRipper")]
        public static void Foo() {
            var step = new AssetRipperStep();
            step.Run().Forget();
        }
        
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
            
            if (assetRipperExePath is null) {
                Debug.LogError("AssetRipper exe path was null");
                return StepResult.Failure;
            }
            
            if (inputPath is null) {
                Debug.LogError("Input path was null");
                return StepResult.Failure;
            }
            
            if (outputPath is null) {
                Debug.LogError("Output path was null");
                return StepResult.Failure;
            }
            
            if (configPath is null) {
                Debug.LogError("Config path was null");
                return StepResult.Failure;
            }

            arSettings.SaveToConfig();

            EditorUtility.DisplayProgressBar("Checking AssetRipper Dependencies", "", 0);
            await UniTask.Yield();
            
            if (arSettings.ConfigurationData.Processing.enablePrefabOutlining) {
                if (!EditorUtility.DisplayDialog("Enable Prefab Outlining", "Are you sure you want to enable prefab outlining?\n\nThis is a highly unstable feature of AssetRipper, so this may not work as expected, such as creating prefabs with infinite loading loops when importing.", "Yes", "No")) {
                    return StepResult.Failure;
                }
            }

            if (!Directory.Exists(outputPath)) {
                Directory.CreateDirectory(outputPath);
            }

            if (arSettings.NeedsManualRip) {
                // wait for the user to run asset ripper manually and then prompt this to continue
                // todo: make a window for it
                //! for now I'll rip it manually prior
                Debug.LogWarning("AssetRipper required manual rip. Please run it manually and then press \"Continue\" to continue.");
                
                // open export folder
                var tmpFile = Path.Combine(outputPath, "README.txt");
                if (File.Exists(tmpFile)) {
                    File.Delete(tmpFile);
                }
                
                using (var file = File.CreateText(tmpFile)) {
                    file.WriteLine("AssetRipper requires a manual rip to handle static mesh separation.");
                    
                    // if (arSettings.ConfigurationData.Processing.enableAssetDeduplication) {
                    //     file.WriteLine();
                    //     file.WriteLine(" - Asset deduplication is enabled, so make sure you enable that in AssetRipper.");
                    // }
                
                    if (arSettings.ConfigurationData.Processing.enableStaticMeshSeparation) {
                        file.WriteLine(" - Static mesh separation is enabled, so make sure you enable that in AssetRipper.");
                    }
                    
                    file.WriteLine();
                    file.WriteLine("Grab version 1.0.10 from here: https://github.com/AssetRipper/AssetRipper/releases/tag/1.0.10");
                    file.WriteLine(" - It's the only version with status mesh separation available for free");
                    file.WriteLine();
                    file.WriteLine("Otherwise, you have to purchase their \"premium\" version and use that instead :/");
                    file.WriteLine();
                    file.WriteLine("Simply run AssetRipper with the wanted settings, then copy the contents into this folder.");
                    file.WriteLine("The contents should be the insides of the folder that AssetRipper exports into. Such as:");
                    file.WriteLine(" - Root");
                    file.WriteLine("   - AuxiliaryFiles     <- Copy these");
                    file.WriteLine("   - ExportedFiles      <- Copy these");
                    file.WriteLine("   - [Any Other Folder] <- Copy these");
                }
                EditorUtility.RevealInFinder(tmpFile);

                var text = $"AssetRipper requires a manual rip to handle asset static mesh separation.\n\nCheck the README.txt for more information at \"{outputPath}\"\n\nPlease run it manually and then press \"Continue\" to continue.\n\n";
                if (!EditorUtility.DisplayDialog("Continue when you patch!", text, "Continue", "Abort")) {
                    return StepResult.Failure;
                }
                
                clearGameFolder();
            } else {
                clearExportFolder();
                clearGameFolder();
                
                Directory.CreateDirectory(outputPath);
                
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
            }

            try {
                SanitizeFolders(arSettings.OutputExportAssetsFolderPath);
            } catch {
                Debug.LogError("Failed to sanitize asset ripper export folders!");
                throw;
            }

            return StepResult.Success;

            void clearExportFolder() {
                // clear the previous output
                if (Directory.Exists(outputPath)) {
                    Directory.Delete(outputPath, recursive: true);
                }
            }
            
            void clearGameFolder() {
                var gameFolderPath = settings.ProjectGameAssetsPath;
                if (Directory.Exists(gameFolderPath)) {
                    try {
                        Directory.Delete(gameFolderPath, true);
                    } catch {
                        Debug.LogError($"Failed to delete \"{gameFolderPath}\"");
                        throw;
                    }
                }
            }
        }

        private async UniTask DownloadAssetRipper(AssetRipperSettings arSettings) {
            // const string dllUrl = "https://github.com/nomnomab/AssetRipper/releases/download/v1.0.0/AssetRipper.SourceGenerated.dll.zip";
            const string buildUrl = @"file:\\C:\Users\nomno\Documents\Github\AssetRipper\Source\0Bins\AssetRipper.Tools.SystemTester\Release\Release.zip";

            var finalPath = arSettings.FolderPath;
            var exePath = arSettings.ExePath;

            if (finalPath is null) {
                Debug.LogError("Final path was null");
                return;
            }
            
            if (exePath is null) {
                Debug.LogError("Exe path was null");
                return;
            }
            
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
                Debug.LogError($"Failed to extract \"{zipOutputPath}\" to \"{finalPath}\":\n{e}");
                throw;
            }
            finally {
                // clean up the files
                try {
                    File.Delete(zipOutputPath);
                } catch (Exception e) {
                    Debug.LogError($"Failed to delete \"{zipOutputPath}\":\n{e}");
                    throw;
                }
            }
        }
        
        private UniTask RunAssetRipper(string assetRipperExePath, string inputPath, string outputPath, string settingsPath) {
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
                    if (line is null) continue;
                    
                    // Debug.Log($"[AssetRipper] <i>{line}</i>");
                    
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
            
            return UniTask.CompletedTask;
        }

        private void SanitizeFolders(string assetsRoot) {
            var folders = Directory.GetDirectories(assetsRoot, "*", SearchOption.TopDirectoryOnly);
            foreach (var folder in folders) {
                var folderName = Path.GetFileName(folder);
                if (folderName.Contains('.') || folderName.Contains(' ')) {
                    var newFolderName = folderName.Replace(".", string.Empty).Replace(' ', '_');
                    var folderRoot = Path.GetDirectoryName(folder);
                    Directory.Move(folder, Path.Combine(folderRoot, newFolderName));
                }
            }
        }
    }
}