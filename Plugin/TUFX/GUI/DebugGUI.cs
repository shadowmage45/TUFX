using ClickThroughFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TUFX
{

    public class DebugGUI : MonoBehaviour
    {

        private static Rect windowRect = new Rect(Screen.width - 900, 40, 805, 600);
        private int windowID = 0;
        private Vector2 scrollPos = new Vector2();

        private bool showLayerFlags;
        private bool[] layerFlags = new bool[32];
        private Camera camera;

        public void Start()
        {
            camera = Camera.main;
            int mask = camera.cullingMask;
            for (int i = 0; i < 32; i++)
            {
                layerFlags[i] = (mask & (1 << i)) > 0;
            }
            Log.debug("Camera layer mask: " + camera.cullingMask);
        }

        public void OnGUI()
        {
            ClickThruBlocker.GUIWindow(windowID, windowRect, renderWindow, "TUFX-Debug");
        }

        private void renderWindow(int id)
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, (GUILayoutOption[])null);


            GUILayout.BeginHorizontal();
            GUILayout.Label("Skybox: ");
            if (GUILayout.Button((camera.clearFlags==CameraClearFlags.Depth).ToString()))
            {
                Log.debug("Clear flags: " + camera.clearFlags);
                if (camera.clearFlags == CameraClearFlags.Skybox) { camera.clearFlags = CameraClearFlags.Depth; }
                else { camera.clearFlags = CameraClearFlags.Skybox; }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Layers: ");
            bool show = showLayerFlags;
            if (GUILayout.Button(showLayerFlags.ToString()))
            {
                showLayerFlags = !showLayerFlags;
            }
            GUILayout.EndHorizontal();

            if (show)
            {
                for (int i = 0; i < 32; i++)
                {
                    renderLayerLabel(i);
                }
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void renderLayerLabel(int layer)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Render Layer: " + layer);
            if (GUILayout.Button(layerFlags[layer].ToString()))
            {
                toggleLayerRendering(layer);
            }
            GUILayout.EndHorizontal();
        }

        private void toggleLayerRendering(int layer)
        {
            layerFlags[layer] = !layerFlags[layer];
            if (layerFlags[layer])
            {
                camera.cullingMask = camera.cullingMask | (1 << layer);//add layer bit
            }
            else
            {
                camera.cullingMask = camera.cullingMask & ~(1 << layer);//remove layer bit
            }
        }

    }

}
