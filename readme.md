# TexturesUnlimited Special Effects (TUFX)

Brings the Unity Post Process Package (v2) into KSP, un-touched and as provided by the package 
developers (exception: minor helper methods added).

## Requirements:
* **Windows / DirectX11**, required for single-camera setup in KSP
* ShaderModel 3.5 or later graphics hardware support (5.0+ recommended)
* KSP 1.9.0 or later, required for single-camera setup
* No exceptions.  No OpenGL.  No DirectX12.  Definitely not DirectX9.
* Any other requirements of the Unity Post Process package (look them up if interested)
* KS3P will cause conflicts.  Choose one or the other.
* Scatterer currently unsupported (it is not yet available for KSP 1.9+)
* EVE currently unsupported (it is not yet available for KSP 1.9+)

## Installation:
From the distributed .zip package, extract the contents of the 'GameData/' subfolder, and place 
those contents into your KSP GameData/ folder; e.g. (XXX/KSP/GameData/TUFX).  

TUFX relies upon ModuleManager for database (re)loaded callbacks, and will not function unless 
ModuleManager is installed. This dependency is included in the distribution package and should be
 installed into the root GameData/ folder.

TUFX fully supports in-game configuration reloading through ModuleManager; any edited (or new)
profile configurations will be reloaded from file, and the updated values made available to the 
effects systems.

## Use:
From within the flight, editor, space-center, or tracking station scenes, press the 'TUfx' 
app-launcher button to open the configuration UI.  

The main screen on the UI allows for selecting of the profile for the currently active scene.  
Click on one of the profile buttons to select it and make it active for the current scene.  These 
changes are persistent, and stored in the savegame persistence file.  

In order to edit settings, press the 'Change to Edit Mode' button on the top of the UI.  This will 
display the configured values for each of the currently enabled effects, will provide methods to 
enable/disable each effect, and will provide methods to adjust each of the property values for each
of the effects.

Changes made through the UI are not permanent, and are only persistent until the game is restarted. 
To make the changes permanent, use the 'Export' buttons at the top of the UI; pressing these will 
export the profile(s) to the KSP.log file, where they can then be copied into a new CFG file.

---
## Profile Config Syntax:

The structure of the profile configuration files is as below:
````
//a root/global level node, one per-profile
TUFX_PROFILE
{

    //The name of the profile.  This is both used for display in the GUI, and to reference the profile in the
    // game persistence data
    name = ProfileNameHere
    
    //for each post-process effect that should be active, there will be one EFFECT node:
    EFFECT
    {
        //The name of the post process effect.  Each effect should only appear once in a given profile
        // the acceptable names here are the same as the names of the classes in the Post Process folder
        // AmbientOcclusion, AutoExposure, Bloom, ChromaticAberration, ColorGrading, 
        // DepthOfField, Grain, LensDistortion, MotionBlur, Vignette
        name = AmbientOcclusion
        
        //For each property in the effect that will be overriden, specify the value, one per-line
        // (except SPLINE parameters, which use a config node, example below)
        // all parameter names are CamelCase, starting with an upper-case character, but should be
        // otherwise identical to the field-names from the classes:
        
        // Enum parameters use the EXACT name of the enums as defined in the source code:
        Mode = MultiScaleVolumetricObscurance
        
        // Float parameters accept standard decimal notation:
        Intensity = 1.0
        
        // Color parameters accept floating-point CSV notation (4-elements: r,g,b,a):
        Color = 1.0, 1.0, 1.0, 1.0
        
        // Bool parameters accept 'true' and 'false' (nothing else)
        AmbientOnly = true
        
        //further parameters here
    }
    
    //example of ColorGrading profile, to provide example of SPLINE config syntax
    EFFECT
    {
        name = ColorGrading
        //define a spline parameter
        SPLINE
        {
            //the name of the parameter
            name = MasterCurve
            //the 'zero point' (no clue)
            zero = 0.5
            //the 'range' (no clue)
            range = 1
			//unknown
			loop = True
            //animation curve/float curve key layout with six floating point elements
            // time, value, in-tangent, out-tangent, in-weight, out-weight
            key = 0.0, 1.0, 0.0, 0.0, 0.0, 0.0
            key = 1.0, 0.5, 0.0, 0.0, 0.0, 0.0
        }
    }
}
````
---
## Effects and Parameters
Listed below will be each effect included in TUFX, the name it uses within the configuration files, 
and the name of the parameters for the effects as used in both the profiles and texture 
specifications.

### Ambient Occlusion
Listed as 'AmbientOcclusion' in configuration files.  Has the following fields:
* Mode
* Intensity
* Color
* AmbientOnly
* NoiseFilterTolerance
* BlurTolerance
* UpsampleTolerance
* ThicknessModifier
* DirectLightingStrength
* Radius
* Quality

### Auto Exposure
Listed as 'AutoExposure' in configuration files.  Has the following fields:
* Filtering
* MinLuminance
* MaxLuminance
* KeyValue
* EyeAdaption
* SpeedUp
* SpeedDown

### Bloom
Listed as 'Bloom' in configuration files.  Has the following fields:
* Intensity
* Threshold
* SoftKnee
* Clamp
* Diffusion
* AnamorphicRatio
* Color
* FastMode
* DirtTexture
* DirtIntensity

### Chromatic Aberration
Listed as 'ChromaticAberration' in configuration files.  Has the following fields:
* SpectralLUT
* Intensity
* FastMode

### Color Grading
Listed as 'ColorGrading' in configuration files.  Has the following fields:
* GradingMode
* ExternalLUT
* Tonemapper
* ToneCurveToeStrength
* ToneCurveToeLength
* ToneCurveShoulderStrength
* ToneCurveShoulderLength
* ToneCurveShoulderAngle
* ToneCurveGamma
* LDRLUT
* LDRLUTContribution
* Temperature
* Tint
* ColorFilter
* HueShift
* Saturation
* Brightness
* PostExposure
* Contrast
* MixerRedOutRedIn
* MixerRedOutGreenIn
* MixerRedOutBlueIn
* MixerGreenOutRedIn
* MixerGreenOutGreenIn
* MixerGreenOutBlueIn
* MixerBlueOutRedIn
* MixerBlueOutGreenIn
* MixerBlueOutBlueIn
* Lift
* Gamma
* Gain
* MasterCurve
* RedCurve
* GreenCurve
* BlueCurve
* HueVsHueCurve
* HueVsSatCurve
* SatVsSatCurve
* LumVsSatCurve

### Depth Of Field
Listed as 'DepthOfField' in configuration files.  Has the following fields:
* FocusDistance
* Aperture
* FocalLength
* KernelSize

### Grain
Listed as 'Grain' in configuration files.  Has the following fields:
* Colored
* Intensity
* Size
* LumContrib

### Lens Distortion
Listed as 'LensDistortion' in configuration files.  Has the following fields:
* Intensity
* IntensityX
* IntensityY
* CenterX
* CenterY
* Scale

### Motion Blur
Listed as 'MotionBlur' in configuration files.  Has the following fields:
* ShutterAngle
* SampleCount

### Vignette
Listed as 'Vignette' in configuration files.  Has the following fields:
* Mode
* Color
* Center
* Intensity
* Smoothness
* Roundness
* Rounded
* Mask
* Opacity

---
## Licensing:
* Custom code and classes (everything outside of the PostProcessing source folder) is under GPL3.0 or later license.
* Modifications to Unity classes are released under public domain or as close as possible under US law.  
* Unity developed classes are provided under Unity companion license, with source and license references available
  from: https://github.com/Unity-Technologies/PostProcessing