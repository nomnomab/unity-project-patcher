using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor {
    public class PatcherWindow: EditorWindow {
        private Type _gameWrapperType;
        private MethodInfo _gameWrapperGuiFunction;
        private string _patcherVersion;
        private string _gameWrapperVersion;
        
        private GUIStyle _leftAlignedGreyLabel;
        private GUIStyle _rightAlignedGreyLabel;

        private bool _hasBepInExPackage;
        private bool _hasBepInExFlag;
        // private bool _hasGameWrapperPackage;
        private UPPatcherAttribute _foundPackageAttribute;
        private PackageCollection _packageCollection;
        private PackageVersion[] _packageVersions;

        [InitializeOnLoadMethod]
        private static void OnLoad() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var window = Resources.FindObjectsOfTypeAll<PatcherWindow>().FirstOrDefault() as PatcherWindow;
            if (!window) {
                GetPackageVersions().ToArray();
            } else {
                window.CheckPackages();
            }
        }
        
        [MenuItem("Tools/Unity Project Patcher/Open Window")]
        private static void Open() {
            var window = GetWindow<PatcherWindow>();
            var gameWrapperType = PatcherUtility.GetGameWrapperType();
            if (gameWrapperType is null) {
                window.titleContent = new GUIContent("UPPatcher");
            } else {
                window.titleContent = new GUIContent($"UPPatcher - {gameWrapperType.Name}");
            }
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable() {
            Nomnom.UnityProjectPatcher.PatcherUtility.GetUserSettings();
            _gameWrapperVersion = null;
            
            EditorApplication.delayCall += () => {
                _packageCollection = null;
                CheckPackages();
                Repaint();
            };
            
            var gameWrapperType = PatcherUtility.GetGameWrapperType();
            if (gameWrapperType is null) return;

            _gameWrapperType = gameWrapperType;
            titleContent = new GUIContent($"UPP - {gameWrapperType.Name}");
            
            var gameWrapperOnGUIFunction = PatcherUtility.GetGameWrapperOnGUIFunction(gameWrapperType);
            if (gameWrapperOnGUIFunction is null) return;
            
            _gameWrapperGuiFunction = gameWrapperOnGUIFunction;
        }

        // private void OnFocus() {
        //     Nomnom.UnityProjectPatcher.PatcherUtility.GetUserSettings();
        //     try {
        //         CheckPackages();
        //     } catch (Exception e) {
        //         Debug.LogWarning(e);
        //     }
        // }

        private static IEnumerable<PackageVersion> GetPackageVersions() {
            // check tool version
            var packages = PatcherUtility.GetPackages();
            var toolGit = "https://github.com/nomnomab/unity-project-patcher";
            var gameWrapper = PatcherUtility.GetGameWrapperAttribute();
            
            var currentToolVersion = packages.FirstOrDefault(x => x.name == "com.nomnom.unity-project-patcher")?.version;
            
            if (!string.IsNullOrEmpty(currentToolVersion) && PatcherUtility.TryFetchGitVersion(toolGit, out var toolVersion)) {
                yield return new PackageVersion("Unity Project Patcher", toolGit, currentToolVersion, toolVersion);
                    
                if (currentToolVersion != toolVersion) {
                    Debug.LogWarning($"[com.nomnom.unity-project-patcher] is <color=yellow>outdated</color>. Please update to {toolVersion} from \"{toolGit}\". Current version: {currentToolVersion}.");
                } else {
                    Debug.Log($"[com.nomnom.unity-project-patcher] is up to date. Current version: {currentToolVersion}.");
                }
            } else {
                Debug.LogWarning($"Failed to fetch [com.nomnom.unity-project-patcher] version from \"{toolGit}\".");
            }

#if UNITY_2020_3_OR_NEWER
            var currentBepInExVersion = packages.FirstOrDefault(x => x.name == "com.nomnom.unity-project-patcher-bepinex")?.version;
            var bepinexGit = "https://github.com/nomnomab/unity-project-patcher-bepinex";
            if (!string.IsNullOrEmpty(currentBepInExVersion) && PatcherUtility.TryFetchGitVersion(bepinexGit, out var bepinexVersion)) {
                yield return new PackageVersion("BepInEx", bepinexGit, currentBepInExVersion, bepinexVersion);
                    
                if (currentBepInExVersion != bepinexVersion) {
                    Debug.LogWarning($"[com.nomnom.unity-project-patcher-bepinex] is <color=yellow>outdated</color>. Please update to {bepinexVersion} from \"{bepinexGit}\". Current version: {currentBepInExVersion}.");
                } else {
                    Debug.Log($"[com.nomnom.unity-project-patcher-bepinex] is up to date. Current version: {currentBepInExVersion}.");
                }
            } else {
                Debug.LogWarning($"Failed to fetch [com.nomnom.unity-project-patcher-bepinex] version from \"{bepinexGit}\".");
            }
#endif
            
            if (gameWrapper != null) {
                var packageName = gameWrapper.PackageName;
                var gamePackage = packages.FirstOrDefault(x => x.name == packageName);
                var gameRepo = gamePackage.packageId;
                if (string.IsNullOrEmpty(gameRepo) || !gameRepo.Contains('@')) {
                    Debug.LogWarning($"[com.nomnom.unity-project-patcher-bepinex] failed to get gamepackage or repository.");
                } else if (gamePackage != null) {
                    var gameGit = gameRepo.Split('@')[1];
                    if (gameGit.EndsWith(".git")) {
                        gameGit = gameGit.Substring(0, gameGit.Length - 4);
                    }
                    var currentGameVersion = gamePackage.version;
                    if (PatcherUtility.TryFetchGitVersion(gameGit, out var gameVersion)) {
                        yield return new PackageVersion(packageName, gameGit, currentGameVersion, gameVersion);
                        if (currentGameVersion != gameVersion) {
                            Debug.LogWarning($"[{gamePackage.name}] is <color=yellow>outdated</color>. Please update to {gameVersion} from \"{gameGit}\". Current version: {currentGameVersion}.");
                        } else {
                            Debug.Log($"[{gamePackage.name}] is up to date. Current version: {currentGameVersion}.");
                        }
                    } else {
                        Debug.LogWarning($"Failed to fetch [{gamePackage.name}] version from \"{gameGit}\".");
                    }
                } else {
                    Debug.LogWarning($"[{gamePackage.name}] failed to get gamepackage or repository.");
                }
            }
        }

        private void CheckPackages() {
            if (_packageCollection is null) {
                _packageCollection = PatcherUtility.GetPackages();
            }

            _packageVersions = GetPackageVersions().ToArray();

            var (version, gameVersion) = PatcherUtility.GetVersions(_packageCollection);
            _patcherVersion = version;
            _gameWrapperVersion = gameVersion;

            // check packages
            _hasBepInExPackage = _packageCollection.Any(x => x.name == "com.nomnom.unity-project-patcher-bepinex");
            _hasBepInExFlag = PatcherUtility.GetScriptingDefineSymbols().Contains("ENABLE_BEPINEX");
            _foundPackageAttribute = PatcherUtility.GetGameWrapperAttribute();
            // _hasGameWrapperPackage = false;
            // if (!string.IsNullOrEmpty(_foundPackageAttribute?.PackageName)) {
            //     _hasGameWrapperPackage = _packageCollection.Any(x => x.name == _foundPackageAttribute.PackageName);
            // }
        }

        private void OnGUI() {
            // header
            if (_leftAlignedGreyLabel == null) {
                _leftAlignedGreyLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleLeft };
            }

            if (_rightAlignedGreyLabel == null) {
                _rightAlignedGreyLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleRight };
            }
            
            var title = _gameWrapperType?.Name ?? null;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Unity Project Patcher v{_patcherVersion ?? "N/A"}", _leftAlignedGreyLabel);
            EditorGUILayout.LabelField($"Found: {title ?? "N/A"} v{_gameWrapperVersion ?? "N/A"}", _rightAlignedGreyLabel);
            EditorGUILayout.EndHorizontal();
            
            if (_gameWrapperType == null) {
                EditorGUILayout.LabelField("A game wrapper was not found.", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            
            if (!PatcherUtility.HasBuildBlocker()) {
                EditorGUILayout.LabelField("No build blocker found. Why did you remove it?", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            
            EditorGUILayout.LabelField("All config assets will be made at the root of your project by default!", EditorStyles.centeredGreyMiniLabel);
            
            var currentStep = StepsExecutor.CurrentStepName;
            GUI.enabled = string.IsNullOrEmpty(currentStep);
            if (!string.IsNullOrEmpty(currentStep)) {
                GUI.enabled = true;
                EditorGUILayout.LabelField($"Current step: \"{currentStep}\"", EditorStyles.centeredGreyMiniLabel);
                GUI.enabled = string.IsNullOrEmpty(currentStep);
            }

            if (EditorApplication.isCompiling) {
                EditorGUILayout.LabelField("Compiling...", EditorStyles.centeredGreyMiniLabel);
                GUI.enabled = false;
            }

            if (GUILayout.Button("Run Patcher")) {
                // if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                //     return;
                // }
                
                EditorSceneManager.SaveOpenScenes();
                
                var userSettings = PatcherUtility.GetUserSettings();

                PatcherUtility.DisplayUsageWarning();
                
                try {
                    if (string.IsNullOrEmpty(userSettings.GameFolderPath) || !Directory.Exists(Path.GetFullPath(userSettings.GameFolderPath))) {
                        EditorUtility.DisplayDialog("Error", "Please select a valid game folder!\n\nPlease fix this in your UPPatcherUserSettings asset!", "Focus UPPatcherUserSettings");
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = userSettings;
                        EditorGUIUtility.PingObject(userSettings);
                        return;
                    }

                    if (string.IsNullOrEmpty(userSettings.AssetRipperDownloadFolderPath)) {
                        EditorUtility.DisplayDialog("Error", "Please select a valid asset ripper download location!\n\nPlease fix this in your UPPatcherUserSettings asset!", "Focus UPPatcherUserSettings");
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = userSettings;
                        EditorGUIUtility.PingObject(userSettings);
                        return;
                    }
                    
                    if (string.IsNullOrEmpty(userSettings.AssetRipperExportFolderPath)) {
                        EditorUtility.DisplayDialog("Error", "Please select a valid asset ripper export location!\n\nPlease fix this in your UPPatcherUserSettings asset!", "Focus UPPatcherUserSettings");
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = userSettings;
                        EditorGUIUtility.PingObject(userSettings);
                        return;
                    }
                } catch {
                    EditorUtility.DisplayDialog("Error", "There is a bad path in the user settings!\n\nPlease fix this in your UPPatcherUserSettings asset!", "Focus UPPatcherUserSettings");
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = userSettings;
                    EditorGUIUtility.PingObject(userSettings);
                    return;
                }
                
                if (PatcherUtility.GetGameWrapperGetStepsFunction(_gameWrapperType) is null) {
                    EditorUtility.DisplayDialog("Error", $"The {_gameWrapperType.Name} does not have a Run function", "Ok");
                    return;
                }

                if (PatcherUtility.IsProbablyPatched()) {
                    if (!EditorUtility.DisplayDialog("Warning", "The project seems to already be patched. Are you sure you want to continue? This may lead to unrecoverable changes.\n\nMake sure you back up your project before doing this!", "Yes", "No")) {
                        return;
                    }
                }

                EditorApplication.delayCall += () => {
                    PatcherSteps.Run();
                };
                
                return;
            }
            
            if (_gameWrapperGuiFunction != null) {
                try {
                    _gameWrapperGuiFunction.Invoke(null, null);
                } catch(Exception e) {
                    Debug.LogError($"Failed to call {_gameWrapperGuiFunction.Name} with error:\n{e}");
                    Close();
                }
            }
            
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical("Box");
            DisplayUpdateButtons();
            
            if (GUILayout.Button("Print Steps to Log")) {
                var pipeline = PatcherSteps.GetPipeline();
                if (pipeline != null) {
                    pipeline.PrintToLog();
                }
            }

            DisplayBepInExButton();
            EditorGUILayout.EndVertical();
        }

        private void DisplayUpdateButtons() {
            var versions = _packageVersions;
            if (versions == null) return;
            if (versions.All(x => x.IsCompatible())) {
                EditorGUILayout.LabelField("All packages are up to date!", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var needsUpdate = versions.Where(x => !x.IsCompatible());
            var needsUpdateCount = needsUpdate.Count();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Check for Updates")) {
                EditorApplication.delayCall += () => {
                    EditorUtility.DisplayProgressBar("Checking...", "Checking packages...", 0.5f);
                    CheckPackages();
                    EditorUtility.ClearProgressBar();
                };
            }
            if (GUILayout.Button($"Update {needsUpdateCount} Package{(needsUpdateCount != 1 ? "s" : "")}")) {
                var urls = versions.Select(x => x.gitUrl);
                if (!EditorUtility.DisplayDialog("Update Packages", "Are you sure you want to update the following packages?\n\n" + string.Join("\n", urls.Select(x => $" - {x}")), "Yes", "No")) {
                    return;
                }
                
                EditorApplication.delayCall += () => {
                    EditorUtility.DisplayProgressBar("Updating...", "Updating packages...", 0.5f);
                    Client.AddAndRemove(urls.ToArray());
                    EditorUtility.ClearProgressBar();
                };
            }
            EditorGUILayout.EndHorizontal();
            
            foreach (var package in needsUpdate) {
                EditorGUILayout.LabelField($"[{package.name}] {package.from} -> {package.to}", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DisplayBepInExButton() {
#if UNITY_2020_3_OR_NEWER
            if (!_hasBepInExPackage) {
                if (GUILayout.Button("Install BepInEx")) {
                    EditorApplication.delayCall += () => {
                        _packageCollection = null;
                        
                        EditorUtility.DisplayProgressBar("Installing...", "Installing BepInEx...", 0.5f);
                        
                        var request = Client.Add("https://github.com/nomnomab/unity-project-patcher-bepinex.git");
                        while (!request.IsCompleted) { }
                        if (request.Status == StatusCode.Success) {
                            EditorUtility.DisplayDialog("Success!", "BepInEx was installed successfully!", "OK");
                        } else {
                            EditorUtility.DisplayDialog("Error", $"Failed to install BepInEx! [{request.Error.errorCode}] {request.Error.message}", "OK");
                        }
                        
                        // enable ENABLE_BEPINEX
                        var existingSymbols = PatcherUtility.GetScriptingDefineSymbols();
                        if (!existingSymbols.Contains("ENABLE_BEPINEX")) {
                            existingSymbols += ";ENABLE_BEPINEX";
                            PatcherUtility.SetScriptingDefineSymbols(existingSymbols);
                        }

                        EditorUtility.ClearProgressBar();
                    };
                }
            } else {
                if (_hasBepInExFlag && GUILayout.Button("Disable BepInEx")) {
                    EditorApplication.delayCall += () => {
                        PatcherUtility.SetScriptingDefineSymbols(PatcherUtility.GetScriptingDefineSymbols().Replace("ENABLE_BEPINEX", ""));
                    };
                } else if (!_hasBepInExFlag && GUILayout.Button("Enable BepInEx")) {
                    EditorApplication.delayCall += () => {
                        PatcherUtility.SetScriptingDefineSymbols(PatcherUtility.GetScriptingDefineSymbols() + ";ENABLE_BEPINEX");
                    };
                }
            }

            if (!_hasBepInExPackage && !(_foundPackageAttribute is null) && _foundPackageAttribute.RequiresBepInEx) {
                EditorGUILayout.LabelField("Please install all packages!", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField($"bepinex: {(_hasBepInExPackage ? "good!" : "missing!")}");
                // EditorGUILayout.LabelField($"{_gameWrapperType.Name}: {(_hasGameWrapperPackage ? "good!" : "missing!")}");
                return;
            }
#else
            EditorGUILayout.LabelField("BepInEx is not supported for older versions of Unity atm", EditorStyles.centeredGreyMiniLabel);
#endif
        }

        private readonly struct PackageVersion {
            public readonly string name;
            public readonly string gitUrl;
            public readonly string from, to;
            
            public bool IsCompatible() {
                return from == to;
            }
            
            public PackageVersion(string name, string gitUrl, string from, string to) {
                this.name = name;
                this.gitUrl = gitUrl;
                this.from = from;
                this.to = to;
            }
        }
    }
}