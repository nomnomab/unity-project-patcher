<div align="center">
  <h1>$GAME_NAME$ Project Patcher</h1>

  <p>
    A game wrapper that generates a Unity project from $GAME_NAME$'s build that can be playable in-editor
  </p>
</div>

<div align="center">
<!-- Badges -->

<span></span>
<a href="https://github.com/nomnomab/unity-project-patcher">Unity Project Patcher</a>
<span> · </span>
<a href="https://github.com/nomnomab/unity-project-patcher/issues/">Report Bug</a>
<span> · </span>
<a href="https://github.com/nomnomab/unity-project-patcher/issues/">Request Feature</a>
</h4>

</div>

<br />

<!-- Table of Contents -->
# Table of Contents

- [About the Project](#about-the-project)
- [Getting Started](#getting-started)
    * [Prerequisites](#prerequisites)
    * [Installation](#installation)
- [Usage](#usage)
- [FAQ](#faq)

<!-- About the Project -->
## About the Project
This tool is a game wrapper on top of the [Unity Project Patcher](https://github.com/nomnomab/unity-project-patcher).

This takes a build of $GAME_NAME$, extracts its assets/scripts/etc, and then generates a project for usage in the Unity editor.

> [!IMPORTANT]  
> This tool does not distribute game files. It simply works off of your copy of the game!
>
> Also, this tool is for **personal** use only. Do not re-distrubute game files to others.

<!-- Getting Started -->
## Getting Started

### Already Bundled

This project is bundled with the following packages:

- Insert text if needed or remove section

### Gets Downloaded

- Insert text if needed or remove section

<!-- Prerequisites -->
### Prerequisites

You will have to make sure you have the following before using the tool in any way:

- [Git](https://git-scm.com/download/win)
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
    - To run Asset Ripper
- [Unity Project Patcher BepInEx](https://github.com/nomnomab/unity-project-patcher-bepinex)
    - [Can be disabled](#disabling-bepinex-usage)

These prerequisites may or may not be already included in your Unity version:

- [Newtonsoft Json](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html): `com.unity.nuget.newtonsoft-json`

<!-- Installation -->
## Installation

### Unity Project

- Requires [Unity %UNITY_VERSION%](https://unity.com/releases/editor/whats-new/%UNITY_VERSION%)
- Unity %UNITY_PIPELINE% pipeline

Create a new Unity project with the above requirements before getting started.

> [!IMPORTANT]  
> These options require [git](https://git-scm.com/download/win) to be installed!

### Installing the Unity Project Patcher core

Install with the package manager:

1. Open the Package Manager from `Window > Package Manager`
2. Click the '+' button in the top-left of the window
3. Click 'Add package from git URL'
4. Provide the URL of the this git repository: https://github.com/nomnomab/unity-project-patcher.git
    - If you are using a specific version, you can append it to the end of the git URL, such as `#v1.2.3`
5. Click the 'add' button


Install with the manifest.json:

1. Open the manifest at `[PROJECT_NAME]\Packages\manifest.json`
2. Insert the following as an entry:

```json
"com.nomnom.unity-project-patcher": "https://github.com/nomnomab/unity-project-patcher.git"
```

- If you are using a specific version, you can append it to the end of the git URL, such as `#v1.2.3`

### Installing the BepInEx Wrapper

If you require BepInEx usage, then follow the instructions at https://github.com/nomnomab/unity-project-patcher-bepinex

#### Disabling BepInEx Usage

If you don't want to use plugins, then follow the steps at https://github.com/nomnomab/unity-project-patcher-bepinex#disabling-this-package

### Installing this Game Wrapper

The same steps as previously, just with `https://github.com/[AUTHOR]/[GAME-WRAPPER-NAME].git`

<!-- Usage -->
## Usage

The tool window can be opened via `Tools > Unity Project Patcher > Open Window`

> [!IMPORTANT]  
> This tool mostly supports patching an already patched project, although this can lead to broken assets.
> So make sure you back up your project beforehand.

Estimated patch durations:

- Fresh patch: $ESTIMATED_DURATION$
- Already patched: $ESTIMATED_DURATION$

## FAQ

The core project's FAQ can be found here: https://github.com/nomnomab/unity-project-patcher#faq

<br/>

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/B0B6R2Z9U)
