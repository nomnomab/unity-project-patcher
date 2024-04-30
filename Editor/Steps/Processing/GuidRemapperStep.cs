﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    // this will be a pain in my ass :)))))))))))
    public readonly struct GuidRemapperStep: IPatcherStep {
        [MenuItem("Tools/UPP/Test")]
        public static void Foo() {
            var step = new GuidRemapperStep();
            step.Run();
        }
        
        public UniTask<StepResult> Run() {
            var arSettings = this.GetAssetRipperSettings();
            
            var arAssets = AssetScrubber.ScrubDiskFolder(arSettings.OutputExportAssetsFolderPath, arSettings.FoldersToExcludeFromRead);
            var projectAssets = AssetScrubber.ScrubProject();

            var matches = projectAssets.CompareProjectToDisk(arAssets).ToArray();
            Debug.Log($"Found {matches.Length} matches");
            
            // okay so, this needs to take each match, swap to the new guid found
            // and then after all of this, replace all the old guids in the entire
            // asset ripper list with the new guid. this is slow as fuck... :/

            var lookup = new Dictionary<string, AssetCatalogue.FoundMatch>();
            foreach (var match in matches) {
                lookup[match.from.Guid] = match;
            }
            
            for (int i = 0; i < matches.Length; i++) {
                var match = matches[i];
                var entryFrom = match.from;
                var entryTo = match.to;
                
                EditorUtility.DisplayProgressBar("Guid Remapping", $"Replacing {entryFrom.Guid} with {entryTo.Guid}", i / (float) matches.Length);
                
                // replace guids & write to disk
                try {
                    AssetScrubber.ReplaceMetaGuid(arAssets.RootAssetsPath, entryFrom, entryTo.Guid);
                    AssetScrubber.ReplaceAssetGuids(arAssets.RootAssetsPath, entryFrom, lookup);
                } catch (Exception e) {
                    Debug.LogError(e);
                }
            }

            for (int i = 0; i < arAssets.Entries.Length; i++) {
                var entry = arAssets.Entries[i];
                
                EditorUtility.DisplayProgressBar("Guid Remapping", $"Checking associations for {entry.RelativePathToRoot}", i / (float) arAssets.Entries.Length);

                try {
                    AssetScrubber.ReplaceAssetGuids(arAssets.RootAssetsPath, entry, lookup);
                } catch (Exception e) {
                    Debug.LogError(e);
                }
            }
            
            EditorUtility.ClearProgressBar();

            return UniTask.FromResult(StepResult.Success);
        }
    }
}