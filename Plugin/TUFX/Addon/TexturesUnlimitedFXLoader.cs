using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace TUFX
{

    /// <summary>
    /// The main KSPAddon that holds profile loading and handling logic, resource reference storage,
    /// provides the public functions to update and enable profiles, and manages ApplicationLauncher button and GUI spawning.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class TexturesUnlimitedFXLoader : MonoBehaviour
    {

        internal static TexturesUnlimitedFXLoader INSTANCE;
        private static ApplicationLauncherButton configAppButton;
        private static ApplicationLauncherButton debugAppButton;
        private ConfigurationGUI configGUI;
        private DebugGUI debugGUI;

        private Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        private Dictionary<string, ComputeShader> computeShaders = new Dictionary<string, ComputeShader>();
        private Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        internal Dictionary<string, TUFXEffectTextureList> EffectTextureLists { get; private set; } = new Dictionary<string, TUFXEffectTextureList>();

        /// <summary>
        /// The currently loaded profiles.
        /// This will be cleared and reset whenever ModuleManagerPostLoad() is called (e.g. in-game config reload).
        /// </summary>
        internal Dictionary<string, TUFXProfile> Profiles { get; private set; } = new Dictionary<string, TUFXProfile>();

        public class Configuration
        {
            [Persistent] public string MainMenuProfile = "Default-MainMenu";
            [Persistent] public string SpaceCenterSceneProfile = "Default-KSC";
            [Persistent] public string EditorSceneProfile = "Default-Editor";
            [Persistent] public string FlightSceneProfile = "Default-Flight";
            [Persistent] public string MapSceneProfile = "Default-Tracking";
            [Persistent] public string IVAProfile = "Default-Flight";
            [Persistent] public string TrackingStationProfile = "Default-Tracking";
            [Persistent] public bool ShowToolbarButton = true;
        }

		internal static readonly Configuration defaultConfiguration = new Configuration();

        private PostProcessVolume mainVolume;
        private PostProcessVolume scaledSpaceVolume;

        /// <summary>
        /// The currently active profile.  Private field to enforce use of the 'setProfileForScene' method.
        /// </summary>
        private TUFXProfile currentProfile;
        /// <summary>
        /// Return a reference to the currently active profile.  To update the current profile, use the <see cref="setProfileForScene(string name, GameScenes scene, bool map, bool apply)"/> method.
        /// </summary>
        internal TUFXProfile CurrentProfile => currentProfile;
        /// <summary>
        /// Return the name of the currently active profile.
        /// </summary>
        internal string CurrentProfileName => currentProfile==null ? string.Empty : currentProfile.ProfileName;

        /// <summary>
        /// Reference to the Unity Post Processing 'Resources' class.  Used to store references to the shaders and textures used by the post-processing system internals.
        /// Does not include references to the 'included' but 'external' resources such as the built-in lens-dirt textures or any custom LUTs.
        /// </summary>
        public static PostProcessResources Resources { get; private set; }

        private PostProcessVolume CreateVolume(int layer)
        {
            var childObject = new GameObject();
            childObject.layer = layer;
            childObject.transform.SetParent(transform, false);
            var volume = childObject.AddComponent<PostProcessVolume>();
            volume.isGlobal = true;
            return volume;
        }

        public void Start()
        {
            MonoBehaviour.print("TUFXLoader - Start()");
            INSTANCE = this;
            DontDestroyOnLoad(this);
            GameEvents.onLevelWasLoaded.Add(new EventData<GameScenes>.OnEvent(onLevelLoaded));
            GameEvents.OnCameraChange.Add(new EventData<CameraManager.CameraMode>.OnEvent(cameraChange));

            mainVolume = CreateVolume(0);
            scaledSpaceVolume = CreateVolume(1);
        }

		public void ModuleManagerPostLoad()
        {
            Log.log("TUFXLoader - MMPostLoad()");

            //only load resources once.  In case of MM reload...
            if (Resources == null)
            {
                loadResources();
            }

            loadTextures();

            loadProfiles();

            //If configs are reloaded via module-manager from the space center scene... reload and reapply the currently selected profile from game persistence data
            //if for some reason that profile does not exist, nothing will be applied and an error will be logged.
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                enableProfileForCurrentScene();
            }
        }

        /// <summary>
        /// Loads the mandatory shaders and textures required by the post-processing stack codebase and effects from the AssetBundles included in the mod.
        /// </summary>
        private void loadResources()
        {
            Resources = ScriptableObject.CreateInstance<PostProcessResources>();
            Resources.shaders = new PostProcessResources.Shaders();
            Resources.computeShaders = new PostProcessResources.ComputeShaders();
            Resources.blueNoise64 = new Texture2D[64];
            Resources.blueNoise256 = new Texture2D[8];
            Resources.smaaLuts = new PostProcessResources.SMAALuts();

            //previously this did not work... but appears to with these bundles/Unity version
            AssetBundle bundle = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData/TUFX/Shaders/tufx-universal.ssf");
            Shader[] shaders = bundle.LoadAllAssets<Shader>();
            int len = shaders.Length;
            for (int i = 0; i < len; i++)
            {
                if (!this.shaders.ContainsKey(shaders[i].name)) { this.shaders.Add(shaders[i].name, shaders[i]); }
            }
            ComputeShader[] compShaders = bundle.LoadAllAssets<ComputeShader>();
            len = compShaders.Length;
            for (int i = 0; i < len; i++)
            {
                if (!this.computeShaders.ContainsKey(compShaders[i].name)) { this.computeShaders.Add(compShaders[i].name, compShaders[i]); }
            }
            bundle.Unload(false);

            //TODO -- cleanup loading
            try
            {
                bundle = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData/TUFX/Shaders/tufx-scattering.ssf");
                shaders = bundle.LoadAllAssets<Shader>();
                len = shaders.Length;
                for (int i = 0; i < len; i++)
                {
                    if (!this.shaders.ContainsKey(shaders[i].name))
                    {
                        this.shaders.Add(shaders[i].name, shaders[i]);
                        Log.debug("Loading scattering shader: " + shaders[i].name);
                    }
                }
                compShaders = bundle.LoadAllAssets<ComputeShader>();
                len = compShaders.Length;
                for (int i = 0; i < len; i++)
                {
                    if (!this.computeShaders.ContainsKey(compShaders[i].name))
                    {
                        this.computeShaders.Add(compShaders[i].name, compShaders[i]);
                        Log.debug("Loading scattering compute shader: " + compShaders[i].name);
                    }
                }
                bundle.Unload(false);
                TUFXScatteringResources.PrecomputeShader = getComputeShader("Precomputation"); ;
                TUFXScatteringResources.ScatteringShader = getShader("TU/BIS");
            }
            catch(Exception e)
            {
                Log.debug(e.ToString());
            }

            #region REGION - Load standard Post Process Effect Shaders
            Resources.shaders.bloom = getShader("Hidden/PostProcessing/Bloom");
            Resources.shaders.copy = getShader("Hidden/PostProcessing/Copy");
            Resources.shaders.copyStd = getShader("Hidden/PostProcessing/CopyStd");
            Resources.shaders.copyStdFromDoubleWide = getShader("Hidden/PostProcessing/CopyStdFromDoubleWide");
            Resources.shaders.copyStdFromTexArray = getShader("Hidden/PostProcessing/CopyStdFromTexArray");
            Resources.shaders.deferredFog = getShader("Hidden/PostProcessing/DeferredFog");
            Resources.shaders.depthOfField = getShader("Hidden/PostProcessing/DepthOfField");
            Resources.shaders.discardAlpha = getShader("Hidden/PostProcessing/DiscardAlpha");
            Resources.shaders.finalPass = getShader("Hidden/PostProcessing/FinalPass");
            Resources.shaders.gammaHistogram = getShader("Hidden/PostProcessing/Debug/Histogram");//TODO - part of debug shaders?
            Resources.shaders.grainBaker = getShader("Hidden/PostProcessing/GrainBaker");
            Resources.shaders.lightMeter = getShader("Hidden/PostProcessing/Debug/LightMeter");//TODO - part of debug shaders?
            Resources.shaders.lut2DBaker = getShader("Hidden/PostProcessing/Lut2DBaker");
            Resources.shaders.motionBlur = getShader("Hidden/PostProcessing/MotionBlur");
            Resources.shaders.multiScaleAO = getShader("Hidden/PostProcessing/MultiScaleVO");
            Resources.shaders.scalableAO = getShader("Hidden/PostProcessing/ScalableAO");
            Resources.shaders.screenSpaceReflections = getShader("Hidden/PostProcessing/ScreenSpaceReflections");
            Resources.shaders.subpixelMorphologicalAntialiasing = getShader("Hidden/PostProcessing/SubpixelMorphologicalAntialiasing");
            Resources.shaders.temporalAntialiasing = getShader("Hidden/PostProcessing/TemporalAntialiasing");
            Resources.shaders.texture2dLerp = getShader("Hidden/PostProcessing/Texture2DLerp");
            Resources.shaders.uber = getShader("Hidden/PostProcessing/Uber");
            Resources.shaders.vectorscope = getShader("Hidden/PostProcessing/Debug/Vectorscope");//TODO - part of debug shaders?
            Resources.shaders.waveform = getShader("Hidden/PostProcessing/Debug/Waveform");//TODO - part of debug shaders?
            #endregion

            #region REGION - Load compute shaders
            Resources.computeShaders.autoExposure = getComputeShader("AutoExposure");
            Resources.computeShaders.exposureHistogram = getComputeShader("ExposureHistogram");
            Resources.computeShaders.gammaHistogram = getComputeShader("GammaHistogram");//TODO - part of debug shaders?
            Resources.computeShaders.gaussianDownsample = getComputeShader("GaussianDownsample");
            Resources.computeShaders.lut3DBaker = getComputeShader("Lut3DBaker");
            Resources.computeShaders.multiScaleAODownsample1 = getComputeShader("MultiScaleVODownsample1");
            Resources.computeShaders.multiScaleAODownsample2 = getComputeShader("MultiScaleVODownsample2");
            Resources.computeShaders.multiScaleAORender = getComputeShader("MultiScaleVORender");
            Resources.computeShaders.multiScaleAOUpsample = getComputeShader("MultiScaleVOUpsample");
            Resources.computeShaders.texture3dLerp = getComputeShader("Texture3DLerp");
            Resources.computeShaders.vectorscope = getComputeShader("Vectorscope");//TODO - part of debug shaders?
            Resources.computeShaders.waveform = getComputeShader("Waveform");//TODO - part of debug shaders?
            #endregion

            #region REGION - Load textures
            bundle = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData/TUFX/Textures/tufx-tex-bluenoise64.ssf");
            Texture2D[] tex = bundle.LoadAllAssets<Texture2D>();
            len = tex.Length;
            for (int i = 0; i < len; i++)
            {
                string idxStr = tex[i].name.Substring(tex[i].name.Length - 2).Replace("_", "");
                int idx = int.Parse(idxStr);
                Resources.blueNoise64[idx] = tex[i];
            }
            bundle.Unload(false);

            bundle = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData/TUFX/Textures/tufx-tex-bluenoise256.ssf");
            tex = bundle.LoadAllAssets<Texture2D>();
            len = tex.Length;
            for (int i = 0; i < len; i++)
            {
                string idxStr = tex[i].name.Substring(tex[i].name.Length - 2).Replace("_", "");
                int idx = int.Parse(idxStr);
                Resources.blueNoise256[idx] = tex[i];
            }
            bundle.Unload(false);

            bundle = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData/TUFX/Textures/tufx-tex-smaa.ssf");
            tex = bundle.LoadAllAssets<Texture2D>();
            len = tex.Length;
            for (int i = 0; i < len; i++)
            {
                if (tex[i].name == "AreaTex") { Resources.smaaLuts.area = tex[i]; }
                else { Resources.smaaLuts.search = tex[i]; }
            }
            bundle.Unload(false);
            #endregion
        }

        private void loadTextures()
        {
            //yeah, wow, that got ugly fast...
            EffectTextureLists.Clear();
            ConfigNode[] textureListNodes = GameDatabase.Instance.GetConfigNodes("TUFX_TEXTURES");
            int len = textureListNodes.Length;
            for (int i = 0; i < len; i++)
            {
                Log.debug("Loading TUFX_TEXTURES[" + textureListNodes[i].GetValue("name") + "]");
                ConfigNode[] effectTextureLists = textureListNodes[i].GetNodes("EFFECT");
                int len2 = effectTextureLists.Length;
                for (int k = 0; k < len2; k++)
                {
                    string effectName = effectTextureLists[k].GetValue("name");
                    Log.debug("Loading EFFECT[" + effectName + "]");
                    if (!this.EffectTextureLists.TryGetValue(effectName, out TUFXEffectTextureList etl))
                    {
                        this.EffectTextureLists[effectName] = etl = new TUFXEffectTextureList();
                    }
                    string[] names = effectTextureLists[k].values.DistinctNames();
                    int len3 = names.Length;
                    for (int m = 0; m < len3; m++)
                    {
                        string propName = names[m];
                        if (propName == "name") { continue; }//don't load textures for the 'name=' entry in the configs
                        Log.debug("Loading Textures for property [" + propName + "]");
                        string[] values = effectTextureLists[k].GetValues(propName);
                        int len4 = values.Length;
                        for (int r = 0; r < len4; r++)
                        {
                            string texName = values[r];
                            Log.debug("Loading Texture for name [" + texName + "]");
                            Texture2D tex = GameDatabase.Instance.GetTexture(texName, false);
                            if (tex != null)
                            {
                                if (!etl.ContainsTexture(propName, tex))
                                {
                                    etl.AddTexture(propName, tex);
                                }
                                else
                                {
                                    Log.log("Ignoring duplicate texture: " + texName + " for effect: " + effectName + " property: " + propName);
                                }
                            }
                            else
                            {
                                Log.exception("Texture specified by path: " + texName + " was not found when attempting to load textures for effect: " + effectName + " propertyName: " + propName);
                            }
                        }
                    }
                }
            }
        }

        private void loadProfiles()
        {
            //discard the existing profile reference, if any
            currentProfile = null;
            //clear profiles in case of in-game reload
            Profiles.Clear();
            //grab all profiles detected in global scope config nodes, load them into local storage
            foreach (var profileConfig in GameDatabase.Instance.root.GetConfigs("TUFX_PROFILE"))
            {
                TUFXProfile profile = new TUFXProfile(profileConfig);
                if (!Profiles.ContainsKey(profile.ProfileName))
                {
                    Profiles.Add(profile.ProfileName, profile);
                }
                else
                {
                    Log.exception("TUFX Profiles already contains profile for name: " + profile.ProfileName + ".  This is the result of a configuration with" +
                        " a duplicate name; please check your configurations and remove any duplicates.  Only the first configuration parsed for any one name will be loaded.");
                }
            }
            ConfigNode config = GameDatabase.Instance.GetConfigNodes("TUFX_CONFIGURATION").FirstOrDefault(m=>m.GetValue("name")=="Default");
            if (config != null)
            {
                ConfigNode.LoadObjectFromConfig(defaultConfiguration, config);
            }
        }

        /// <summary>
        /// Internal function to retrieve a shader from the dictionary, by name.  These names will include the package level prefixing, e.g. 'Unity/Foo/Bar/ShaderName'
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal Shader getShader(string name)
        {
            shaders.TryGetValue(name, out Shader s);
            return s;
        }

        /// <summary>
        /// Internal function to retrieve a compute shader from the dictionary, by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal ComputeShader getComputeShader(string name)
        {
            computeShaders.TryGetValue(name, out ComputeShader s);
            return s;
        }

        /// <summary>
        /// Attempts to retrieve a built-in texture by name.  The list of built-in textures is limited to lens dirt and a few LUTs; only those textures that were included in the Unity Post Process Package.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal Texture2D getTexture(string name)
        {
            textures.TryGetValue(name, out Texture2D tex);
            return tex;
        }

        /// <summary>
        /// Returns true if the input texture is present in the list of built-in textures that were loaded from asset bundles.
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        internal bool isBuiltinTexture(Texture2D tex)
        {
            return textures.Values.Contains(tex);
        }

        /// <summary>
        /// Callback for when a scene has been fully loaded.
        /// </summary>
        /// <param name="scene"></param>
        private void onLevelLoaded(GameScenes scene)
        {
            Log.debug("TUFXLoader - onLevelLoaded( "+scene+" )");

            if (scene == GameScenes.FLIGHT || scene == GameScenes.SPACECENTER || scene == GameScenes.EDITOR || scene == GameScenes.TRACKSTATION)
            {
                Log.debug("TUFX - Updating AppLauncher button...");
                Texture2D tex;
                if (configAppButton == null && defaultConfiguration.ShowToolbarButton)//static reference; track if the button was EVER created, as KSP keeps them even if the addon is destroyed
                {
                    //Create a new button
                    tex = GameDatabase.Instance.GetTexture("TUFX/Assets/TUFX-Icon1", false);
                    configAppButton = ApplicationLauncher.Instance.AddModApplication(configGuiEnable, configGuiDisable, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.MAPVIEW, tex);
                }
                else if (configAppButton != null)
                {
                    //Reseat the buttons' callback method references.  Should not be needed for this implementation, as this is a persistent AddOn.
                    configAppButton.onTrue = configGuiEnable;
                    configAppButton.onFalse = configGuiDisable;
                }
#if DEBUG
                if (debugAppButton == null)
                {
                    tex = GameDatabase.Instance.GetTexture("TUFX/Assets/TUFX-Icon2", false);
                    debugAppButton = ApplicationLauncher.Instance.AddModApplication(debugGuiEnable, debugGuiDisable, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.MAPVIEW, tex);
                }
                else if (debugAppButton != null)
                {
                    debugAppButton.onTrue = debugGuiEnable;
                    debugAppButton.onFalse = debugGuiDisable;
                }
#endif
            }
            else if (configAppButton != null)
            {
                Log.debug("TUFX - Removing AppLauncher button...");
                ApplicationLauncher.Instance.RemoveModApplication(configAppButton);
#if DEBUG
                if (debugAppButton != null)
                {
                    Log.debug("TUFX - Removing DebugLauncher button...");
                    ApplicationLauncher.Instance.RemoveModApplication(debugAppButton);
                }
#endif
            }

            configGuiDisable();
            //finally, enable the profile for the current scene
            enableProfileForCurrentScene();
        }

        private void cameraChange(CameraManager.CameraMode newCameraMode)
        {
            enableProfileForCurrentScene();
        }

        /// <summary>
        /// Public method to specify a new profile name for the input game scene (and map view setting, in the case of flight-scene).
        /// This will udpate the game persistence data with the name specified, and optionally enable the profile now.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="scene">the game scene to which the new profile should be applied</param>
        /// <param name="isMapScene">Update the 'flight map scene' if this is true and scene==flight</param>
        /// <param name="enableNow">True to enable the profile for the current scene</param>
        internal void setProfileForScene(string profile, GameScenes scene, bool enableNow = false)
        {
            switch (scene)
            {
                case GameScenes.MAINMENU:
                    Log.exception("The main menu profile must be set via config!");
                    break;
                case GameScenes.SPACECENTER:
                    HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().SpaceCenterSceneProfile = profile;
                    break;
                case GameScenes.EDITOR:
                    HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().EditorSceneProfile = profile;
                    break;
                case GameScenes.FLIGHT:
                    switch (CameraManager.Instance.currentCameraMode)
                    {
                        case CameraManager.CameraMode.Flight:
                            HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().FlightSceneProfile = profile;
                            break;
                        case CameraManager.CameraMode.Map:
                            HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().MapSceneProfile = profile;
                            break;
                        case CameraManager.CameraMode.IVA:
                        case CameraManager.CameraMode.Internal:
                            HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().IVAProfile = profile;
                            break;
                    }
                    break;
                case GameScenes.TRACKSTATION:
                    HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().TrackingStationProfile = profile;
                    break;
            }
            if (enableNow)
            {
                SetCurrentProfile(profile);
            }
        }

		public string GetProfileNameForCurrentScene(TUFXGameSettings gameSettings)
		{
			string profileName = string.Empty;
			switch (HighLogic.LoadedScene)
			{
				case GameScenes.MAINMENU:
					profileName = defaultConfiguration.MainMenuProfile;
					break;
				case GameScenes.SPACECENTER:
					profileName = gameSettings.SpaceCenterSceneProfile;
					break;
				case GameScenes.EDITOR:
					profileName = gameSettings.EditorSceneProfile;
					break;
				case GameScenes.FLIGHT:
					switch (CameraManager.Instance.currentCameraMode)
					{
						case CameraManager.CameraMode.Flight:
							profileName = gameSettings.FlightSceneProfile;
							break;
						case CameraManager.CameraMode.Map:
							profileName = gameSettings.MapSceneProfile;
							break;
						case CameraManager.CameraMode.IVA:
						case CameraManager.CameraMode.Internal:
							profileName = gameSettings.IVAProfile;
							break;
					}
					break;
				case GameScenes.TRACKSTATION:
					profileName = gameSettings.TrackingStationProfile;
					break;
			}

			return profileName;
		}

		/// <summary>
		/// Looks up the profile for the current scene from the game persistence data and attempts to enable it.
		/// </summary>
		internal void enableProfileForCurrentScene()
        {
            string profileName = GetProfileNameForCurrentScene(HighLogic.CurrentGame?.Parameters?.CustomParams<TUFXGameSettings>());
            if (string.IsNullOrEmpty(profileName) || !Profiles.ContainsKey(profileName))
            {
                Log.debug($"TUFX - game settings for scene {HighLogic.LoadedScene} named {profileName} not found; falling back to defaults");
                var defaultSettings = new TUFXGameSettings();
                profileName = GetProfileNameForCurrentScene(defaultSettings);
            }

            Log.debug("TUFX - Enabling profile for current scene: " + HighLogic.LoadedScene + " profile: " + profileName);
            SetCurrentProfile(profileName);
        }

        private void ApplyProfileToCamera(Camera camera, TUFXProfile tufxProfile)
        {
            if (camera == null) return;

            var layer = camera.gameObject.AddOrGetComponent<PostProcessLayer>();
            layer.Init(Resources);
            camera.allowHDR = tufxProfile.HDREnabled;

            // if this is the scaled camera, don't allow TAA because it makes clouds flicker
            if (camera == ScaledCamera.Instance?.cam)
            {
                layer.antialiasingMode = tufxProfile.AntiAliasing == PostProcessLayer.Antialiasing.TemporalAntialiasing ? PostProcessLayer.Antialiasing.None : tufxProfile.AntiAliasing;
                layer.volumeLayer = 1 << scaledSpaceVolume.gameObject.layer;
            }
            else
            {
                layer.antialiasingMode = tufxProfile.AntiAliasing;
                layer.volumeLayer = 1 << mainVolume.gameObject.layer;
            }
        }

        private  void ApplyCurrentProfile()
        {
            if (currentProfile.HDREnabled && currentProfile.GetSettingsFor<Bloom>().active)
            {
                QualitySettings.antiAliasing = 0;
            }

            mainVolume.sharedProfile = currentProfile.CreatePostProcessProfile();

				// clear the copied profile and reset the sharedProfile so we can make a new copy
				scaledSpaceVolume.profile = null;
            scaledSpaceVolume.sharedProfile = mainVolume.sharedProfile;

            // disallow DoF for the scaledspace camera (note using the `profile` property will clone the sharedProfile into a new copy that we can modify individually)
            if (scaledSpaceVolume.profile.TryGetSettings<DepthOfField>(out var dofSettings))
            {
                dofSettings.enabled.Override(false);
            }

			if (HighLogic.LoadedScene == GameScenes.MAINMENU || HighLogic.LoadedScene == GameScenes.SPACECENTER)
			{
				ApplyProfileToCamera(Camera.main, currentProfile);
			}
			if (HighLogic.LoadedScene == GameScenes.EDITOR)
			{
				var editorCameras = EditorCamera.Instance.cam.gameObject.GetComponentsInChildren<Camera>();
				foreach (var cam in editorCameras)
				{
					ApplyProfileToCamera(cam, currentProfile);
				}
			}
			ApplyProfileToCamera(PlanetariumCamera.Camera, currentProfile);
			ApplyProfileToCamera(InternalCamera.Instance?.GetComponent<Camera>(), currentProfile);
			ApplyProfileToCamera(ScaledCamera.Instance?.cam, currentProfile);
			ApplyProfileToCamera(CameraManager.GetCurrentCamera(), currentProfile);
		}

        /// <summary>
        /// Enables the input profile for the currently rendering scene (menu, ksc, editor, tracking, flight, flight-map)
        /// </summary>
        /// <param name="profileName"></param>
        internal void SetCurrentProfile(string profileName)
        {
			if (Profiles.TryGetValue(profileName, out TUFXProfile profile))
            {
                currentProfile = profile;
                ApplyCurrentProfile();
            }
		}

        // called from the UI when HDR or antialiasing settings have changed; which need to change settings on the camera itself
        internal void RefreshCameras()
        {
            ApplyCurrentProfile();
        }

        /// <summary>
        /// Callback for when the ApplicationLauncher button is clicked.
        /// </summary>
        private void configGuiEnable()
        {
            if (configGUI == null)
            {
                configGUI = this.gameObject.AddOrGetComponent<ConfigurationGUI>();
            }
        }

        /// <summary>
        /// Callback for when the ApplicationLauncher button is clicked.  Can also be called from within the GUI itself from a 'Close' button.
        /// </summary>
        internal void configGuiDisable()
        {
            if (configGUI != null)
            {
                GameObject.Destroy(configGUI);
                configGUI = null;
            }
            if (configAppButton != null && configAppButton.toggleButton != null)
            {
                configAppButton.toggleButton.Value = false;
            }
        }

        private void debugGuiEnable()
        {
#if DEBUG
            debugGUI = GetComponent<DebugGUI>();
            if (debugGUI == null)
            {
                debugGUI = this.gameObject.AddComponent<DebugGUI>();
            }
#endif
        }

        internal void debugGuiDisable()
        {
            if (debugGUI != null)
            {
                GameObject.Destroy(debugGUI);
            }
            if (debugAppButton != null && debugAppButton.toggleButton != null)
            {
                debugAppButton.toggleButton.Value = false;
            }
        }
    }

}
