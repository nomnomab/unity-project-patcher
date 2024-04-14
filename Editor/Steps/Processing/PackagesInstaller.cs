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
    /// </summary>
    public readonly struct PackagesInstaller: IPatcherStep {
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var packages = settings.ExactPackagesFound;
            var gitPackages = settings.GitPackages;

            var packageStrings = packages
                .Select(x => x.ToString())
                .Concat(gitPackages.Select(x => x.ToString()))
                .ToArray();
            
            EditorUtility.DisplayProgressBar("Installing packages", "Checking if packages are already installed", 0.25f);
            try {
                var packageList = Client.List(false, false);
                while (!packageList.IsCompleted) { }

                var missingPackages = packageStrings
                    .Where(x => packageList.Result.All(y => y.name != x))
                    .ToArray();

                if (!missingPackages.Any()) {
                    return UniTask.FromResult(StepResult.Success);
                }

                EditorUtility.DisplayProgressBar("Installing packages", $"Installing {missingPackages.Length} package{(missingPackages.Length == 1 ? string.Empty : "s")}", 0.5f);

                var request = Client.AddAndRemove(missingPackages);
                while (!request.IsCompleted) { }

                Client.Resolve();
                ManuallyResolveManifest();
                
                EditorUtility.ClearProgressBar();
                EditorUtility.RequestScriptReload();
            } catch {
                Debug.LogError("Failed to install packages.");
                throw;
            }
            finally {
                EditorUtility.ClearProgressBar();
            }
            
            return UniTask.FromResult(StepResult.Success);
        }

        private void ManuallyResolveManifest() {
            var manifestFile = Path.GetFullPath(Path.Combine("Packages", "manifest.json"));
            if (!File.Exists(manifestFile)) {
                Debug.LogError($"Could not find {manifestFile}");
                return;
            }
            
            var settings = this.GetSettings();
            var packages = settings.ExactPackagesFound;
            var gitPackages = settings.GitPackages;
            var allPackages = packages.Concat(gitPackages.Select(x => new FoundPackageInfo(x.name, string.Empty, null, PackageMatchType.Exact)));
            
            var manifest = File.ReadAllText(manifestFile);
            var manifestJson = JObject.Parse(manifest);
            var dependencies = (JObject)manifestJson["dependencies"]!;
            var changed = false;
            
            EditorUtility.DisplayProgressBar("Updating packages", "Updating dependencies", 0.75f);

            try {
                foreach (var package in allPackages) {
                    var name = package.name;
                    var version = package.version;
                
                    EditorUtility.DisplayProgressBar("Updating packages", $"Updating {name}#{version}", 0.75f);
                
                    if (!dependencies.TryGetValue(name, out var versionObj)) {
                        dependencies[name] = new JValue(version);
                        changed = true;
                    }

                    var value = (JValue)versionObj!;
                    var valueString = value.ToString(CultureInfo.InvariantCulture);
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
                return;
            }

            var json = manifestJson.ToString(Formatting.Indented);
            File.WriteAllText(manifestFile, json);
        }
    }
}