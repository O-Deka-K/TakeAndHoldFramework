# Take & Hold Framework
A BepInEx plugin for H3VR that allows for adding custom characters to the Take & Hold gamemode, as well as a few quality-of-life features.

## Features
- Adds support for custom characters using YAML, with legacy support for TNH Tweaker/Deli-based custom characters.
- Features a replacement for Magazine Patcher. No longer will you have crashes due to using too much RAM while loading.
- Institution support.
- Bugs I have yet to find.

## Installation
Install via the [r2modman](https://thunderstore.io/c/h3vr/p/ebkr/r2modman/) mod manager.

## Character Creation
[This guide](https://docs.google.com/document/d/1j92RENR0DX1t_81b4gsZzou_FpKNPUTj9CVgfmesqe0/edit?usp=sharing) should cover creating a custom character. Though hopefully this repository will have creating custom character information.

## Building
Create a new folder inside of your main H3VR install, then place this repository's folder inside of that. Your file structure should look something like, say... `(Steam path)/common/H3VR/(Repos)/TNHFramework/TNHFramework.sln`.
Next, create a mod manager profile called 'Dev' and install the prerequisite mods.
(to-do: create a mod profile file that allows people to import all necessary mods)