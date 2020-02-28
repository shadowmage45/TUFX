using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using static KSPShaderTools.TexturesUnlimitedLoader;

namespace TexturesUnlimitedFX
{

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class TexturesUnlimitedFXLoader : MonoBehaviour
    {

        public static TexturesUnlimitedFXLoader INSTANCE;

        private ConfigurationGUI configGUI;
        private static ApplicationLauncherButton debugAppButton;

        public PostProcessResources Resources { get; private set; }

        public void Start()
        {
            MonoBehaviour.print("TUFXLoader - Start()");
            INSTANCE = this;
            DontDestroyOnLoad(this);
            GameEvents.onLevelWasLoaded.Add(new EventData<GameScenes>.OnEvent(onSceneChange));
        }
        
        public void ModuleManagerPostLoad()
        {
            MonoBehaviour.print("TUFXLoader - MMPostLoad()");
            //grab references to shaders, register events?
            //has TU run at this point -- are shaders available?
            Resources = new PostProcessResources();
            Resources.shaders = new PostProcessResources.Shaders();
            Resources.computeShaders = new PostProcessResources.ComputeShaders();
            Resources.blueNoise64 = null;//TODO
            Resources.blueNoise256 = null;//TODO
            Resources.smaaLuts = null;//TODO

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
            Resources.shaders.gammaHistogram = getShader("Hidden/PostProcessing/Debug/Histogram");
            Resources.shaders.grainBaker = getShader("Hidden/PostProcessing/GrainBaker");
            Resources.shaders.lightMeter = getShader("Hidden/PostProcessing/Debug/LightMeter");
            Resources.shaders.lut2DBaker = getShader("Hidden/PostProcessing/Lut2DBaker");
            Resources.shaders.motionBlur = getShader("Hidden/PostProcessing/MotionBlur");
            Resources.shaders.multiScaleAO = getShader("Hidden/PostProcessing/MultiScaleVO");//TODO -- find this shader (only can see MultiScaleVO)
            Resources.shaders.scalableAO = getShader("Hidden/PostProcessing/ScalableAO");
            Resources.shaders.screenSpaceReflections = getShader("Hidden/PostProcessing/ScreenSpaceReflections");
            Resources.shaders.subpixelMorphologicalAntialiasing = getShader("Hidden/PostProcessing/SubpixelMorphologicalAntialiasing");
            Resources.shaders.temporalAntialiasing = getShader("Hidden/PostProcessing/TemporalAntialiasing");
            Resources.shaders.texture2dLerp = getShader("Hidden/PostProcessing/Texture2DLerp");
            Resources.shaders.uber = getShader("Hidden/PostProcessing/Uber");
            Resources.shaders.vectorscope = getShader("Hidden/PostProcessing/Debug/Vectorscope");
            Resources.shaders.waveform = getShader("Hidden/PostProcessing/Debug/Waveform");
            #endregion

            #region REGION - Load compute shaders
            //TODO -- will TU even load compute shaders out of the asset bundle?
            //TODO -- how the F are these named by Unity?
            Resources.computeShaders.autoExposure = getComputeShader("AutoExposure");
            //TODO -- finish instantiating compute shaders...
            #endregion
        }

        private ComputeShader getComputeShader(string name) { return null; }//TODO -- load compute shaders in TU, make available through getXXX methods...

        private void onSceneChange(GameScenes scene)
        {
            MonoBehaviour.print("TUFXLoader - onSceneChange()");            
            if (scene == GameScenes.FLIGHT)
            {
                Texture2D tex;
                if (debugAppButton == null)//static reference; track if the button was EVER created, as KSP keeps them even if the addon is destroyed
                {
                    //create a new button
                    tex = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/RDIcon_fuelSystems-highPerformance", false);
                    debugAppButton = ApplicationLauncher.Instance.AddModApplication(debugGuiEnable, debugGuiDisable, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB, tex);
                }
                else if (debugAppButton != null)
                {
                    //reseat callback refs to the ones from THIS instance of the KSPAddon (old refs were stale, pointing to methods for a deleted class instance)
                    debugAppButton.onTrue = debugGuiEnable;
                    debugAppButton.onFalse = debugGuiDisable;
                }
                EffectManager effect = Camera.main.gameObject.AddOrGetComponent<EffectManager>();
                //fixMaterials();
            }
        }

        private void fixMaterials()
        {
            //Dictionary<string, string> shaderReplacements = new Dictionary<string, string>();
            //Dictionary<string, string> specShaderReplacements = new Dictionary<string, string>();
            //shaderReplacements.Add("KSP/Scenery/Diffuse", "KSP/Diffuse");
            //specShaderReplacements.Add("KSP/Scenery/Specular", "KSP/Specular");
            //shaderReplacements.Add("KSP/Scenery/Bumped", "KSP/Bumped");
            //specShaderReplacements.Add("KSP/Scenery/Bumped Specular", "KSP/Bumped Specular");
            //shaderReplacements.Add("KSP/Scenery/Emissive/Diffuse", "KSP/Emissive/Diffuse");
            //specShaderReplacements.Add("KSP/Scenery/Emissive/Specular", "KSP/Emissive/Specular");
            //specShaderReplacements.Add("KSP/Scenery/Emissive/Bumped Specular", "KSP/Emissive/Bumped Specular");
            //Material[] mats = Resources.FindObjectsOfTypeAll<Material>();
            //int len = mats.Length;
            //for (int i = 0; i < len; i++)
            //{
            //    Material mat = mats[i];
            //    if (mat.shader != null && mat.shader.name.StartsWith("KSP/Scenery"))
            //    {
            //        mat.SetOverrideTag("RenderType", "Opaque");
            //        if (shaderReplacements.ContainsKey(mat.shader.name))
            //        {
            //            //mat.shader = KSPShaderTools.TexturesUnlimitedLoader.getShader(shaderReplacements[mat.shader.name]);
            //            //mat.shader = KSPShaderTools.TexturesUnlimitedLoader.getShader("TU/Specular");
            //            mat.SetColor("_GlossColor", Color.black);
            //            mat.SetFloat("_Smoothness", 0);
            //            //MonoBehaviour.print("\n"+KSPShaderTools.Debug.getMaterialPropertiesDebug(mat));
            //        }
            //        else if (specShaderReplacements.ContainsKey(mat.shader.name))
            //        {
            //            //mat.shader = KSPShaderTools.TexturesUnlimitedLoader.getShader("TU/Specular");
            //            mat.SetColor("_GlossColor", mat.GetColor("_SpecColor"));
            //            mat.EnableKeyword("TU_STOCK_SPEC");
            //            //mat.shader = KSPShaderTools.TexturesUnlimitedLoader.getShader(shaderReplacements[mat.shader.name]);
            //        }
            //    }
            //}
        }

        public static void onHDRToggled()
        {
            MonoBehaviour.print("Toggling HDR");
            Camera[] cams = GameObject.FindObjectsOfType<Camera>();
            int len = cams.Length;
            MonoBehaviour.print("Found cameras: " + len);
            for (int i = 0; i < len; i++)
            {
                MonoBehaviour.print("Camera: " + cams[i].name);
                if (cams[i].name == "Camera 00" || cams[i].name == "Camera 01")
                {
                    cams[i].allowHDR = EffectManager.hdrEnabled;
                }
            }
        }

        private void debugGuiEnable()
        {
            if (configGUI == null)
            {
                configGUI = this.gameObject.AddOrGetComponent<ConfigurationGUI>();
            }
        }

        public void debugGuiDisable()
        {
            if (configGUI != null)
            {
                GameObject.Destroy(configGUI);
            }
        }

    }

}
