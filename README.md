# COM3D2.i18nEx -- Internationalization Extensible

This plug-in allows use of COM3D2's localization features in order to translate the game into multiple languages.

Currently, this plug-in allows you to translate the following game elements:

* Compatible UI (as long as it's marked as localizable by the game)
* Textures (by allowing to replace textures the same way as [YetAnotherTranslator](https://github.com/denikson/CM3D2.YATranslator) does)
* Story scripts, yotogi subtitles and other [compatible items]([#translation-details](https://github.com/denikson/COM3D2.i18nEx/wiki/How-to-translate)).

This plug-in allows lightweight and simple way to use official English translations in the Japanese game 
**and** manually fixing translations in the English game. 

## Requirements

You will need these:

* COM3D2 build version 1.32 or newer
* Sybaris ([Noct's AIO](https://custommaid3d2.com/index.php?downloads/noctsouls-sybaris-for-com3d2.63/)) **or** [BepInEx](https://github.com/NeighTools/COM3D2.BepInEx.AIO)
* [*Optional*] COM3D2 English version if you want to get the official English translations

## Installing

> ⚠️ **IMPORTANT** ⚠️
> Before installing, you **must** delete ForceEnglishUI (`COM3D2.ForceEnglishUI.Patcher.dll` and `COM3D2.ForceEnglishUI.Managed.dll`), 
> because this tool is meant to be a replacement for it.
>  
> In addition, it's *encouraged* to delete YATranslator as well, since this tool can translate
> all of the game.  
> If you need to translate parts of the game that this tool can't, it's suggested to use
> [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator/) as it shouldn't cause any
> translation mixups compared to `CM3D2.AutoTranslator.Plugin.dll`.

Download the latest version from the [releases](https://github.com/denikson/COM3D2.i18nEx/releases). Pick either the `_Sybaris.zip` or `_BepInEx.zip` depending on which loader you have.
If you have both, I suggest using BepInEx version.

After downloading, extract the **contents** downloaded archive into your game's directory (and not a folder inside the game's directory!).  
If it asks to overwrite some files, do so.

Open and `i18nEx\configuration.ini` to view all available options. Edit them if needed.

Finally, start the game. If everything went correctly, you should see `i18nEx` messages in the console.

**NOTE:** If you're running the JP version of the game (or ENG version with DLC from JP version), some events might not have English translations. As a result,
in some places the game's text box will simply be empty. To fix this do **either one** of the following:

* Go to game settings and set `Display language` to `English and Japanese`
* Open `i18nEx\configuration.ini`, find and set `InsertJapaneseTextIntoEnglishText` to `True`. In the game, set `Display language` to `English`.

## Installing translations

i18nEx allows to store translations in separate folders.

Simply create a folder with the language name you want in `i18nEx` folder.  
For example, you can add English translations by creating `English` folder in `i18nEx`.

Inside the created folder, create the following subfolders:

* `Script` -- you can place translations for scripts here. More info in [translation details](#translation-details).
* `UI` -- you can place translations for UI texts here. More info in [translation details](#translation-details).
* `Textures` -- you can place textures here as .PNG files. More info in [translation details](#translation-details).

## Extracting English translations

It is possible to extract the current text translations that are available in the game.  
For that, you will need Sybaris or the [COM3D2.BepInEx.AIO](https://github.com/NeighTools/COM3D2.BepInEx.AIO).

Download `EngExtract.dll` from [releases](https://github.com/denikson/COM3D2.i18nEx/releases) and place it into `Sybaris\UnityInjector` folder.  
Open the game and **wait for it to load**.

**If you have i18nEx installed in the game, disable the plug-in!**
**Make sure you have the game's command line open so you can see the progress.** 

When you're in the main menu, **press <kbd>D</kbd>** and observe the dumping progress.  
It might take a while for it to dump everything (up to 5 minutes), just wait until it reports that everything is dumped.

When it's done, close the game. You should now see folder named `COM3D2_Localisation` appearing in your game's directory.  
Inside it, you will find `Script` and `UI` folders that contain all localizations that you might want.
**Remove `EngExtract.dll` plug-in when you're done extracting all the text!**

Take those folders and move them into `i18nEx\English` folder.

Launch the game; if you have done everything correctly, you will now have i18nEx load the English translations into the game!

> **NOTE:**
> Not all content from JP game has been translated to English.  
> Owing to that, make sure you set `Display language` to `English and Japanese` in your game settings, 
> so that you will be able to get Japanese text in places where there are no translations.  
> 
> If you want to translate other Japanese elements, it's encouraged to install [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator/).

## More info

For information on how to translate the game, visit the [wiki](https://github.com/denikson/COM3D2.i18nEx/wiki/How-to-translate).