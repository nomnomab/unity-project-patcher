using System;
using System.Linq;

namespace Nomnom.UnityProjectPatcher.UnityPackages {
    [Serializable]
    public struct FoundPackageInfo {
        /*[ReadOnly]*/ public string name;
        /*[ReadOnly]*/ public string version;
#if UNITY_2020_3_OR_NEWER
        /*[ReadOnly]*/ public FoundDependencyInfo[]? dependencies;
#else
        /*[ReadOnly]*/ public FoundDependencyInfo[] dependencies;
#endif
        /*[ReadOnly]*/ public PackageMatchType matchType;
        
#if UNITY_2020_3_OR_NEWER
        public FoundPackageInfo(string name, string version, FoundDependencyInfo[]? dependencies, PackageMatchType matchType) {
#else
        public FoundPackageInfo(string name, string version, FoundDependencyInfo[] dependencies, PackageMatchType matchType) {
#endif
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