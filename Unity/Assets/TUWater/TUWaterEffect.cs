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
            PropertySheet sheet = context.propertySheets.Get(TUFXScatteringResources.WaterShader);
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
            for (int i = 0; i < len - 1; i++)
            {
                context.command.ReleaseTemporaryRT(i);
            }
            cmd.EndSample("TUWater");
        }

        private void bindModel(MaterialPropertyBlock material, Model model)
        {
            model.BindToMaterial(material);
            material.SetFloat("_Radius", (float)model.BottomRadius / 100);
            material.SetVector("_PlanetCenter", model.PlanetCenter);
            material.SetVector("_SunDirection", model.SunDirection);
        }

    }

}
