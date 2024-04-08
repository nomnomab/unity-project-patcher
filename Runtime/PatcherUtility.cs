namespace Nomnom.UnityProjectPatcher {
    public static class PatcherUtility {
        public static void SetDirty(UnityEngine.Object obj) {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(obj);
#endif
        }
    }
}