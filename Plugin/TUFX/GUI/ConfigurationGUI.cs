using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TUFX
{

    public class ConfigurationGUI : MonoBehaviour
    {
        private static Rect windowRect = new Rect(Screen.width - 900, 40, 800, 600);
        private int windowID = 0;

        public void Awake()
        {
            windowID = GetInstanceID();
        }

        public void OnGUI()
        {
            try
            {
                windowRect = GUI.Window(windowID, windowRect, updateWindow, "TUFXSettings");
            }
            catch (Exception e)
            {
                MonoBehaviour.print("Caught exception while rendering TUFX Settings UI");
                MonoBehaviour.print(e.Message);
                MonoBehaviour.print(System.Environment.StackTrace);
            }
        }

        private void updateWindow(int id)
        {
            //bool hdr = addButtonRowToggle("HDR", EffectManager.hdrEnabled);
            //if (hdr != EffectManager.hdrEnabled)
            //{
            //    EffectManager.hdrEnabled = hdr;
            //    TexturesUnlimitedFXLoader.onHDRToggled();
            //}
            //addLabelRow("----------Ambient Occlusion----------");
            //EffectManager.ambientOcclusion.enabled.value = addButtonRowToggle("AO Enabled", EffectManager.ambientOcclusion.enabled);
            //if (EffectManager.ambientOcclusion.enabled.value)
            //{
            //    EffectManager.ambientOcclusion.intensity.value = addSliderRow("Intensity", EffectManager.ambientOcclusion.intensity.value, 0, 2);
            //    EffectManager.ambientOcclusion.thicknessModifier.value = addSliderRow("Thickness", EffectManager.ambientOcclusion.thicknessModifier.value, 0, 2);
            //    EffectManager.ambientOcclusion.ambientOnly.value = addButtonRowToggle("Ambient Only", EffectManager.ambientOcclusion.ambientOnly.value);
            //}
            //addLabelRow("----------Bloom----------");
            //EffectManager.bloom.enabled.value = addButtonRowToggle("Bloom Enabled", EffectManager.bloom.enabled);
            //if (EffectManager.bloom.enabled)
            //{
            //    EffectManager.bloom.intensity.value = addSliderRow("Intensity", EffectManager.bloom.intensity.value, 0, 5);
            //    EffectManager.bloom.threshold.value = addSliderRow("Threshold", EffectManager.bloom.threshold.value, 0, 1);
            //    EffectManager.bloom.softKnee.value = addSliderRow("Soft Knee", EffectManager.bloom.softKnee.value, 0, 1);
            //    EffectManager.bloom.diffusion.value = addSliderRow("Diffusion", EffectManager.bloom.diffusion.value, 0, 10);
            //}
            GUI.DragWindow();
        }

        private void addLabelRow(string text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.EndHorizontal();
        }

        private float addSliderRow(string text, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayoutOption width = GUILayout.Width(100);
            GUILayout.Label(text, width);
            GUILayout.Label(value.ToString(), width);
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(300));
            GUILayout.EndHorizontal();
            return value;
        }

        private bool addButtonRowToggle(string text, bool value)
        {
            GUILayout.BeginHorizontal();
            GUILayoutOption width = GUILayout.Width(100);
            GUILayout.Label(text, width);
            GUILayout.Label(value.ToString(), width);
            if (GUILayout.Button("Toggle", width))
            {
                value = !value;
            }
            GUILayout.EndHorizontal();
            return value;
        }

    }

}
