using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TUFX
{

    /// <summary>
    /// Static resource class used by the BIS settings and effect.
    /// The references in this class -must- be populated, or the entire system will crash and burn.
    /// Models within the model list must be managed externally.  They should have their body position
    /// and scale factor updated appropriately for the rendering scenario (scaled space bodies vs. from ground)
    /// </summary>
    public static class TUFXScatteringResources
    {

        /// <summary>
        /// The precompute shader used to generate lookup textures for the screen space effect
        /// </summary>
        public static ComputeShader PrecomputeShader { get; set; }
        /// <summary>
        /// The screenspace effect shader
        /// </summary>
        public static Shader ScatteringShader { get; set; }
        /// <summary>
        /// List of models, updated externally.  If no models are present, nothing will be rendered.
        /// TODO - internal or external view-culling of models?
        /// </summary>
        public static List<Model> Models { get; } = new List<Model>();

    }

}
