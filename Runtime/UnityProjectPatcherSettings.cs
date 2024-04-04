using System.Collections.Generic;
using System.IO;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher {
    [CreateAssetMenu(fileName = "UnityProjectPatcherSettings", menuName = "Unity Project Patcher/Settings")]
    public class UnityProjectPatcherSettings : ScriptableObject {
        public IReadOnlyDictionary<string, string> FolderMappings => _folderMappings;
        
        [SerializeField]
        private SerializedDictionary<string, string> _folderMappings = new() {
            ["AnimationClip"] = Path.Combine("Animation", "AnimationClips"),
            ["AnimatorController"] = Path.Combine("Animation", "AnimatorControllers"),
            ["AudioClip"] = Path.Combine("Audio", "AudioClips"),
            ["AudioMixerController"] = Path.Combine("Audio", "AudioMixerControllers"),
            ["Font"] = Path.Combine("Fonts", "TextMeshPro"),
            ["LightingSettings"] = "LightingSettings",
            ["Material"] = "Materials",
            ["Mesh"] = "Meshes",
            ["PrefabInstance"] = "Prefabs",
            ["PhysicsMaterial"] = "PhysicsMaterials",
            ["Resources"] = "Resources",
            ["Settings"] = "Settings",
            ["Scenes"] = "Scenes",
            ["MonoBehaviour"] = "ScriptableObjects",
            ["NavMeshData"] = Path.Combine("Scenes", "NavMeshData"),
            ["Cubemap"] = Path.Combine("Scenes", "Cubemaps"),
            ["TerrainData"] = Path.Combine("Scenes", "TerrainData"),
            ["Shader"] = "Shaders",
            ["Scripts"] = "Scripts",
            ["Texture2D"] = Path.Combine("Textures", "Texture2Ds"),
            ["Texture3D"] = Path.Combine("Textures", "Texture3Ds"),
            ["RenderTexture"] = Path.Combine("Textures", "RenderTextures"),
            ["TerrainLayer"] = Path.Combine("Textures", "TerrainLayers"),
            ["Sprite"] = Path.Combine("Textures", "Sprites"),
            ["VideoClip"] = "Videos",
        };
    }
}