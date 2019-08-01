# COM3D2.i18nEx -- Internationalization Extensible

This plug-in allows use of COM3D2's localization features in order to translate the game into multiple languages.

Currently, this plug-in allows you to translate the following game elements:

* Compatible UI (as long as it's marked as localizable by the game)
* Textures (by allowing to replace textures the same way as [YetAnotherTranslator]() does)
* Story scripts, yotogi subtitles and other [compatible items](#translation-details).

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

Download the latest version from the [releases](). Pick either the `_Sybaris.zip` or `_BepInEx.zip` depending on which loader you have.
If you have both, I suggest using BepInEx version.

After downloading, extract the **contents** downloaded archive into your game's directory (and not a folder inside the game's directory!).  
If it asks to overwrite some files, do so.

Open and `i18nEx\configuration.ini` to view all available options. Edit them if needed.

Finally, start the game. If everything went correctly, you should see `i18nEx` messages in the console.

**NOTE:** If you're running the JP version of the game (or ENG version with DLC from JP version), open the game's settings 
and set `Display language` to `English and Japanese`. Otherwise you might get blank textboxes in some places.

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

Download `EngExtract.dll` from [releases]() and place it into `Sybaris\UnityInjector` folder.  
Open the game and **wait for it to load**.

**Make sure you have the game's command line open so you can see the progress**. 

When you're in the main menu, **press <kbd>D</kbd>** and observe the dumping progress.  
It might take a while for it to dump everything (up to 5 minutes), just wait until it reports that everything is dumped.

When it's done, close the game. You should now see folder named `COM3D2_Localisation` appearing in your game's directory.  
Inside it, you will find `Script` and `UI` folders that contain all localizations that you might want.

Take those folders and move them into `i18nEx\English` folder.

Launch the game; if you have done everything correctly, you will now have i18nEx load the English translations into the game!

> **NOTE:**
> Not all content from JP game has been translated to English.  
> Owing to that, make sure you set `Display language` to `English and Japanese` in your game settings, 
> so that you will be able to get Japanese text in places where there are no translations.  
> 
> If you want to translate other Japanese elements, it's encouraged to install [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator/).

## Translation details

**NOTE**: These details are meant for translators and other developers. You don't have to read these if you just want to use the translations.

i18nEx works by hooking in-game methods for parsing VN scripts and loading textures in order to provide translations. In addition, this plugin 
utilizes the game's own I2LocalizationManager to load UI translations. 
As a major benefit, loading and using translations is fast an efficient. However, this also means that this plug-in won't translate UIs and texts 
that the game itself doesn't mark as translatable. Because of that, you might still need to use much heavier tools like YATranslator and XUnity.AutoTranslator to achieve full game translation.

Multi-language translations are currently supported only in a very basic manner: you add a new language by creating a new folder in `i18nEx` 
using the name of the target language. Then, to enable said language, you edit the `i18nEx\configuration.ini` and set `ActiveLanguage` to the name of the folder that you want to load.

This plugin categorizes translation resources in thee ways: a resource can be a *script translation*, a *UI translation* or a *texture replacement*.

### Script translations

Script translations go into `i18nEx\<Language>\Script`. Script translations are applied to all in-game scripts where there might be text -- that is, all in-game events, yotogi subtitles and other management scenes.

All translations **must** be categorized by the name of the script that has the translatable items.  
For instance, if you want to translate text in `a1_job_0001.ks`, you must put these translations in `a1_job_0001.txt`.  
You can use tools like Sybaris ARC Editor to open game's script ARCs and look up the names of the scripts.  

The translations inside each text file are of the form `<ORIGINAL><TAB><TRANSLATED>`. Notice that original and translated lines 
are separated by a **tabulator**, not space. 
In some cases you might need to use escape sequences (`\n\t\r` etc.) to mark special characters (new line, tabulator, etc.).

The translation lines must be specified the way they are written in the original scripts, but with `"<E>"` replaced with a tabulator. 

Any lines that start with character `;` are ignored.

#### Getting raw strings
You can set `VerboseLogging` to `True` under `[ScriptTranslations]` section, which will cause i18nEx automatically 
log the names of the scripts and translation lines in the console.

Additionally, you can enable live dumping with `DumpUntranslatedLines` config option to automatically write untranslated lines into correct files.  

Finally, you can use [the translation dumper plugin](#extracting-english-translations) to dump all translated and untranslated lines.


### UI translations

UI translations are handled by I2LocalizationManager. With them you can translate some UI elements, like yotogi skills, yotogi commands and 
subtitles for songs. 

In `UI` folders, each **translation unit** must be put into a separate subfolder. For instance, for translating yotogi commands,
you need to create a subfolder `YotogiCommands`. The names of the translation units do not matter.

Inside each translation unit, all translations are separated into categories and terms. Every category is it's own `.csv` file and all terms 
inside the category are defined in the `.csv` file on each row.  
The game looks up each term using path-like notation, for example `Config/VR/VR空間優先`. This notation is translated as follows:

* The "deepest" part of the term (i.e. the most right item separated by `/`s) is **inside a `.csv` file**
* The `.csv` file is named after the second deepest part.
* All other parts of the terms are subfolders.

For example the term `Config/VR/VR空間優先` can be found in `i18nEx\<Language>\UI\<unit_name>\Config\VR.csv`.  
As another example, the term `YotogiSkillName/MP全身洗い` is in `i18nEx\<Language>\UI\<unit_name>\YotogiSkillName.csv`.

### Layout of the .csv files

Each `.csv` file must have the following header:

```csv
Key,Type,Desc,Japanese,English
```

All consecutive rows must have the same columns. The columns have the following meanings:

* `Key` -- the key of the term. For instance, in term `Config/VR/VR空間優先`, the key is `VR空間優先`.
* `Type` -- type of the key. In most cases you want this to be `Text`.
* `Desc` -- description of the key. Not shown anywhere in the game.
* `Japanese` -- the text to show when `Japanese` is set as display language
* `English` -- the text to show when **your custom language is selected**.

Unfortunately there is no easy way to obtain all the terms directly from the game. As of right now, 
the best way is to [extract the translations from the English game](#extracting-english-translations).

### Texture replacements

Texture replacements function the same way they function in YATranslator.  
With this you can replace an in-game texture with your own.

Unlike in YAT, **all** texture translations go to `Textures` folder -- even the asset textures. 
**All textures must be .PNG files.**

Just like YAT, i18nEx supports naming each texture either by its name in Unity game or by a special hash code that is unique for each in-game scene.  
To find out the names i18nEx uses for each texture, you can enable `VerboseLogging` under `[TextureReplacement]` section.

Finally, i18nEx support dumping textures from memory. For that, enable `DumpOriginalTextures` under `[TextureReplacement]` section. 
The dumped textures will be saved in `i18nEx\<Language>\Textures\Dumped`, so that they will be automatically loaded the next time you run the game.