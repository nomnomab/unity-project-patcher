This is a BepInEx plugin, so grab BepInEx 5 for Mono from https://docs.bepinex.dev/articles/user_guide/installation/index.html
and follow the installation guide. We need BepInEx to load up a custom plugin so we can run some game code to scrub all the needed guids and paths.

After setting up BepInEx, put this plugin inside of `BepInEx > plugins`.

> If you want to see the game console, open `BepInEx > configs > BepInEx.cfg` and enable:
> ```cfg
> [Logging.Console]
> ## Enables showing a console for log output.
> # Setting type: Boolean
> # Default value: false
> Enabled = true
> ```

When the game runs, it will take awhile to show the game, as it instantly starts grabbing all the guids and paths for the game.

If you have the console enabled from above, it will show the things being worked on.

The files get output into `[GAME_NAME] > [GAME_NAME]_Data > Addressables_Rip`. The project generator will read them from this location, so don't move them!