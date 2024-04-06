using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using EditorAttributes;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher {
    [CreateAssetMenu(fileName = "UnityProjectPatcherSettings", menuName = "Unity Project Patcher/Settings")]
    public class UPPatcherSettings : ScriptableObject {
        public string? GameExePath => _gameExePath;
        public string? GamePath => Path.GetDirectoryName(_gameExePath);
        public string? GameDataPath => Path.Combine(GamePath, $"{_gameName}_Data");
        public string? GameManagedPath => Path.Combine(GameManagedPath, "Managed");

        [SerializeField, FolderPath(getRelativePath: false)]
        private string? _gameExePath;

        [SerializeField] private string? _gameName = "Game Name";
    }
}