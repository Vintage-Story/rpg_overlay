# RPG Overlay
Add level after the entity name showing their power, the level is calculated based on entity damage and max health, works perfect with [RPG Difficulty](https://mods.vintagestory.at/rpgdifficulty) but is not necessary

If [LevelUP](https://mods.vintagestory.at/levelup) is enabled the mod will also create a custom level "Global" that will increase every time the player gets experience, and the level will be show after the player name

If [RPG Difficulty](https://mods.vintagestory.at/rpgdifficulty) is enabled and the region system is enabled the region name and level will be displayed once you across the region

# About RPG Overlay
RPG Overlay is open source project and can easily be accessed on the github, all contents from this mod is completly free.

If you want to contribute into the project you can access the project github and make your pull request.

You are free to fork the project and make your own version of RPG Overlay, as long the name is changed.

# Building
- Install .NET in your system, open terminal type: ``dotnet new install VintageStory.Mod.Templates``
- Create a template with the name ``RPGOverlay``: ``dotnet new vsmod --AddSolutionFile -o RPGOverlay``
- [Clone the repository](https://github.com/LeandroTheDev/rpg_overlay/archive/refs/heads/main.zip)
- Copy the ``CakeBuild`` and ``build.ps1`` or ``build.sh`` and paste inside the repository

Now you can build using the ``build.ps1`` or ``build.sh`` file

FTM License
