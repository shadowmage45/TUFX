using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TUFX
{

    /// <summary>
    /// Per-effect storage for textures, by parameter name.  Each instance of TUFXEffectTextureList stores the
    /// applicable textures for an entire effect, internally mapped by the property name to which each is applicable.
    /// </summary>
    public class TUFXEffectTextureList
    {

        /// <summary>
        /// List of applicable textures, by the name of the property to which they can be applied. 
        /// </summary>
        private Dictionary<string, List<Texture2D>> propertyTextures = new Dictionary<string, List<Texture2D>>();

        /// <summary>
        /// Add a texture to this effects texture list, for the specified property name
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="texture"></param>
        public void AddTexture(string propertyName, Texture2D texture)
        {
            List<Texture2D> textures;
            if (!propertyTextures.TryGetValue(propertyName, out textures))
            {
                propertyTextures[propertyName] = textures = new List<Texture2D>();
            }
            textures.AddUnique(texture);
        }

        /// <summary>
        /// Return the list of textures for the input property name, or an empty list if the name was invalid, or there are no textures available for the property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public List<Texture2D> GetTextures(string propertyName)
        {
            List<Texture2D> textures;
            if (!propertyTextures.TryGetValue(propertyName, out textures))
            {
                Log.debug("No textures found for property: " + propertyName);
                return new List<Texture2D>();//because C# doesn't have a static (and typed) empty list construct?
            }
            return textures;
        }

        public bool ContainsTexture(string propertyName, Texture2D tex)
        {
            return GetTextures(propertyName).Contains(tex);
        }

    }

}
