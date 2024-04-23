using Cysharp.Threading.Tasks;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    // this will be a pain in my ass :)))))))))))
    public readonly struct GuidRemapperStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            var arSettings = this.GetAssetRipperSettings();
            
            var arAssets = AssetScrubber.ScrubDiskFolder(arSettings.OutputExportFolderPath);
            var projectAssets = AssetScrubber.ScrubProject();
            
            throw new System.NotImplementedException();
        }
    }
}