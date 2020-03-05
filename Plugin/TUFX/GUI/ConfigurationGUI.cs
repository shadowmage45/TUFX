using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace TUFX
{

    public class ConfigurationGUI : MonoBehaviour
    {

        private static Rect windowRect = new Rect(Screen.width - 900, 40, 805, 600);
        private int windowID = 0;
        private Vector2 scrollPos = new Vector2();
        private Vector2 editScrollPos = new Vector2();

        /// <summary>
        /// Cached list of all profile names currently loaded at the time the GUI was created.
        /// </summary>
        private List<string> profileNames = new List<string>();
        private Dictionary<string, string> propertyStringStorage = new Dictionary<string, string>();
        private Dictionary<string, float> propertyFloatStorage = new Dictionary<string, float>();
        private Dictionary<string, bool> effectBoolStorage = new Dictionary<string, bool>();

        private bool selectionMode = true;

        public void Awake()
        {
            windowID = GetInstanceID();
            profileNames.Clear();
            profileNames.AddRange(TexturesUnlimitedFXLoader.INSTANCE.Profiles.Keys);
        }

        public void OnGUI()
        {
            try
            {
                windowRect = GUI.Window(windowID, windowRect, updateWindow, "TUFXSettings");
            }
            catch (Exception e)
            {
                MonoBehaviour.print("Caught exception while rendering TUFX Settings UI");
                MonoBehaviour.print(e.Message);
                MonoBehaviour.print(System.Environment.StackTrace);
            }
        }

        private void AddLabelRow(string text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.EndHorizontal();
        }

        private void updateWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mode: ", GUILayout.Width(100));
            bool selectionMode = this.selectionMode;
            if (selectionMode)
            {
                GUILayout.Label("Selection", GUILayout.Width(100));
                if (GUILayout.Button("Change to Edit Mode", GUILayout.Width(200)))
                {
                    this.selectionMode = false;
                }
            }
            else
            {
                GUILayout.Label("Edit", GUILayout.Width(100));
                if (GUILayout.Button("Change to Select Mode", GUILayout.Width(200)))
                {
                    this.selectionMode = true;
                }
            }
            GUILayout.EndHorizontal();
            if (selectionMode)
            {
                renderSelectionWindow();
            }
            else
            {
                renderConfigurationWindow();
            }
            GUI.DragWindow();
        }

        private void renderSelectionWindow()
        {
            AddLabelRow("Current Scene: " + HighLogic.LoadedScene +" map view active: " + MapView.MapIsEnabled);
            AddLabelRow("Current Profile: " + TexturesUnlimitedFXLoader.INSTANCE.CurrentProfileName);
            AddLabelRow("Select a new profile for current scene: ");
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            int len = profileNames.Count;
            for (int i = 0; i < len; i++)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Profile: " + profileNames[i]))
                {
                    string newProfileName = profileNames[i];
                    Log.debug("Profile Selected: " + newProfileName);
                    TexturesUnlimitedFXLoader.INSTANCE.setProfileForScene(newProfileName, HighLogic.LoadedScene, MapView.MapIsEnabled, true);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void renderConfigurationWindow()
        {
            if (TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("No Profile Selected!");
                GUILayout.EndHorizontal();
                return;
            }
            editScrollPos = GUILayout.BeginScrollView(editScrollPos, false, true);
            renderAmbientOcclusionSettings();
            renderAutoExposureSettings();
            renderBloomSettings();
            renderChromaticAberrationSettings();
            renderColorGradingSettings();
            renderDepthOfFieldSettings();
            renderGrainSettings();
            renderLensDistortionSettings();
            renderMotionBlurSettings();
            renderVignetteSettings();
            GUILayout.EndScrollView();
        }

        #region REGION Effect Settings Rendering

        private void renderAmbientOcclusionSettings()
        {
            AmbientOcclusion ao = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.GetSettingsFor<AmbientOcclusion>();
            bool enabled = ao != null && ao.enabled;
            bool showProps = AddEffectHeader("Ambient Occlusion", ao);
            if (enabled && showProps)
            {
                AddFloatParameter("Intensity", ao.intensity, 0, 10);
                AddColorParameter("Color", ao.color);
                AddBoolParameter("Ambient Only", ao.ambientOnly);
                AddFloatParameter("NoiseFilterTolerance", ao.noiseFilterTolerance, -8, 0);
                AddFloatParameter("BlurTolerance", ao.blurTolerance, -8, -1);
                AddFloatParameter("UpsampleTolerance", ao.upsampleTolerance, -12, -1);
                AddFloatParameter("ThicknessModifier", ao.thicknessModifier, 0, 5);
                AddFloatParameter("DirectLightStr", ao.directLightingStrength, 0, 1);
                AddFloatParameter("Radius", ao.radius, 0, 1);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("---------------------------------------");
            GUILayout.EndHorizontal();
        }

        private void renderAutoExposureSettings()
        {
            AutoExposure ae = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.GetSettingsFor<AutoExposure>();
            bool enabled = ae != null && ae.enabled;
            bool showProps = AddEffectHeader("Auto Exposure", ae);
            if (enabled && showProps)
            {
                AddVector2Parameter("Filtering", ae.filtering);
                AddFloatParameter("Min Luminance", ae.minLuminance, -9, 9);
                AddFloatParameter("Max Luminance", ae.maxLuminance, -9, 9);
                AddFloatParameter("Key Value", ae.keyValue, 0, 10);
                AddEnumParameter("Eye Adaption", ae.eyeAdaptation);
                AddFloatParameter("Speed Up", ae.speedUp, 0, 10);
                AddFloatParameter("Speed Down", ae.speedDown, 0, 10);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("---------------------------------------");
            GUILayout.EndHorizontal();
        }

        private void renderBloomSettings()
        {
            Bloom bl = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.GetSettingsFor<Bloom>();
            bool enabled = bl != null && bl.enabled;
            bool showProps = AddEffectHeader("Bloom", bl);
            if (enabled && showProps)
            {
                AddFloatParameter("Intensity", bl.intensity, 0, 10);
                AddFloatParameter("Threshold", bl.threshold, 0, 2);
                AddFloatParameter("SoftKnee", bl.softKnee, 0, 1);
                AddFloatParameter("Clamp", bl.clamp, 0, 64000);
                AddFloatParameter("Diffusion", bl.diffusion, 0, 20);
                AddFloatParameter("Anamorphic Ratio", bl.anamorphicRatio, -1, 1);
                AddColorParameter("Color", bl.color);
                AddBoolParameter("Fast Mode", bl.fastMode);
                AddTextureParameter("Dirt Texture", bl.dirtTexture);
                AddFloatParameter("Dirt Intensity", bl.dirtIntensity, 0, 2);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("---------------------------------------");
            GUILayout.EndHorizontal();
        }

        private void renderChromaticAberrationSettings()
        {
            ChromaticAberration ca = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.GetSettingsFor<ChromaticAberration>();
            bool enabled = ca != null && ca.enabled;
            bool showProps = AddEffectHeader("Chromatic Aberration", ca);
            if (enabled && showProps)
            {
                AddTextureParameter("Spectral LUT", ca.spectralLut);
                AddFloatParameter("Intensity", ca.intensity, 0, 1);
                AddBoolParameter("Fast Mode", ca.fastMode);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("---------------------------------------");
            GUILayout.EndHorizontal();
        }

        private void renderColorGradingSettings()
        {
            ColorGrading cg = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.GetSettingsFor<ColorGrading>();
            bool enabled = cg != null && cg.enabled;
            bool showProps = AddEffectHeader("Color Grading", cg);
            if (enabled && showProps)
            {
                AddEnumParameter("Mode", cg.gradingMode);
                AddTextureParameter("External LUT", cg.externalLut);
                AddEnumParameter("Tonemapper", cg.tonemapper);
                AddFloatParameter("T.Curve Toe Strength", cg.toneCurveToeStrength, 0, 1);
                AddFloatParameter("T.Curve Toe Length", cg.toneCurveToeLength, 0, 1);
                AddFloatParameter("T.Curve Shd Strength", cg.toneCurveShoulderStrength, 0, 1);
                AddFloatParameter("T.Curve Shd Length", cg.toneCurveShoulderLength, 0, 64000);
                AddFloatParameter("T.Curve Shd Angle", cg.toneCurveShoulderAngle, 0, 1);
                AddFloatParameter("Tone Curve Gamma", cg.toneCurveGamma, 0.001f, 64000);
                AddTextureParameter("LDR LUT", cg.ldrLut);
                AddFloatParameter("LDR LUT Contrib.", cg.ldrLutContribution, 0, 1);
                AddFloatParameter("Temperature", cg.temperature, -100, 100);
                AddFloatParameter("Tint", cg.tint, -100, 100);
                AddColorParameter("ColorFilter", cg.colorFilter);
                AddFloatParameter("HueShift", cg.hueShift, -180, 180);
                AddFloatParameter("Saturation", cg.saturation, -100, 100);
                AddFloatParameter("Brightness", cg.brightness, -100, 100);
                AddFloatParameter("PostExposure", cg.postExposure, -64000, 64000);
                AddFloatParameter("Contrast", cg.contrast, -100, 100);

                AddFloatParameter("RedOutRedIn", cg.mixerRedOutRedIn, -200, 200);
                AddFloatParameter("RedOutGreenIn", cg.mixerRedOutGreenIn, -200, 200);
                AddFloatParameter("RedOutBlueIn", cg.mixerRedOutBlueIn, -200, 200);
                AddFloatParameter("GreenOutRedIn", cg.mixerGreenOutRedIn, -200, 200);
                AddFloatParameter("GreenOutGreenIn", cg.mixerGreenOutGreenIn, -200, 200);
                AddFloatParameter("GreenOutBlueIn", cg.mixerGreenOutBlueIn, -200, 200);
                AddFloatParameter("BlueOutRedIn", cg.mixerBlueOutRedIn, -200, 200);
                AddFloatParameter("BlueOutGreenIn", cg.mixerBlueOutGreenIn, -200, 200);
                AddFloatParameter("BlueOutBlueIn", cg.mixerBlueOutBlueIn, -200, 200);

                AddVector4Parameter("Lift", cg.lift);
                AddVector4Parameter("Gamma", cg.gamma);
                AddVector4Parameter("Gain", cg.gain);

                AddSplineParameter("MasterCurve", cg.masterCurve);
                AddSplineParameter("RedCurve", cg.redCurve);
                AddSplineParameter("GreenCurve", cg.greenCurve);
                AddSplineParameter("BlueCurve", cg.blueCurve);

                AddSplineParameter("HueVsHueCurve", cg.hueVsHueCurve);
                AddSplineParameter("HueVsSatCurve", cg.hueVsSatCurve);
                AddSplineParameter("SatVsSatCurve", cg.satVsSatCurve);
                AddSplineParameter("LumVsSatCurve", cg.lumVsSatCurve);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("---------------------------------------");
            GUILayout.EndHorizontal();
        }

        private void renderDepthOfFieldSettings()
        {
            DepthOfField df = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.GetSettingsFor<DepthOfField>();
            bool enabled = df != null && df.enabled;
            bool showProps = AddEffectHeader("Depth Of Field", df);
            if (enabled && showProps)
            {
                AddFloatParameter("Focus Distance", df.focusDistance, 0.1f, 64000);
                AddFloatParameter("Aperture", df.aperture, 0.05f, 32f);
                AddFloatParameter("Focal Length", df.focalLength, 1f, 300f);
                AddEnumParameter("Kernel Size", df.kernelSize);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("---------------------------------------");
            GUILayout.EndHorizontal();
        }

        private void renderGrainSettings()
        {
            Grain gr = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.GetSettingsFor<Grain>();
            bool enabled = gr != null && gr.enabled;
            bool showProps = AddEffectHeader("Grain", gr);
            if (enabled && showProps)
            {
                AddBoolParameter("Colored", gr.colored);
                AddFloatParameter("Intensity", gr.intensity, 0, 1);
                AddFloatParameter("Size", gr.size, 0.3f, 3f);
                AddFloatParameter("Lum. Contrib", gr.lumContrib, 0, 1);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("---------------------------------------");
            GUILayout.EndHorizontal();
        }

        private void renderLensDistortionSettings()
        {
            LensDistortion ld = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.GetSettingsFor<LensDistortion>();
            bool enabled = ld != null && ld.enabled;
            bool showProps = AddEffectHeader("Lens Distortion", ld);
            if (enabled && showProps)
            {
                AddFloatParameter("Intensity", ld.intensity, -100, 100);
                AddFloatParameter("IntensityX", ld.intensityX, 0, 1);
                AddFloatParameter("IntensityY", ld.intensityY, 0, 1);
                AddFloatParameter("CenterX", ld.centerX, -1, 1);
                AddFloatParameter("CenterY", ld.centerY, -1, 1);
                AddFloatParameter("Scale", ld.scale, 0.01f, 5f);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("---------------------------------------");
            GUILayout.EndHorizontal();
        }

        private void renderMotionBlurSettings()
        {
            MotionBlur mb = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.GetSettingsFor<MotionBlur>();
            bool enabled = mb != null && mb.enabled;
            bool showProps = AddEffectHeader("Motion Blur", mb);
            if (enabled && showProps)
            {
                AddFloatParameter("Shutter Angle", mb.shutterAngle, 0f, 360f);
                AddIntParameter("Sample Count", mb.sampleCount, 4, 32);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("---------------------------------------");
            GUILayout.EndHorizontal();
        }

        private void renderVignetteSettings()
        {
            Vignette vg = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.GetSettingsFor<Vignette>();
            bool enabled = vg != null && vg.enabled;
            bool showProps = AddEffectHeader("Vignette", vg);
            if (enabled && showProps)
            {
                AddEnumParameter("Mode", vg.mode);
                AddColorParameter("Color", vg.color);
                AddVector2Parameter("Center", vg.center);
                AddFloatParameter("Intensity", vg.intensity, 0, 1);
                AddFloatParameter("Smoothness", vg.smoothness, 0.01f, 1f);
                AddFloatParameter("Roundness", vg.roundness, 0, 1);
                AddBoolParameter("Rounded", vg.rounded);
                AddTextureParameter("Mask", vg.mask);
                AddFloatParameter("Opacity", vg.opacity, 0, 1);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("---------------------------------------");
            GUILayout.EndHorizontal();
        }

        #endregion

        #region REGION - Parameter Rendering Methods

        private bool AddEffectHeader<T>(string label, T effect) where T : PostProcessEffectSettings
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("----- " + label, GUILayout.Width(200));
            bool enabled = effect != null && effect.enabled;
            bool showProps = true;
            if (enabled) //if it is enabled, draw button to disable it
            {
                if (GUILayout.Button("Disable", GUILayout.Width(100)))
                {
                    effect.enabled.Override(false);
                }
                string hash = effect.GetHashCode().ToString();
                if (!effectBoolStorage.TryGetValue(hash, out showProps))
                {
                    showProps = true;
                    effectBoolStorage.Add(hash, true);
                }
                if (GUILayout.Button((showProps ? "Hide Props" : "Show Props"), GUILayout.Width(110)))
                {
                    showProps = !showProps;
                    effectBoolStorage[hash] = showProps;
                }
            }
            else //else draw the button to enable it
            {
                if (GUILayout.Button("Enable", GUILayout.Width(100)))
                {
                    if (effect != null)
                    {
                        effect.enabled.Override(true);
                    }
                    else
                    {
                        effect = ScriptableObject.CreateInstance<T>();
                        effect.enabled.Override(true);
                        TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.Settings.Add(effect);
                        TexturesUnlimitedFXLoader.INSTANCE.enableProfileForCurrentScene();
                    }
                }
            }
            GUILayout.EndHorizontal();
            return showProps;
        }

        /// <summary>
        /// Fun with generics...
        /// </summary>
        /// <typeparam name="Tenum"></typeparam>
        /// <param name="label"></param>
        /// <param name="param"></param>
        private void AddEnumParameter<Tenum>(string label, ParameterOverride<Tenum> param)
        {
            Tenum value = param.value;
            Type type = value.GetType();
            Tenum[] values = (Tenum[])Enum.GetValues(type);
            int index = values.IndexOf(value);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(200));
            bool enabled = param.overrideState;
            if (enabled)
            {
                if (GUILayout.Button("Disable", GUILayout.Width(100)))
                {
                    param.overrideState = false;
                }
                if (GUILayout.Button("<", GUILayout.Width(110)))
                {
                    index--;
                    if (index < 0) { index = values.Length - 1; }
                    param.Override(values[index]);
                }
                GUILayout.Label(value.ToString(), GUILayout.Width(220));
                if (GUILayout.Button(">", GUILayout.Width(110)))
                {
                    index++;
                    if (index >= values.Length) { index = 0; }
                    param.Override(values[index]);
                }
            }
            else
            {
                if (GUILayout.Button("Enable", GUILayout.Width(100)))
                {
                    param.overrideState = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void AddBoolParameter(string label, ParameterOverride<bool> param)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(200));
            bool enabled = param.overrideState;
            if (enabled)
            {
                if (GUILayout.Button("Disable", GUILayout.Width(100)))
                {
                    param.overrideState = false;
                }
                if (GUILayout.Button(param.value.ToString(), GUILayout.Width(110)))
                {
                    param.value = !param.value;
                }
            }
            else
            {
                if (GUILayout.Button("Enable", GUILayout.Width(100)))
                {
                    param.overrideState = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void AddIntParameter(string label, ParameterOverride<int> param, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(200));
            bool enabled = param.overrideState;
            if (enabled)
            {
                string hash = param.GetHashCode().ToString();
                if (GUILayout.Button("Disable", GUILayout.Width(100)))
                {
                    param.overrideState = false;
                }
                string textValue = string.Empty;
                if (propertyStringStorage.ContainsKey(hash))
                {
                    textValue = propertyStringStorage[hash];
                }
                else
                {
                    textValue = param.value.ToString();
                    propertyStringStorage.Add(hash, textValue);
                }
                string newValue = GUILayout.TextArea(textValue, GUILayout.Width(110));
                if (newValue != textValue)
                {
                    textValue = newValue;
                    if (int.TryParse(textValue, out int v))
                    {
                        param.Override(v);
                    }
                    propertyStringStorage[hash] = textValue;
                }
                if (!propertyFloatStorage.TryGetValue(hash, out float sliderValue))
                {
                    sliderValue = param.value;
                    propertyFloatStorage[hash] = sliderValue;
                }
                float sliderValue2 = GUILayout.HorizontalSlider(sliderValue, min, max, GUILayout.Width(330));
                if (sliderValue2 != sliderValue)
                {
                    param.Override((int)sliderValue2);
                    textValue = ((int)sliderValue2).ToString();
                    propertyStringStorage[hash] = textValue;
                    propertyFloatStorage[hash] = sliderValue2;
                }
            }
            else
            {
                if (GUILayout.Button("Enable", GUILayout.Width(100)))
                {
                    param.overrideState = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void AddFloatParameter(string label, ParameterOverride<float> param, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(200));
            bool enabled = param.overrideState;
            if (enabled)
            {
                string hash = param.GetHashCode().ToString();
                if (GUILayout.Button("Disable", GUILayout.Width(100)))
                {
                    param.overrideState = false;
                }
                string textValue = string.Empty;
                if (propertyStringStorage.ContainsKey(hash))
                {
                    textValue = propertyStringStorage[hash];
                }
                else
                {
                    textValue = param.value.ToString();
                    propertyStringStorage.Add(hash, textValue);
                }
                string newValue = GUILayout.TextArea(textValue, GUILayout.Width(110));
                if (newValue != textValue)
                {
                    textValue = newValue;
                    if (float.TryParse(textValue, out float v))
                    {
                        param.Override(v);
                    }
                    propertyStringStorage[hash] = textValue;
                }
                float sliderValue = GUILayout.HorizontalSlider(param.value, min, max, GUILayout.Width(330));
                if (sliderValue != param.value)
                {
                    param.Override(sliderValue);
                    textValue = sliderValue.ToString();
                    propertyStringStorage[hash] = textValue;
                }
            }
            else
            {
                if (GUILayout.Button("Enable", GUILayout.Width(100)))
                {
                    param.overrideState = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void AddColorParameter(string label, ParameterOverride<Color> param)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(200));
            bool enabled = param.overrideState;
            if (enabled)
            {
                string hash = param.GetHashCode().ToString();
                if (GUILayout.Button("Disable", GUILayout.Width(100)))
                {
                    param.overrideState = false;
                }
                AddColorInput("Red", hash, ref param.value.r);
                AddColorInput("Green", hash, ref param.value.g);
                AddColorInput("Blue", hash, ref param.value.b);
                AddColorInput("Alpha", hash, ref param.value.a);
            }
            else
            {
                if (GUILayout.Button("Enable", GUILayout.Width(100)))
                {
                    param.overrideState = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void AddColorInput(string name, string hash, ref float val)
        {
            string key = hash + name;
            string curTextVal = string.Empty;
            if (propertyStringStorage.ContainsKey(key))
            {
                curTextVal = propertyStringStorage[key];
            }
            else
            {
                curTextVal = val.ToString();
                propertyStringStorage.Add(key, curTextVal);
            }
            string newValue = GUILayout.TextArea(curTextVal, GUILayout.Width(110));
            if (newValue != curTextVal)
            {
                curTextVal = newValue;
                if (float.TryParse(curTextVal, out float v))
                {
                    val = v;
                }
                propertyStringStorage[key] = curTextVal;
            }
        }

        private void AddVector2Parameter(string label, ParameterOverride<Vector2> param)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(200));
            bool enabled = param.overrideState;
            if (enabled)
            {
                string hash = param.GetHashCode().ToString();
                if (GUILayout.Button("Disable", GUILayout.Width(100)))
                {
                    param.overrideState = false;
                }
                AddColorInput("X", hash, ref param.value.x);
                AddColorInput("Y", hash, ref param.value.y);
            }
            else
            {
                if (GUILayout.Button("Enable", GUILayout.Width(100)))
                {
                    param.overrideState = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void AddVector4Parameter(string label, ParameterOverride<Vector4> param)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(200));
            bool enabled = param.overrideState;
            if (enabled)
            {
                string hash = param.GetHashCode().ToString();
                if (GUILayout.Button("Disable", GUILayout.Width(100)))
                {
                    param.overrideState = false;
                }
                AddColorInput("X", hash, ref param.value.x);
                AddColorInput("Y", hash, ref param.value.y);
                AddColorInput("Z", hash, ref param.value.z);
                AddColorInput("W", hash, ref param.value.w);
            }
            else
            {
                if (GUILayout.Button("Enable", GUILayout.Width(100)))
                {
                    param.overrideState = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void AddSplineParameter(string label, ParameterOverride<Spline> param)//TODO
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("TODO - Spline parameter.");
            GUILayout.EndHorizontal();
        }

        private void AddTextureParameter(string label, ParameterOverride<Texture> param)//TODO
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("TODO - Texture parameter.");
            GUILayout.EndHorizontal();
        }

        #endregion

    }

}
