# TUFX - Textures Unlimited Special Effects

This is a mod for Kerbal Space Program that makes the Unity Post Process (v2) package available for use within the game.
  All supported effects from the package are available for use within KSP.
  
The goal of this mod is to add the ability to use industry-standard image post-processing effects during realtime rendering
of the game world, allowing for increased visual fidelity in general use, or for specific effects for cinematic purposes
through specially configured profiles.

TUFX allows for users to select a specific 'profile' to be used for each game scene.  These profiles specify which effects 
will be enabled for the scene, and specify the values for each of the parameters for the effects.

|Before|After|
|----|----|
| ![](https://i.imgur.com/NbYlicE.png) | ![](https://i.imgur.com/dG4UQdz.png) |
| ![](https://i.imgur.com/gEBsHnU.png) | ![](https://i.imgur.com/cOkNiuI.png) |
| ![](https://i.imgur.com/sxiGJ5X.png) | ![](https://i.imgur.com/d0PrPJY.png) |
| ![](https://i.imgur.com/a9xVi27.png) | ![](https://i.imgur.com/Mm6OmRx.png) |
| ![](https://i.imgur.com/EpStaK5.png) | ![](https://i.imgur.com/PdiBqns.png) |
| ![](https://i.imgur.com/kF9cBvv.png) | ![](https://i.imgur.com/GvakWTa.png) |

Cinematic Effects (not default profile)  

|Before|After|
|----|----|
| ![](https://i.imgur.com/1UPTEym.png) | ![](https://i.imgur.com/4c0y5Y5.png) |
| ![](https://i.imgur.com/y9crGm6.png) | ![](https://i.imgur.com/iMno4eE.png) |

---
## Requirements:
* **Windows / DirectX11**, required for single-camera setup in KSP
* ShaderModel 3.5 or later graphics hardware support (5.0+ recommended)
* KSP 1.9.0 or later, required for single-camera setup
* No exceptions.  No OpenGL.  No DirectX12.  Definitely not DirectX9.
* Any other requirements of the Unity Post Process package (look them up if interested)
* KS3P will cause conflicts.  Choose one or the other.
* Scatterer currently unsupported (it is not yet available for KSP 1.9+)
* EVE currently unsupported (it is not yet available for KSP 1.9+)

## Installation
Releases of the mod will be available from GitHub:  https://github.com/shadowmage45/TUFX/releases  

Download the most recent release for your version of KSP, open the .zip file, and extract the contents of the GameData/ 
subfolder from the package into your KSP installations' GameData/ folder (e.g. Your/KSP/Path/GameData/TUFX).  Make sure to
include any other dependencies and folders (e.g. ModuleManager) that were included in the release package.


**Pre-Release Note:** Anything marked as a 'Pre-Release' on the GitHub page is exactly that -- a pre-release intended for
testing purposes; if you install one of these releases, you are accepting the risks that come with its use (it may not work,
may conflict with other mods, may have unfinished features, etc).

## Configuration
The mod includes a default profile for each supported game-scene, and these profiles are included in the standard 
installation.  Additional profiles can be added as KSP-CFG files anywhere in the GameData folder; the loading system 
will locate all installed TUFX_PROFILE configs, and load all detected profiles into memory for selection and use in-game.

Configuration of the mod is best accomplished through the in-game configuration UI.  Click the 'TUfx' app-launcher button to
open or close the UI; this button should be available in all supported game scenes (except for main-menu, where app-launcher
is not supported).  

From within the configuration UI, select a profile for the current scene/to be edited (**only the active profile can be
 edited**).  Once a profile has been selected, press the 'Change to Edit Mode' button on the top of the configuration UI. 
 This will prompt the UI to display the current configuration of the profile.

Press the 'Enable'/'Disable' toggle by an effect title to toggle that effect on or off.

When an effect is enabled the UI will list each of its adjustable properties directly below the effect header row.  Each
of these properties will have a toggle to enable specifying a custom value; when this is enabled further controls will be
displayed to edit the property depending upon the type of property being edited; integer and decimal values show a text 
input box and slider, color values provide four component input boxes, boolean values provide a simple toggle, etc.

The property list for a currently enabled effect can be hidden/shown by toggling the 'Show Props'/'Hide Props' button on
the effect header row.  This does not disable the parameters, merely collapses them to help clean up the UI view.

When finished editing a profile, press the 'Export Selected' button at the top of the UI to export the current configuration 
into the KSP.log file.  From there, you can copy the profile out of the log and rename it to create a new profile, or use the
data to overwrite the contents of an existing profile config file.  **TUFX does not support direct-to-file exporting, as no
 'Save File Dialog' has been provided by either Unity or KSP, and I have no desire to create one myself, nor deal with the
 security issues that would arise from its use.**
 
**See the included readme.md document for information on config file syntax, and how to manually edit/create profiles.**
 
---
## Performance Comparison
Comparison data gathered on the following hardware (your results may differ):
* Intel i5-2500k @ 4.5ghz
* 24g DDR3
* GTX970

|Scene|KS3P|TUFX|TUFX-Disabled|Stock|
|----|----|----|----|----|
|KSC|NA|142|170|180|
|Kerbin-Ground|NA|105|118|128|
|Kerbin-Orbit|NA|125|150|165|

KS3P was unavailabe at the time the testing was performed, so no data was gathered.

## Dependencies
* **ModuleManager** - TUFX depends directly on ModuleManager for its 'Database Reloaded' callback, and will not function unless ModuleManager is
 installed.  ModuleManager has been included in the release packages, and is redistributed under the terms of its own license.
 Further information on Module Manager can be found on the KSP Forums at: ( https://forum.kerbalspaceprogram.com/index.php?/topic/50533-18x-19x-module-manager-413-november-30th-2019-right-to-ludicrous-speed/ ), and the license and source-code may be found
 on the ModuleManager GitHub repository: ( https://github.com/sarbian/ModuleManager ).

## Known Issues and Bug Reports
See the github issues repository: https://github.com/shadowmage45/TUFX/issues  
The change to use HDR rendering can cause occasional artifacts.  If these are present, you can turn
off HDR in the profiles by setting the hdr flag to 'False'.


## Licensing/Legal
* Full source code for the TUFX assembly is available on github: https://github.com/shadowmage45/TUFX
* Source code for Unity post processing shaders are available on github: https://github.com/Unity-Technologies/PostProcessing
* Custom code and classes (everything outside of the PostProcessing source folder) is under GPL3.0 or later license, the
 full text of this license may be found included in the release packages and on the GitHub repository:  
 https://github.com/shadowmage45/TUFX/blob/master/LICENSE.txt
* Unity developed classes, shaders, and textures are provided under Unity companion license, with source and license references available
  from:  
  https://github.com/Unity-Technologies/PostProcessing
* Modifications to Unity classes (adding ConfigNode load/save methods) are released under public domain or as close as possible under US law.  
* ModuleManager (fork by Sarbian) is included and redistributed under its license:   
  https://github.com/sarbian/ModuleManager/blob/master/README.md

## Credits
* **Shadowmage** - Coding and development.
* **SQUAD** - For creating and publishing KSP.
* **TheWhiteGuardian** - For creating KS3P, without which I would have never known about the Unity Post Process packages.