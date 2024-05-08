using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Lachee.Utilities.Serialization;
using Nomnom.UnityProjectPatcher.AssetRipper;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using UObject = Lachee.Utilities.Serialization.UObject;

namespace Nomnom.UnityProjectPatcher.Editor {
    public static class PatcherUtility {
        public static UPPatcherSettings GetSettings(this IPatcherStep step) {
            return GetSettings();
        }
        
        public static UPPatcherSettings GetSettings() {
            var assets = AssetDatabase.FindAssets($"t:{nameof(UPPatcherSettings)}");
            if (assets.Length == 0) {
                CreateSettings();
                Debug.LogWarning("Created UPPatcherSettings asset since it was missing");
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
            return AssetDatabase.LoadAssetAtPath<UPPatcherSettings>(assetPath);
        }
        
        private static void CreateSettings() {
            // create one at root
            var settings = ScriptableObject.CreateInstance<UPPatcherSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/UPPatcherSettings.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        public static AssetRipperSettings GetAssetRipperSettings(this IPatcherStep step) {
            return GetAssetRipperSettings();
        }
        
        public static AssetRipperSettings GetAssetRipperSettings() {
            var assets = AssetDatabase.FindAssets($"t:{nameof(AssetRipperSettings)}");
            if (assets.Length == 0) {
                CreateAssetRipperSettings();
                Debug.LogWarning("Created AssetRipperSettings asset since it was missing");
            }
            
            var assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
            return AssetDatabase.LoadAssetAtPath<AssetRipperSettings>(assetPath);
        }

        private static void CreateAssetRipperSettings() {
            // create one at root
            var settings = ScriptableObject.CreateInstance<AssetRipperSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/AssetRipperSettings.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static bool ScriptsAreStubs(this IPatcherStep step) {
            return GetAssetRipperSettings().ConfigurationData.Export.scriptExportMode != ScriptExportMode.Decompiled;
        }

        public static Type GetGameWrapperType() {
            foreach (var type in TypeCache.GetTypesWithAttribute<UPPatcherAttribute>()) {
                // does it have a Run function?
                if (GetGameWrapperRunFunction(type) is null) continue;
                return type;
            }
            
            return null;
        }

        public static MethodInfo GetGameWrapperRunFunction(Type wrapperType) {
            var runFunction = wrapperType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (runFunction is null) return null;
            if (runFunction.GetParameters().Length > 0) return null;

            return runFunction;
        }
        
        public static MethodInfo GetGameWrapperOnGUIFunction(Type wrapperType) {
            var guiFunction = wrapperType.GetMethod("OnGUI", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (guiFunction is null) return null;
            if (guiFunction.GetParameters().Length > 0) return null;

            return guiFunction;
        }

        public static (string version, string gameWrapperVersion) GetVersions() {
            EditorUtility.DisplayProgressBar("Fetching...", "Fetching versions...", 0.5f);
            try {
                var list = Client.List();
                while (!list.IsCompleted) { }

                var patcher = list.Result.FirstOrDefault(x => x.name == "com.nomnom.unity-project-patcher");
                (string, string) results = (null, null);
                if (patcher != null) {
                    results.Item1 = patcher.version;
                }

                var gameWrapperType = GetGameWrapperType();
                var assembly = gameWrapperType.Assembly;
                var packageName = assembly.GetName().Name.ToLower();
                if (packageName.EndsWith(".editor")) {
                    packageName = packageName.Replace(".editor", string.Empty);
                }
                patcher = list.Result.FirstOrDefault(x => x.name == packageName);
                if (patcher != null) {
                    results.Item2 = patcher.version;
                }
                
                return results;
            } catch (Exception e) {
                Debug.LogError(e);
                return (null, null);
            }
            finally {
                EditorUtility.ClearProgressBar();
            }
        }

        public static bool IsProbablyPatched() {
            var settings = GetSettings();
            if (!AssetDatabase.IsValidFolder(settings.ProjectGameAssetsPath)) {
                return false;
            }
            
            return AssetDatabase.FindAssets(string.Empty, new string[] {
                settings.ProjectGameAssetsPath
            }).Length > 0;
        }
        
        public static bool TryToCreatePath(string path) {
            if (Directory.Exists(path)) {
                return true;
            }

            try {
                Directory.CreateDirectory(path);
                return true;
            } catch(Exception e) {
                Debug.LogError($"Failed to create path at \"{path}\" with error:\n{e}");
                return false;
            }

            return false;
        }
        
        public static Object GetGraphicsSettings() {
            return AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        }
        
        public static Object GetQualitySettings() {
            return AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/QualitySettings.asset");
        }
        
        public static Object GetProjectSettings() {
            return AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/ProjectSettings.asset");
        }

        public static string GetPathRoot(string path) {
            var separatorChar = Path.DirectorySeparatorChar;
            path = path.Replace('/', separatorChar);
            var indexOfSeparator = path.IndexOf(separatorChar);
            if (indexOfSeparator == -1) {
                return path;
            }
            return path.Substring(0, indexOfSeparator);
        }
        
        public static string GetPathWithoutRoot(string path) {
            var separatorChar = Path.DirectorySeparatorChar;
            path = path.Replace('/', separatorChar);
            var indexOfSeparator = path.IndexOf(separatorChar);
            if (indexOfSeparator == -1) {
                return path;
            }
            return path.Substring(indexOfSeparator + 1);
        }

        public static string GetStartingFolders(string path, int count) {
            var separatorChar = Path.DirectorySeparatorChar;
            path = path.Replace('/', separatorChar);
            var indexOfSeparator = path.IndexOf(separatorChar);
            if (indexOfSeparator == -1) {
                return path;
            }
            
            var split = path.Split(separatorChar);
            var result = string.Join(separatorChar.ToString(), split.Take(count));
            return result;
        }

#if UNITY_2020_3_OR_NEWER
        public static SerializedProperty? GetCustomRenderPipelineProperty() {
#else
        public static SerializedProperty GetCustomRenderPipelineProperty() {
#endif
            var qualitySettings = PatcherUtility.GetQualitySettings();
            var serializedObject = new SerializedObject(qualitySettings);
            
            return serializedObject.FindProperty("m_QualitySettings.Array.data[0].customRenderPipeline");
        }

        private const string ExcludeFromAllPlatformsMetaDummyString = @"
fileFormatVersion: 2
guid: e467066723a20064d8f96fc222107589";
        
        private const string ExcludeFromAllPlatformsMetaString = @"
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      : Any
    second:
      enabled: 0
      settings:
        Exclude Editor: 1
        Exclude Linux64: 1
        Exclude OSXUniversal: 1
        Exclude Win: 1
        Exclude Win64: 1
  - first:
      Any: 
    second:
      enabled: 0
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 0
      settings:
        DefaultValueInitialized: true
  - first:
      Standalone: Linux64
    second:
      enabled: 0
      settings:
        CPU: None
  - first:
      Standalone: OSXUniversal
    second:
      enabled: 0
      settings:
        CPU: None
  - first:
      Standalone: Win
    second:
      enabled: 0
      settings:
        CPU: None
  - first:
      Standalone: Win64
    second:
      enabled: 0
      settings:
        CPU: None
  - first:
      Windows Store Apps: WindowsStoreApps
    second:
      enabled: 0
      settings:
        CPU: AnyCPU
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";
        
        public static void ExcludeDllFromLoading(string path) {
            var metaPath = path + ".meta";
            if (File.Exists(metaPath)) {
                var metaLines = File.ReadAllLines(metaPath);
                // keep the first two lines
                var stringBuilder = new StringBuilder();
                for (var i = 0; i < metaLines.Length; i++) {
                    if (i < 2) {
                        stringBuilder.AppendLine(metaLines[i]);
                    }
                }
                stringBuilder.Append(ExcludeFromAllPlatformsMetaString);
                File.WriteAllText(metaPath, stringBuilder.ToString());
            } else {
                File.WriteAllText(metaPath, $"{ExcludeFromAllPlatformsMetaDummyString}\n{ExcludeFromAllPlatformsMetaString}");
            }
        }

        [MenuItem("CONTEXT/Object/Debug/Guid")]
        [MenuItem("Edit/Test/Debug/Guid")]
        public static void DebugGuid() {
            var selection = Selection.activeObject;
            if (!selection) return;
            
            var path = AssetDatabase.GetAssetPath(selection);
            var guid = AssetDatabase.AssetPathToGUID(path);
            Debug.Log(guid);

            if (string.IsNullOrEmpty(guid)) {
                Debug.Log(GlobalObjectId.GetGlobalObjectIdSlow(selection));
                return;
            }
            
            var instance = AssetDatabase.LoadMainAssetAtPath(path);
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instance, out guid, out long fileId)) {
                Debug.Log($"{guid} {fileId}");
            }
            
            var globalID = GlobalObjectId.GetGlobalObjectIdSlow(instance);
            Debug.Log(globalID);
        }

        [MenuItem("CONTEXT/Object/Debug/Local FileId")]
        [MenuItem("CONTEXT/GameObject/Debug/Local FileId")]
        [MenuItem("Edit/Test/Debug/Local FileId")]
        public static void DebugLocalFileId() {
            var selection = Selection.activeObject;
            if (!selection) return;
            
            var inspectorModeInfo =
                typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
 
            var serializedObject = new SerializedObject(selection);
            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);
 
            var localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!
            var localId = localIdProp.intValue;
            
            Debug.Log(localId);
        }
        
        [MenuItem("CONTEXT/MonoScript/Debug/FullTypeName")]
        [MenuItem("Assets/Debug/FullTypeName")]
        public static void DebugFullTypeName() {
            var selection = Selection.activeObject;
            if (!selection) return;
            if (!(selection is MonoScript)) return;
            
            var monoScript = (MonoScript)selection;
            var assetPath = AssetDatabase.GetAssetPath(monoScript);
            foreach (var entry in AssetScrubber.ScrubNonMonoData(monoScript, assetPath, 0)) {
                Debug.Log(entry); 
            } 
        }
        
        private readonly static Regex FileIdPattern = new Regex(@"fileID:\s(?<fileId>[0-9A-Za-z]+)", RegexOptions.Compiled);
        
        [MenuItem("Assets/Experimental/Re-import from Export")]
        public static void ReimportFromExport() {
            var activeObject = Selection.activeObject;
            var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            var settings = GetSettings();
            var arSettings = GetAssetRipperSettings();
        
            var exportPath = AssetScrubber.GetExportPathFromProjectPath(assetPath, settings, arSettings);
            if (string.IsNullOrEmpty(exportPath)) {
                Debug.LogWarning("Could not find export path");
                return;
            }
            
            if (!File.Exists(exportPath)) {
                Debug.LogWarning("Export file does not exist");
                return;
            }
            
            Debug.Log($"Reimporting {assetPath} from {exportPath}");
            
            var metaFilePath = exportPath + ".meta";
            try {
                File.Copy(exportPath, assetPath, true);

                if (File.Exists(metaFilePath)) {
                    File.Copy(metaFilePath, assetPath + ".meta", true);
                }
            } catch {
                Debug.LogError($"Failed to reimport {assetPath} from {exportPath}");
                throw;
            }
            
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Assets/Experimental/Re-import from Export", true)]
        public static bool ReimportFromExportValidate() {
            if (Selection.count != 1) return false;
            
            var activeObject = Selection.activeObject;
            var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            var settings = GetSettings();
        
            if (!assetPath.StartsWith(settings.ProjectGameAssetsPath)) {
                return false;
            }
        
            var extension = Path.GetExtension(assetPath);
            if (extension == ".unity" || extension == ".prefab" || string.IsNullOrEmpty(extension)) {
                return false;
            }
        
            return true;
        }
    }
}