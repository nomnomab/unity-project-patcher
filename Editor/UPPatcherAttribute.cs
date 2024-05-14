using System;

namespace Nomnom.UnityProjectPatcher.Editor {
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UPPatcherAttribute: Attribute {
        public readonly bool RequiresBepInEx;
        
        public UPPatcherAttribute(bool requiresBepInEx = false) {
            RequiresBepInEx = requiresBepInEx;
        }
    }
}