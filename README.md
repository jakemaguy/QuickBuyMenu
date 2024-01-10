# Quick Buy Menu - Lethal Company Mod

With quick buy menu, you can buy handheld items in Lethal Company in a faster and easier way. No more wasting time for the Drop Ship to show up. This mod lets you instantly get an item from the Terminal and put it in your inventory.

<p align="center">
<img src="https://github.com/jakemaguy/QuickBuyMenu/assets/36657294/d5ddcbde-15cd-4fb5-ac9a-079084b7576f" width="70%">
</p>

## Features

- Adds a new option to the Terminal menu: `quickbuy`
	- Alternative Abbreviations:
		- `qb`
		- `quickb`
		- `qbuy`
- Allows you to select from a list of available handheld items, similar to the vanilla STORE page
- Automatically deducts the item cost from your credits and adds the item to your inventory
- Compatible with [Reserved Slot Item](https://thunderstore.io/c/lethal-company/p/FlipMods/ReservedItemSlotCore/) related Mods

## Usage

- Open the Terminal in the game
- Type the `quickbuy` option from the menu (or alternative abbreviations)
- Purchase an Item from the list with the command: `buy (Item Name)`
	- The item name doesn't have to be the entire name.  It can be a substring.
		- Examples: 
			- `buy shov` will purchase a shovel
			- `buy walk` will purchase a walkie-talkie
- Enjoy your new item!


## Installation - Thunderstore

Download with the Thunderstore Mod Manager: [Quick Buy Menu](https://thunderstore.io/c/lethal-company/p/befuddled_productions/Quick_Buy_Menu/)

## Installation - Manual

- Download the following Prerequisite Mods into your `BepInEx\plugins\` folder
	- [Terminal Api](https://thunderstore.io/c/lethal-company/p/NotAtomicBomb/TerminalApi/)
	- [Simple Command API](https://thunderstore.io/c/lethal-company/p/XDev/SimpleCommandAPI/)
- Download the latest release from the Releases page
- Copy the `QuickBuyMenu.dll` and `QuickBuyMenuAssets` files to your `BepInEx\plugins\` folder

# Contributing

PR's and suggestions are welcome.

## Build Instructions

- Install the [Unity Netcode Patcher](https://github.com/EvaisaDev/UnityNetcodePatcher) via the dotnet cli: `dotnet tool install -g Evaisa.NetcodePatcher.Cli`
	- If you already have it installed, make sure you upgrade to the latest version: `dotnet tool update -g Evaisa.NetcodePatcher.Cli`
- Clone the repository and clone it to your local machine.
- Download the PreRequisite Mod DLL's to your `QuickBuyMenu\DLL` folder:
	- [Terminal Api](https://thunderstore.io/c/lethal-company/p/NotAtomicBomb/TerminalApi/)
	- [Simple Command API](https://thunderstore.io/c/lethal-company/p/XDev/SimpleCommandAPI/)
- Copy all of the Lethal Company DLL files from `C:\Program Files (x86)\Steam\steamapps\common\Lethal Company\Lethal Company_Data\Managed\` into  the `QuickBuyMenu\DLL` folder.
- Build with visual Studio using the `Debug` configuration
	- We are using Debug, not Release, because the Netcode Patcher requires the `.pdb` files in order to work.

## Issues

If you encounter any bugs, errors, or feature requests, please open an issue on the [issue tracker](https://github.com/jakemaguy/QuickBuyMenu/issues). Before creating a new issue, please check if there is already an existing one that addresses your problem. When creating an issue, please provide as much information as possible to help resolve it.

## Pull Requests

If you want to contribute code or documentation to the project, you are welcome to submit a pull request. Please follow these steps to create a pull request:

- Make sure to review the Build Instructions.  
	- Ensure you have the latest version of the Unity Netcode Patcher Installed.
- Fork the repository and clone it to your local machine.
- Create a new branch from the main branch with a descriptive name.
- Make your changes in the new branch.
- Test out your changes in game.
- Commit your changes with a clear and concise message, referencing any issues that your pull request fixes or relates to.
- Push your branch to your forked repository and create a pull request from it to the main branch of the original repository.
- Wait for the project maintainer or a reviewer to review your pull request and provide feedback or approval.
- If requested, make any necessary changes and update your pull request.
- Once your pull request is merged, delete your branch and sync your fork with the upstream repository.

# Changelog

All notable changes to this project will be documented in this section.

## [1.0.3] - 01/10/2024

- Fixed [github issue #6](https://github.com/jakemaguy/QuickBuyMenu/issues/6) where single handed items bought from Quick Buy were overlapping if you bought multiple items consecutively, without exiting the terminal.

## [1.0.2] - 01/10/2024

- Fixed [github issue #5](https://github.com/jakemaguy/QuickBuyMenu/issues/5) where hyphens `-` weren't parsed correctly.

## [1.0.1] - 01/09/2024

- Change terminal text from `buy (Item Number)` to `buy (Item Name)`
- Changed thunderstore Icon

## [1.0.0] - 01/08/2024

- Initial release of the mod

## Credits

- Lethal Company is developed by [Zeekerss](https://twitter.com/ZeekerssRBLX)
- Quick Buy Menu is created by Jake Maguy (jakemaguy@gmail.com)
- Thanks to modding-community for providing helpful resources and tutorials
