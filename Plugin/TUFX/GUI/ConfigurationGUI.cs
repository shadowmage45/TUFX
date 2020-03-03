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
        private Vector2 scrollPos = new Vector2();

        private List<string> profileNames = new List<string>();
        private string currentProfile = string.Empty;
        private GameScenes currentScene = GameScenes.LOADING;

        public void Awake()
        {
            windowID = GetInstanceID();
            profileNames.Clear();
            profileNames.AddRange(TexturesUnlimitedFXLoader.INSTANCE.Profiles.Keys);
            currentScene = HighLogic.LoadedScene;
            if (currentScene == GameScenes.EDITOR) { currentProfile = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().EditorSceneProfile; }
            else if (currentScene == GameScenes.FLIGHT) { currentProfile = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().FlightSceneProfile; }
            else if (currentScene == GameScenes.SPACECENTER) { currentProfile = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().SpaceCenterSceneProfile; }
            else if (currentScene == GameScenes.TRACKSTATION) { currentProfile = HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().TrackingStationProfile; }
            else
            {
                //TODO -- throw unsupported operation exception -- incorrect game scene
            }
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
            addLabelRow("Current Scene: " + currentScene);
            addLabelRow("Current Profile: " + currentProfile);
            addLabelRow("Select a new profile for current scene: ");
            GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            int len = profileNames.Count;
            for (int i = 0; i < len; i++)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Profile: " + profileNames[i]))
                {
                    Log.debug("Profile Selected: " + profileNames[i]);
                    TexturesUnlimitedFXLoader.INSTANCE.enableProfile(profileNames[i]);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
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
