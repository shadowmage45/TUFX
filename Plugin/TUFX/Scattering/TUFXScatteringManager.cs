using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace TUFX
{

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class TUFXScatteringManager : MonoBehaviour
    {

        public static TUFXScatteringManager INSTANCE { get; private set; }

        private Model debugModel;

        public void Start()
        {
            MonoBehaviour.print("TUFXScattering - Start()");
            DontDestroyOnLoad(this);
            INSTANCE = this;
            GameEvents.onLevelWasLoaded.Add(new EventData<GameScenes>.OnEvent(onLevelLoaded));
            GameEvents.OnMapEntered.Add(new EventVoid.OnEvent(mapEntered));
            GameEvents.OnMapExited.Add(new EventVoid.OnEvent(mapExited));
            //GameEvents.onGameSceneSwitchRequested()

            //for testing, we are only creating a single model; kerbin unscaled
            //will need to do per-frame updating to the world and camera positions
            
        }

        private void mapEntered() { }
        private void mapExited() { }

        private void onLevelLoaded(GameScenes scene)
        {
            if (scene == GameScenes.MAINMENU)
            {
                GameObject[] objects = GameObject.FindObjectsOfType<GameObject>();
                foreach (GameObject go in objects)
                {
                    Log.debug("Main Menu Object: " + go.name + " pos: " + go.transform.position +" parent: "+go.transform.parent?.name);
                }
                if (debugModel != null)
                {
                    debugModel.PlanetCenter = GameObject.Find("Kerbin").transform.position;
                    debugModel.SunDirection = Vector3.Normalize(-debugModel.PlanetCenter);
                }
            }
        }

        internal void debugProfileSetup(PostProcessVolume volume, PostProcessLayer layer)
        {
            if (TUFXScatteringResources.PrecomputeShader == null)
            {
                Log.debug("Could not create model; precompute shader is null...");
                return;
            }
            createTestModel();
            //TUBISEffect effect = (TUBISEffect)volume.sharedProfile.settings.FirstOrDefault(m => m.GetType() == typeof(TUBISEffect));
            //if (effect == null)
            //{
            //    Log.debug("Creating TUBIS Effect, and adding it to settings for active profile...");
            //    effect = ScriptableObject.CreateInstance<TUBISEffect>();
            //    effect.enabled.Override(true);
            //    effect.Exposure.Override(5f);
            //    volume.sharedProfile.settings.Add(effect);
            //}
        }

        public void Update()
        {
            if (debugModel == null) { return; }
            Vector3 worldCenter = Vector3.zero;
            Vector3 sunDir = Vector3.zero;
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                CelestialBody body = FlightGlobals.currentMainBody;
                if (body != null)
                {
                    CelestialBody sun = FlightGlobals.Bodies[0];
                    //Log.debug("Main body: " + body.name + " : " + body.transform.position);
                    //Log.debug("Camera pos: " + Camera.main?.transform.position);
                    worldCenter = body.transform.position;
                    sunDir = Vector3.Normalize(sun.transform.position - body.transform.position);
                }
                //find world center, set to model
                //find sun direction from world center, and set to model
            }
            else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                CelestialBody body = FlightGlobals.currentMainBody;
                if (body != null)
                {
                    CelestialBody sun = FlightGlobals.Bodies[0];
                    //Log.debug("Main body: " + body.name + " : " + body.position);
                    //Log.debug("Camera pos: " + Camera.main?.transform.position);
                    worldCenter = body.transform.position;
                    sunDir = Vector3.Normalize(sun.transform.position - body.transform.position);
                }
            }
            debugModel.PlanetCenter = worldCenter;
            debugModel.SunDirection = sunDir;
        }

        private void createTestModel()
        {
            if (debugModel != null) { return; }
            Log.debug("Creating debug atmo model...");
            // Values from "Reference Solar Spectral Irradiance: ASTM G-173", ETR column
            // (see http://rredc.nrel.gov/solar/spectra/am1.5/ASTMG173/ASTMG173.html),
            // summed and averaged in each bin (e.g. the value for 360nm is the average
            // of the ASTM G-173 values for all wavelengths between 360 and 370nm).
            // Values in W.m^-2.
            int kLambdaMin = 360;
            int kLambdaMax = 830;

            double[] kSolarIrradiance = new double[]
            {
                1.11776, 1.14259, 1.01249, 1.14716, 1.72765, 1.73054, 1.6887, 1.61253,
                1.91198, 2.03474, 2.02042, 2.02212, 1.93377, 1.95809, 1.91686, 1.8298,
                1.8685, 1.8931, 1.85149, 1.8504, 1.8341, 1.8345, 1.8147, 1.78158, 1.7533,
                1.6965, 1.68194, 1.64654, 1.6048, 1.52143, 1.55622, 1.5113, 1.474, 1.4482,
                1.41018, 1.36775, 1.34188, 1.31429, 1.28303, 1.26758, 1.2367, 1.2082,
                1.18737, 1.14683, 1.12362, 1.1058, 1.07124, 1.04992
            };

            // Values from http://www.iup.uni-bremen.de/gruppen/molspec/databases/
            // referencespectra/o3spectra2011/index.html for 233K, summed and averaged in
            // each bin (e.g. the value for 360nm is the average of the original values
            // for all wavelengths between 360 and 370nm). Values in m^2.
            double[] kOzoneCrossSection = new double[]
            {
                1.18e-27, 2.182e-28, 2.818e-28, 6.636e-28, 1.527e-27, 2.763e-27, 5.52e-27,
                8.451e-27, 1.582e-26, 2.316e-26, 3.669e-26, 4.924e-26, 7.752e-26, 9.016e-26,
                1.48e-25, 1.602e-25, 2.139e-25, 2.755e-25, 3.091e-25, 3.5e-25, 4.266e-25,
                4.672e-25, 4.398e-25, 4.701e-25, 5.019e-25, 4.305e-25, 3.74e-25, 3.215e-25,
                2.662e-25, 2.238e-25, 1.852e-25, 1.473e-25, 1.209e-25, 9.423e-26, 7.455e-26,
                6.566e-26, 5.105e-26, 4.15e-26, 4.228e-26, 3.237e-26, 2.451e-26, 2.801e-26,
                2.534e-26, 1.624e-26, 1.465e-26, 2.078e-26, 1.383e-26, 7.105e-27
            };

            float kSunAngularRadius = 0.00935f / 2.0f;
            float kLengthUnitInMeters = 1.0f;

            bool UseConstantSolarSpectrum = false;
            bool UseOzone = true;
            bool UseCombinedTextures = true;
            bool UseHalfPrecision = false;
            LUMINANCE UseLuminance = LUMINANCE.NONE;

            // From https://en.wikipedia.org/wiki/Dobson_unit, in molecules.m^-2.
            double kDobsonUnit = 2.687e20;

            // Maximum number density of ozone molecules, in m^-3 (computed so at to get
            // 300 Dobson units of ozone - for this we divide 300 DU by the integral of
            // the ozone density profile defined below, which is equal to 15km).
            double kMaxOzoneNumberDensity = 300.0 * kDobsonUnit / 15000.0;

            // Wavelength independent solar irradiance "spectrum" (not physically
            // realistic, but was used in the original implementation).
            double kConstantSolarIrradiance = 1.5;
            double kBottomRadius    = 600000.0;
            double kTopRadius       = 700000.0;
            double kRayleigh = 1.24062e-6;
            double kRayleighScaleHeight = 8000.0;
            double kMieScaleHeight = 1200.0;
            double kMieAngstromAlpha = 0.0;
            double kMieAngstromBeta = 5.328e-3;
            double kMieSingleScatteringAlbedo = 0.9;
            double kMiePhaseFunctionG = 0.8;
            double kGroundAlbedo = 0.1;
            double max_sun_zenith_angle = (UseHalfPrecision ? 102.0 : 120.0) / 180.0 * Mathf.PI;


            kRayleighScaleHeight /= 2;
            kMieScaleHeight /= 2;
            DensityProfileLayer rayleigh_layer = new DensityProfileLayer("rayleigh", 0.0, 1.0, -1.0 / kRayleighScaleHeight, 0.0, 0.0);
            DensityProfileLayer mie_layer = new DensityProfileLayer("mie", 0.0, 1.0, -1.0 / kMieScaleHeight, 0.0, 0.0);

            // Density profile increasing linearly from 0 to 1 between 10 and 25km, and
            // decreasing linearly from 1 to 0 between 25 and 40km. This is an approximate
            // profile from http://www.kln.ac.lk/science/Chemistry/Teaching_Resources/
            // Documents/Introduction%20to%20atmospheric%20chemistry.pdf (page 10).
            List<DensityProfileLayer> ozone_density = new List<DensityProfileLayer>();
            ozone_density.Add(new DensityProfileLayer("absorption0", 25000.0, 0.0, 0.0, 1.0 / 15000.0, -2.0 / 3.0));
            ozone_density.Add(new DensityProfileLayer("absorption1", 0.0, 0.0, 0.0, -1.0 / 15000.0, 8.0 / 3.0));

            List<double> wavelengths = new List<double>();
            List<double> solar_irradiance = new List<double>();
            List<double> rayleigh_scattering = new List<double>();
            List<double> mie_scattering = new List<double>();
            List<double> mie_extinction = new List<double>();
            List<double> absorption_extinction = new List<double>();
            List<double> ground_albedo = new List<double>();

            for (int l = kLambdaMin; l <= kLambdaMax; l += 10)
            {
                double lambda = l * 1e-3;  // micro-meters
                double mie = kMieAngstromBeta / kMieScaleHeight * Math.Pow(lambda, -kMieAngstromAlpha);

                wavelengths.Add(l);

                if (UseConstantSolarSpectrum)
                    solar_irradiance.Add(kConstantSolarIrradiance);
                else
                    solar_irradiance.Add(kSolarIrradiance[(l - kLambdaMin) / 10]);

                rayleigh_scattering.Add(kRayleigh * Math.Pow(lambda, -4));
                mie_scattering.Add(mie * kMieSingleScatteringAlbedo);
                mie_extinction.Add(mie);
                absorption_extinction.Add(UseOzone ? kMaxOzoneNumberDensity * kOzoneCrossSection[(l - kLambdaMin) / 10] : 0.0);
                ground_albedo.Add(kGroundAlbedo);
            }

            Model model = new Model();

            model.HalfPrecision = UseHalfPrecision;
            model.CombineScatteringTextures = UseCombinedTextures;
            model.UseLuminance = UseLuminance;
            model.Wavelengths = wavelengths;
            model.SolarIrradiance = solar_irradiance;
            model.SunAngularRadius = kSunAngularRadius;
            model.BottomRadius = kBottomRadius;
            model.TopRadius = kTopRadius;
            model.RayleighDensity = rayleigh_layer;
            model.RayleighScattering = rayleigh_scattering;
            model.MieDensity = mie_layer;
            model.MieScattering = mie_scattering;
            model.MieExtinction = mie_extinction;
            model.MiePhaseFunctionG = kMiePhaseFunctionG;
            model.AbsorptionDensity = ozone_density;
            model.AbsorptionExtinction = absorption_extinction;
            model.GroundAlbedo = ground_albedo;
            model.MaxSunZenithAngle = max_sun_zenith_angle;
            model.LengthUnitInMeters = kLengthUnitInMeters;
            model.SunDirection = -Vector3.forward;

            int numScatteringOrders = 6;
            model.Init(TUFXScatteringResources.PrecomputeShader, numScatteringOrders);
            TUFXScatteringResources.Models.Add(model);
            debugModel = model;
        }

    }

}
