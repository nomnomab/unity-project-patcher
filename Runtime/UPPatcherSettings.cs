using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using EditorAttributes;
using Nomnom.UnityProjectPatcher.UnityPackages;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Nomnom.UnityProjectPatcher {
    [CreateAssetMenu(fileName = "UnityProjectPatcherSettings", menuName = "Unity Project Patcher/Settings")]
    public partial class UPPatcherSettings : ScriptableObject {
        public string GameFolderPath => _gameFolderPath ?? throw new NullReferenceException(nameof(GameFolderPath));
        public string GameExePath => Path.Combine(GameFolderPath, $"{GameName}.exe");
        public string GameDataPath => Path.Combine(GameFolderPath, $"{GameName}_Data");
        public string GameManagedPath => Path.Combine(GameDataPath, "Managed");
        
        public string ProjectUnityPath => Path.Combine(Application.dataPath, "Unity");
        public string ProjectUnityAssetStorePath => Path.Combine(Application.dataPath, "AssetStore");
        
        public string ProjectGamePath => Path.Combine(Application.dataPath, GameName.Replace(" ", string.Empty));
        public string ProjectGameAssetsPath => Path.Combine(ProjectGamePath, "Game");
        public string ProjectGameModsPath => Path.Combine(ProjectGamePath, "Mods");
        public string ProjectGameToolsPath => Path.Combine(ProjectGamePath, "Tools");
        
        public string GameName => _gameName ?? throw new NullReferenceException(nameof(GameName));
        public string GameVersion => _gameVersion ?? throw new NullReferenceException(nameof(GameVersion));
        
        public IReadOnlyCollection<string> IgnoredDllPrefixes => _ignoredDllPrefixes;
        public IReadOnlyCollection<FoundPackageInfo> ExactPackagesFound => _exactPackagesFound;
        public IReadOnlyCollection<GitPackageInfo> GitPackages => _gitPackages;

        [SerializeField, FolderPath(getRelativePath: false)]
        private string? _gameFolderPath;

        [SerializeField, InlineButton(nameof(GetGameName), "Get", buttonWidth: 30)]
        private string? _gameName = null;
        
        [SerializeField, InlineButton(nameof(GetGameVersion), "Get", buttonWidth: 30)] 
        private string? _gameVersion = null;

        [Header("Packages")]
        [SerializeField] private string[] _ignoredDllPrefixes = new[] {
            "System.",
            "UnityEngine.",
            "Unity.Services."
        };

        [SerializeField] private List<FoundPackageInfo> _exactPackagesFound = new();
        [SerializeField] private List<FoundPackageInfo> _possiblePackagesFound = new();
        [SerializeField] private List<FoundPackageInfo> _improbablePackagesFound = new();
        [SerializeField] private List<GitPackageInfo> _gitPackages = new();

        private void GetGameName() {
            if (GameFolderPath is null) {
                Debug.LogError("Game Folder Path is null");
                return;
            }

            _gameName = Path.GetFileNameWithoutExtension(GameFolderPath);
        }
        
        private void GetGameVersion() {
            if (GameExePath is null) {
                Debug.LogError("Game Exe Path is null");
                return;
            }
            
            if (!File.Exists(GameExePath)) {
                Debug.LogError("Game Exe Path does not exist");
                return;
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(GameExePath);
            _gameVersion = versionInfo.FileVersion;
            
            PatcherUtility.SetDirty(this);
        }
    }
}