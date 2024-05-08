using System;
using System.Reflection;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor {
    public class PatcherWindow: EditorWindow {
        private Type _gameWrapperType;
        private MethodInfo _gameWrapperGuiFunction;
        private string _patcherVersion;
        private string _gameWrapperVersion;
        
        private GUIStyle _leftAlignedGreyLabel;
        private GUIStyle _rightAlignedGreyLabel;
        
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
            EditorApplication.delayCall += () => {
                var (version, gameVersion) = PatcherUtility.GetVersions();
                _patcherVersion = version;
                _gameWrapperVersion = gameVersion;
                Repaint();
            };
            _gameWrapperVersion = null;
            
            var gameWrapperType = PatcherUtility.GetGameWrapperType();
            if (gameWrapperType is null) return;

            _gameWrapperType = gameWrapperType;
            titleContent = new GUIContent($"UPP - {gameWrapperType.Name}");
            
            var gameWrapperOnGUIFunction = PatcherUtility.GetGameWrapperOnGUIFunction(gameWrapperType);
            if (gameWrapperOnGUIFunction is null) return;
            
            _gameWrapperGuiFunction = gameWrapperOnGUIFunction;
        }

        private void OnGUI() {
            if (_leftAlignedGreyLabel == null) {
                _leftAlignedGreyLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleLeft };
            }

            if (_rightAlignedGreyLabel == null) {
                _rightAlignedGreyLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleRight };
            }
            
            var title = _gameWrapperType.Name;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Unity Project Patcher v{_patcherVersion ?? "N/A"}", _leftAlignedGreyLabel);
            EditorGUILayout.LabelField($"Found: {title ?? "N/A"} v{_gameWrapperVersion ?? "N/A"}", _rightAlignedGreyLabel);
            EditorGUILayout.EndHorizontal();
            
            if (_gameWrapperType == null) {
                EditorGUILayout.LabelField("A game wrapper was not found.", EditorStyles.centeredGreyMiniLabel);
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
            }

            if (GUILayout.Button("Run Patcher")) {
                var runFunction = PatcherUtility.GetGameWrapperRunFunction(_gameWrapperType);
                if (runFunction is null) {
                    EditorUtility.DisplayDialog("Error", $"The {_gameWrapperType.Name} does not have a Run function", "Ok");
                    return;
                }

                if (PatcherUtility.IsProbablyPatched()) {
                    if (!EditorUtility.DisplayDialog("Warning", "The project seems to already be patched. Are you sure you want to continue? This may lead to unrecoverable changes.\n\nMake sure you back up your project before doing this!", "Yes", "No")) {
                        return;
                    }
                }

                EditorApplication.delayCall += () => {
                    runFunction.Invoke(null, null);
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
        }
    }
}