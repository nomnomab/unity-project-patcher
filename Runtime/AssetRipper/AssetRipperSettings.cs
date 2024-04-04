using System;
using System.ComponentModel;
using Newtonsoft.Json;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.AssetRipper {
    [CreateAssetMenu(fileName = "AssetRipperSettings", menuName = "Unity Project Patcher/AssetRipper Settings")]
    public class AssetRipperSettings : ScriptableObject {
        public Import import => _import;
        public Processing processing => _processing;
        public Export export => _export;
        
        [SerializeField] 
        private Import _import = new Import {
            scriptContentLevel = ScriptContentLevel.Level2,
            streamingAssetsMode = StreamingAssetsMode.Extract,
        };
        
        [SerializeField] 
        private Processing _processing = new Processing {
            enableStaticMeshSeparation = true,
            enableAssetDeduplication = true
        };
        
        [SerializeField] 
        private Export _export = new Export {
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
        public ScriptContentLevel scriptContentLevel;

        [JsonProperty("StreamingAssetsMode")]
        [DefaultValue(StreamingAssetsMode.Extract)]
        public StreamingAssetsMode streamingAssetsMode;

        [JsonProperty("DefaultVersion")]
        public DefaultVersion defaultVersion;

        [JsonProperty("BundledAssetsExportMode")]
        public BundledAssetsExportMode bundledAssetsExportMode;
    }

    [Serializable]
    public struct Processing {
        [JsonProperty("EnablePrefabOutlining")]
        public bool enablePrefabOutlining;

        [JsonProperty("EnableStaticMeshSeparation")]
        [DefaultValue(true)]
        public bool enableStaticMeshSeparation;

        [JsonProperty("EnableAssetDeduplication")]
        [DefaultValue(true)]
        public bool enableAssetDeduplication;
    }

    [Serializable]
    public struct Export {
        [JsonProperty("AudioExportFormat")]
        [DefaultValue(AudioExportFormat.Default)]
        public AudioExportFormat audioExportFormat;

        [JsonProperty("ImageExportFormat")]
        [DefaultValue(ImageExportFormat.Png)]
        public ImageExportFormat imageExportFormat;

        [JsonProperty("MeshExportFormat")]
        public MeshExportFormat meshExportFormat;

        [JsonProperty("ScriptExportMode")]
        public ScriptExportMode scriptExportMode;

        [JsonProperty("ScriptLanguageVersion")]
        [DefaultValue(ScriptLanguageVersion.AutoSafe)]
        public ScriptLanguageVersion scriptLanguageVersion;

        [JsonProperty("ShaderExportMode")]
        public ShaderExportMode shaderExportMode;

        [JsonProperty("SpriteExportMode")]
        public SpriteExportMode spriteExportMode;

        [JsonProperty("TerrainExportMode")]
        public TerrainExportMode terrainExportMode;

        [JsonProperty("TextExportMode")]
        [DefaultValue(TextExportMode.Parse)]
        public TextExportMode textExportMode;

        [JsonProperty("SaveSettingsToDisk")]
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