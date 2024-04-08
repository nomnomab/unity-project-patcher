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
        public string? GameFolderPath => _gameFolderPath;
        public string? GameExePath => Path.Combine(GameFolderPath, $"{_gameName}.exe");
        public string? GameDataPath => Path.Combine(GameFolderPath, $"{_gameName}_Data");
        public string? GameManagedPath => Path.Combine(GameDataPath, "Managed");
        
        public string? GameName => _gameName;
        public string? GameVersion => _gameVersion;
        
        public IReadOnlyCollection<string> IgnoredDllPrefixes => _ignoredDllPrefixes;

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