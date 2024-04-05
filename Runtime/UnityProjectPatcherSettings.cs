using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using EditorAttributes;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher {
    [CreateAssetMenu(fileName = "UnityProjectPatcherSettings", menuName = "Unity Project Patcher/Settings")]
    public class UnityProjectPatcherSettings : ScriptableObject {
        [SerializeField, FolderPath(getRelativePath: false)]
        private string? _assetRipperPath;

        [SerializeField, FolderPath(getRelativePath: false)]
        private string? _gameExePath;

        [SerializeField]
        private FolderMapping[] _folderMappings = new[] {
            new FolderMapping(DefaultFolderMapping.AnimationClipKey, DefaultFolderMapping.AnimationClipOutput),
            new FolderMapping(DefaultFolderMapping.AnimatorControllerKey, DefaultFolderMapping.AnimatorControllerOutput),
            new FolderMapping(DefaultFolderMapping.AudioClipKey, DefaultFolderMapping.AudioClipOutput),
            new FolderMapping(DefaultFolderMapping.AudioMixerControllerKey, DefaultFolderMapping.AudioMixerControllerOutput),
            new FolderMapping(DefaultFolderMapping.FontKey, DefaultFolderMapping.FontOutput),
            new FolderMapping(DefaultFolderMapping.LightingSettingsKey, DefaultFolderMapping.LightingSettingsOutput),
            new FolderMapping(DefaultFolderMapping.MaterialKey, DefaultFolderMapping.MaterialOutput),
            new FolderMapping(DefaultFolderMapping.MeshKey, DefaultFolderMapping.MeshOutput),
            new FolderMapping(DefaultFolderMapping.PrefabInstanceKey, DefaultFolderMapping.PrefabInstanceOutput),
            new FolderMapping(DefaultFolderMapping.PhysicsMaterialKey, DefaultFolderMapping.PhysicsMaterialOutput),
            new FolderMapping(DefaultFolderMapping.ResourcesKey, DefaultFolderMapping.ResourcesOutput),
            new FolderMapping(DefaultFolderMapping.SettingsKey, DefaultFolderMapping.SettingsOutput),
            new FolderMapping(DefaultFolderMapping.ScenesKey, DefaultFolderMapping.ScenesOutput),
            new FolderMapping(DefaultFolderMapping.MonoBehaviourKey, DefaultFolderMapping.MonoBehaviourOutput),
            new FolderMapping(DefaultFolderMapping.NavMeshDataKey, DefaultFolderMapping.NavMeshDataOutput),
            new FolderMapping(DefaultFolderMapping.CubemapKey, DefaultFolderMapping.CubemapOutput),
            new FolderMapping(DefaultFolderMapping.TerrainDataKey, DefaultFolderMapping.TerrainDataOutput),
            new FolderMapping(DefaultFolderMapping.ShaderKey, DefaultFolderMapping.ShaderOutput),
            new FolderMapping(DefaultFolderMapping.ScriptsKey, DefaultFolderMapping.ScriptsOutput),
            new FolderMapping(DefaultFolderMapping.Texture2DKey, DefaultFolderMapping.Texture2DOutput),
            new FolderMapping(DefaultFolderMapping.Texture3DKey, DefaultFolderMapping.Texture3DOutput),
            new FolderMapping(DefaultFolderMapping.RenderTextureKey, DefaultFolderMapping.RenderTextureOutput),
            new FolderMapping(DefaultFolderMapping.TerrainLayerKey, DefaultFolderMapping.TerrainLayerOutput),
            new FolderMapping(DefaultFolderMapping.SpriteKey, DefaultFolderMapping.SpriteOutput),
            new FolderMapping(DefaultFolderMapping.VideoClipKey, DefaultFolderMapping.VideoClipOutput),
        };

        public bool TryGetFolderMapping(string key, out string? folder, string? fallbackPath = null) {
            foreach (var mapping in _folderMappings) {
                if (mapping.sourceName.Equals(key, StringComparison.OrdinalIgnoreCase)) {
                    folder = mapping.outputPath;
                    return true;
                }
            }

            folder = fallbackPath;
            return false;
        }
    }
}