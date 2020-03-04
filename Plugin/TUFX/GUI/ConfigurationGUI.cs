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
        private static Rect windowRect = new Rect(Screen.width - 900, 40, 800, 600);
        private int windowID = 0;
        private Vector2 scrollPos = new Vector2();
        private Vector2 editScrollPos = new Vector2();

        private List<string> profileNames = new List<string>();
        private string currentProfileName = string.Empty;
        private TUFXProfile currentProfile;
        private GameScenes currentScene = GameScenes.LOADING;

        private Dictionary<string, string> propertyStringStorage = new Dictionary<string, string>();
        private Dictionary<string, float> propertyFloatStorage = new Dictionary<string, float>();
        private Dictionary<string, bool> effectBoolStorage = new Dictionary<string, bool>();

        private bool selectionMode = true;

        public void Awake()
        {
            windowID = GetInstanceID();
            profileNames.Clear();
            profileNames.AddRange(TexturesUnlimitedFXLoader.INSTANCE.Profiles.Keys);
            currentScene = HighLogic.LoadedScene;

            //TODO - move this logic to loader class; it should track active profile
            if (currentScene == GameScenes.EDITOR)
            {
                currentProfileName = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().EditorSceneProfile;
            }
            else if (currentScene == GameScenes.FLIGHT)
            {
                currentProfileName = MapView.MapIsEnabled? HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().MapSceneProfile : HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().FlightSceneProfile;
            }
            else if (currentScene == GameScenes.SPACECENTER)
            {
                currentProfileName = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().SpaceCenterSceneProfile;
            }
            else if (currentScene == GameScenes.TRACKSTATION)
            {
                currentProfileName = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().TrackingStationProfile;
            }
            else
            {
                //TODO -- throw unsupported operation exception -- incorrect game scene
            }
            currentProfile = TexturesUnlimitedFXLoader.INSTANCE.Profiles[currentProfileName];
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
                GUILayout.Label("Edit Configuration", GUILayout.Width(100));
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
            addLabelRow("Current Scene: " + currentScene +" map view active: " + MapView.MapIsEnabled);
            addLabelRow("Current Profile: " + currentProfileName);
            addLabelRow("Select a new profile for current scene: ");
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            int len = profileNames.Count;
            for (int i = 0; i < len; i++)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Profile: " + profileNames[i]))
                {
                    currentProfileName = profileNames[i];
                    currentProfile = TexturesUnlimitedFXLoader.INSTANCE.Profiles[currentProfileName];
                    Log.debug("Profile Selected: " + currentProfileName);
                    TexturesUnlimitedFXLoader.INSTANCE.setProfileForScene(currentProfileName, currentScene, true);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void renderConfigurationWindow()
        {
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

        private void renderAmbientOcclusionSettings()
        {
            AmbientOcclusion ao = currentProfile.GetSettingsFor<AmbientOcclusion>();
            bool enabled = ao != null && ao.enabled;
            AddEffectHeader("Ambient Occlusion", ao);
            if (enabled)
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
            AutoExposure ae = currentProfile.GetSettingsFor<AutoExposure>();
            bool enabled = ae != null && ae.enabled;
            AddEffectHeader("Auto Exposure", ae);
            if (enabled)
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
            Bloom bl = currentProfile.GetSettingsFor<Bloom>();
            bool enabled = bl != null && bl.enabled;
            AddEffectHeader("Bloom", bl);
            if (enabled)
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
            ChromaticAberration ca = currentProfile.GetSettingsFor<ChromaticAberration>();
            bool enabled = ca != null && ca.enabled;
            AddEffectHeader("Chromatic Aberration", ca);
            if (enabled)
            {
                AddTextureParameter("Spectral LUT", ca.spectralLut);
                AddFloatParameter("Intensity", ca.intensity, 0, 1);
                AddBoolParameter("Fast Mode", ca.fastMode);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("---------------------------------------");
            GUILayout.EndHorizontal();
        }

        private void renderColorGradingSettings()//TODO
        {
            ColorGrading cg = currentProfile.GetSettingsFor<ColorGrading>();
            bool enabled = cg != null && cg.enabled;
            AddEffectHeader("Color Grading", cg);
            if (enabled)
            {
                //TODO
                GUILayout.BeginHorizontal();
                GUILayout.Label("TODO");
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("---------------------------------------");
            GUILayout.EndHorizontal();
        }

        private void renderDepthOfFieldSettings()
        {
            DepthOfField df = currentProfile.GetSettingsFor<DepthOfField>();
            bool enabled = df != null && df.enabled;
            AddEffectHeader("Depth Of Field", df);
            if (enabled)
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
            Grain gr = currentProfile.GetSettingsFor<Grain>();
            bool enabled = gr != null && gr.enabled;
            AddEffectHeader("Grain", gr);
            if (enabled)
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
            LensDistortion ld = currentProfile.GetSettingsFor<LensDistortion>();
            bool enabled = ld != null && ld.enabled;
            AddEffectHeader("Lens Distortion", ld);
            if (enabled)
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
            MotionBlur mb = currentProfile.GetSettingsFor<MotionBlur>();
            bool enabled = mb != null && mb.enabled;
            AddEffectHeader("Motion Blur", mb);
            if (enabled)
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
            Vignette vg = currentProfile.GetSettingsFor<Vignette>();
            bool enabled = vg != null && vg.enabled;
            AddEffectHeader("Vignette", vg);
            if (enabled)
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

        private void addLabelRow(string text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.EndHorizontal();
        }

        private float addSliderRow(string text, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayoutOption width = GUILayout.Width(100);
            GUILayout.Label(text, width);
            GUILayout.Label(value.ToString(), width);
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(300));
            GUILayout.EndHorizontal();
            return value;
        }

        private bool addButtonRowToggle(string text, bool value)
        {
            GUILayout.BeginHorizontal();
            GUILayoutOption width = GUILayout.Width(100);
            GUILayout.Label(text, width);
            GUILayout.Label(value.ToString(), width);
            if (GUILayout.Button("Toggle", width))
            {
                value = !value;
            }
            GUILayout.EndHorizontal();
            return value;
        }

        private bool AddButtonRow(string label)
        {
            GUILayout.BeginHorizontal();
            bool val = GUILayout.Button(label, GUILayout.Width(300));
            GUILayout.EndHorizontal();
            return val;
        }

        private void AddEffectHeader<T>(string label, T effect) where T : PostProcessEffectSettings
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("----- "+label, GUILayout.Width(200));
            bool enabled = effect != null && effect.enabled;
            if (enabled) //if it is enabled, draw button to disable it
            {
                if (GUILayout.Button("Disable", GUILayout.Width(100)))
                {
                    effect.enabled.Override(false);
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
                        currentProfile.Settings.Add(effect);
                        TexturesUnlimitedFXLoader.INSTANCE.enableProfileForCurrentScene();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void AddEffectParameters<T>(T effect)//TODO delete
        {
            //would work... but would need to anotate each field with better min/max values and titles, and examine attributes to get those ranges
            Type t = effect.GetType();
            //get fields of class ParameterOverride
            FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            FieldInfo field;
            Type ft;
            int len = fields.Length;
            for (int i = 0; i < len; i++)
            {
                field = fields[i];
                ft = field.FieldType;
                if (ft == typeof(BoolParameter))
                {
                    BoolParameter bp = (BoolParameter)field.GetValue(effect);
                    //AddBoolParameter(field.Name)
                }
                else if (ft == typeof(FloatParameter))
                {

                }
            }
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
                if (GUILayout.Button("<", GUILayout.Width(50)))
                {
                    index--;
                    if (index < 0) { index = values.Length - 1; }
                    param.Override(values[index]);
                }
                GUILayout.Label(value.ToString(), GUILayout.Width(340));
                if (GUILayout.Button(">", GUILayout.Width(50)))
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
                if (GUILayout.Button(param.value.ToString(), GUILayout.Width(100)))
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
                string newValue = GUILayout.TextArea(textValue, GUILayout.Width(100));
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
                float sliderValue2 = GUILayout.HorizontalSlider(sliderValue, min, max, GUILayout.Width(340));
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
                string newValue = GUILayout.TextArea(textValue, GUILayout.Width(100));
                if (newValue != textValue)
                {
                    textValue = newValue;
                    if (float.TryParse(textValue, out float v))
                    {
                        param.Override(v);
                    }
                    propertyStringStorage[hash] = textValue;
                }
                float sliderValue = GUILayout.HorizontalSlider(param.value, min, max, GUILayout.Width(340));
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
            string newValue = GUILayout.TextArea(curTextVal, GUILayout.Width(100));
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

    }

}
