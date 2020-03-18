using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace TUFX
{

    [PostProcess(typeof(TUWaterRenderer), PostProcessEvent.BeforeStack, "TU/Water")]
    public class TUWaterEffect : PostProcessEffectSettings
    {

        [Tooltip("Maximum +/- height of waves compared to sea level.")]
        public FloatParameter MaxDisplacement = new FloatParameter() { value = 2f };
        [Tooltip("Color and intensity of the main light-source. May use > 1 values for very intense lights.")]
        public ColorParameter LightColor = new ColorParameter() { value = new Color(1.00f, 0.95f, 0.83f) };
        [Tooltip("True color of the water.")]
        public ColorParameter WaterColor = new ColorParameter() { value = new Color(0.40f, 0.60f, 0.80f) };
        [Tooltip("The extinction ratio of the primary colors, how quickly they reduce relative to one another. Higher ratios remain stable through further depths.")]
        public Vector3Parameter ExtinctionRatio = new Vector3Parameter() { value = new Vector3(0.014551f, 0.242510f, 0.970040f) };
        [Tooltip("The clarity of the water, how quickly it 'fogs'.  Zero is crystal clear (no fog), 1 is 'default', higher values will fog faster.")]
        public FloatParameter WaterClarity = new FloatParameter() { value = 1 };

        [Tooltip("Index Of Refraction")]
        public FloatParameter R0 = new FloatParameter() { value = 0.5f };
        [Tooltip("Refraction Strength")]
        public FloatParameter R2 = new FloatParameter() { value = 0.0f };
        [Tooltip("Specular Angle")]
        public FloatParameter S0 = new FloatParameter() { value = 0.2f };
        [Tooltip("Specular Strength")]
        public FloatParameter S1 = new FloatParameter() { value = 0.5f };


        [Tooltip("How sharp the transition from water to shore is.")]
        public FloatParameter ShoreHardness = new FloatParameter() { value = 1f };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return base.IsEnabledAndSupported(context) && TUFXScatteringResources.WaterShader != null;
        }

    }

    public class TUWaterRenderer : PostProcessEffectRenderer<TUWaterEffect>
    {

        public override void Render(PostProcessRenderContext context)
        {
            CommandBuffer cmd = context.command;
            cmd.BeginSample("TUWater");
            Camera camera = context.camera;
            PropertySheet sheet = context.propertySheets.Get(Shader.Find("TU/ScreenSpaceShadowDepth"));// TUFXScatteringResources.WaterShader);
            MaterialPropertyBlock material = sheet.properties;

            //bounding box frustum corners, for world-space view direction decoding
            Vector3[] frustumCorners = new Vector3[4];
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

            for (int i = 0; i < 4; i++)
            {
                //transform the vectors to contain frustum depth information, used to recompose view ray and world-space pos from depth-buffer
                frustumCorners[i] = camera.transform.TransformVector(frustumCorners[i]);
            }

            Vector3 botLeft = frustumCorners[0];
            Vector3 topLeft = frustumCorners[1];
            Vector3 topRight = frustumCorners[2];
            Vector3 botRight = frustumCorners[3];

            material.SetVector("_Left", topLeft);
            material.SetVector("_Right", topRight);
            material.SetVector("_Left2", botLeft);
            material.SetVector("_Right2", botRight);

            GameObject sun = GameObject.Find("Sun");
            
            material.SetVector("_LightUp", sun.transform.up);
            material.SetVector("_LightLeft", -sun.transform.right);
            material.SetVector("_LightForward", sun.transform.forward);

            int len = TUFXScatteringResources.Models.Count;
            RenderTargetIdentifier source = context.source;
            RenderTargetIdentifier target = context.destination;
            for (int i = 0; i < len; i++)
            {
                bindModel(material, TUFXScatteringResources.Models[i]);
                if (i < len - 1)
                {
                    context.GetScreenSpaceTemporaryRT(context.command, i, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, 0, 0);
                    target = i;
                }
                else { target = context.destination; }
                if (i > 0)
                {
                    source = i - 1;
                }
                context.command.BlitFullscreenTriangle(source, target, sheet, 0);
            }
            for (int i = 0; i < len - 1; i++)
            {
                context.command.ReleaseTemporaryRT(i);
            }
            cmd.EndSample("TUWater");
        }

        private void bindModel(MaterialPropertyBlock material, Model model)
        {
            model.BindToMaterial(material);
            material.SetVector("_PlanetCenter", model.PlanetCenter);
            material.SetFloat("_Radius", (float)(model.BottomRadius / model.LengthUnitInMeters));
            material.SetVector("_SunDirection", model.SunDirection);
            material.SetColor("_LightColor", settings.LightColor.value);

            material.SetFloat("_Timer", Time.realtimeSinceStartup);

            material.SetFloat("_R0", settings.R0.value);
            material.SetFloat("_R2", settings.R2.value);
            material.SetFloat("_S0", settings.S0.value);
            material.SetFloat("_S1", settings.S1.value);

            material.SetFloat("_MaxDisplacement", (float)(settings.MaxDisplacement.value / model.LengthUnitInMeters));
            material.SetFloat("_ShoreHardness", settings.ShoreHardness.value);

            material.SetColor("_WaterColor", settings.WaterColor.value);
            material.SetVector("_Extinction", Vector3.Normalize(settings.ExtinctionRatio.value));
            material.SetFloat("_Clarity", (float)(settings.WaterClarity.value / model.LengthUnitInMeters));
        }

    }

}
