using System.IO;

namespace Nomnom.UnityProjectPatcher {
    public static class PatcherUtility {
        public static void SetDirty(UnityEngine.Object obj) {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(obj);
#endif
        }
        
        public static string ToAssetDatabaseSafePath(this string path) {
            return path.Replace(Path.DirectorySeparatorChar, '/');
        }
        
        public static string ToOSPath(this string path) {
            return path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}