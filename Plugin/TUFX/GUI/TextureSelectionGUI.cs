using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace TUFX
{

    /// <summary>
    /// 
    /// </summary>
    public class TextureSelectionGUI : MonoBehaviour
    {

        private static Rect windowBounds = new Rect(Screen.width - 1300, 40, 405, 600);
        private int windowID = 0;
        private Vector2 scrollPos = new Vector2();
        private Action<Texture2D> textureUpdateCallback = null;
        private Action onClose = null;

        private List<Texture2D> textures = new List<Texture2D>();
        private string effect, property, texture;

        public void Initialize(string effectName, string propertyName, string currentTextureName, Action<Texture2D> onSelect, Action onClose)
        {
            effect = property = texture = string.Empty;
            textureUpdateCallback = null;
            textures.Clear();
            TUFXEffectTextureList list;
            if (!TexturesUnlimitedFXLoader.INSTANCE.EffectTextureLists.TryGetValue(effectName, out list))
            {
                onClose?.Invoke();
                return;
            }
            textures.AddRange(list.GetTextures(propertyName));
            effect = effectName;
            property = propertyName;
            texture = currentTextureName;
            textureUpdateCallback = onSelect;
            this.onClose = onClose;
            Log.debug("Initialized GUI texcount: " + textures.Count);
        }

        public void OnGUI()
        {
            GUI.Window(windowID, windowBounds, drawWindow, "Select a texture");
        }

        private void drawWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Effect: " + effect);
            GUILayout.Label("Property: " + property);
            GUILayout.Label("Current: " + texture);
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            listTextures();
            GUILayout.FlexibleSpace();
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close"))
            {
                closeGUI();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void listTextures()
        {
            int len = textures.Count;
            for (int i = 0; i < len; i++)
            {
                if (GUILayout.Button(textures[i].name, GUILayout.Width(340)))
                {
                    textureUpdateCallback?.Invoke(textures[i]);
                }
            }
        }

        private void closeGUI()
        {
            effect = property = texture = string.Empty;
            textureUpdateCallback = null;
            textures.Clear();
            onClose?.Invoke();
        }

    }

}
