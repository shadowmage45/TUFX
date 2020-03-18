using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.TUBIS
{

    /// <summary>
    /// Runtime rendering helper class for TUFX.
    /// Manages creation and updating of 
    /// * Normals Buffer (full-screen, world-space normals)
    /// * Main light-source light-perspective shadow maps and matrices (light -> camera space, for sampling depths from shadowmap, world -> light for vol. shadows)
    /// * Material Buffers (metalic and shininess properties)
    /// * Reflection data (?)
    /// </summary>
    [ExecuteInEditMode]
    public class TUFXRenderHelper : MonoBehaviour
    {

        public Camera cam;
        public GameObject sun;
        public GameObject planet;
        public float radius;
        public RenderTexture texture;

        public void OnEnable()
        {
            cam = GetComponent<Camera>();
            cam.SetReplacementShader(Shader.Find("TU/ShadowMap"), string.Empty);
        }

        public void OnDisable()
        {
            Camera cam = GetComponent<Camera>();
            cam.ResetReplacementShader();
        }

        public void Update()
        {
            if (sun == null || planet == null || texture==null) { return; }
            Vector3 pos = planet.transform.position - sun.transform.forward * radius;
            cam.orthographicSize = radius;
            cam.farClipPlane = radius * 1.1f;
            cam.gameObject.transform.position = pos;
            cam.gameObject.transform.rotation = sun.transform.rotation;
            Shader.SetGlobalTexture("_ShadowMap", texture);
            Shader.SetGlobalFloat("_LinearDepth", 7370);//TODO -- this needs to be set based on the model/etc.
            //TODO - the bounds and depth of the shadow map camera should be dynamically set based on current main-world camera frustum
            //TODO - also include stuff inbetween the light-source and the planet, such as -other bodies-; these could be captured with a secondary back-facing camera.
            //material.SetFloat("_LinearDepth", (float)(model.TopRadius / model.LengthUnitInMeters) * 1.1f);
        }

    }

}
