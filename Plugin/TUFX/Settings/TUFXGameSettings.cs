using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUFX
{

    public class TUFXGameSettings : GameParameters.CustomParameterNode
    {

        public override string Title => "TUFX Profile Settings";

        public override string DisplaySection => "TUFX";

        public override string Section => "Scene Profiles";

        public override int SectionOrder => 1;

        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;

        public override bool HasPresets => false;

        [GameParameters.CustomStringParameterUI("TUFX Profiles - Must be selected through in-game profile editor.", gameMode = GameParameters.GameMode.ANY, lines = 3, toolTip = "Profiles only shown here to enable per-save-game persistence.")]
        public string WarningLabel = string.Empty;

        [GameParameters.CustomStringParameterUI("Flight Scene Profile: ", gameMode = GameParameters.GameMode.ANY, lines = 1, toolTip = "Active Profile in the Flight Scene")]
        public string FlightSceneProfile = "Default-Flight";

        [GameParameters.CustomStringParameterUI("Map Scene Profile: ", gameMode = GameParameters.GameMode.ANY, lines = 1, toolTip = "Active Profile in the Map Scene")]
        public string MapSceneProfile = "Default-Flight";

        [GameParameters.CustomStringParameterUI("Editor Scene Profile: ", gameMode = GameParameters.GameMode.ANY, lines = 1, toolTip = "Active Profile in the Editor Scene")]
        public string EditorSceneProfile = "Default-Editor";

        [GameParameters.CustomStringParameterUI("Space Center Profile: ", gameMode = GameParameters.GameMode.ANY, lines = 1, toolTip = "Active Profile in the Space Center Scene")]
        public string SpaceCenterSceneProfile = "Default-KSC";

        [GameParameters.CustomStringParameterUI("Tracking Station Profile: ", gameMode = GameParameters.GameMode.ANY, lines = 1, toolTip = "Active Profile in the Tracking Station Scene")]
        public string TrackingStationProfile = "Default-Tracking";

    }

}
