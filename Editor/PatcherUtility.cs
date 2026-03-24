using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Lachee.Utilities.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nomnom.UnityProjectPatcher.AssetRipper;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using UObject = Lachee.Utilities.Serialization.UObject;

namespace Nomnom.UnityProjectPatcher.Editor {
    public static class PatcherUtility {
        public static bool LockedAssemblies;
        
        public static UPPatcherUserSettings GetUserSettings(this IPatcherStep step) {
            return GetUserSettings();
        }
        
        public static UPPatcherUserSettings GetUserSettings() {
            return Nomnom.UnityProjectPatcher.PatcherUtility.GetUserSettings();
        }
        
        public static UPPatcherSettings GetSettings(this IPatcherStep step) {
            return GetSettings();
        }
        
        public static UPPatcherSettings GetSettings() {
            return Nomnom.UnityProjectPatcher.PatcherUtility.GetSettings();
        }
        
        public static AssetRipperSettings GetAssetRipperSettings(this IPatcherStep step) {
            return GetAssetRipperSettings();
        }
        
        public static AssetRipperSettings GetAssetRipperSettings() {
            return Nomnom.UnityProjectPatcher.PatcherUtility.GetAssetRipperSettings();
        }

        public static bool ScriptsAreStubs(this IPatcherStep step) {
            return GetAssetRipperSettings().ConfigurationData.Export.scriptExportMode != ScriptExportMode.Decompiled;
        }

        public static Type GetGameWrapperType() {
            foreach (var type in TypeCache.GetTypesWithAttribute<UPPatcherAttribute>()) {
                // does it have a Run function?
                if (GetGameWrapperGetStepsFunction(type) is null) continue;
                return type;
            }
            
            return null;
        }
        
        public static UPPatcherAttribute GetGameWrapperAttribute() {
            var type = GetGameWrapperType();
            if (type is null) return null;
            return type.GetCustomAttribute<UPPatcherAttribute>();
        }

        // public static MethodInfo GetGameWrapperRunFunction(Type wrapperType) {
        //     var runFunction = wrapperType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        //     if (runFunction is null) return null;
        //     if (runFunction.GetParameters().Length > 0) return null;
        //
        //     return runFunction;
        // }
        
        public static MethodInfo GetGameWrapperGetStepsFunction(Type wrapperType) {
            var getStepsFunction = wrapperType.GetMethod("GetSteps", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (getStepsFunction is null) return null;

            var parameters = getStepsFunction.GetParameters();
            if (parameters.Length != 1) return null;
            if (parameters[0].ParameterType != typeof(StepPipeline)) {
                return null;
            }

            return getStepsFunction;
        }
        
        public static MethodInfo GetGameWrapperOnGUIFunction(Type wrapperType) {
            var guiFunction = wrapperType.GetMethod("OnGUI", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (guiFunction is null) return null;
            if (guiFunction.GetParameters().Length > 0) return null;

            return guiFunction;
        }

        public static PackageCollection GetPackages() {
            try {
                var list = Client.List();
                while (!list.IsCompleted) { }
                return list.Result;
            } catch (Exception e) {
                Debug.LogError(e);
                return null;
            }
        }

        public static (string version, string gameWrapperVersion) GetVersions(PackageCollection packages) {
            EditorUtility.DisplayProgressBar("Fetching...", "Fetching versions...", 0.5f);
            try {
                var patcher = packages.FirstOrDefault(x => x.name == "com.nomnom.unity-project-patcher");
                (string, string) results = (null, null);
                if (patcher != null) {
                    results.Item1 = patcher.version;
                }

                var gameWrapperType = GetGameWrapperType();
                var gameWrapperAttribute = GetGameWrapperAttribute();
                if (gameWrapperType is null || gameWrapperAttribute is null) {
                    results.Item2 = null;
                    return results;
                }
                // var assembly = gameWrapperType.Assembly;
                // var packageName = assembly.GetName().Name.ToLower();
                // if (packageName.EndsWith(".editor")) {
                //     packageName = packageName.Replace(".editor", string.Empty);
                // }
                patcher = packages.FirstOrDefault(x => x.name == gameWrapperAttribute.PackageName);
                if (patcher != null) {
                    results.Item2 = patcher.version;
                }
                
                return results;
            } catch (Exception e) {
                Debug.LogWarning(e);
                return (null, null);
            }
            finally {
                EditorUtility.ClearProgressBar();
            }
        }

        public static string GetScriptingDefineSymbols() {
#if UNITY_2020_3_OR_NEWER
            return PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
#else
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
#endif
        }
        
        public static void SetScriptingDefineSymbols(string symbols) {
#if UNITY_2020_3_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbols);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
#endif
        }

        public static bool TryFetchGitVersion(string gitUrl, out string version) {
            try {
                // https://raw.githubusercontent.com/nomnomab/unity-lc-project-patcher/master/package.json
                var packageUrl = $"{gitUrl.Replace("github.com", "raw.githubusercontent.com")}/master/package.json";
                var request = UnityWebRequest.Get(packageUrl);
                var r = request.SendWebRequest();
                while (!r.isDone) { }
                
#if UNITY_2020_3_OR_NEWER
                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError) {
                    version = null;
                    return false;
                }
#else
                if (request.isHttpError || request.isNetworkError) {
                    version = null;
                    return false;
                }
#endif
            
                var json = request.downloadHandler.text;
                var packageContents = JObject.Parse(json);
                version = packageContents["version"].Value<string>();
                return true;
            } catch (Exception e) {
                Debug.LogWarning($"Failed to fetch version from \"{gitUrl}\". Exception: {e}");
                version = null;
                return false;
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

            // return false;
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

        public static IEnumerable<Type> GetValidTypes(this Assembly assembly) {
            Type[] types;
            try {
                types = assembly.GetTypes();
            } catch (ReflectionTypeLoadException e) {
                types = e.Types;
            }

            return types.Where(t => t != null);
        }

        internal static bool HasBuildBlocker() {
            return typeof(UPPatcherSettings).Assembly.GetType("Nomnom.UnityProjectPatcher.BuildBlocker") != null;
        }

        public static void DisplayUsageWarning() {
            EditorUtility.DisplayDialog("Use at your own risk!", @"
Resources, binary code, and source code might be protected by copyright and trademark laws. Before using this software make sure that decompilation
is not prohibited by the applicable license agreement, permitted under applicable law, or you obtained explicit permission from the copyright owner.

The authors and copyright holders of this software do neither encourage, nor condone, the use of this software, and disclaim any liability for use
of the software in violation of applicable laws.", "OK");
        }

        public static IEnumerable<Type> GetParentTypes(this Type type) {
            // is there any base type?
            if (type == null) {
                yield break;
            }

            yield return type;

            // return all implemented or inherited interfaces
            foreach (var i in type.GetInterfaces()) {
                yield return i;
            }

            // return all inherited types
            var currentBaseType = type.BaseType;
            while (currentBaseType != null) {
                yield return currentBaseType;
                currentBaseType = currentBaseType.BaseType;
            }
        }
        
        public static SerializedProperty GetCustomRenderPipelineProperty() {
            var qualitySettings = PatcherUtility.GetQualitySettings();
            var serializedObject = new SerializedObject(qualitySettings);
            
            return serializedObject.FindProperty("m_QualitySettings.Array.data[0].customRenderPipeline");
        }

        public static string ToValidPath(this string path) {
#if UNITY_EDITOR_WIN
            if (path.StartsWith(@"\\?\")) {
                return path;
            }
            if (path.Length >= 255) {
                path = @"\\?\" + path;
            }
            return path;
#else
            return path;
#endif
        }

        public static string ReadAllText(string path) {
            path = ToValidPath(path);
            return File.ReadAllText(path);
        }

        public static void WriteAllText(string path, string contents) {
            path = ToValidPath(path);
            File.WriteAllText(path, contents);
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

        [MenuItem("CONTEXT/Object/Debug/Assembly Type Name")]
        [MenuItem("Edit/Test/Debug/Assembly Type Name")]
        public static void DebugAssemblyTypeName() {
            var selection = Selection.activeObject;
            if (!selection) return;
            
            if (selection is MonoScript m) {
                Debug.Log(m.GetClass().AssemblyQualifiedName);
            } else {
                Debug.Log(selection.GetType().AssemblyQualifiedName);
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
            
            var instances = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var i in instances) {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(i, out guid, out long fileId)) {
                    if (i is MonoScript s) {
                        Debug.Log($"[{i.GetType().Name}] \"{s.GetClass()}\" -> {guid} {fileId}");
                        // get name from guid and fileid
                        
                    } else {
                        Debug.Log($"[{i.GetType().Name}] \"{i}\" -> {guid} {fileId}");
                    }
                }
            
                var globalID = GlobalObjectId.GetGlobalObjectIdSlow(i);
                Debug.Log(globalID);
            }
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
            var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject).ToOSPath();
            var settings = GetSettings();
            var arSettings = GetAssetRipperSettings();
        
            var exportPath = AssetScrubber.GetExportPathFromProjectPath(assetPath, settings, arSettings).ToOSPath();
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
#if UNITY_2020_3_OR_NEWER
            if (Selection.count != 1) return false;
#else
            if (Selection.objects.Length != 1) return false;
#endif
            
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

        public static void StartProfiler() {
            Profiler.logFile = "scrub_project_profile";
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
            Profiler.enableAllocationCallstacks = true;
            // Profiler.maxUsedMemory = 512 * 1024 * 1024;
        }
        
        public static void StopProfiler() {
            Profiler.enabled = false;
            Profiler.logFile = "";
            Profiler.enableBinaryLog = false;
            Profiler.enableAllocationCallstacks = false;
        }

        public static Task ForEachAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, Task<TResult>> taskSelector, Action<TSource, TResult> resultProcessor) {
            var oneAtATime = new SemaphoreSlim(initialCount: 1, maxCount: 1);
            return Task.WhenAll(
                from item in source
                select ProcessAsync(item, taskSelector, resultProcessor, oneAtATime));
        }

        private static async Task ProcessAsync<TSource, TResult>(
            TSource item,
            Func<TSource, Task<TResult>> taskSelector, Action<TSource, TResult> resultProcessor,
            SemaphoreSlim oneAtATime) {
            TResult result = await taskSelector(item);
            await oneAtATime.WaitAsync();
            try { resultProcessor(item, result); }
            finally { oneAtATime.Release(); }
        }

        public static bool HasDomainReloadingDisabled() {
            if (EditorSettings.enterPlayModeOptionsEnabled &&
                EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload)) {
                Debug.LogWarning("Domain reloading is disabled!");
                return true;
            }
            
            return false;
        }
        
        [MenuItem("Tools/Unity Project Patcher/Configs/" + nameof(UPPatcherUserSettings))]
        private static void OpenUPPatcherUserSettings() {
            var config = GetUserSettings();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
        
// #if UNITY_EDITOR_WIN
//         [DllImport("user32.dll")]
//         public static extern bool SetForegroundWindow(IntPtr hWnd);
// #endif
//
//         public static void FocusUnity() {
//             // todo: support linux
// #if UNITY_EDITOR_WIN
//             var process = Process.GetCurrentProcess();
//             if (process != null) {
//                 // process.WaitForInputIdle();
//                 var ptr = process.MainWindowHandle;
//                 SetForegroundWindow(ptr);
//                 Debug.Log($"Set foreground window: {ptr}");
//             }
// #endif
//         }
    }
}

#if !UNITY_2020_3_OR_NEWER
public static class OldUnityExtensions {
    public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) {
        if (dictionary.ContainsKey(key)) return false;
        dictionary.Add(key, value);
        return true;
    }
        
    public static bool TryPop<T>(this Stack<T> stack, out T value) {
        if (stack.Count == 0) {
            value = default;
            return false;
        }
            
        value = stack.Pop();
        return true;
    }
        
    public static string[] Split(this string value, char separator, int count) {
        return value.Split(new char[] { separator }, count);
    }
}
#endif