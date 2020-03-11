using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TUFX
{

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class TUFXScatteringManager : MonoBehaviour
    {

        public Vector3 WorldCenter { get; set; }
        public float ScaleFactor { get; set; }

        public void Start()
        {
            MonoBehaviour.print("TUFXLoader - Start()");
            DontDestroyOnLoad(this);
            GameEvents.onLevelWasLoaded.Add(new EventData<GameScenes>.OnEvent(onLevelLoaded));
            GameEvents.OnMapEntered.Add(new EventVoid.OnEvent(mapEntered));
            GameEvents.OnMapExited.Add(new EventVoid.OnEvent(mapExited));
            //GameEvents.onGameSceneSwitchRequested()
        }

        private void mapEntered() { }
        private void mapExited() { }

        private void onLevelLoaded(GameScenes scene)
        {

        }

    }

}
