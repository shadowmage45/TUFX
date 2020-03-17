using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
    public class TUFXRenderHelper : MonoBehaviour
    {

        public void Start()
        {
            Camera camera = GetComponent<Camera>();
            camera.SetReplacementShader(Shader.Find("TU/NormBuffer"), string.Empty);
        }

    }

}
