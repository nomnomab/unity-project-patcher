using System;
using System.IO;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher {
    [Serializable]
    public struct FolderMapping {
        public string sourceName;
        public string outputPath;

        public FolderMapping(string sourceName, string outputPath) {
            this.sourceName = sourceName;
            this.outputPath = outputPath;
        }
    }

    public static class DefaultFolderMapping {
        public const string AnimationClipKey = "AnimationClip";
        public static readonly string AnimationClipOutput = Path.Combine("Animation", "AnimationClips");
        
        public const string AnimatorControllerKey = "AnimatorController";
        public static readonly string AnimatorControllerOutput = Path.Combine("Animation", "AnimatorControllers");
        
        public const string AudioClipKey = "AudioClip";
        public static readonly string AudioClipOutput = Path.Combine("Audio", "AudioClips");
        
        public const string AudioMixerControllerKey = "AudioMixerController";
        public static readonly string AudioMixerControllerOutput = Path.Combine("Audio", "AudioMixerControllers");
        
        public const string FontKey = "Font";
        public static readonly string FontOutput = Path.Combine("Fonts", "TextMeshPro");
        
        public const string LightingSettingsKey = "LightingSettings";
        public static readonly string LightingSettingsOutput = "LightingSettings";
        
        public const string MaterialKey = "Material";
        public static readonly string MaterialOutput = "Materials";
        
        public const string MeshKey = "Mesh";
        public static readonly string MeshOutput = "Meshes";
        
        public const string PrefabInstanceKey = "PrefabInstance";
        public static readonly string PrefabInstanceOutput = "Prefabs";
        
        public const string PhysicsMaterialKey = "PhysicsMaterial";
        public static readonly string PhysicsMaterialOutput = "PhysicsMaterials";
        
        public const string ResourcesKey = "Resources";
        public static readonly string ResourcesOutput = "Resources";
        
        public const string SettingsKey = "Settings";
        public static readonly string SettingsOutput = "Settings";
        
        public const string ScenesKey = "Scenes";
        public static readonly string ScenesOutput = "Scenes";
        
        public const string MonoBehaviourKey = "MonoBehaviour";
        public static readonly string MonoBehaviourOutput = "ScriptableObjects";
        
        public const string NavMeshDataKey = "NavMeshData";
        public static readonly string NavMeshDataOutput = Path.Combine("Scenes", "NavMeshData");
        
        public const string CubemapKey = "Cubemap";
        public static readonly string CubemapOutput = Path.Combine("Scenes", "Cubemaps");
        
        public const string TerrainDataKey = "TerrainData";
        public static readonly string TerrainDataOutput = Path.Combine("Scenes", "TerrainData");
        
        public const string ShaderKey = "Shader";
        public static readonly string ShaderOutput = "Shaders";
        
        public const string ScriptsKey = "Scripts";
        public static readonly string ScriptsOutput = "Scripts";
        
        public const string Texture2DKey = "Texture2D";
        public static readonly string Texture2DOutput = Path.Combine("Textures", "Texture2Ds");
        
        public const string Texture3DKey = "Texture3D";
        public static readonly string Texture3DOutput = Path.Combine("Textures", "Texture3Ds");
        
        public const string RenderTextureKey = "RenderTexture";
        public static readonly string RenderTextureOutput = Path.Combine("Textures", "RenderTextures");
        
        public const string TerrainLayerKey = "TerrainLayer";
        public static readonly string TerrainLayerOutput = Path.Combine("Textures", "TerrainLayers");
        
        public const string SpriteKey = "Sprite";
        public static readonly string SpriteOutput = Path.Combine("Textures", "Sprites");
        
        public const string VideoClipKey = "VideoClip";
        public static readonly string VideoClipOutput = "Videos";
        
    }
}