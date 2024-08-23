# 1.1.16

- Added possible fix for long path names on Windows for File reads and writes

# 1.1.15

- Updated package upgrade message to prompt the user to open the tool window for upgrades

# 1.1.14

- Better messages for dialogues
- Configs menu for easier asset focusing
- Added configs to `Tools > Unity Project Patcher > Configs`
- Won't show log for package not needing an upgrade

# 1.1.13

- Added asset replacement utility
- Added option to update packages in tool window itself
- Added a popup if a step fails
- Fixed package installer breaking with git packages

# 1.1.12

- Added version checker in tool window opening event

# 1.1.11

- Migrate materials step

# 1.1.10

- Can define a custom game name if required in the `UPPatcherSettings`
- Older Unity version support for various code blocks

# 1.1.9

- Fixed Project to Project migration not doing the full process
- Removed many warnings related to nullables

# 1.1.8

- Fixed game wrapper version not being found

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