using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct ChangeGameViewResolutionStep: IPatcherStep {
        private readonly string? _size;
        
        public ChangeGameViewResolutionStep(string? size) {
            _size = size;
        }
        
        public UniTask<StepResult> Run() {
            if (_size is null) {
                Debug.LogWarning("GameView resolution not set");
                return UniTask.FromResult(StepResult.Success);
            }
            
            if (GameViewUtils.TrySetSize(_size)) {
                Debug.Log($"GameView resolution set to {_size}");
            } else {
                Debug.LogWarning($"Could not set GameView resolution to {_size}");
            }
            
            return UniTask.FromResult(StepResult.Success);
        }
    }
}