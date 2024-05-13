using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct MakeProxyScriptsStep: IPatcherStep {
        private readonly Proxy[] _proxys;
        private const string CONTENT = @"
namespace %NAMESPACE% {
    public class %TYPE%: global::%TYPE% { }
}";
        
        public MakeProxyScriptsStep(params Proxy[] proxys) {
            _proxys = proxys;
        }
        
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var arSettings = this.GetAssetRipperSettings();
            if (!arSettings.TryGetFolderMapping("Scripts", out var scriptsFolder, out var exclude) || exclude) {
                Debug.Log("Skipping MakeProxyScriptsStep because no scripts folder was found");
                return UniTask.FromResult(StepResult.Success);
            }
            
            foreach (var proxy in _proxys) {
                var content = CONTENT
                    .Replace("%NAMESPACE%", proxy.typeNamespace)
                    .Replace("%TYPE%", proxy.typeName);

                var path = Path.GetFullPath(Path.Combine(settings.ProjectGameAssetsPath, scriptsFolder, $"{proxy.typeName}.cs"));
                var folder = Path.GetDirectoryName(path);
                if (!Directory.Exists(folder)) {
                    Directory.CreateDirectory(folder);
                }
                File.WriteAllText(path, content);
            }
            
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }

        public readonly struct Proxy {
            public readonly string typeName;
            public readonly string typeNamespace;
            
            public Proxy(string typeName, string typeNamespace) {
                this.typeName = typeName;
                this.typeNamespace = typeNamespace;
            }
        }
    }
}