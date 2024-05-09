using System;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AfterStepAttribute: Attribute {
        public readonly Type Type;
        
        public AfterStepAttribute(Type type) {
            Type = type;
        }
    }
}