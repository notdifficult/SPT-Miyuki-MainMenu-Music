# MiyukiMainMenuMusic [SPT 4.0.13](https://www.sp-tarkov.com/) 
(This is [Escape from Tarkov](https://www.escapefromtarkov.com) [SPT Client Plugin](https://www.sp-tarkov.com/) )

> This simple mod adds music to the menu (without replacing the game music)

> I tried to make this mod universal, I think it will work for any game on UNITY3d where there is BepInEx.

> In the mod settings, you can try to adjust the allowed game scenes to your game


## Requirements for building
- [SPT](https://www.sp-tarkov.com/) **4.0.13** or compatible
- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (for building from source)
- Minimum [Rider](https://www.jetbrains.com/rider/) version required: 2024.3


## How to build
- Download `main` 
- Open `./MiyukiMainMenuMusic.sln` In `Rider`
- Press BuildSolution `F5` 
- Mod create >> `./!release/BepInEx/plugins/MiyukiMainMenuMusic/MiyukiMainMenuMusic.dll` <<


## Installation
- Build the project (see above) **or** download the latest release DLL.
- Copy folder`./!release/BepInEx/plugins/MiyukiMainMenuMusic` in EFT SPT Folder `./Escape from Tarkov`
- Launch SPT as usual.
- Press **F12** in-game to open the config manager and adjust settings.


## Mod Resources and info

| Resource | URL |
|---|---|
| BepInEx Configuration Docs | https://github.com/BepInEx/BepInEx.ConfigurationManager/blob/master/README.md|
| SPT Client Mod Examples | https://github.com/Jehree/SPTClientModExamples |
| SPT Wiki Modding Resources | https://wiki.sp-tarkov.com/modding/Modding_Resources |
