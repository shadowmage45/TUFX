using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TUFX.PostProcessing;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace TUFX
{

    /// <summary>
    /// Enumeration of the built-in effect classes, to provide mapping between the name of the class and creation of an instance of that class.
    /// Used in order to provide run-time profile creation from a ConfigNode based configuration system.
    /// </summary>
    public enum BuiltinEffect
    {
        AmbientOcclusion,
        AutoExposure,
        Bloom,
        ChromaticAberration,
        ColorGrading,
        DepthOfField,
        Grain,
        LensDistortion,
        MotionBlur,
        Scattering,
        Vignette
    }

    public class TUFXProfileManager
    {

        public static PostProcessEffectSettings CreateEmptySettingsForEffect(BuiltinEffect effect)
        {
            switch (effect)
            {
                case BuiltinEffect.AmbientOcclusion:
                    return ScriptableObject.CreateInstance<AmbientOcclusion>();
                case BuiltinEffect.AutoExposure:
                    return ScriptableObject.CreateInstance<AutoExposure>();
                case BuiltinEffect.Bloom:
                    return ScriptableObject.CreateInstance<Bloom>();
                case BuiltinEffect.ChromaticAberration:
                    return ScriptableObject.CreateInstance<ChromaticAberration>();
                case BuiltinEffect.ColorGrading:
                    return ScriptableObject.CreateInstance<ColorGrading>();
                case BuiltinEffect.DepthOfField:
                    return ScriptableObject.CreateInstance<DepthOfField>();
                case BuiltinEffect.Grain:
                    return ScriptableObject.CreateInstance<Grain>();
                case BuiltinEffect.LensDistortion:
                    return ScriptableObject.CreateInstance<LensDistortion>();
                case BuiltinEffect.MotionBlur:
                    return ScriptableObject.CreateInstance<MotionBlur>();
                case BuiltinEffect.Scattering:
                    return ScriptableObject.CreateInstance<TUBISEffect>();
                case BuiltinEffect.Vignette:
                    return ScriptableObject.CreateInstance<Vignette>();
                default:
                    break;
            }
            return null;
        }

        public static BuiltinEffect GetBuiltinEffect(PostProcessEffectSettings settings)
        {
            if (settings is AmbientOcclusion) { return BuiltinEffect.AmbientOcclusion; }
            else if (settings is AutoExposure) { return BuiltinEffect.AutoExposure; }
            else if (settings is Bloom) { return BuiltinEffect.Bloom; }
            else if (settings is ChromaticAberration) { return BuiltinEffect.ChromaticAberration; }
            else if (settings is ColorGrading) { return BuiltinEffect.ColorGrading; }
            else if (settings is DepthOfField) { return BuiltinEffect.DepthOfField; }
            else if (settings is Grain) { return BuiltinEffect.Grain; }
            else if (settings is LensDistortion) { return BuiltinEffect.LensDistortion; }
            else if (settings is MotionBlur) { return BuiltinEffect.MotionBlur; }
            else if (settings is TUBISEffect) { return BuiltinEffect.Scattering; }
            else if (settings is Vignette) { return BuiltinEffect.Vignette; }
            return BuiltinEffect.AmbientOcclusion;
        }

    }

    /// <summary>
    /// Storage of data for a single
    /// </summary>
    public class TUFXProfile
    {

        /// <summary>
        /// Name of the profile
        /// </summary>
        public string ProfileName { get; private set; }

        /// <summary>
        /// Configured value on if HDR is enabled for this profile or not.
        /// </summary>
        public bool HDREnabled { get; set; }

        /// <summary>
        /// Configured AntiAliasing setting for the profile.
        /// </summary>
        public PostProcessLayer.Antialiasing AntiAliasing { get; set; }

        /// <summary>
        /// List of the override settings currently configured for this profile
        /// </summary>
        public readonly List<PostProcessEffectSettings> Settings = new List<PostProcessEffectSettings>();

        /// <summary>
        /// Profile constructor, takes a ConfigNode containing the profile configuration.
        /// </summary>
        /// <param name="node"></param>
        public TUFXProfile(ConfigNode node)
        {
            LoadProfile(node);
        }

        /// <summary>
        /// Profile constructor for a blank and empty profile -- no settings
        /// </summary>
        public TUFXProfile()
        {

        }

        /// <summary>
        /// Saves the profile into the input configuration node.
        /// </summary>
        /// <param name="node"></param>
        public void SaveProfile(ConfigNode node)
        {
            if (ProfileName.StartsWith("Default-"))//provide in-config file warning for people to not trample on the default configs...
            {
                node.SetValue("name", ProfileName, "Current profile name.  Please make copies of the default profiles and specify new names if you wish to change settings.", true);
            }
            else
            {
                node.SetValue("name", ProfileName, true);
            }
            node.SetValue("hdr", HDREnabled, true);
            node.SetValue("antialiasing", AntiAliasing.ToString(), true);
            int len = Settings.Count;
            for (int i = 0; i < len; i++)
            {
                if (Settings[i].enabled)
                {
                    ConfigNode effectNode = new ConfigNode("EFFECT");
                    effectNode.SetValue("name", TUFXProfileManager.GetBuiltinEffect(Settings[i]).ToString(), true);
                    Settings[i].Save(effectNode);
                    node.AddNode(effectNode);
                }
            }
        }

        /// <summary>
        /// Saves the Unity PostProcessProfile into the input config node.<para/>
        /// It may or may not have a valid 'name' field, depending upon the name of the input profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="node"></param>
        public static void SaveProfile(PostProcessProfile profile, ConfigNode node)
        {
            node.SetValue("name", profile.name, true);
            node.SetValue("hdr", false, true);
            node.SetValue("antialiasing", PostProcessLayer.Antialiasing.None.ToString());
            int len = profile.settings.Count;
            for (int i = 0; i < len; i++)
            {
                if (profile.settings[i].enabled)
                {
                    ConfigNode effectNode = new ConfigNode("EFFECT");
                    effectNode.SetValue("name", TUFXProfileManager.GetBuiltinEffect(profile.settings[i]).ToString(), true);
                    profile.settings[i].Save(effectNode);
                    node.AddNode(effectNode);
                }
            }
        }

        /// <summary>
        /// Loads the profile from the input configuration node.
        /// </summary>
        /// <param name="node"></param>
        public void LoadProfile(ConfigNode node)
        {
            ProfileName = node.GetStringValue("name");
            HDREnabled = node.GetBoolValue("hdr", false);
            AntiAliasing = node.GetEnumValue("antialiasing", PostProcessLayer.Antialiasing.None);
            Settings.Clear();
            ConfigNode[] effectNodes = node.GetNodes("EFFECT");
            int len = effectNodes.Length;
            for (int i = 0; i < len; i++)
            {
                BuiltinEffect effect = effectNodes[i].GetEnumValue("name", BuiltinEffect.AmbientOcclusion);
                PostProcessEffectSettings set = TUFXProfileManager.CreateEmptySettingsForEffect(effect);
                set.enabled.Override(true);
                set.Load(effectNodes[i]);
                Settings.Add(set);
            }
        }

        /// <summary>
        /// Adds the settings from this profile to the input PostProcessVolume
        /// </summary>
        /// <param name="volume"></param>
        public void Enable(PostProcessVolume volume)
        {
            int len = Settings.Count;
            for (int i = 0; i < len; i++)
            {
                volume.sharedProfile.settings.Add(Settings[i]);
            }
            volume.sharedProfile.isDirty = true;
        }

        /// <summary>
        /// Returns a Unity PostProcessProfile instance with the settings contained in this TUFXProfile
        /// </summary>
        public PostProcessProfile GetPostProcessProfile()
        {
            PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            int len = Settings.Count;
            for (int i = 0; i < len; i++)
            {
                profile.settings.Add(Settings[i]);
            }
            profile.isDirty = true;
            profile.name = this.ProfileName;
            return profile;
        }

        /// <summary>
        /// Returns the PostProcessEffectSettings present in the settings list for the input Type, or null if no settings of that type are present.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSettingsFor<T>() where T : PostProcessEffectSettings
        {
            return (T)Settings.FirstOrDefault(m => m is T);
        }

    }

}
