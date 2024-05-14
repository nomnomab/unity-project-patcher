using System;
using System.IO;
#if UNITY_2020_3_OR_NEWER
using EditorAttributes;
#endif
using UnityEngine;

namespace Nomnom.UnityProjectPatcher {
    [CreateAssetMenu(fileName = "UnityProjectPatcherUserSettings", menuName = "Unity Project Patcher/User Settings")]
    public sealed class UPPatcherUserSettings: ScriptableObject {
        public string GameFolderPath => string.IsNullOrEmpty(_gameFolderPath) 
            ? throw new NullReferenceException(nameof(GameFolderPath)) 
            : _gameFolderPath ?? throw new NullReferenceException(nameof(GameFolderPath));
        
        public string AssetRipperDownloadFolderPath => Path.GetFullPath(_assetRipperDownloadFolderPath);
        public string AssetRipperExportFolderPath => Path.GetFullPath(_assetRipperExportFolderPath);
        
#if UNITY_2020_3_OR_NEWER
        [SerializeField, FolderPath(getRelativePath: false)]
        [Header("Where the game is installed")]
        [HelpBox(@"This path is absolute to your game folder. Such as: ""C:\Program Files (x86)\Steam\steamapps\common\Lethal Company""")]
        private string? _gameFolderPath;

        [SerializeField, FolderPath]
        [Header("Where AssetRipper will be downloaded to")]
        [HelpBox(@"This path is relative to your project folder. It defaults to ""[Project Name]\AssetRipper""")]
        private string? _assetRipperDownloadFolderPath = "AssetRipper";

        [SerializeField, FolderPath]
        [Header("Where AssetRipper will store exported files")]
        [HelpBox(@"This path is relative to your project folder. It defaults to ""[Project Name]\AssetRipperOutput""")]
        private string? _assetRipperExportFolderPath = "AssetRipperOutput";
#else
        [SerializeField]
        [Header(@"Where the game is installed. This path is absolute to your game folder. Such as: ""C:\Program Files (x86)\Steam\steamapps\common\Lethal Company""")]
        private string _gameFolderPath;
        
        [SerializeField]
        [Header(@"Where AssetRipper will be downloaded to. This path is relative to your project folder. It defaults to ""[Project Name]\AssetRipper""")]
        private string _assetRipperDownloadFolderPath = "AssetRipper";
        
        [SerializeField]
        [Header(@"Where AssetRipper will store exported files. This path is relative to your project folder. It defaults to ""[Project Name]\AssetRipperOutput""")]
        private string _assetRipperExportFolderPath = "AssetRipperOutput";
#endif
    }
}