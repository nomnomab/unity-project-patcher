using System;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class BeforeStepAttribute: Attribute {
        public readonly string? Name;
        
        public BeforeStepAttribute(string? name) {
            Name = name;
        }
    }
    
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class AfterStepAttribute: Attribute {
        public readonly string? Name;
        
        public AfterStepAttribute(string? name) {
            Name = name;
        }
    }
}