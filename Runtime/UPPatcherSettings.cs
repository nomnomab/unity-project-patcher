using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Nomnom.UnityProjectPatcher.UnityPackages;
using UnityEngine;
using Debug = UnityEngine.Debug;

#if UNITY_2020_3_OR_NEWER
using EditorAttributes;
#endif

namespace Nomnom.UnityProjectPatcher {
    [CreateAssetMenu(fileName = "UnityProjectPatcherSettings", menuName = "Unity Project Patcher/Settings")]
    public partial class UPPatcherSettings : ScriptableObject {
#if UNITY_EDITOR
        public string GameFolderPath => PatcherUtility.GetUserSettings().GameFolderPath ?? throw new NullReferenceException(nameof(GameFolderPath));
        public string GameExePath => Path.Combine(GameFolderPath, $"{(string.IsNullOrEmpty(_customGameName) ? GameName : _customGameName)}.exe");
        public string GameDataPath => Path.Combine(GameFolderPath, $"{(string.IsNullOrEmpty(_customGameName) ? GameName : _customGameName)}_Data");
        public string GameManagedPath => Path.Combine(GameDataPath, "Managed");
#endif
        
        public string ProjectUnityPath => Path.Combine(Application.dataPath, "Unity");
        public string ProjectUnityAssetStorePath => Path.Combine(Application.dataPath, "AssetStore");
        
        public string ProjectGamePath => Path.Combine("Assets", GameName.Replace(" ", string.Empty)).ToAssetDatabaseSafePath();
        public string ProjectGameFullPath => Path.Combine(Application.dataPath, GameName.Replace(" ", string.Empty));
        public string ProjectGameAssetsPath => Path.Combine(ProjectGamePath, "Game").ToAssetDatabaseSafePath();
        public string ProjectGameAssetsFullPath => Path.Combine(ProjectGameFullPath, "Game");
        public string ProjectGameModsPath => Path.Combine(ProjectGamePath, "Mods").ToAssetDatabaseSafePath();
        public string ProjectGameModsFullPath => Path.Combine(ProjectGameFullPath, "Mods");
        public string ProjectGameToolsPath => Path.Combine(ProjectGamePath, "Tools").ToAssetDatabaseSafePath();
        public string ProjectGameToolsFullPath => Path.Combine(ProjectGameFullPath, "Tools");
        public string ProjectGamePluginsPath => Path.Combine(ProjectGamePath, "Plugins").ToAssetDatabaseSafePath();
        public string ProjectGamePluginsFullPath => Path.Combine(ProjectGameFullPath, "Plugins");
        
        public string GameName => _gameName ?? throw new NullReferenceException(nameof(GameName));
        public string GameVersion => _gameVersion ?? throw new NullReferenceException(nameof(GameVersion));
        public PipelineType GamePipeline => _pipelineType;
        
        public IReadOnlyList<FolderMapping> DllsToCopy => _dllsToCopy.Where(x => !x.exclude).ToList();
        public IReadOnlyList<string> ScriptDllFoldersToCopy => _scriptDllFoldersToCopy;
        
        public IReadOnlyCollection<string> IgnoredDllPrefixes => _ignoredDllPrefixes;
        public IReadOnlyCollection<FoundPackageInfo> ExactPackagesFound => _exactPackagesFound;
        public IReadOnlyCollection<GitPackageInfo> GitPackages => _gitPackages;

#if UNITY_2020_3_OR_NEWER
        [SerializeField, InlineButton(nameof(GetGameName), "Get", buttonWidth: 30)]
        private string? _gameName = null;
        
        [SerializeField]
        private string? _customGameName = null;
        
        [SerializeField, InlineButton(nameof(GetGameVersion), "Get", buttonWidth: 30)] 
        private string? _gameVersion = null;
        
        [SerializeField, InlineButton(nameof(GetPipelineType), "Get", buttonWidth: 30)] 
        private PipelineType _pipelineType;
#else
        [SerializeField]
        private string _gameName = null;
        
        [SerializeField]
        private string _customGameName = null;

        [SerializeField] 
        private string _gameVersion = null;
        
        [SerializeField] 
        private PipelineType _pipelineType;
#endif

        [Header("Dlls")]
        [SerializeField] private FolderMapping[] _dllsToCopy = Array.Empty<FolderMapping>();
        [SerializeField] private string[] _scriptDllFoldersToCopy = Array.Empty<string>();

        [Header("Packages")]
        [SerializeField] private string[] _ignoredDllPrefixes = new[] {
            "System.",
            "UnityEngine.",
            "Unity.Services."
        };

        [SerializeField] private List<FoundPackageInfo> _exactPackagesFound = new List<FoundPackageInfo>();
        [SerializeField] private List<FoundPackageInfo> _possiblePackagesFound = new List<FoundPackageInfo>();
        [SerializeField] private List<FoundPackageInfo> _improbablePackagesFound = new List<FoundPackageInfo>();
        [SerializeField] private List<GitPackageInfo> _gitPackages = new List<GitPackageInfo>();

        private void GetGameName() {
#if UNITY_EDITOR
            if (GameFolderPath is null) {
                Debug.LogError("Game Folder Path is null");
                return;
            }

            _gameName = Path.GetFileNameWithoutExtension(GameFolderPath);
#endif
        }
        
        private void GetGameVersion() {
#if UNITY_EDITOR
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
#endif
        }
        
        private void GetPipelineType() {
#if UNITY_EDITOR
            var dllPath = GameManagedPath;
            if (!Directory.Exists(dllPath)) {
                Debug.LogError("Game Managed Path does not exist");
                return;
            }
            
            var dlls = Directory.EnumerateFiles(dllPath, "*.dll", SearchOption.AllDirectories);
            _pipelineType = PipelineType.BuiltIn;
            
            foreach (var dll in dlls) {
                var fileName = Path.GetFileName(dll);
                if (fileName.Contains("RenderPipelines.HighDefinition.Runtime")) {
                    _pipelineType = PipelineType.HDRP;
                    break;
                }
                
                if (fileName.Contains("RenderPipelines.Universal.Runtime")) {
                    _pipelineType = PipelineType.URP;
                    break;
                }
            }
            
            PatcherUtility.SetDirty(this);
#endif
        }
    }
}