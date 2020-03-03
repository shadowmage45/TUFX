using System;
using System.Collections.Generic;
using System.Linq;
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

        private Dictionary<int, string> propertyStringValuesByHashCode = new Dictionary<int, string>();

        private bool selectionMode = true;

        public void Awake()
        {
            windowID = GetInstanceID();
            profileNames.Clear();
            profileNames.AddRange(TexturesUnlimitedFXLoader.INSTANCE.Profiles.Keys);
            currentScene = HighLogic.LoadedScene;
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
            addLabelRow("Current Scene: " + currentScene +" map view active: "+MapView.MapIsEnabled);
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
            editScrollPos = GUILayout.BeginScrollView(editScrollPos);
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
            AddEffect("Ambient Occlusion", ao);
            //GUILayout.BeginHorizontal();
            //GUILayout.Label("----- Ambient Occlusion", GUILayout.Width(200));
            //bool enabled = ao != null && ao.enabled;            
            //if (enabled) //if it is enabled, draw button to disable it
            //{
            //    if (GUILayout.Button("Disable", GUILayout.Width(100)))
            //    {
            //        ao.enabled.Override(false);
            //    }
            //}
            //else //else draw the button to enable it
            //{
            //    if (GUILayout.Button("Enable", GUILayout.Width(100)))
            //    {
            //        if (ao != null)
            //        {
            //            ao.enabled.Override(true);
            //        }
            //        else
            //        {
            //            ao = ScriptableObject.CreateInstance<AmbientOcclusion>();
            //            ao.enabled.Override(true);
            //            currentProfile.Settings.Add(ao);
            //        }
            //    }
            //}
            //GUILayout.EndHorizontal();
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

                GUILayout.BeginHorizontal();
                GUILayout.Label("---------------------------------------");
                GUILayout.EndHorizontal();
            }
        }

        private void renderAutoExposureSettings()
        {
            AutoExposure ae = currentProfile.GetSettingsFor<AutoExposure>();
            bool enabled = ae != null && ae.enabled;
            AddEffect("Auto Exposure", ae);
            if (enabled)
            {
                //TODO -- auto-exposure parameters
            }
        }

        private void renderBloomSettings() { }//TODO
        private void renderChromaticAberrationSettings() { }//TODO
        private void renderColorGradingSettings() { }//TODO
        private void renderDepthOfFieldSettings() { }//TODO
        private void renderGrainSettings() { }//TODO
        private void renderLensDistortionSettings() { }//TODO
        private void renderMotionBlurSettings() { }//TODO
        private void renderVignetteSettings() { }//TODO

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

        private void AddEffect<T>(string label, T effect) where T : PostProcessEffectSettings
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
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void AddEnumParameter<Tenum>(string label, ParameterOverride<Tenum> param)
        {
            //TODO
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
            //TODO
        }

        private void AddFloatParameter(string label, ParameterOverride<float> param, float min, float max)
        {
            int hash = param.GetHashCode();
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(200));
            bool enabled = param.overrideState;
            if (enabled)
            {
                if (GUILayout.Button("Disable", GUILayout.Width(100)))
                {
                    param.overrideState = false;
                }
                string textValue = string.Empty;
                if (propertyStringValuesByHashCode.ContainsKey(hash))
                {
                    textValue = propertyStringValuesByHashCode[hash];
                }
                else
                {
                    textValue = param.value.ToString();
                    propertyStringValuesByHashCode.Add(hash, textValue);
                }
                string newValue = GUILayout.TextArea(textValue, GUILayout.Width(100));
                if (newValue != textValue)
                {
                    textValue = newValue;
                    if (float.TryParse(textValue, out float v))
                    {
                        param.Override(v);
                    }
                    propertyStringValuesByHashCode[hash] = textValue;
                }
                float sliderValue = GUILayout.HorizontalSlider(param.value, min, max, GUILayout.Width(360));
                if (sliderValue != param.value)
                {
                    param.Override(sliderValue);
                    textValue = sliderValue.ToString();
                    propertyStringValuesByHashCode[hash] = textValue;
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
            //TODO
        }

        private void AddVector2Parameter(string label, ParameterOverride<Vector2> param) { }
        private void AddVector4Parameter(string label, ParameterOverride<Vector4> param) { }
        private void AddSplineParameter(string label, ParameterOverride<Spline> param) { }
        private void AddTextureParameter(string label, ParameterOverride<Texture> param) { }

    }

}
