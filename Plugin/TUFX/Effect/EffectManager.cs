using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace TUFX
{

    public class EffectManager : MonoBehaviour
    {

        //general settings
        public static bool hdrEnabled = false;

        public static AmbientOcclusion ambientOcclusion;
        public static AutoExposure autoExposure;
        public static Bloom bloom;
        public static ChromaticAberration chromaticAberration;
        public static ColorGrading colorGrading;
        public static DepthOfField depthOfField;
        public static MotionBlur motionBlur;


        public EffectManager()
        {

            PostProcessResources res = TexturesUnlimitedFXLoader.INSTANCE.Resources;
            PostProcessLayer layer = Camera.main.gameObject.AddOrGetComponent<PostProcessLayer>();
            layer.Init(res);
            layer.volumeLayer = ~0;//everything
            PostProcessVolume volume = Camera.main.gameObject.AddOrGetComponent<PostProcessVolume>();
            volume.isGlobal = true;
            volume.priority = 100;

            PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            volume.sharedProfile = profile;

            ambientOcclusion = ScriptableObject.CreateInstance<AmbientOcclusion>();
            ambientOcclusion.enabled.Override(true);
            ambientOcclusion.mode.Override(AmbientOcclusionMode.MultiScaleVolumetricObscurance);
            ambientOcclusion.intensity.Override(1f);
            ambientOcclusion.thicknessModifier.Override(1f);
            ambientOcclusion.ambientOnly.Override(true);
            volume.sharedProfile.AddSettings(ambientOcclusion);

            bloom = ScriptableObject.CreateInstance<Bloom>();
            bloom.enabled.Override(true);
            bloom.intensity.Override(5);
            bloom.threshold.Override(1f);
            bloom.softKnee.Override(0.5f);
            bloom.diffusion.Override(7f);
            volume.sharedProfile.AddSettings(bloom);


        }

    }

}
