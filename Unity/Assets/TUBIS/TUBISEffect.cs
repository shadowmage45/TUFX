using BrunetonsImprovedAtmosphere;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace TUFX
{

    [PostProcess(typeof(BISRenderer), PostProcessEvent.AfterStack, "TU/BIS")]
    public class TUBISEffect : PostProcessEffectSettings
    {

        internal float kSunAngularRadius = 0.00935f / 2.0f;
        internal float kBottomRadius = 6360000.0f;

        //rescaleFactor
        public float kLengthUnitInMeters = 100000.0f;

        public Light Sun;

        public GameObject planet;

        public bool UseConstantSolarSpectrum = false;

        public bool UseOzone = true;

        public bool UseCombinedTextures = true;

        public bool UseHalfPrecision = false;

        public bool DoWhiteBalance = false;

        public LUMINANCE UseLuminance = LUMINANCE.NONE;

        public float Exposure = 10.0f;

        public ComputeShader m_compute;

        internal Model m_model;

        /// <summary>
        /// The "real" initialization work, which is specific to our atmosphere model,
        /// is done in the following method. It starts with the creation of an atmosphere
        /// Model instance, with parameters corresponding to the Earth atmosphere:
        /// </summary>
        internal void Init(MaterialPropertyBlock m_material) 
        {
            Sun = GameObject.Find("Sun").GetComponent<Light>();
            planet = GameObject.Find("Planet");
            TUBISReferences refs = Camera.main.GetComponent<TUBISReferences>();
            m_compute = refs.precompute;
            MonoBehaviour.print("TUBIS script start!!  compute:  " + m_compute + " instance: " + GetHashCode());
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

            // From https://en.wikipedia.org/wiki/Dobson_unit, in molecules.m^-2.
            double kDobsonUnit = 2.687e20;
            // Maximum number density of ozone molecules, in m^-3 (computed so at to get
            // 300 Dobson units of ozone - for this we divide 300 DU by the integral of
            // the ozone density profile defined below, which is equal to 15km).
            double kMaxOzoneNumberDensity = 300.0 * kDobsonUnit / 15000.0;
            // Wavelength independent solar irradiance "spectrum" (not physically
            // realistic, but was used in the original implementation).
            double kConstantSolarIrradiance = 1.5;
            //double kTopRadius = 6420000.0;
            double kTopRadius = 6560000.0;
            double kRayleigh = 1.24062e-6;
            double kRayleighScaleHeight = 8000.0;
            double kMieScaleHeight = 1200.0;
            double kMieAngstromAlpha = 0.0;
            double kMieAngstromBeta = 5.328e-3;
            double kMieSingleScatteringAlbedo = 0.9;
            double kMiePhaseFunctionG = 0.8;
            double kGroundAlbedo = 0.1;
            double max_sun_zenith_angle = (UseHalfPrecision ? 102.0 : 120.0) / 180.0 * Mathf.PI;

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

            m_model = new Model();

            m_model.HalfPrecision = UseHalfPrecision;
            m_model.CombineScatteringTextures = UseCombinedTextures;
            m_model.UseLuminance = UseLuminance;
            m_model.Wavelengths = wavelengths;
            m_model.SolarIrradiance = solar_irradiance;
            m_model.SunAngularRadius = kSunAngularRadius;
            m_model.BottomRadius = kBottomRadius;
            m_model.TopRadius = kTopRadius;
            m_model.RayleighDensity = rayleigh_layer;
            m_model.RayleighScattering = rayleigh_scattering;
            m_model.MieDensity = mie_layer;
            m_model.MieScattering = mie_scattering;
            m_model.MieExtinction = mie_extinction;
            m_model.MiePhaseFunctionG = kMiePhaseFunctionG;
            m_model.AbsorptionDensity = ozone_density;
            m_model.AbsorptionExtinction = absorption_extinction;
            m_model.GroundAlbedo = ground_albedo;
            m_model.MaxSunZenithAngle = max_sun_zenith_angle;
            m_model.LengthUnitInMeters = kLengthUnitInMeters;

            int numScatteringOrders = 6;
            m_model.Init(m_compute, numScatteringOrders);

            m_model.BindToMaterial(m_material);
        }

        private void OnDestroy()
        {
            MonoBehaviour.print("BIS:OnDestroy(): "+GetHashCode());
            if (m_model != null)
                m_model.Release();
        }

    }

    public class BISRenderer : PostProcessEffectRenderer<TUBISEffect>
    {

        private bool init = false;
        Shader shader;

        public override void Render(PostProcessRenderContext context)
        {
            if (shader == null) { shader = Shader.Find("TU/BIS"); }
            Camera camera = context.camera;
            PropertySheet sheet = context.propertySheets.Get(shader);
            MaterialPropertyBlock m_material = sheet.properties;
            if (!init) { settings.Init(m_material); init = true; }

            m_material.SetFloat("exposure", settings.UseLuminance != LUMINANCE.NONE ? settings.Exposure * 1e-5f : settings.Exposure);
            m_material.SetVector("earth_center", settings.planet == null ? new Vector3(0.0f, -settings.kBottomRadius / settings.kLengthUnitInMeters, 0.0f) : settings.planet.transform.position);
            m_material.SetVector("sun_size", new Vector2(Mathf.Tan(settings.kSunAngularRadius), Mathf.Cos(settings.kSunAngularRadius)));
            m_material.SetVector("sun_direction", ((settings.Sun == null) ? Vector3.up : settings.Sun.transform.forward) * -1.0f);

            double white_point_r = 1.0;
            double white_point_g = 1.0;
            double white_point_b = 1.0;
            if (settings.DoWhiteBalance)
            {
                settings.m_model.ConvertSpectrumToLinearSrgb(out white_point_r, out white_point_g, out white_point_b);

                double white_point = (white_point_r + white_point_g + white_point_b) / 3.0;
                white_point_r /= white_point;
                white_point_g /= white_point;
                white_point_b /= white_point;
            }

            m_material.SetVector("white_point", new Vector3((float)white_point_r, (float)white_point_g, (float)white_point_b));


            Vector3[] frustumCorners = new Vector3[4];
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

            for (int i = 0; i < 4; i++)
            {
                //transform the vectors to contain frustum depth information, used to recompose world-space pos from depth-buffer
                frustumCorners[i] = camera.transform.TransformVector(frustumCorners[i]);
            }

            Vector3 botLeft = frustumCorners[0];
            Vector3 topLeft = frustumCorners[1];
            Vector3 topRight = frustumCorners[2];
            Vector3 botRight = frustumCorners[3];

            //bounding box frustum corners, for world-space view direction decoding
            m_material.SetVector("_Left", topLeft);
            m_material.SetVector("_Right", topRight);
            m_material.SetVector("_Left2", botLeft);
            m_material.SetVector("_Right2", botRight);


            //add the far-clip vertices as well, to aid in depth-buffer-to-world-space recomposition

            frustumCorners = new Vector3[4];
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

            for (int i = 0; i < 4; i++)
            {
                //transform the vectors to contain frustum depth information, used to recompose world-space pos from depth-buffer
                frustumCorners[i] = camera.transform.TransformVector(frustumCorners[i]);
            }

            botLeft = frustumCorners[0];
            topLeft = frustumCorners[1];
            topRight = frustumCorners[2];
            botRight = frustumCorners[3];

            //bounding box frustum corners, for world-space view direction decoding
            m_material.SetVector("_NLeft", topLeft);
            m_material.SetVector("_NRight", topRight);
            m_material.SetVector("_NLeft2", botLeft);
            m_material.SetVector("_NRight2", botRight);

            m_material.SetFloat("_ClipDepth", camera.farClipPlane - camera.nearClipPlane);

            //context.command.Blit(context.source, context.destination, sheet, 0);
            
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }

    }

}