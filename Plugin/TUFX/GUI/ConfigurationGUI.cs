using ClickThroughFix;
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
        private Vector2 texScrollPos = new Vector2();

        /// <summary>
        /// Cached list of all profile names currently loaded at the time the GUI was created.
        /// </summary>
        private List<string> profileNames = new List<string>();

        /// <summary>
        /// Cached dictionaries of temporary variables used by the UI for user-input values.
        /// </summary>
        private Dictionary<string, string> propertyStringStorage = new Dictionary<string, string>();
        private Dictionary<string, float> propertyFloatStorage = new Dictionary<string, float>();
        private Dictionary<string, bool> effectBoolStorage = new Dictionary<string, bool>();

        /// <summary>
        /// Available textures for the curret 'select texture' assignment.
        /// </summary>
        private List<Texture2D> textures = new List<Texture2D>();
        /// <summary>
        /// Values used by texture selection mode for UI display
        /// </summary>
        private string effect, property, texture;
        /// <summary>
        /// Callback for when texture is selected...
        /// </summary>
        private Action<Texture2D> textureUpdateCallback = null;

        enum GUIMode
        {
            SelectProfile,
            EditProfile,
            SelectTexture,
            EditSpline,
        }

        private GUIMode selectionMode = 0;

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
                windowRect = ClickThruBlocker.GUIWindow(windowID, windowRect, updateWindow, "TUFXSettings");
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

        void DrawHeader()
        {
            var currentProfile = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile;
            var allProfilers = TexturesUnlimitedFXLoader.INSTANCE.Profiles;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Mode: ", GUILayout.Width(100));
			GUIMode selectionMode = this.selectionMode;
			if (selectionMode == GUIMode.SelectProfile)
			{
				GUILayout.Label("Selection", GUILayout.Width(100));
				if (GUILayout.Button("Change to Edit Mode"))
				{
					this.selectionMode = GUIMode.EditProfile;
				}
			}
			else if (selectionMode == GUIMode.EditProfile)
			{
				GUILayout.Label("Edit", GUILayout.Width(100));
				if (GUILayout.Button("Change to Select Mode", GUILayout.Width(200)))
				{
					this.selectionMode = GUIMode.SelectProfile;
				}
			}
			else //texture or spline edit modes
			{
				GUILayout.Label("Parameter", GUILayout.Width(100));
				if (GUILayout.Button("Return to Edit Mode", GUILayout.Width(200)))
				{
					this.selectionMode = GUIMode.EditProfile;
					this.textures.Clear();
					this.effect = this.property = this.texture = string.Empty;
					textureUpdateCallback = null;
				}
			}

            // save current / reload current
            if (selectionMode <= GUIMode.EditProfile && currentProfile != null)
            {
                if (GUILayout.Button("Save Selected"))
                {
                    currentProfile.SaveToDisk();
                    ScreenMessages.PostScreenMessage("<color=orange>Saved selected profile to cfg</color>", 5f, ScreenMessageStyle.UPPER_LEFT);
                }
                if (GUILayout.Button("Reload Selected"))
                {
                    currentProfile.ReloadFromNode();
                    TexturesUnlimitedFXLoader.INSTANCE.RefreshCameras();
                }
            }

            if (selectionMode == GUIMode.SelectProfile)
            {
                if (GUILayout.Button("Save All"))
                {
					foreach (var profile in allProfilers.Values)
					{
						profile.SaveToDisk();
					}
                    ScreenMessages.PostScreenMessage("<color=orange>Saved all profiles to cfg files</color>", 5f, ScreenMessageStyle.UPPER_LEFT);
                }
                if (GUILayout.Button("Reload All"))
                {
                    foreach (var profile in allProfilers.Values)
                    {
                        profile.ReloadFromNode();
                    }
					TexturesUnlimitedFXLoader.INSTANCE.RefreshCameras();
				}
            }

			if (GUILayout.Button("Close Window"))
			{
				TexturesUnlimitedFXLoader.INSTANCE.CloseConfigGui();
			}
			GUILayout.EndHorizontal();

		}

		private void updateWindow(int id)
        {
            DrawHeader();

            if (selectionMode == 0)
            {
                renderSelectionWindow();
            }
            else if (selectionMode == GUIMode.EditProfile)
            {
                renderConfigurationWindow();
            }
            else if (selectionMode == GUIMode.SelectTexture)
            {
                renderTextureSelectWindow();
            }
            else if (selectionMode == GUIMode.EditSpline)
            {
                renderSplineConfigurationWindow();
            }
            GUI.DragWindow();
        }

        private void renderSelectionWindow()
        {
            AddLabelRow("Current Scene: " + HighLogic.LoadedScene +" map view active: " + MapView.MapIsEnabled + " internal cam active: " + (InternalCamera.Instance != null && InternalCamera.Instance.isActive));
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
                    TexturesUnlimitedFXLoader.INSTANCE.setProfileForScene(newProfileName, HighLogic.LoadedScene, true);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void renderConfigurationWindow()
        {
            //if no profile is selected, disable the edit mode
            //there are cases where there can be no active profile if the default profiles were removed or edited out of the persistence data
            //should generally not occur, but just in case...
            if (TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("No Profile Selected!");
                GUILayout.EndHorizontal();
                return;
            }
            editScrollPos = GUILayout.BeginScrollView(editScrollPos, false, true, (GUILayoutOption[])null);
            renderGeneralSettings();
            renderAmbientOcclusionSettings();
            renderAutoExposureSettings();
            renderBloomSettings();
            renderChromaticAberrationSettings();
            renderColorGradingSettings();
            renderDepthOfFieldSettings();
            renderGrainSettings();
            renderLensDistortionSettings();
            renderMotionBlurSettings();
            renderScatteringSettings();
            renderVignetteSettings();
            GUILayout.EndScrollView();
        }

        private void renderTextureSelectWindow()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Effect: " + effect);
            GUILayout.Label("Property: " + property);
            GUILayout.Label("Current: " + texture);
            texScrollPos = GUILayout.BeginScrollView(texScrollPos);
            int len = textures.Count;
            for (int i = 0; i < len; i++)
            {
                if (GUILayout.Button(textures[i].name, GUILayout.Width(340)))
                {
                    textureUpdateCallback?.Invoke(textures[i]);
                    this.texture = textures[i].name;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void renderSplineConfigurationWindow()//TODO - spline configuration window
        {

        }

        private void initializeTextureSelectMode(string effectName, string propertyName, string currentTextureName, Action<Texture2D> onSelect)
        {
            effect = property = texture = string.Empty;
            textureUpdateCallback = null;
            textures.Clear();
            TUFXEffectTextureList list;
            if (!TexturesUnlimitedFXLoader.INSTANCE.EffectTextureLists.TryGetValue(effectName, out list))
            {
                this.selectionMode = GUIMode.EditProfile;
                return;
            }
            textures.AddRange(list.GetTextures(propertyName));
            effect = effectName;
            property = propertyName;
            texture = currentTextureName;
            textureUpdateCallback = onSelect;
        }

        #region REGION Effect Settings Rendering

        private void renderGeneralSettings()
        {
            bool enabled = true;
            bool showProps = DrawGroupHeader("General Settings", ref enabled);

            if (showProps)
            {
                renderHDRSettings();
                renderAntialiasingSettings();
            }
            GUILayout.EndVertical();
        }

        private void renderHDRSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("HDR", GUILayout.Width(200));
            bool enabled = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.HDREnabled;
            string buttonText = enabled ? "Disable" : "Enable";
			if (GUILayout.Button(buttonText, GUILayout.Width(100)))
			{
                TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.HDREnabled = !enabled;
				TexturesUnlimitedFXLoader.INSTANCE.RefreshCameras();
			}

			GUILayout.EndHorizontal();
        }

        private void renderAntialiasingSettings()
        {
            bool primaryChanged = AddEnumField("Primary Camera Antialiasing", ref TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.AntiAliasing);
            bool secondaryChanged = AddEnumField("Secondary Camera Antialiasing", ref TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.SecondaryCameraAntialiasing);

			if (primaryChanged || secondaryChanged)
            {
                TexturesUnlimitedFXLoader.INSTANCE.RefreshCameras();
            }
            //TODO -- add parameters for the AA modes
            //if (mode == PostProcessLayer.Antialiasing.FastApproximateAntialiasing)
            //{
            //    //AddBoolField("FXAA Fast Mode", ref layer.fastApproximageAntialiasing.fastMode);
            //    //AddBoolField("FXAA Keep Alpha", ref layer.fastApproximageAntialiasing.keepAlpha);
            //}
        }

        private void renderAmbientOcclusionSettings()
        {
            bool showProps = AddEffectHeader("Ambient Occlusion", out AmbientOcclusion ao);
            if (showProps)
            {
                AddEnumParameter("Mode", ao.mode);
                AddFloatParameter("Intensity", ao.intensity, 0, 10);
                AddColorParameter("Color", ao.color);
                AddBoolParameter("Ambient Only", ao.ambientOnly);
                AddFloatParameter("NoiseFilterTolerance", ao.noiseFilterTolerance, -8, 0);
                AddFloatParameter("BlurTolerance", ao.blurTolerance, -8, -1);
                AddFloatParameter("UpsampleTolerance", ao.upsampleTolerance, -12, -1);
                AddFloatParameter("ThicknessModifier", ao.thicknessModifier, 0, 5);
                AddFloatParameter("ZBias", ao.zBias, 0, 1);
                AddFloatParameter("DirectLightStr", ao.directLightingStrength, 0, 1);
                AddFloatParameter("Radius", ao.radius, 0, 1);
            }
            GUILayout.EndVertical();
        }

        private void renderAutoExposureSettings()
        {
            bool showProps = AddEffectHeader("Auto Exposure", out AutoExposure ae);
            if (showProps)
            {
                AddVector2Parameter("Filtering", ae.filtering);
                AddFloatParameter("Min Luminance", ae.minLuminance, -9, 9);
                AddFloatParameter("Max Luminance", ae.maxLuminance, -9, 9);
                AddFloatParameter("Key Value", ae.keyValue, 0, 10);
                AddEnumParameter("Eye Adaption", ae.eyeAdaptation);
                AddFloatParameter("Speed Up", ae.speedUp, 0, 10);
                AddFloatParameter("Speed Down", ae.speedDown, 0, 10);
            }
            GUILayout.EndVertical();
        }

        private void renderBloomSettings()
        {
            bool showProps = AddEffectHeader("Bloom", out Bloom bl);
            if (showProps)
            {
                AddFloatParameter("Intensity", bl.intensity, 0, 10);
                AddFloatParameter("Threshold", bl.threshold, 0, 2);
                AddFloatParameter("SoftKnee", bl.softKnee, 0, 1);
                AddFloatParameter("Clamp", bl.clamp, 0, 64000);
                AddFloatParameter("Diffusion", bl.diffusion, 0, 20);
                AddFloatParameter("Anamorphic Ratio", bl.anamorphicRatio, -1, 1);
                AddColorParameter("Color", bl.color);
                AddBoolParameter("Fast Mode", bl.fastMode);
                AddTextureParameter("Dirt Texture", bl.dirtTexture, BuiltinEffect.Bloom.ToString(), "DirtTexture");
                AddFloatParameter("Dirt Intensity", bl.dirtIntensity, 0, 2);
            }
            GUILayout.EndVertical();
        }

        private void renderChromaticAberrationSettings()
        {
            bool showProps = AddEffectHeader("Chromatic Aberration", out ChromaticAberration ca);
            if (showProps)
            {
                AddTextureParameter("Spectral LUT", ca.spectralLut, BuiltinEffect.ChromaticAberration.ToString(), "SpectralLut");
                AddFloatParameter("Intensity", ca.intensity, 0, 1);
                AddBoolParameter("Fast Mode", ca.fastMode);
            }
            GUILayout.EndVertical();
        }

        private void renderColorGradingSettings()
        {
            bool showProps = AddEffectHeader("Color Grading", out ColorGrading cg);
            if (showProps)
            {
                AddEnumParameter("Mode", cg.gradingMode);
                if (cg.gradingMode == GradingMode.External)
                {
                    AddTextureParameter("External LUT", cg.externalLut, BuiltinEffect.ColorGrading.ToString(), "ExternalLut");
                }
                else if (cg.gradingMode == GradingMode.LowDefinitionRange)
                {
                    AddTextureParameter("LDR LUT", cg.ldrLut, BuiltinEffect.ColorGrading.ToString(), "LdrLut");
                    AddFloatParameter("LDR LUT Contrib.", cg.ldrLutContribution, 0, 1);
                }
                else if (cg.gradingMode == GradingMode.HighDefinitionRange)
                {
                    AddEnumParameter("Tonemapper", cg.tonemapper);
                    if (cg.tonemapper == Tonemapper.Custom)
                    {
                        AddFloatParameter("T.Curve Toe Strength", cg.toneCurveToeStrength, 0, 1);
                        AddFloatParameter("T.Curve Toe Length", cg.toneCurveToeLength, 0, 1);
                        AddFloatParameter("T.Curve Shd Strength", cg.toneCurveShoulderStrength, 0, 1);
                        AddFloatParameter("T.Curve Shd Length", cg.toneCurveShoulderLength, 0, 64000);
                        AddFloatParameter("T.Curve Shd Angle", cg.toneCurveShoulderAngle, 0, 1);
                        AddFloatParameter("Tone Curve Gamma", cg.toneCurveGamma, 0.001f, 64000);
                    }
                }
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

                if (cg.gradingMode == GradingMode.LowDefinitionRange)
                {
                    AddSplineParameter("MasterCurve", cg.masterCurve);
                    AddSplineParameter("RedCurve", cg.redCurve);
                    AddSplineParameter("GreenCurve", cg.greenCurve);
                    AddSplineParameter("BlueCurve", cg.blueCurve);
                }

                AddSplineParameter("HueVsHueCurve", cg.hueVsHueCurve);
                AddSplineParameter("HueVsSatCurve", cg.hueVsSatCurve);
                AddSplineParameter("SatVsSatCurve", cg.satVsSatCurve);
                AddSplineParameter("LumVsSatCurve", cg.lumVsSatCurve);
            }
            GUILayout.EndVertical();
        }

        private void renderDepthOfFieldSettings()
        {
            bool showProps = AddEffectHeader("Depth Of Field", out DepthOfField df);
            if (showProps)
            {
                AddFloatParameter("Focus Distance", df.focusDistance, 0.1f, 64000);
                AddFloatParameter("Aperture", df.aperture, 0.05f, 32f);
                AddFloatParameter("Focal Length", df.focalLength, 1f, 300f);
                AddEnumParameter("Kernel Size", df.kernelSize);
                AddBoolParameter("Use Camera Fov", df.useCameraFov);
            }
            GUILayout.EndVertical();
        }

        private void renderGrainSettings()
        {
            bool showProps = AddEffectHeader("Grain", out Grain gr);
            if (showProps)
            {
                AddBoolParameter("Colored", gr.colored);
                AddFloatParameter("Intensity", gr.intensity, 0, 1);
                AddFloatParameter("Size", gr.size, 0.3f, 3f);
                AddFloatParameter("Lum. Contrib", gr.lumContrib, 0, 1);
            }
            GUILayout.EndVertical();
        }

        private void renderLensDistortionSettings()
        {
            bool showProps = AddEffectHeader("Lens Distortion", out LensDistortion ld);
            if (showProps)
            {
                AddFloatParameter("Intensity", ld.intensity, -100, 100);
                AddFloatParameter("IntensityX", ld.intensityX, 0, 1);
                AddFloatParameter("IntensityY", ld.intensityY, 0, 1);
                AddFloatParameter("CenterX", ld.centerX, -1, 1);
                AddFloatParameter("CenterY", ld.centerY, -1, 1);
                AddFloatParameter("Scale", ld.scale, 0.01f, 5f);
            }
            GUILayout.EndVertical();
        }

        private void renderMotionBlurSettings()
        {
            bool showProps = AddEffectHeader("Motion Blur", out MotionBlur mb);
            if (showProps)
            {
                AddFloatParameter("Shutter Angle", mb.shutterAngle, 0f, 360f);
                AddIntParameter("Sample Count", mb.sampleCount, 4, 32);
            }
            GUILayout.EndVertical();
        }

        private void renderScatteringSettings()
        {
            //Log.debug("SC start");
            bool showProps = AddEffectHeader("Scattering", out TUBISEffect sc);
            //if (enabled != sc.enabled)
            //{
            //    //TODO
            //}
            if (showProps)
            {
                AddFloatParameter("Exposure", sc.Exposure, 0f, 50f);
            }
            GUILayout.EndVertical();
            //Log.debug("SC end");
        }

        private void renderVignetteSettings()
        {
            bool showProps = AddEffectHeader("Vignette", out Vignette vg);
            if (showProps)
            {
                AddEnumParameter("Mode", vg.mode);
                AddColorParameter("Color", vg.color);
                AddVector2Parameter("Center", vg.center);
                AddFloatParameter("Intensity", vg.intensity, 0, 1);
                AddFloatParameter("Smoothness", vg.smoothness, 0.01f, 1f);
                AddFloatParameter("Roundness", vg.roundness, 0, 1);
                AddBoolParameter("Rounded", vg.rounded);
                AddTextureParameter("Mask", vg.mask, BuiltinEffect.Vignette.ToString(), "Mask");
                AddFloatParameter("Opacity", vg.opacity, 0, 1);
            }
            GUILayout.EndVertical();
        }

        #endregion

        #region REGION - Parameter Rendering Methods

        private bool DrawGroupHeader(string label, ref bool enabled)
        {
			GUILayout.BeginVertical(HighLogic.Skin.box);
            GUILayout.BeginVertical(HighLogic.Skin.box);
            GUILayout.BeginHorizontal();

			if (!effectBoolStorage.TryGetValue(label, out bool showProps))
            {
                effectBoolStorage.Add(label, showProps = true);
            }

            if (GUILayout.Button(showProps ? "v" : ">", GUILayout.Width(20)))
            {
                showProps = !showProps;
				effectBoolStorage[label] = showProps;
			}

			enabled = GUILayout.Toggle(enabled, label);

			GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            return showProps && enabled;
        }

		private bool AddEffectHeader<T>(string label, out T effect) where T : PostProcessEffectSettings
        {
            effect = TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.GetSettingsFor<T>();

            bool effectEnabled = effect != null && effect.enabled;
            bool showProps = DrawGroupHeader(label, ref effectEnabled);

            if (effectEnabled && effect == null)
            {
                effect = ScriptableObject.CreateInstance<T>();
                TexturesUnlimitedFXLoader.INSTANCE.CurrentProfile.Settings.Add(effect);
				TexturesUnlimitedFXLoader.INSTANCE.enableProfileForCurrentScene();
			}

            if (effect)
            {
                effect.enabled.Override(effectEnabled);
            }

            
            return showProps;
        }

        bool DrawParamToggle(string label, ParameterOverride param)
        {
			param.overrideState = GUILayout.Toggle(param.overrideState, label, GUILayout.Width(200), GUILayout.Height(22));
            return param.overrideState;
		}

		private void AddEnumParameter<Tenum>(string label, ParameterOverride<Tenum> param)
        {
            Tenum value = param.value;
            Type type = value.GetType();
            Tenum[] values = (Tenum[])Enum.GetValues(type);
            int index = values.IndexOf(value);

            GUILayout.BeginHorizontal();
			if (DrawParamToggle(label, param))
            {
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
            
            GUILayout.EndHorizontal();
        }

        private bool AddEnumField<Tenum>(string label, ref Tenum param)
        {
            Tenum value = param;
            Type type = value.GetType();
            Tenum[] values = (Tenum[])Enum.GetValues(type);
            int index = values.IndexOf(value);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(300));
            bool changed = false;
            if (GUILayout.Button("<", GUILayout.Width(110)))
            {
                index--;
                if (index < 0) { index = values.Length - 1; }
                param = (values[index]);
                changed = true;
            }
            GUILayout.Label(value.ToString(), GUILayout.Width(220));
            if (GUILayout.Button(">", GUILayout.Width(110)))
            {
                index++;
                if (index >= values.Length) { index = 0; }
                param = (values[index]);
                changed = true;
            }
            GUILayout.EndHorizontal();
            return changed;
        }

        private void AddBoolParameter(string label, ParameterOverride<bool> param)
        {
            GUILayout.BeginHorizontal();

            if (DrawParamToggle(label, param))
            {
                if (GUILayout.Button(param.value.ToString(), GUILayout.Width(110)))
                {
                    param.value = !param.value;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void AddIntParameter(string label, ParameterOverride<int> param, int min, int max)
        {
            GUILayout.BeginHorizontal();
            if (DrawParamToggle(label, param))
            {
                string hash = param.GetHashCode().ToString();
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
            GUILayout.EndHorizontal();
        }

        private void AddFloatParameter(string label, ParameterOverride<float> param, float min, float max)
        {
            GUILayout.BeginHorizontal();
            if (DrawParamToggle(label, param))
            {
                string hash = param.GetHashCode().ToString();
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
            GUILayout.EndHorizontal();
        }

        private void AddColorParameter(string label, ParameterOverride<Color> param)
        {
            GUILayout.BeginHorizontal();
            if (DrawParamToggle(label, param))
            {
                string hash = param.GetHashCode().ToString();
                AddColorInput("Red", hash, ref param.value.r);
                AddColorInput("Green", hash, ref param.value.g);
                AddColorInput("Blue", hash, ref param.value.b);
                AddColorInput("Alpha", hash, ref param.value.a);
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
            if (DrawParamToggle(label, param))
            {
                string hash = param.GetHashCode().ToString();
                AddColorInput("X", hash, ref param.value.x);
                AddColorInput("Y", hash, ref param.value.y);
            }
            GUILayout.EndHorizontal();
        }

        private void AddVector4Parameter(string label, ParameterOverride<Vector4> param)
        {
            GUILayout.BeginHorizontal();
            if (DrawParamToggle(label, param))
            {
                string hash = param.GetHashCode().ToString();
                AddColorInput("X", hash, ref param.value.x);
                AddColorInput("Y", hash, ref param.value.y);
                AddColorInput("Z", hash, ref param.value.z);
                AddColorInput("W", hash, ref param.value.w);
            }
            GUILayout.EndHorizontal();
        }

        private void AddSplineParameter(string label, ParameterOverride<Spline> param)//TODO spine parameter configuration rendering
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("TODO - Spline parameter.");
            GUILayout.EndHorizontal();
        }

        private void AddTextureParameter(string label, ParameterOverride<Texture> param, string effect, string paramName)
        {
            GUILayout.BeginHorizontal();
            if (DrawParamToggle(label, param))
            {
                string texLabel = param.value == null ? "Nothing selected" : param.value.name;
                if (GUILayout.Button(texLabel, GUILayout.Width(440)))
                {
                    this.selectionMode = GUIMode.SelectTexture;
                    this.texScrollPos = new Vector2();
                    Action<Texture2D> update = (a) => 
                    {
                        param.Override(a);
                    };
                    initializeTextureSelectMode(effect, paramName, texLabel, update);
                }
            }
            GUILayout.EndHorizontal();
        }

        #endregion

    }

}
