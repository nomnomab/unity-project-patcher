# 1.1.7

- Fixed `UPPatcherUserSettings` using relative project path as game path, and no longer uses Path.GetFullPath

# 1.1.6

- Added extra help text to `UPPatcherSettings`
- Added `RestartEditorStep` to end of default steps
- Added `Install BepInEx` button to tool window
  - `UPPatcher` attribute can require BepInEx now
  - Will automatically add the `ENABLE_BEPINEX` compile flag
- Added an Enable/Disable button for BepInEx after installation
- Fixed `RenameAnimatorParamatersStep` not renaming `m_ConditionEvent`
- Fixed various building issues when building asset bundles
- Updated `README_TEMPLATE.md`

# 1.1.5

- Added scene saving dialogue when pressing the patch button
- Fixed New Input System enabling not restarting if enabled via package step

# 1.1.4

- Project to project migration tool
- Rename animator paramaters step
- Updated `README.md`
- Updated `README_TEMPLATE.md`

# 1.1.3

- Fixed issue with unsafe code enabling not restarting the editor
- Proxy script step

# 1.1.2

- Fixed asset ripper + output path not being relative paths
- Fixed null check when no game wrapper is available
- User settings ScriptableObject

# 1.1.1
- 
- Better handling of steps across editor restarts!
- ScriptableObject sorting
- Prefab sorting

# 1.1.0

- Step pipeline workflow
- Default steps in pipeline
- Basic step list validator
- Final steps list printing via window button
- Slightly faster project scrubbing
- Readme.md generator

# 1.0.0

- Initial version