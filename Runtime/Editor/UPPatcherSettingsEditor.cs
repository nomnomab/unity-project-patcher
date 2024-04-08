﻿#if UNITY_EDITOR
using System.IO;
using EditorAttributes;
using Nomnom.UnityProjectPatcher.UnityPackages;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher {
    public partial class UPPatcherSettings {
        [Button]
        public void GetGamePackages() {
            if (GameManagedPath is null) {
                Debug.LogError("Game Managed Path is null");
                return;
            }
            
            if (!Directory.Exists(GameManagedPath)) {
                Debug.LogError("Game Managed Path does not exist");
                return;
            }
            
            _exactPackagesFound.Clear();
            _possiblePackagesFound.Clear();
            _improbablePackagesFound.Clear();

            foreach (var package in PackagesUtility.GetGamePackages(this)) {
                switch (package.matchType) {
                    case PackageMatchType.Exact:
                        _exactPackagesFound.Add(package);
                        break;
                    case PackageMatchType.Possible:
                        _possiblePackagesFound.Add(package);
                        break;
                    case PackageMatchType.Improbable:
                        _improbablePackagesFound.Add(package);
                        break;
                }
            }
            
            PatcherUtility.SetDirty(this);
        }
    }
}
#endif