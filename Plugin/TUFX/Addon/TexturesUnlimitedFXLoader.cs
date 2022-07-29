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
            [Persistent] public bool ShowToolbarButton = true;
        }

		private Configuration configuration = new Configuration();

        private PostProcessLayer layer;
        private PostProcessVolume volume;
        private GameScenes previousScene=GameScenes.LOADING;
        private Camera previousCamera;

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

        public void Start()
        {
            MonoBehaviour.print("TUFXLoader - Start()");
            INSTANCE = this;
            DontDestroyOnLoad(this);
            GameEvents.onLevelWasLoaded.Add(new EventData<GameScenes>.OnEvent(onLevelLoaded));
            GameEvents.OnCameraChange.Add(new EventData<CameraManager.CameraMode>.OnEvent(cameraChange));
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
            ConfigNode[] profileConfigs = GameDatabase.Instance.GetConfigNodes("TUFX_PROFILE");
            int len = profileConfigs.Length;
            for (int i = 0; i < len; i++)
            {
                TUFXProfile profile = new TUFXProfile(profileConfigs[i]);
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
                ConfigNode.LoadObjectFromConfig(configuration, config);
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
                if (configAppButton == null && configuration.ShowToolbarButton)//static reference; track if the button was EVER created, as KSP keeps them even if the addon is destroyed
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
                    configuration.MainMenuProfile = profile;
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
                enableProfile(profile);
            }
        }

        /// <summary>
        /// Looks up the profile for the current scene from the game persistence data and attempts to enable it.
        /// </summary>
        internal void enableProfileForCurrentScene()
        {
            string profileName = string.Empty;
            switch (HighLogic.LoadedScene)
            {
                case GameScenes.MAINMENU:
                    profileName = configuration.MainMenuProfile;
                    break;
                case GameScenes.SPACECENTER:
                    profileName = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().SpaceCenterSceneProfile;
                    break;
                case GameScenes.EDITOR:
                    profileName = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().EditorSceneProfile;
                    break;
                case GameScenes.FLIGHT:
                    switch (CameraManager.Instance.currentCameraMode)
                    {
                        case CameraManager.CameraMode.Flight:
                            profileName = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().FlightSceneProfile;
                            break;
                        case CameraManager.CameraMode.Map:
                            profileName = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().MapSceneProfile;
                            break;
                        case CameraManager.CameraMode.IVA:
                        case CameraManager.CameraMode.Internal:
                            profileName = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().IVAProfile;
                            break;
                    }
                    break;
                case GameScenes.TRACKSTATION:
                    profileName = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().TrackingStationProfile;
                    break;
            }
            Log.debug("TUFX - Enabling profile for current scene: " + HighLogic.LoadedScene + " profile: " + profileName);
            enableProfile(profileName);
        }

        /// <summary>
        /// Helper method to return a reference to the active camera object.
        /// </summary>
        /// <returns></returns>
        internal Camera getActiveCamera()
        {
            Camera activeCam = null;
            if (HighLogic.LoadedScene == GameScenes.MAINMENU) { activeCam = Camera.main; }
            else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION) { activeCam = PlanetariumCamera.Camera; }
            //else if (HighLogic.LoadedScene == GameScenes.EDITOR) { activeCam = null; }// EditorCamera.Instance.cam; } // TODO simply referencing this camera screws up the editor scene... (incorrect matrix? wrong camera ref? is this a UI camera?)
            //else if (HighLogic.LoadedScene == GameScenes.EDITOR) { activeCam = null; }// Camera.main; }//TODO -- this one isn't the right camera either....
            else if (HighLogic.LoadedScene == GameScenes.SPACECENTER) { activeCam = Camera.main; }
            else if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                if (InternalCamera.Instance.isActive)
                {
                    activeCam = InternalCamera.Instance.GetComponent<Camera>();
                }
                else if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Map)
                {
                    activeCam = ScaledCamera.Instance.cam;
                }
                else
                {
                    activeCam = CameraManager.GetCurrentCamera(); // NOTE: this will return one of EditorLogic.fetch.editorCamera, PlanetariumCamera.Camera, or FlightCamera.fetch.mainCamera
                }
            }
            else { Log.exception("Could not locate camera for scene: " + HighLogic.LoadedScene); }
            return activeCam;
        }

        /// <summary>
        /// Enables the input profile for the currently rendering scene (menu, ksc, editor, tracking, flight, flight-map)
        /// </summary>
        /// <param name="profileName"></param>
        internal void enableProfile(string profileName)
        {
            currentProfile = null;
            Camera activeCam = getActiveCamera();
            
            
            Log.debug("TUFX: enableProfile( " + profileName + " )  scene: ( "+HighLogic.LoadedScene+" ) camera: ( "+activeCam?.name+" )");
            Log.debug(System.Environment.StackTrace);
            if (previousCamera != activeCam)
            {
                Log.log("Detected change of active camera; recreating post-process objects.");
                if (volume != null)
                {
                    Log.log("Destroying existing PostProcessVolume (from previous camera).");
                    Component.DestroyImmediate(layer);
                    UnityEngine.Object.DestroyImmediate(volume.sharedProfile);
                    UnityEngine.Object.DestroyImmediate(volume);
                    layer = null;
                    volume = null;
                }
                previousScene = HighLogic.LoadedScene;
                previousCamera = activeCam;
            }

            Log.debug("Active Camera (hashcode): " + activeCam?.GetHashCode());
            if (activeCam == null)
            {
                Log.log("Active camera was null.  Skipping profile setup for scene: " + HighLogic.LoadedScene);
            }
            else if (!string.IsNullOrEmpty(profileName) && Profiles.ContainsKey(profileName))
            {
                Log.log("Enabling profile: " + profileName + ".  Current GameScene: " + HighLogic.LoadedScene);
                TUFXProfile tufxProfile = Profiles[profileName];
                currentProfile = tufxProfile;
                Log.debug("Profile (hashcode): " + tufxProfile?.GetHashCode() + " :: "+tufxProfile?.ProfileName);
                Log.log("Setting HDR for camera: " + activeCam.name + " to: " + tufxProfile.HDREnabled);
                activeCam.allowHDR = tufxProfile.HDREnabled;
                layer = activeCam.gameObject.AddOrGetComponent<PostProcessLayer>();
                layer.Init(Resources);
                layer.volumeLayer = ~0;//everything //TODO -- fix layer assignment...
                Log.debug("Layer: " + layer?.GetHashCode());
                onAntiAliasingSelected(tufxProfile.AntiAliasing, false);
                volume = activeCam.gameObject.AddOrGetComponent<PostProcessVolume>();
                volume.isGlobal = true;
                volume.priority = 100;
                Log.debug("Volume: " + volume.GetHashCode());
                if (volume.sharedProfile == null)
                {
                    volume.sharedProfile = tufxProfile.GetPostProcessProfile();
                }
                else
                {
                    volume.sharedProfile.settings.Clear();
                    tufxProfile.Enable(volume);
                }
                Log.log("Profile enabled: " + profileName);
                TUFXScatteringManager.INSTANCE.debugProfileSetup(volume, layer);
            }
            else if (string.IsNullOrEmpty(profileName))
            {
                Log.log("Clearing current profile for scene: " + HighLogic.LoadedScene);
            }
            else
            {
                Log.exception("Profile load was requested for: " + profileName + ", but no profile exists for that name.");
            }
            
        }

        /// <summary>
        /// Internal method to toggle the HDR setting on the current profile, and apply it to the active camera for the scene
        /// </summary>
        internal void onHDRToggled()
        {
            Camera activeCam = getActiveCamera();
            if (CurrentProfile != null && activeCam != null)
            {
                CurrentProfile.HDREnabled = !CurrentProfile.HDREnabled;
                activeCam.allowHDR = CurrentProfile.HDREnabled;
                Log.log("Toggled HDR for camera: " + activeCam.name + " to: " + CurrentProfile.HDREnabled);
            }
            else
            {
                Log.exception("Attempted to toggle HDR while either the profile or camera were null.  Profile: " + CurrentProfile?.ProfileName + " camera: " + activeCam?.name);
            }
        }

        internal void onAntiAliasingSelected(PostProcessLayer.Antialiasing mode, bool updateProfile)
        {
            if (updateProfile && CurrentProfile != null)
            {
                Log.debug("Updated profile AA value to: " + mode);
                CurrentProfile.AntiAliasing = mode;
            }
            else if(updateProfile)
            {
                Log.exception("Profile was null when attempting to update AA mode.");
            }
            if (layer != null)
            {
                Log.debug("Updated post process layer AA value to: " + mode);
                layer.antialiasingMode = mode;
            }
            else
            {
                Log.exception("Layer was null when attempting to update AA mode.");
            }
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

        /// <summary>
        /// Exports the current/active profile to the log in KSP-CFG format.
        /// </summary>
        internal void exportCurrentProfile()
        {
            if (currentProfile != null)
            {
                StringBuilder builder = exportProfile(currentProfile);
                Log.log("Export of profile: " + CurrentProfileName + "\n" + builder.ToString());
            }
        }

        /// <summary>
        /// Exports all of the loaded profiles to the log in KSP-CFG format.
        /// </summary>
        internal void exportAllProfiles()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Export of all profiles:");
            TUFXProfile[] profiles = Profiles.Values.ToArray();
            int len = profiles.Length;
            for (int i = 0; i < len; i++)
            {
                exportProfile(profiles[i], builder);
            }
            Log.log(builder.ToString());
        }

        /// <summary>
        /// Adds the config-node string representation to the input string builder, and returns the builder when finished.  If the input builder is null, a new instance will be created and returned.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        private StringBuilder exportProfile(TUFXProfile profile, StringBuilder builder = null)
        {
            if (builder == null) { builder = new StringBuilder(); }
            ConfigNode node = new ConfigNode("TUFX_PROFILE");
            profile.SaveProfile(node);
            builder.Append(node.ToString());
            builder.AppendLine();
            return builder;
        }

    }

}
