# Take & Hold Framework

A successor to DevynDaMonster's [TNHTweaker](https://thunderstore.io/c/h3vr/p/devyndamonster/TakeAndHoldTweaker/), TNHFramework aims to act as a base for anyone who wants to implement custom content for [Hot Dogs, Horseshoes & Hand Grenades](https://store.steampowered.com/app/450540/Hot_Dogs_Horseshoes__Hand_Grenades/)'s Take & Hold gamemode.

## NEW

- You can disable each type of construct (like the floating mines) in Institution separately. Check the TNHFramework options. Details on the [Wiki](https://thunderstore.io/c/h3vr/p/ISD/TNHFramework/wiki/).

- I've added ODekaK-MagazinePatcher as a dependency because not enough people know that it fixes the "0% Caching Items" bug.
  - It will automatically replace devyndamonster-MagazinePatcher. You don't have to do anything in r2modman.
  - If you want, you can disable MagazinePatcher in r2modman and it will use the TNHFramework internal mag patcher. It's not as thorough, but it eats less RAM while caching and it's fast.

## H3VR Build Version

This is for H3VR build 119, which is currently on Main branch and Alpha branch (as of Dec 2025). It will probably continue to work for previous builds. It will NOT work on Experimental branch, as there are many incompatible changes. There will be a separate version of TNHFramework for Experimental branch (and later 1.0).

## What is this?

This is the successor to TNHTweaker. It is forked from the original code, and has additions like Institution support and a new character selection UI. It also has a lot of bugfixes compared to TNHTweaker, and is up to date with the latest changes in build 119.

As Devyn is no longer working on H3VR mods, this is will be more up-to-date than TNHTweaker.

## I still have Take & Hold Tweaker installed, what should I do?

Short version:
- Leave it installed. TNHFramework will handle everything for you.

Long version:
- TNHFramework has a patcher included that will modify TNHTweaker's files. If you want to disable TNHFramework and just run TNHTweaker, you should disable TNHFramework, and then disable TNHTweaker and re-enable it again.

## Features

- Disables TNHTweaker automatically. Just leave it installed and let it be!
- Adds support for custom characters using YAML, with legacy support for TNHTweaker/Deli-based custom characters.
- Optional replacement for Magazine Patcher. No longer will you have crashes due to using too much RAM while loading.
- Full Institution support, with options to disable each type of construct separately.
- Backported features from Experimental build.
- Global blacklist for any type of item.
- TNHTweaker bugfixes.
- Vanilla game bugfixes.
- Legacy mod fixes.
- This mod has been tested with EVERY custom character on Thunderstore.

Please see the CHANGELOG for the full list of changes from TNHTweaker.

## Options

There are two ways to access the TNHFramework options:
1. In r2modman, go to Config editor > h3vr.tnhframework.cfg > Edit Config.
2. In game, spawn the mod panel using your wrist menu. Go to Plugins > TNHFramework.

## Wiki

See the [Wiki](https://thunderstore.io/c/h3vr/p/ISD/TNHFramework/wiki/) for more detailed information, including information on specific custom characters.

## Installation

Install via the [r2modman](https://thunderstore.io/c/h3vr/p/ebkr/r2modman/) mod manager.

## Character Creation

[This guide](https://docs.google.com/document/d/1j92RENR0DX1t_81b4gsZzou_FpKNPUTj9CVgfmesqe0/edit?usp=sharing) should cover creating a custom character. Though hopefully this repository will have creating custom character information.

You can also refer to Devyn's [original TNHTweaker guide](https://github.com/devyndamonster/TakeAndHoldTweaker/wiki).

## Source Code

The code will remain free and open at GitHub:
https://github.com/O-Deka-K/TakeAndHoldFramework

## Credits

- **O-Deka-K** - Main code contributor.
- **APintOfGravy** - Original author (of this fork), major code contributor.
- **Ethiom101** - Text editor, general nuisance, corruptor of builds.
- **DevynDaMonster** - Creator of the original Take & Hold Tweaker.
- **Alexine** - Tester and idea machine.
