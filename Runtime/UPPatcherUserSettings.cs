using System;
using System.IO;
using EditorAttributes;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher {
    [CreateAssetMenu(fileName = "UnityProjectPatcherUserSettings", menuName = "Unity Project Patcher/User Settings")]
    public sealed class UPPatcherUserSettings: ScriptableObject {
        public string GameFolderPath => Path.GetFullPath(_gameFolderPath) ?? throw new NullReferenceException(nameof(GameFolderPath));
        
        public string AssetRipperDownloadFolderPath => Path.GetFullPath(_assetRipperDownloadFolderPath);
        public string AssetRipperExportFolderPath => Path.GetFullPath(_assetRipperExportFolderPath);
        
#if UNITY_2020_3_OR_NEWER
        [SerializeField, FolderPath(getRelativePath: false)]
        [Header("Where the game is installed")]
        private string? _gameFolderPath;

        [SerializeField, FolderPath]
        [Header("Where AssetRipper will be downloaded to")]
        private string? _assetRipperDownloadFolderPath = "AssetRipper";

        [SerializeField, FolderPath]
        [Header("Where AssetRipper will store exported files")]
        private string? _assetRipperExportFolderPath = "AssetRipperOutput";
#else
        [SerializeField]
        [Header("Where the game is installed")]
        private string _gameFolderPath;
        
        [SerializeField]
        [Header("Where AssetRipper will be downloaded to")]
        private string _assetRipperDownloadFolderPath = "AssetRipper";
        
        [SerializeField]
        [Header("Where AssetRipper will store exported files")]
        private string _assetRipperExportFolderPath = "AssetRipperOutput";
#endif
    }
}