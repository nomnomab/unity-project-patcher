using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nomnom.UnityProjectPatcher.UnityPackages;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// Installs the packages defined in the UPPatcherSettings asset.
    /// <br/><br/>
    /// Will attempt to first install packages normally via the Client api.
    /// If that misses some packages, then this will manually define them
    /// in the manifest file.
    /// <br/><br/>
    /// Recompiles the editor if a package was installed or the manifest was changed.
    /// </summary>
    public readonly struct PackagesInstallerStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var packages = settings.ExactPackagesFound;
            var gitPackages = settings.GitPackages;
            var allPackages = packages
                .Concat(gitPackages.Select(x => new FoundPackageInfo(x.version, null, null, PackageMatchType.Exact)))
                .ToArray();
            // var gitPackages = settings.GitPackages;

            // var packageStrings = packages
            //     .Select(x => x.ToString())
            //     .Concat(gitPackages.Select(x => x.ToString()))
            //     .ToArray();
            
            EditorUtility.DisplayProgressBar("Installing packages", "Checking if packages are already installed", 0.25f);
            try {
                var packageList = Client.List(false, false);
                while (true) {
                    if (packageList.IsCompleted) break;
                    if (packageList.Status == StatusCode.Failure && packageList.Error != null) {
                        EditorUtility.ClearProgressBar();
                        throw new Exception($"Failed to list packages! [{packageList.Error.errorCode}] {packageList.Error.message}");
                    }
                }

                var missingPackages = allPackages
                    .Where(x => packageList.Result.All(y => y.name != x.name))
                    .Select(x => x.ToString())
                    .ToArray();

                if (!missingPackages.Any()) {
                    return UniTask.FromResult(StepResult.Success);
                }
                
                EditorUtility.DisplayDialog("Installing packages", $"Press \"OK\" to install the following packages: {string.Join(", ", missingPackages)}", "OK");
                EditorUtility.DisplayProgressBar("Installing packages", $"Installing {missingPackages.Length} package{(missingPackages.Length == 1 ? string.Empty : "s")}", 0.5f);

#if UNITY_2020_3_OR_NEWER
                var request = Client.AddAndRemove(missingPackages);
                while (true) {
                    if (request.IsCompleted) break;
                    if (request.Status == StatusCode.Failure && request.Error is { } error) {
                        EditorUtility.ClearProgressBar();
                        throw new Exception($"Failed to list packages! [{error.errorCode}] {error.message}");
                    }
                }

                Client.Resolve();
#else
                foreach (var package in missingPackages) {
                    var request = Client.Add(package);
                    while (true) {
                        if (request.IsCompleted) break;
                        if (request.Status == StatusCode.Failure && request.Error != null) {
                            EditorUtility.ClearProgressBar();
                            throw new Exception($"Failed to list packages! [{request.Error.errorCode}] {request.Error.message}");
                        }
                    }
                }
#endif
                
                packageList = Client.List(false, false);
                while (true) {
                    if (packageList.IsCompleted) break;
                    if (packageList.Status == StatusCode.Failure && packageList.Error != null) {
                        EditorUtility.ClearProgressBar();
                        throw new Exception($"Failed to list packages! [{packageList.Error.errorCode}] {packageList.Error.message}");
                    }
                }
                
                missingPackages = allPackages
                    .Where(x => packageList.Result.All(y => y.name != x.name))
                    .Select(x => x.ToString())
                    .ToArray();

                if (!missingPackages.Any()) {
                    EditorUtility.ClearProgressBar();
                    
                    if (packages.Any(x => x.ToString().StartsWith("com.unity.inputsystem"))) {
                        if (new EnableNewInputSystemStep().Assign()) {
                            return UniTask.FromResult(StepResult.RestartEditor);
                        }
                    }
                    
                    return UniTask.FromResult(StepResult.Recompile);
                }
                
                ManuallyResolveManifest();
                EditorUtility.ClearProgressBar();

                if (packages.Any(x => x.ToString().StartsWith("com.unity.inputsystem"))) {
                    if (new EnableNewInputSystemStep().Assign()) {
                        return UniTask.FromResult(StepResult.RestartEditor);
                    }
                }
            } catch {
                Debug.LogError("Failed to install packages.");
                throw;
            }
            finally {
                EditorUtility.ClearProgressBar();
            }
            
            return UniTask.FromResult(StepResult.Recompile);
        }

        private bool ManuallyResolveManifest() {
            var manifestFile = Path.GetFullPath(Path.Combine("Packages", "manifest.json"));
            if (!File.Exists(manifestFile)) {
                Debug.LogError($"Could not find {manifestFile}");
                return false;
            }
            
            var settings = this.GetSettings();
            var packages = settings.ExactPackagesFound;
            var gitPackages = settings.GitPackages;
            var allPackages = packages.Concat(gitPackages.Select(x => new FoundPackageInfo(x.name, x.version, null, PackageMatchType.Exact)));
            
            var manifest = File.ReadAllText(manifestFile);
            var manifestJson = JObject.Parse(manifest);
            var dependencies = (JObject)manifestJson["dependencies"];
            var changed = false;
            
            EditorUtility.DisplayProgressBar("Updating packages", "Updating dependencies", 0.75f);

            try {
                foreach (var package in allPackages) {
                    var name = package.name;
                    var version = package.version;
                
                    EditorUtility.DisplayProgressBar("Updating packages", $"Updating {name}#{version}", 0.75f);
                    Debug.Log($"Updating {name}#{version}");
                
                    if (!dependencies.TryGetValue(name, out var versionObj)) {
                        dependencies[name] = versionObj = new JValue(version);
                        changed = true;
                    }

                    var value = (JValue)versionObj;
                    var valueString = value.ToString(CultureInfo.InvariantCulture);
                    Debug.Log(valueString);
                    if (valueString != version) {
                        value.Value = version;
                        changed = true;
                    }
                }
            } catch {
                Debug.LogError($"Failed to update \"{manifestFile}\"");
                throw;
            } finally {
                EditorUtility.ClearProgressBar();
            }

            if (!changed) {
                Debug.Log($"No changes to {manifestFile}");
                return false;
            }

            var json = manifestJson.ToString(Formatting.Indented);
            File.WriteAllText(manifestFile, json);
            return true;
        }
        
        public void OnComplete(bool failed) { }
    }
}