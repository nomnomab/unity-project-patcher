using System;
using System.Linq;

namespace Nomnom.UnityProjectPatcher.UnityPackages {
    [Serializable]
    public struct FoundPackageInfo {
        /*[ReadOnly]*/ public string name;
        /*[ReadOnly]*/ public string version;
        /*[ReadOnly]*/ public FoundDependencyInfo[]? dependencies;
        /*[ReadOnly]*/ public PackageMatchType matchType;
        
        public FoundPackageInfo(string name, string version, FoundDependencyInfo[]? dependencies, PackageMatchType matchType) {
            this.name = name;
            this.version = version;
            this.dependencies = dependencies;
            this.matchType = matchType;
        }
        
#if UNITY_EDITOR
        public FoundPackageInfo(UnityEditor.PackageManager.PackageInfo package, PackageMatchType matchType) {
            this.name = package.name;
            this.version = package.version;
            this.dependencies = package.dependencies.Select(x => new FoundDependencyInfo(x)).ToArray();
            this.matchType = matchType;
        }
#endif

        public override string ToString() {
            return $"{name}@{version}";
        }
    }
    
    [Serializable]
    public struct GitPackageInfo {
        /*[ReadOnly]*/ public string name;
        /*[ReadOnly]*/ public string version;
        
        public GitPackageInfo(string name, string version) {
            this.name = name;
            this.version = version;
        }

        public override string ToString() {
            if (string.IsNullOrEmpty(version)) {
                return name;
            }
            
            return $"{name}#{version}";
        }
    }
    
    [Serializable]
    public struct FoundDependencyInfo {
        /*[ReadOnly]*/ public string name;
        /*[ReadOnly]*/ public string version;
        
        public FoundDependencyInfo(string name, string version) {
            this.name = name;
            this.version = version;
        }
        
#if UNITY_EDITOR
        public FoundDependencyInfo(UnityEditor.PackageManager.DependencyInfo dependency) {
            this.name = dependency.name;
            this.version = dependency.version;
        }
#endif
    }

    public enum PackageMatchType {
        Exact,
        Possible,
        Improbable
    }
}