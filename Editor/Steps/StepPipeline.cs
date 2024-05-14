using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public sealed class StepPipeline {
        // default steps
        public readonly List<IPatcherStep> Steps = new List<IPatcherStep>() {
            new GenerateDefaultProjectStructureStep(),
            new ImportTextMeshProStep(),
            new GenerateGitIgnoreStep(),
            new GenerateReadmeStep(),
            new PackagesInstallerStep(), // recompile
            new CacheProjectCatalogueStep(),
            new AssetRipperStep(),
            new CopyGamePluginsStep(), // recompile
            new CopyExplicitScriptFolderStep(), // restarts
            new EnableUnsafeCodeStep(), // recompiles
            new CopyProjectSettingsStep(allowUnsafeCode: true), // restart
            new GuidRemapperStep(),
            new CopyAssetRipperExportToProjectStep(), // restarts
            new FixProjectFileIdsStep(),
            new SortAssetTypesSteps(),
            new RestartEditorStep()
        };

        public StepPipeline() {
            var settings = PatcherUtility.GetSettings();
            switch (settings.GamePipeline) {
                case PipelineType.URP:
                    InsertAfter<FixProjectFileIdsStep>(new InjectURPAssetsStep());
                    break;
                case PipelineType.HDRP:
                    InsertAfter<FixProjectFileIdsStep>(new InjectHDRPAssetsStep());
                    break;
            }
        }
        
        public StepPipeline SetInputSystem(InputSystemType inputSystemType) {
            InsertAfter<PackagesInstallerStep>(new EnableNewInputSystemStep(inputSystemType));
            if (inputSystemType != InputSystemType.InputManager_Old) {
                InsertBefore<FixProjectFileIdsStep>(new FixInputSystemAssetsStep());
            }
            return this;
        }
        
        public StepPipeline IsUsingNetcodeForGameObjects() {
            InsertAfter<AssetRipperStep>(new StripNGOGeneratedCodeStep());
            return this;
        }

        public StepPipeline IsUsingAddressables() {
            InsertBefore<GenerateDefaultProjectStructureStep>(new PromptUserWithAddressablePluginStep());
            InsertAfter<GuidRemapperStep>(new AddressablesGuidRemapperStep());
            return this;
        }

        public StepPipeline SetGameViewResolution(string resolution) {
            return InsertLast(new ChangeGameViewResolutionStep(resolution));
        }
        
        public StepPipeline OpenSceneAtEnd(string sceneName) {
            return InsertLast(new OpenSceneStep(sceneName));
        }

        public StepPipeline InsertFirst(IPatcherStep step) {
            Steps.Insert(0, step);
            return this;
        }
        
        public StepPipeline InsertLast(IPatcherStep step) {
            Steps.Add(step);
            return this;
        }
        
        public StepPipeline InsertBefore<T>(IPatcherStep step) where T: IPatcherStep {
            var foundStep = Steps.FindIndex(x => x is T);
            if (foundStep == -1) {
                throw new Exception($"Could not find step of type {typeof(T).Name} in pipeline");
            }
            
            Steps.Insert(foundStep, step);
            return this;
        }
        
        public StepPipeline InsertBefore<T>(params IPatcherStep[] steps) where T: IPatcherStep {
            var foundStep = Steps.FindIndex(x => x is T);
            if (foundStep == -1) {
                throw new Exception($"Could not find step of type {typeof(T).Name} in pipeline");
            }
            
            Steps.InsertRange(foundStep, steps);
            return this;
        }
        
        public StepPipeline InsertAfter<T>(IPatcherStep step) where T: IPatcherStep {
            var foundStep = Steps.FindIndex(x => x is T);
            if (foundStep == -1) {
                throw new Exception($"Could not find step of type {typeof(T).Name} in pipeline");
            }
            
            Steps.Insert(foundStep + 1, step);
            return this;
        }
        
        public StepPipeline InsertAfter<T>(params IPatcherStep[] steps) where T: IPatcherStep {
            var foundStep = Steps.FindIndex(x => x is T);
            if (foundStep == -1) {
                throw new Exception($"Could not find step of type {typeof(T).Name} in pipeline");
            }
            
            Steps.InsertRange(foundStep + 1, steps);
            return this;
        }

        public bool Validate() {
            if (Steps.Count == 0) {
                Debug.LogWarning("No steps in pipeline");
                return false;
            }
            
            // any duplicates?
            var duplicates = Steps.GroupBy(x => x.GetType()).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicates.Count > 0) {
                Debug.LogWarning($"Duplicate steps in pipeline: {string.Join(", ", duplicates.Select(x => x.Name))}");
                return false;
            }

            return true;
        }

        public void PrintToLog() {
            Debug.Log($"StepPipeline with {Steps.Count} step(s):");
            for (var i = 0; i < Steps.Count; i++) {
                Debug.Log($" - [{i}] {Steps[i].GetType().Name}");
            }
        }
    }
}