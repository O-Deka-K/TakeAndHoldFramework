# Take & Hold Framework
A successor to DevynDaMonster's [TNH Tweaker](https://thunderstore.io/c/h3vr/p/devyndamonster/TakeAndHoldTweaker/), TNH Framework aims to act as a base for anyone who wants to implement custom content for [Hot Dogs, Horseshoes & Hand Grenades](https://store.steampowered.com/app/450540/Hot_Dogs_Horseshoes__Hand_Grenades/)'s Take & Hold gamemode.

## H3VR Build Version
This is for H3VR build 119, which is currently on Main branch (as of Dec 2025). It will NOT work on Experimental branch, as there are many incompatible changes. There will be a separate version of TNH Framework for Experimental branch (and later 1.0).

## What is this?
This is a successor to TNH Tweaker. It is forked from the original code, and has additions like Institution support and a new character selection UI. It also has a lot of bugfixes compared to TNH Tweaker. It's up to date with the latest changes in build 119.

As Devyn is no longer working on H3VR mods, this is will be more up-to-date than TNH Tweaker.

## I still have Take & Hold Tweaker installed, what should I do?
Short version: Leave it installed. TNH Framework will handle everything for you.
Long version:
TNH Framework has a patcher included that will modify TNH Tweaker's files. If you want to disable TNH Framework and just run TNH Tweaker, you should disable TNH Framework, and then disable TNH Tweaker and re-enable it again.

## Features
As of right now, this mod does the following:
- Adds support for custom characters using YAML, with legacy support for TNH Tweaker/Deli-based custom characters.
- Optional replacement for Magazine Patcher. No longer will you have crashes due to using too much RAM while loading.
- Full Institution support, with options to disable each type of construct separately.
- TNH Tweaker bugfixes.
- Vanilla game bugfixes.
- Legacy mod fixes.
- Backported features from Experimetal build.
- Global blacklist for any type of item.

## Options
There are two ways to access the TNH Framework options:
1. In r2modman, go to Config editor > h3vr.tnhframework.cfg > Edit Config.
2. In game, spawn the mod panel using your wrist menu. Go to Plugins > TNHFramework.

## Installation
Install via the [r2modman](https://thunderstore.io/c/h3vr/p/ebkr/r2modman/) mod manager.

## Character Creation
[This guide](https://docs.google.com/document/d/1j92RENR0DX1t_81b4gsZzou_FpKNPUTj9CVgfmesqe0/edit?usp=sharing) should cover creating a custom character. Though hopefully this repository will have creating custom character information.

You can also refer to Devyn's [original TNH Tweaker guide](https://github.com/devyndamonster/TakeAndHoldTweaker/wiki).

## Credits
- **O-Deka-K** - Main code contributor.
- **APintOfGravy** - Original author (of the fork), code contributor.
- **Ethiom101** - Text editor, general nuisance, corruptor of builds.
- **DevynDaMonster** - Creator of the original Take & Hold Tweaker.
