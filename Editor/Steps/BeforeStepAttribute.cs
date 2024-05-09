using System;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class BeforeStepAttribute: Attribute {
        public readonly Type Type;
        
        public BeforeStepAttribute(Type type) {
            Type = type;
        }
    }
}