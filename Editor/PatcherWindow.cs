using System;
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

        [InitializeOnLoadMethod]
        private static void OnLoad() {
            // check tool version
            var packages = PatcherUtility.GetPackages();
            var toolGit = "https://github.com/nomnomab/unity-project-patcher";
            var bepinexGit = "https://github.com/nomnomab/unity-project-patcher-bepinex";
            var gameWrapper = PatcherUtility.GetGameWrapperAttribute();
            
            var currentToolVersion = packages.FirstOrDefault(x => x.name == "com.nomnom.unity-project-patcher")?.version;
            var currentBepInExVersion = packages.FirstOrDefault(x => x.name == "com.nomnom.unity-project-patcher-bepinex")?.version;

            try {
                if (!string.IsNullOrEmpty(currentToolVersion) && PatcherUtility.TryFetchGitVersion(toolGit, out var toolVersion)) {
                    if (currentToolVersion != toolVersion) {
                        Debug.LogWarning($"[com.nomnom.unity-project-patcher] is <color=yellow>outdated</color>. Please update to {toolVersion} from \"{toolGit}\". Current version: {currentToolVersion}.");
                    } else {
                        Debug.Log($"[com.nomnom.unity-project-patcher] is up to date. Current version: {currentToolVersion}.");
                    }
                } else {
                    Debug.LogWarning($"Failed to fetch [com.nomnom.unity-project-patcher] version from \"{toolGit}\".");
                }
            } catch (Exception e) {
                Debug.LogWarning($"Failed to fetch [com.nomnom.unity-project-patcher] version from \"{toolGit}\". Exception: {e}");
            }

            try {
                if (!string.IsNullOrEmpty(currentBepInExVersion) && PatcherUtility.TryFetchGitVersion(bepinexGit, out var bepinexVersion)) {
                    if (currentBepInExVersion != bepinexVersion) {
                        Debug.LogWarning($"[com.nomnom.unity-project-patcher-bepinex] is <color=yellow>outdated</color>. Please update to {bepinexVersion} from \"{bepinexGit}\". Current version: {currentBepInExVersion}.");
                    } else {
                        Debug.Log($"[com.nomnom.unity-project-patcher-bepinex] is up to date. Current version: {currentBepInExVersion}.");
                    }
                } else {
                    Debug.LogWarning($"Failed to fetch [com.nomnom.unity-project-patcher-bepinex] version from \"{bepinexGit}\".");
                }
            } catch (Exception e) {
                Debug.LogWarning($"Failed to fetch [com.nomnom.unity-project-patcher-bepinex] version from \"{bepinexGit}\". Exception: {e}");
            }
            
            if (gameWrapper != null) {
                try {
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
                } catch (Exception e) {
                    Debug.LogWarning($"Failed to fetch [{gameWrapper.PackageName}] version. Exception: {e}");
                }
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
            window.minSize = new Vector2(500, 100);
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

        private void OnFocus() {
            Nomnom.UnityProjectPatcher.PatcherUtility.GetUserSettings();
            CheckPackages();
        }

        private void CheckPackages() {
            _packageCollection ??= PatcherUtility.GetPackages();
            
            var (version, gameVersion) = PatcherUtility.GetVersions(_packageCollection);
            _patcherVersion = version;
            _gameWrapperVersion = gameVersion;

            // check packages
            _hasBepInExPackage = _packageCollection.Any(x => x.name == "com.nomnom.unity-project-patcher-bepinex");
            _hasBepInExFlag = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone).Contains("ENABLE_BEPINEX");
            _foundPackageAttribute = PatcherUtility.GetGameWrapperAttribute();
            // _hasGameWrapperPackage = false;
            // if (!string.IsNullOrEmpty(_foundPackageAttribute?.PackageName)) {
            //     _hasGameWrapperPackage = _packageCollection.Any(x => x.name == _foundPackageAttribute.PackageName);
            // }
        }

        private void OnGUI() {
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
                        var existingSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
                        if (!existingSymbols.Contains("ENABLE_BEPINEX")) {
                            existingSymbols += ";ENABLE_BEPINEX";
                            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, existingSymbols);
                        }

                        EditorUtility.ClearProgressBar();
                    };
                }
            } else {
                if (_hasBepInExFlag && GUILayout.Button("Disable BepInEx")) {
                    EditorApplication.delayCall += () => {
                        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone).Replace("ENABLE_BEPINEX", ""));
                    };
                } else if (!_hasBepInExFlag && GUILayout.Button("Enable BepInEx")) {
                    EditorApplication.delayCall += () => {
                        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone) + ";ENABLE_BEPINEX");
                    };
                }
            }

            if (!_hasBepInExPackage && _foundPackageAttribute is not null && _foundPackageAttribute.RequiresBepInEx) {
                EditorGUILayout.LabelField("Please install all packages!", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField($"bepinex: {(_hasBepInExPackage ? "good!" : "missing!")}");
                // EditorGUILayout.LabelField($"{_gameWrapperType.Name}: {(_hasGameWrapperPackage ? "good!" : "missing!")}");
                return;
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

            if (GUILayout.Button("Print Steps to Log")) {
                var pipeline = PatcherSteps.GetPipeline();
                if (pipeline != null) {
                    pipeline.PrintToLog();
                }
            }
            
            if (_gameWrapperGuiFunction != null) {
                try {
                    _gameWrapperGuiFunction.Invoke(null, null);
                } catch(Exception e) {
                    Debug.LogError($"Failed to call {_gameWrapperGuiFunction.Name} with error:\n{e}");
                    Close();
                }
            }
            
            EditorGUILayout.LabelField("All config assets will be made at the root of your project by default!", EditorStyles.centeredGreyMiniLabel);
        }
    }
}