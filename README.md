# Siren Head (for Lethal Company)

Adds [Trevor Henderson](https://trevorhenderson.format.com/)'s Siren Head to Lethal Company, with support for v55/56. Available to download on [Thunderstore](https://thunderstore.io/c/lethal-company/p/Ccode_lang/SirenHead/).

# Configuration

You can customise the way Siren Head functions using the configuration options, as shown below.
* AI Start Animation: Which animation Siren Head will spawn in. Options: `tree`, `standard`, `random`
* Siren Head Walk Speed: Walking speed of Siren Head. Options: float value, default `3.5`
* Siren Head Run Speed: Running speed of Siren Head. Options: float value, default `7.0`
* Moons: Moons that Siren Head will spawn on. Format as: `MoonName:SpawnWeight`, for example `VowLevel:50` or `VowLevel:100;MarchLevel:100`. To spawn on all moons, `All:SpawnWeight` (includes modded moons). See below for more options.

## Moons

Uses [LethalLib.Modules.Levels.LevelTypes](https://github.com/EvaisaDev/LethalLib/blob/main/LethalLib/Modules/Levels.cs#L15):

`ExperimentationLevel`, `AssuranceLevel`, `VowLevel`, `OffenseLevel`, `MarchLevel`, `RendLevel`, `DineLevel`, `TitanLevel`, `AdamanceLevel`, `ArtificeLevel`, `EmbrionLevel`, `Vanilla`: only the moons included with the base game, `Modded`: only modded moons, `All`: all possible moons in game.

# Credits

* Model from [Siren Head by wollabebes on Sketchfab](https://sketchfab.com/3d-models/siren-head-e2a7eca5bc814b6ca5b2d2e85f478360)
* Alert sounds: @birdekek and @spinmaster6701 on Discord
* Animation: @dotflare on Discord
* Step sounds: @spinmaster6701 on Discord
* Walkie sounds: @spinmaster6701 on Discord
* Terminal entry: @starstruck1234 and @dotflare on Discord

The assets used in the mod can be found [here in this GitHub repository](https://github.com/Ccode-lang/LethalCompanyUnityTemplate-SirenHead).