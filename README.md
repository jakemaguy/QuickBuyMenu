# Quick Buy Menu - Lethal Company Mod

With quick buy menu, you can buy handheld items in Lethal Company in a faster and easier way. No more wasting time for the Drop Ship to show up. This mod lets you instantly purchase items and put them in your inventory.


## Features

- Adds a couple new chat commands to Lethal Company
	- `/quickbuy <item-name> <optional: item quantity>`
    	- Alternative Abbreviations:
    		- `qb`
    		- `buy`
    		- `qbuy`
        - Automatically deducts the item cost from your credits and adds the item to your inventory.
        - Compatible with [Reserved Slot Item](https://thunderstore.io/c/lethal-company/p/FlipMods/ReservedItemSlotCore/) related Mods
    - `/pricecheck <item-name> <optional: item quantity>`
    	- Alternative Abbreviations:
    		- `qc`
    		- `inquire`
    		- `check`
    		- `pc`
		- Quickly Show the price of an order and it tells you if you can afford it.

## Usage

- Press `/` to start typing in chat
- Type `/quickbuy <item-name> <optional: quantity>` (or alternative abbreviations), and press enter
	- The item name doesn't have to be the entire name.  It can be a substring.
		- Examples: 
			- `/quickbuy shov` will purchase a shovel
			- `/quickbuy walk` will purchase a walkie-talkie
- Since version ```1.1.0```, you can purchase multiple items by specifying an optional quantity argument
   - Usage: ```/quickbuy <Item Name> <optional: quantity>```
        - Examples: 
			- `/quickbuy 3 shov` will purchase 3 shovels
			- `/quickbuy 4 walk` will purchase 4 walkie talkies


## Installation - Thunderstore

Download with the Thunderstore Mod Manager: [Quick Buy Menu](https://thunderstore.io/c/lethal-company/p/befuddled_productions/Quick_Buy_Menu/)

## Installation - Manual

- Download the following Prerequisite Mods into your `BepInEx\plugins\` folder
	- [LC_API_V50](https://thunderstore.io/c/lethal-company/p/DrFeederino/LC_API_V50/)
	- [Lethal Network Api](https://github.com/Xilophor/LethalNetworkAPI)
	- [CSync](https://thunderstore.io/c/lethal-company/p/Owen3H/CSync/)
- Download the latest release from the Releases page
- Copy the `QuickBuyMenu.dll` to your `BepInEx\plugins\` folder

# Contributing

PR's and suggestions are welcome.

## Build Instructions

- Set an environment variable `LETHAL_COMPANY_DIR` that points to where Lethal company is installed.
    - example `LETHAL_COMPANY_DIR=C:\Program Files (x86)\Steam\steamapps\common\Lethal Company`
- Clone the repository to your local machine.
- Download the PreRequisite Mod DLL's to your `QuickBuyMenu\DLL` folder:
	- [LC_API_V50](https://thunderstore.io/c/lethal-company/p/DrFeederino/LC_API_V50/)
	- [Lethal Network Api](https://github.com/Xilophor/LethalNetworkAPI)
	- [CSync](https://thunderstore.io/c/lethal-company/p/Owen3H/CSync/)
- Build with visual Studio

## Issues

If you encounter any bugs, errors, or feature requests, please open an issue on the [issue tracker](https://github.com/jakemaguy/QuickBuyMenu/issues). Before creating a new issue, please check if there is already an existing one that addresses your problem. When creating an issue, please provide as much information as possible to help resolve it.

## Credits

- Lethal Company is developed by [Zeekerss](https://twitter.com/ZeekerssRBLX)
- Quick Buy Menu is created by Jake Maguy (jakemaguy@gmail.com)
- Thanks to modding-community for providing helpful resources and tutorials
