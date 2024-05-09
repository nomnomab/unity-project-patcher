using System;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RequiresStepAttribute: Attribute {
        public readonly Type Type;
        public readonly StepPrecedence Precedence;
        
        public RequiresStepAttribute(Type type, StepPrecedence precedence) {
            Type = type;
            Precedence = precedence;
        }
    }
    
    public enum StepPrecedence {
        Before = 0,
        After = 1
    }
}