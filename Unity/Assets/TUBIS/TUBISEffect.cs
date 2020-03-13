using BrunetonsImprovedAtmosphere;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace TUFX
{

    [PostProcess(typeof(BISRenderer), PostProcessEvent.BeforeStack, "TU/BIS")]
    public class TUBISEffect : PostProcessEffectSettings
    {

        //uhh, yeah, it has no settings?

        public FloatParameter Exposure = new FloatParameter() { value = 10f };
        public BoolParameter DoWhiteBalance = new BoolParameter() { value = false };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return base.IsEnabledAndSupported(context) && TUFXScatteringResources.PrecomputeShader!=null && TUFXScatteringResources.ScatteringShader!=null;
        }

    }

    public class BISRenderer : PostProcessEffectRenderer<TUBISEffect>
    {

        public override void Render(PostProcessRenderContext context)
        {
            context.command.BeginSample("TUBIS");
            Camera camera = context.camera;
            PropertySheet sheet = context.propertySheets.Get(TUFXScatteringResources.ScatteringShader);
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
            for (int i = 0; i < len-1; i++)
            {
                context.command.ReleaseTemporaryRT(i);
            }
            context.command.EndSample("TUBIS");
        }

        private void bindModel(MaterialPropertyBlock material, Model model)
        {
            model.BindToMaterial(material);
            material.SetFloat("exposure", model.UseLuminance != LUMINANCE.NONE ? settings.Exposure.value * 1e-5f : settings.Exposure.value);
            material.SetVector("earth_center", model.PlanetCenter);
            material.SetVector("sun_size", new Vector2(Mathf.Tan((float)model.SunAngularRadius), Mathf.Cos((float)model.SunAngularRadius)));
            material.SetVector("sun_direction", model.SunDirection);

            double white_point_r = 1.0;
            double white_point_g = 1.0;
            double white_point_b = 1.0;
            if (settings.DoWhiteBalance.value)
            {
                model.ConvertSpectrumToLinearSrgb(out white_point_r, out white_point_g, out white_point_b);
                double white_point = (white_point_r + white_point_g + white_point_b) / 3.0;
                white_point_r /= white_point;
                white_point_g /= white_point;
                white_point_b /= white_point;
            }
            material.SetVector("white_point", new Vector3((float)white_point_r, (float)white_point_g, (float)white_point_b));
        }

    }

}