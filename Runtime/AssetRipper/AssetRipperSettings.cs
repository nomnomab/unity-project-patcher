using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_2020_3_OR_NEWER
using EditorAttributes;
#endif

namespace Nomnom.UnityProjectPatcher.AssetRipper {
    [CreateAssetMenu(fileName = "AssetRipperSettings", menuName = "Unity Project Patcher/AssetRipper Settings")]
    public class AssetRipperSettings : ScriptableObject {
#if UNITY_EDITOR
        public string FolderPath => PatcherUtility.GetUserSettings().AssetRipperDownloadFolderPath;
        public string ExePath => Path.Combine(FolderPath, "AssetRipper.Tools.SystemTester.exe");
        public string OutputFolderPath => PatcherUtility.GetUserSettings().AssetRipperExportFolderPath;
        public string ConfigPath => Path.Combine(FolderPath, "AssetRipper.Settings.json");
        public string OutputExportFolderPath => Path.Combine(OutputFolderPath, "ExportedProject");
        public string OutputExportAssetsFolderPath => Path.Combine(OutputExportFolderPath, "Assets");
#endif

        public IReadOnlyCollection<FolderMapping> FolderMappings => _folderMappings;
        public AssetRipperJsonData ConfigurationData => _configurationData;
        public IReadOnlyList<string> FoldersToCopy => _foldersToCopy.Select(x => x.ToOSPath()).ToList();
        public IReadOnlyList<string> FilesToExcludeFromCopy => _filesToExcludeFromCopy.Select(x => x.ToOSPath()).ToList();
        public IReadOnlyList<string> FoldersToExcludeFromRead => _foldersToExcludeFromRead.Select(x => x.ToOSPath()).ToList();
        public IReadOnlyList<string> ProjectSettingFilesToCopy => _projectSettingFilesToCopy.Select(x => x.ToOSPath()).ToList();
        
        const string defaultBuildUrl = "https://github.com/nomnomab/AssetRipper/releases/download/v1.0.12-patcher/Release.zip";
        public string BuildUrl => string.IsNullOrWhiteSpace(_customBuildUrl) ? defaultBuildUrl : _customBuildUrl;
        
        // public bool NeedsManualRip => _configurationData.Processing.enableStaticMeshSeparation;
        // public bool NeedsManualRip => _configurationData.Processing.enableStaticMeshSeparation || _configurationData.Processing.enableStaticMeshSeparation;
        public bool NeedsManualRip => false;

        [SerializeField]
        private FolderMapping[] _folderMappings = new[] {
            new FolderMapping(DefaultFolderMapping.AnimationClipKey, DefaultFolderMapping.AnimationClipKey, DefaultFolderMapping.AnimationClipOutput),
            new FolderMapping(DefaultFolderMapping.AnimatorControllerKey, DefaultFolderMapping.AnimatorControllerKey, DefaultFolderMapping.AnimatorControllerOutput),
            new FolderMapping(DefaultFolderMapping.AudioClipKey, DefaultFolderMapping.AudioClipKey, DefaultFolderMapping.AudioClipOutput),
            new FolderMapping(DefaultFolderMapping.AudioMixerControllerKey, DefaultFolderMapping.AudioMixerControllerKey, DefaultFolderMapping.AudioMixerControllerOutput),
            new FolderMapping(DefaultFolderMapping.EditorKey, DefaultFolderMapping.EditorKey, DefaultFolderMapping.EditorOutput),
            new FolderMapping(DefaultFolderMapping.FontKey, DefaultFolderMapping.FontKey, DefaultFolderMapping.FontOutput),
            new FolderMapping(DefaultFolderMapping.LightingSettingsKey, DefaultFolderMapping.LightingSettingsKey, DefaultFolderMapping.LightingSettingsOutput),
            new FolderMapping(DefaultFolderMapping.MaterialKey, DefaultFolderMapping.MaterialKey, DefaultFolderMapping.MaterialOutput),
            new FolderMapping(DefaultFolderMapping.MeshKey, DefaultFolderMapping.MeshKey, DefaultFolderMapping.MeshOutput),
            new FolderMapping(DefaultFolderMapping.PrefabInstanceKey, DefaultFolderMapping.PrefabInstanceKey, DefaultFolderMapping.PrefabInstanceOutput),
            new FolderMapping(DefaultFolderMapping.PhysicsMaterialKey, DefaultFolderMapping.PhysicsMaterialKey, DefaultFolderMapping.PhysicsMaterialOutput),
            new FolderMapping(DefaultFolderMapping.PluginsKey, DefaultFolderMapping.PluginsKey, DefaultFolderMapping.PluginsOutput),
            new FolderMapping(DefaultFolderMapping.ResourcesKey, DefaultFolderMapping.ResourcesKey, DefaultFolderMapping.ResourcesOutput),
            new FolderMapping(DefaultFolderMapping.SettingsKey, DefaultFolderMapping.SettingsKey, DefaultFolderMapping.SettingsOutput),
            new FolderMapping(DefaultFolderMapping.ScenesKey, DefaultFolderMapping.ScenesKey, DefaultFolderMapping.ScenesOutput),
            new FolderMapping(DefaultFolderMapping.MonoBehaviourKey, DefaultFolderMapping.MonoBehaviourKey, DefaultFolderMapping.MonoBehaviourOutput),
            new FolderMapping(DefaultFolderMapping.NavMeshDataKey, DefaultFolderMapping.NavMeshDataKey, DefaultFolderMapping.NavMeshDataOutput),
            new FolderMapping(DefaultFolderMapping.CubemapKey, DefaultFolderMapping.CubemapKey, DefaultFolderMapping.CubemapOutput),
            new FolderMapping(DefaultFolderMapping.TerrainDataKey, DefaultFolderMapping.TerrainDataKey, DefaultFolderMapping.TerrainDataOutput),
            new FolderMapping(DefaultFolderMapping.ShaderKey, DefaultFolderMapping.ShaderKey, DefaultFolderMapping.ShaderOutput),
            new FolderMapping(DefaultFolderMapping.ScriptsKey, DefaultFolderMapping.ScriptsKey, DefaultFolderMapping.ScriptsOutput),
            new FolderMapping(DefaultFolderMapping.Texture2DKey, DefaultFolderMapping.Texture2DKey, DefaultFolderMapping.Texture2DOutput),
            new FolderMapping(DefaultFolderMapping.Texture3DKey, DefaultFolderMapping.Texture3DKey, DefaultFolderMapping.Texture3DOutput),
            new FolderMapping(DefaultFolderMapping.RenderTextureKey, DefaultFolderMapping.RenderTextureKey, DefaultFolderMapping.RenderTextureOutput),
            new FolderMapping(DefaultFolderMapping.TerrainLayerKey, DefaultFolderMapping.TerrainLayerKey, DefaultFolderMapping.TerrainLayerOutput),
            new FolderMapping(DefaultFolderMapping.SpriteKey, DefaultFolderMapping.SpriteKey, DefaultFolderMapping.SpriteOutput),
            new FolderMapping(DefaultFolderMapping.StreamingAssetsKey, DefaultFolderMapping.StreamingAssetsKey, DefaultFolderMapping.StreamingAssetsOutput),
            new FolderMapping(DefaultFolderMapping.VideoClipKey, DefaultFolderMapping.VideoClipKey, DefaultFolderMapping.VideoClipOutput),
        }.OrderBy(x => x.sourceName).ToArray();

        [FormerlySerializedAs("_scriptFoldersToCopy")] [SerializeField]
        private string[] _foldersToCopy = new[] {
            DefaultFolderMapping.AnimationClipKey,
            DefaultFolderMapping.AnimatorControllerKey,
            DefaultFolderMapping.AudioClipKey,
            DefaultFolderMapping.AudioMixerControllerKey,
            DefaultFolderMapping.EditorKey,
            DefaultFolderMapping.FontKey,
            DefaultFolderMapping.LightingSettingsKey,
            DefaultFolderMapping.MaterialKey,
            DefaultFolderMapping.MeshKey,
            DefaultFolderMapping.PrefabInstanceKey,
            DefaultFolderMapping.PhysicsMaterialKey,
            DefaultFolderMapping.PluginsKey,
            DefaultFolderMapping.ResourcesKey,
            DefaultFolderMapping.SettingsKey,
            DefaultFolderMapping.ScenesKey,
            DefaultFolderMapping.MonoBehaviourKey,
            DefaultFolderMapping.NavMeshDataKey,
            DefaultFolderMapping.CubemapKey,
            DefaultFolderMapping.TerrainDataKey,
            DefaultFolderMapping.ShaderKey,
            DefaultFolderMapping.Texture2DKey,
            DefaultFolderMapping.Texture3DKey,
            DefaultFolderMapping.RenderTextureKey,
            DefaultFolderMapping.TerrainLayerKey,
            DefaultFolderMapping.SpriteKey,
            DefaultFolderMapping.StreamingAssetsKey,
            DefaultFolderMapping.VideoClipKey,
            @"Scripts\Assembly-CSharp",
        };
        
        [SerializeField]
        private string[] _filesToExcludeFromCopy = Array.Empty<string>();

        [FormerlySerializedAs("_scriptFoldersToExcludeFromRead")] [SerializeField]
        private string[] _foldersToExcludeFromRead = new string[] {
            @"Scripts\Unity.Burst",
            @"Scripts\Unity.Burst.Unsafe",
            @"Scripts\Unity.Burst.Mathematics",
            @"Scripts\Unity.Jobs",
        };
        
        [SerializeField]
        private string[] _projectSettingFilesToCopy = new[] {
            "NavMeshAreas.asset",
            "TagManager.asset",
            "TimeManager.asset",
            "DynamicsManager.asset",
            "QualitySettings.asset"
        };

        [SerializeField]
        private AssetRipperJsonData _configurationData = new AssetRipperJsonData();

        [SerializeField] 
        private string _customBuildUrl = string.Empty;
        
#if UNITY_2020_3_OR_NEWER
        public bool TryGetFolderMapping(string key, out string folder, out bool exclude, string? fallbackPath = null) {
#else
        public bool TryGetFolderMapping(string key, out string folder, out bool exclude, string fallbackPath = null) {
#endif
            foreach (var mapping in _folderMappings) {
                if (mapping.key.Equals(key, StringComparison.OrdinalIgnoreCase)) {
                    folder = mapping.outputPath.ToAssetDatabaseSafePath();
                    exclude = mapping.exclude;
                    return true;
                }
            }

            folder = (fallbackPath ?? string.Empty).ToAssetDatabaseSafePath();
            exclude = false;
            return !string.IsNullOrEmpty(folder);
        }
        
#if UNITY_2020_3_OR_NEWER
        public bool TryGetFolderMappingFromSource(string sourceName, out string folder, out bool exclude, string? fallbackPath = null) {
#else
        public bool TryGetFolderMappingFromSource(string sourceName, out string folder, out bool exclude, string fallbackPath = null) {
#endif
            foreach (var mapping in _folderMappings) {
                if (mapping.sourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase)) {
                    folder = mapping.outputPath.ToAssetDatabaseSafePath();
                    exclude = mapping.exclude;
                    return true;
                }
            }

            folder = (fallbackPath ?? string.Empty).ToAssetDatabaseSafePath();
            exclude = false;
            return !string.IsNullOrEmpty(folder);
        }

#if UNITY_2020_3_OR_NEWER
        public bool TryGetFolderMappingFromOutput(string outputName, out string folder, out bool exclude, string? fallbackPath = null) {
#else
        public bool TryGetFolderMappingFromOutput(string outputName, out string folder, out bool exclude, string fallbackPath = null) {
#endif
            foreach (var mapping in _folderMappings) {
                if (mapping.outputPath.Equals(outputName, StringComparison.OrdinalIgnoreCase)) {
                    folder = mapping.sourceName.ToOSPath();
                    exclude = mapping.exclude;
                    return true;
                }
            }

            folder = (fallbackPath ?? string.Empty).ToOSPath();
            exclude = false;
            return !string.IsNullOrEmpty(folder);
        }

        public string ToJson() {
            var data = _configurationData;
            data.Processing.enableAssetDeduplication = false;
            data.Processing.enableStaticMeshSeparation = false;
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public void SaveToConfig() {
#if UNITY_EDITOR
            var json = ToJson();
            
            if (!Directory.Exists(FolderPath)) {
                Directory.CreateDirectory(FolderPath);
            }
            
            File.WriteAllText(ConfigPath, json);
#endif
        }
    }

    [Serializable]
    public class AssetRipperJsonData {
        [SerializeField] 
        public Import Import = new Import {
            scriptContentLevel = ScriptContentLevel.Level2,
            streamingAssetsMode = StreamingAssetsMode.Extract,
        };

        [SerializeField]
        // [HelpBox("manual = Will prompt the user to run normal Asset Ripper manually")]
        public Processing Processing;
        
        [SerializeField] 
        public Export Export = new Export {
            audioExportFormat = AudioExportFormat.Default,
            imageExportFormat = ImageExportFormat.Png,
            scriptLanguageVersion = ScriptLanguageVersion.AutoSafe,
            textExportMode = TextExportMode.Parse
        };
    }

    [Serializable]
    public struct Import {
        [JsonProperty("ScriptContentLevel")]
        [DefaultValue(ScriptContentLevel.Level2)]
        [Tooltip("Level0: Scripts are not loaded.\n\nLevel1: Methods are stubbed during processing.\n\nLevel2: This level is the default. It has full methods for Mono games and empty methods for IL2Cpp games.")]
        public ScriptContentLevel scriptContentLevel;

        [JsonProperty("StreamingAssetsMode")]
        [DefaultValue(StreamingAssetsMode.Extract)]
        public StreamingAssetsMode streamingAssetsMode;

        [JsonProperty("BundledAssetsExportMode")]
        [Tooltip("GroupByAssetType: Bundled assets are treated the same as assets from other files.\n\nGroupByBundleName: Bundled assets are grouped by their asset bundle name.\n\nDirectExport: Bundled assets are exported without grouping.")]
        public BundledAssetsExportMode bundledAssetsExportMode;
    }

    [Serializable]
    public struct Processing {
        [JsonProperty("EnablePrefabOutlining")]
#if UNITY_2020_3_OR_NEWER
        [Suffix("experimental")]
#endif
        public bool enablePrefabOutlining;

        [JsonProperty("EnableStaticMeshSeparation")]
        [DefaultValue(false)]
#if UNITY_2020_3_OR_NEWER
        [Suffix("paid")]
#endif
        [HideInInspector]
        public bool enableStaticMeshSeparation;

        [JsonProperty("EnableAssetDeduplication")]
        [DefaultValue(false)]
#if UNITY_2020_3_OR_NEWER
        [Suffix("paid")]
#endif
        [HideInInspector]
        public bool enableAssetDeduplication;
    }

    [Serializable]
    public struct Export {
        [JsonProperty("AudioExportFormat")]
        [DefaultValue(AudioExportFormat.Default)]
        [Tooltip("Yaml: Export as a yaml asset and resS file. This is a safe option and is the backup when things go wrong.\n\nNative: For advanced users. This exports in a native format, usually FSB (FMOD Sound Bank). FSB files cannot be used in Unity Editor.\n\nDefault: This is the recommended option. Audio assets are exported in the compression of the source, usually OGG.\n\nPreferWav: Not advised if rebundling. This converts audio to the WAV format when possible")]
        public AudioExportFormat audioExportFormat;

        [JsonProperty("ImageExportFormat")]
        [DefaultValue(ImageExportFormat.Png)]
        [Tooltip("Bmp: Lossless. Bitmap\n\nExr: Lossless. OpenEXR\n\nHdr: Lossless. Radiance HDR\n\nJpeg: Lossy. Joint Photographic Experts Group\n\nPng: Lossless. Portable Network Graphics\n\nTga: Lossless. Truevision TGA")]
        public ImageExportFormat imageExportFormat;

        [JsonProperty("MeshExportFormat")]
        [Tooltip("Native: A robust format for using meshes in the editor. Can be converted to other formats by a variety of unity packages.\n\nGlb: An opensource alternative to FBX. It is the binary version of GLTF. Unity does not support importing this format.")]
        public MeshExportFormat meshExportFormat;

        [JsonProperty("ScriptExportMode")]
        [Tooltip("Decompiled: Use the ILSpy decompiler to generate CS scripts. This is reliable. However, it's also time-consuming and contains many compile errors.\n\nHybrid: Special assemblies, such as Assembly-CSharp, are decompiled to CS scripts with the ILSpy decompiler. Other assemblies are saved as DLL files.\n\nDllExportWithRenaming: Special assemblies, such as Assembly-CSharp, are renamed to have compatible names.\n\nDllExportWithoutRenaming: Export assemblies in their compiled Dll form. Experimental. Might not work at all.")]
        public ScriptExportMode scriptExportMode;

        [JsonProperty("ScriptLanguageVersion")]
        [DefaultValue(ScriptLanguageVersion.AutoSafe)]
        public ScriptLanguageVersion scriptLanguageVersion;

        [JsonProperty("ShaderExportMode")]
        [Tooltip("Dummy: Export as dummy shaders which compile in the editor\n\nYaml: Export as yaml assets which can be viewed in the editor\n\nDisassembly: Export as disassembly which does not compile in the editor\n\nDecompile: Export as decompiled hlsl (unstable!)")]
        public ShaderExportMode shaderExportMode;

        [JsonProperty("SpriteExportMode")]
        [Tooltip("Yaml: Export as yaml assets which can be viewed in the editor.\n\nNative: Export in the native asset format, where all sprites data are stored in texture importer settings.\n\nTexture2D: Export as a Texture2D png image")]
        public SpriteExportMode spriteExportMode;

        [JsonProperty("TerrainExportMode")]
        [Tooltip("Yaml: The default export mode. This is the only one that exports in a format Unity can use for terrains.\n\nMesh: This converts the terrain data into a mesh. Unity cannot import this.\n\nHeatmap: A heatmap of the terrain height. Probably not usable for anything but a visual representation.")]
        public TerrainExportMode terrainExportMode;

        [JsonProperty("TextExportMode")]
        [DefaultValue(TextExportMode.Parse)]
        [Tooltip("Bytes: Export as bytes\n\nTxt: Export as plain text files\n\nParse: Export as plain text files, but try to guess the file extension")]
        public TextExportMode textExportMode;

        [JsonProperty("SaveSettingsToDisk")]
        [HideInInspector]
        public bool saveSettingsToDisk;
    }

    public enum ScriptContentLevel {
        /// <summary>
        /// Scripts are not loaded.
        /// </summary>
        Level0,

        /// <summary>
        /// Methods are stubbed during processing.
        /// </summary>
        Level1,

        /// <summary>
        /// This level is the default. It has full methods for Mono games and empty methods for IL2Cpp games.
        /// </summary>
        Level2,
    }

    public enum StreamingAssetsMode {
        Ignore,
        Extract,
    }

    [Serializable]
    public struct DefaultVersion {
        [JsonProperty("Major")]
        public int major;

        [JsonProperty("Minor")]
        public int minor;

        [JsonProperty("Build")]
        public int build;

        [JsonProperty("Type")]
        public int type;

        [JsonProperty("TypeNumber")]
        public int typeNumber;
    }

    public enum BundledAssetsExportMode {
        /// <summary>
        /// Bundled assets are treated the same as assets from other files.
        /// </summary>
        GroupByAssetType,

        /// <summary>
        /// Bundled assets are grouped by their asset bundle name.<br/>
        /// For example: Assets/Asset_Bundles/NameOfAssetBundle/InternalPath1/.../InternalPathN/assetName.extension
        /// </summary>
        GroupByBundleName,

        /// <summary>
        /// Bundled assets are exported without grouping.<br/>
        /// For example: Assets/InternalPath1/.../InternalPathN/bundledAssetName.extension
        /// </summary>
        DirectExport,
    }

    public enum AudioExportFormat {
        /// <summary>
        /// Export as a yaml asset and resS file. This is a safe option and is the backup when things go wrong.
        /// </summary>
        Yaml,

        /// <summary>
        /// For advanced users. This exports in a native format, usually FSB (FMOD Sound Bank). FSB files cannot be used in Unity Editor.
        /// </summary>
        Native,

        /// <summary>
        /// This is the recommended option. Audio assets are exported in the compression of the source, usually OGG.
        /// </summary>
        Default,

        /// <summary>
        /// Not advised if rebundling. This converts audio to the WAV format when possible
        /// </summary>
        PreferWav,
    }

    public enum ImageExportFormat {
        /// <summary>
        /// Lossless. Bitmap<br/>
        /// <see href="https://en.wikipedia.org/wiki/BMP_file_format"/>
        /// </summary>
        Bmp,

        /// <summary>
        /// Lossless. OpenEXR<br/>
        /// <see href="https://en.wikipedia.org/wiki/OpenEXR"/>
        /// </summary>
        Exr,

        /// <summary>
        /// Lossless. Radiance HDR<br/>
        /// <see href="https://en.wikipedia.org/wiki/RGBE_image_format"/>
        /// </summary>
        Hdr,

        /// <summary>
        /// Lossy. Joint Photographic Experts Group<br/>
        /// <see href="https://en.wikipedia.org/wiki/JPEG"/>
        /// </summary>
        Jpeg,

        /// <summary>
        /// Lossless. Portable Network Graphics<br/>
        /// <see href="https://en.wikipedia.org/wiki/Portable_Network_Graphics"/>
        /// </summary>
        Png,

        /// <summary>
        /// Lossless. Truevision TGA<br/>
        /// <see href="https://en.wikipedia.org/wiki/Truevision_TGA"/>
        /// </summary>
        Tga,
    }

    public enum MeshExportFormat {
        /// <summary>
        /// A robust format for using meshes in the editor. Can be converted to other formats by a variety of unity packages.
        /// </summary>
        Native,

        /// <summary>
        /// An opensource alternative to FBX. It is the binary version of GLTF. Unity does not support importing this format.
        /// </summary>
        Glb,
    }

    public enum ScriptExportMode {
        /// <summary>
        /// Use the ILSpy decompiler to generate CS scripts. This is reliable. However, it's also time-consuming and contains many compile errors.
        /// </summary>
        Decompiled,

        /// <summary>
        /// Special assemblies, such as Assembly-CSharp, are decompiled to CS scripts with the ILSpy decompiler. Other assemblies are saved as DLL files.
        /// </summary>
        Hybrid,

        /// <summary>
        /// Special assemblies, such as Assembly-CSharp, are renamed to have compatible names.
        /// </summary>
        DllExportWithRenaming,

        /// <summary>
        /// Export assemblies in their compiled Dll form. Experimental. Might not work at all.
        /// </summary>
        DllExportWithoutRenaming,
    }

    public enum ScriptLanguageVersion {
        AutoExperimental = -2,
        AutoSafe = -1,
        CSharp1 = 1,
        CSharp2 = 2,
        CSharp3 = 3,
        CSharp4 = 4,
        CSharp5 = 5,
        CSharp6 = 6,
        CSharp7 = 7,
        CSharp7_1 = 701,
        CSharp7_2 = 702,
        CSharp7_3 = 703,
        CSharp8_0 = 800,
        CSharp9_0 = 900,
        CSharp10_0 = 1000,
        CSharp11_0 = 1100,
        Latest = int.MaxValue
    }

    public enum ShaderExportMode {
        /// <summary>
        /// Export as dummy shaders which compile in the editor
        /// </summary>
        Dummy,

        /// <summary>
        /// Export as yaml assets which can be viewed in the editor
        /// </summary>
        Yaml,

        /// <summary>
        /// Export as disassembly which does not compile in the editor
        /// </summary>
        Disassembly,

        /// <summary>
        /// Export as decompiled hlsl (unstable!)
        /// </summary>
        Decompile
    }

    public enum SpriteExportMode {
        /// <summary>
        /// Export as yaml assets which can be viewed in the editor.
        /// This is the only mode that ensures a precise recovery of all metadata of sprites.
        /// <see href="https://github.com/trouger/AssetRipper/issues/2"/>
        /// </summary>
        Yaml,

        /// <summary>
        /// Export in the native asset format, where all sprites data are stored in texture importer settings.
        /// </summary>
        /// <remarks>
        /// The output from this mode was substantially changed by
        /// <see href="https://github.com/AssetRipper/AssetRipper/commit/084b3e5ea7826ac2f54ed2b11cbfbbf3692ddc9c"/>.
        /// Using this is inadvisable.
        /// </remarks>
        Native,

        /// <summary>
        /// Export as a Texture2D png image
        /// </summary>
        /// <remarks>
        /// The output from this mode was substantially changed by
        /// <see href="https://github.com/AssetRipper/AssetRipper/commit/084b3e5ea7826ac2f54ed2b11cbfbbf3692ddc9c"/>.
        /// Using this is inadvisable.
        /// </remarks>
        Texture2D,
    }

    public enum TerrainExportMode {
        /// <summary>
        /// The default export mode. This is the only one that exports in a format Unity can use for terrains.
        /// </summary>
        Yaml,

        /// <summary>
        /// This converts the terrain data into a mesh. Unity cannot import this.
        /// </summary>
        Mesh,

        /// <summary>
        /// A heatmap of the terrain height. Probably not usable for anything but a visual representation.
        /// </summary>
        Heatmap,
    }

    public enum TextExportMode {
        /// <summary>
        /// Export as bytes
        /// </summary>
        Bytes,

        /// <summary>
        /// Export as plain text files
        /// </summary>
        Txt,

        /// <summary>
        /// Export as plain text files, but try to guess the file extension
        /// </summary>
        Parse,
    }
}