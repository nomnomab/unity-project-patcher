#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher {
    internal class BuildBlocker : IPreprocessBuildWithReport {
        public int callbackOrder => -9999;

        public void OnPreprocessBuild(BuildReport report) {
            UnityEditor.EditorUtility.DisplayDialog("No", "This tool is not provided so you can build the project.", "OK");
            Debug.LogError("This tool is not provided so you can build the project.");
            throw new BuildPlayerWindow.BuildMethodException("This tool is not provided so you can build the project.");
            throw new BuildFailedException("This tool is not provided so you can build the project.");
        }
    }
}
#endif