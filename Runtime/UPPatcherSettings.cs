using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using EditorAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nomnom.UnityProjectPatcher {
    [CreateAssetMenu(fileName = "UnityProjectPatcherSettings", menuName = "Unity Project Patcher/Settings")]
    public class UPPatcherSettings : ScriptableObject {
        public string? GameFolderPath => _gameFolderPath;
        public string? GameExePath => Path.Combine(GameFolderPath, $"{_gameName}.exe");
        public string? GameDataPath => Path.Combine(GameFolderPath, $"{_gameName}_Data");
        public string? GameManagedPath => Path.Combine(GameManagedPath, "Managed");

        [SerializeField, FolderPath(getRelativePath: false)]
        private string? _gameFolderPath;

        [SerializeField] private string? _gameName = "Game Name";
    }
}