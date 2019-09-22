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
* **Either** Sybaris ([Noct's AIO](https://custommaid3d2.com/index.php?downloads/noctsouls-sybaris-for-com3d2.63/) recommended) **or** [COM3D2.BepInEx.AIO](https://github.com/NeighTools/COM3D2.BepInEx.AIO)
* [*Optional*] COM3D2 English version if you want to get the official English translations

## Installing

> ⚠️ **IMPORTANT** ⚠️
> If you have ForceEnglishUI (`COM3D2.ForceEnglishUI.Patcher.dll` and `COM3D2.ForceEnglishUI.Managed.dll`) installed, **you must delete it** 
> because this tool is meant to be a replacement for it.
>  
> In addition, it's *encouraged* to delete YATranslator as well, since this tool can translate
> entirety of the game.  
> If you need to translate parts of the game that this tool can't, it's suggested to use
> [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator/) as it shouldn't cause any
> translation mixups compared to `CM3D2.AutoTranslator.Plugin.dll`.

1. Download the latest version from the [releases](https://github.com/denikson/COM3D2.i18nEx/releases). Pick either the `_Sybaris.zip` or `_BepInEx.zip` depending on which loader you have.

2. After downloading, put the contents of the archive into the game folder (i.e. where `COM3D2.exe` is located).  

3. Start the game. 

4. Open the game settings and change the settings as follows:
    
    * `System language` to `English`
    * `Display language` to `English and Japanese`

4. (*Optional*) Open `i18nEx\configuration.ini` to view all available options. Edit them if needed.

#### I don't want to see both English and Japanese text! 

As of version 1.2.0.0, i18nEx allows you to selectively use Japanese text in places where the English translation is missing. 

1. Open `i18nEx\configuration.ini`, find and set `RerouteTranslationsTo` to `RouteToEnglish`.
2.  In the game, set `Display language` to `English`.

## Installing translations

#### If you an end-user who has downloaded prepackaged translations

1. Create a folder in `i18nEx` folder with the name of the language (e.g. `English`)
2. Extract the obtained translations into the created folder. **Follow the instructions included with the translations, if there are any!**  
    If you extracted everything correctly, your translation folder should have *only the following folders*
    
    * `Script`
    * `Textures`
    * `UI`
3. Open `i18nEx/configuration.ini` in Notepad (or other text editor of your choice). Set `Language` option to the name of the folder you created in `i18nEx` folder. 
4. Save the configuration file and run the game. The plugin will now use your translations. 

#### If you are editing or creating translations 

i18nEx allows to store translations in separate folders.

Create a folder with the language name you want in `i18nEx` folder.  
For example, you can add English translations by creating `English` folder in `i18nEx`.

Inside the created folder, create the following subfolders:

* `Script` -- you can place translations for scripts here. More info in [translation details](https://github.com/denikson/COM3D2.i18nEx/wiki/How-to-translate).
* `UI` -- you can place translations for UI texts here. More info in [translation details](https://github.com/denikson/COM3D2.i18nEx/wiki/How-to-translate).
* `Textures` -- you can place textures here as .PNG files. More info in [translation details](https://github.com/denikson/COM3D2.i18nEx/wiki/How-to-translate).

## Extracting translations from the English game

**IMPORTANT:** If you want to extract English translations, you must do this process on the English COM3D2.  
If you do this in the Japanese game, you will only get untranslated story which you will need to translate manually 
(if you're a translator, this is probably what you want).  

1. Download and install [COM3D2.BepInEx.AIO](https://github.com/NeighTools/COM3D2.BepInEx.AIO) as per requirements.
    * You can use Sybaris and any of its AIOs as well. However in that case you need to **make sure i18nEx** is disabled. To do that, remove `COM3D2.i18nEx.Patcher.dll` and `COM3D2.i18nEx.Managed.dll` from `Sybaris` folder.

2. Download `EngExtract.dll` from [releases](https://github.com/denikson/COM3D2.i18nEx/releases) and place it into `Sybaris\UnityInjector` folder.  

3. Open the game and **wait for it to load**.

**If you have i18nEx installed in the game, disable the plug-in!**
**Make sure you have the game's command line open so you can see the progress.** 

4. When you're in the main menu, **press <kbd>D</kbd>** to open the dumping UI.

5. Press `Dump!` button. **You do not need to change any options unless you want to dump untranslated text too!**  
After you press the dump button, observe the console output to see progress. It may take up to 5 minutes to dump everything.

6. When it's done, close the game. You should now see folder named `COM3D2_Localisation` appearing in your game's directory.  

7. From `COM3D2_Localisation`, take `Script` and `UI` folders and copy them over into `i18nEx/English` of your Japanese game. 

8. **Remove `EngExtract.dll from your English game when you're done extracting all the text!**

9. Launch the Japanese game; if you have done everything correctly, you will now have i18nEx load the English translations into the game!

> **NOTE:**
> Not all content from JP game has been translated to English.  
> Owing to that, make sure you set `Display language` to `English and Japanese` in your game settings, 
> so that you will be able to get Japanese text in places where there are no translations.  
> 
> If you want to translate other Japanese elements, it's encouraged to install [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator/).

## Configuration

The configuration file is located in `i18nEx/configuration.ini`. Each configuration option is documented inside the configuration file directly.

## More info

For information on how to translate the game, visit the [wiki](https://github.com/denikson/COM3D2.i18nEx/wiki/How-to-translate).
