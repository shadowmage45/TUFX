using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;
using TUFX;
using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// The base class for all post-processing effect settings. Any <see cref="ParameterOverride"/>
    /// members found in this class will be automatically handled and interpolated by the volume
    /// framework.
    /// </summary>
    /// <example>
    /// <code>
    /// [Serializable]
    /// [PostProcess(typeof(ExampleRenderer), "Custom/ExampleEffect")]
    /// public sealed class ExampleEffect : PostProcessEffectSettings
    /// {
    ///     [Range(0f, 1f), Tooltip("Effect intensity.")]
    ///     public FloatParameter intensity = new FloatParameter { value = 0f };
    ///
    ///     public override bool IsEnabledAndSupported(PostProcessRenderContext context)
    ///     {
    ///         return enabled.value
    ///             &amp;&amp; intensity.value > 0f; // Only render the effect if intensity is greater than 0
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public abstract class PostProcessEffectSettings : ScriptableObject
    {
        /// <summary>
        /// The active state of the set of parameter defined in this class.
        /// </summary>
        /// <seealso cref="enabled"/>
        public bool active = true;

        /// <summary>
        /// The true state of the effect override in the stack. Setting this to <c>false</c> will
        /// disable rendering for this effect assuming a volume with a higher priority doesn't
        /// override it to <c>true</c>.
        /// </summary>
        public BoolParameter enabled = new BoolParameter { overrideState = true, value = false };

        internal ReadOnlyCollection<ParameterOverride> parameters;

        void OnEnable()
        {
            // Automatically grab all fields of type ParameterOverride for this instance
            parameters = GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(t => t.FieldType.IsSubclassOf(typeof(ParameterOverride)))
                .OrderBy(t => t.MetadataToken) // Guaranteed order
                .Select(t => (ParameterOverride)t.GetValue(this))
                .ToList()
                .AsReadOnly();

            foreach (var parameter in parameters)
                parameter.OnEnable();
        }

        void OnDisable()
        {
            if (parameters == null)
                return;

            foreach (var parameter in parameters)
                parameter.OnDisable();
        }

        /// <summary>
        /// Sets all the overrides for this effect to a given value.
        /// </summary>
        /// <param name="state">The value to set the override states to</param>
        /// <param name="excludeEnabled">If <c>false</c>, the <see cref="enabled"/> field will also
        /// be set to the given <see cref="state"/> value.</param>
        public void SetAllOverridesTo(bool state, bool excludeEnabled = true)
        {
            foreach (var prop in parameters)
            {
                if (excludeEnabled && prop == enabled)
                    continue;

                prop.overrideState = state;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the effect is currently enabled and supported.
        /// </summary>
        /// <param name="context">The current post-processing render context</param>
        /// <returns><c>true</c> if the effect is currently enabled and supported</returns>
        public virtual bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value;
        }

        /// <summary>
        /// Returns the computed hash code for this parameter.
        /// </summary>
        /// <returns>A computed hash code</returns>
        public int GetHash()
        {
            // Custom hashing function used to compare the state of settings (it's not meant to be
            // unique but to be a quick way to check if two setting sets have the same state or not).
            // Hash collision rate should be pretty low.
            unchecked
            {
                //return parameters.Aggregate(17, (i, p) => i * 23 + p.GetHash());

                int hash = 17;

                foreach (var p in parameters)
                    hash = hash * 23 + p.GetHash();

                return hash;
            }
        }

        #region REGION - Custom Load and Save methods and utility functions

        public abstract void Load(ConfigNode config);

        public abstract void Save(ConfigNode config);

        internal void loadBoolParameter(ConfigNode node, string name, ParameterOverride<bool> param)
        {
            if (node.HasValue(name)) { param.Override(node.GetBoolValue(name)); }
        }

        internal void loadIntParameter(ConfigNode node, string name, ParameterOverride<int> param)
        {
            if (node.HasValue(name)) { param.Override(node.GetIntValue(name)); }
        }

        internal void loadFloatParameter(ConfigNode node, string name, ParameterOverride<float> param)
        {
            if (node.HasValue(name)) { param.Override(node.GetFloatValue(name)); }
        }

        internal void loadEnumParameter<T>(ConfigNode node, string name, ParameterOverride<T> param, Type enumType)
        {
            if (node.HasValue(name))
            {
                param.Override((T)Enum.Parse(enumType, node.GetStringValue(name)));
            }
        }

        internal void loadTextureParameter(ConfigNode node, string name, ParameterOverride<Texture> param)
        {
            const string BUILTIN_PREFIX = "BUILTIN:";

            if (!node.HasValue(name))
            {
                return;
            }
            string texName = node.GetStringValue(name);

            Texture2D texture = null;
            if (texName.StartsWith(BUILTIN_PREFIX))
            {
                texName = texName.Substring(BUILTIN_PREFIX.Length);
                texture = TexturesUnlimitedFXLoader.INSTANCE.getTexture(texName);
            }
            else
            {
                texture = GameDatabase.Instance.GetTexture(texName, false);
            }
            if (texture != null)
            {
                param.Override(texture);
            }
        }

        internal void loadColorParameter(ConfigNode node, string name, ParameterOverride<Color> param)
        {
            if (node.HasValue(name)) { param.Override(node.getColor(name)); }
        }

        internal void loadVector2Parameter(ConfigNode node, string name, ParameterOverride<Vector2> param)
        {
            if (node.HasValue(name)) { param.Override(ConfigNode.ParseVector2(node.GetValue(name))); }
        }

        internal void loadVector4Parameter(ConfigNode node, string name, ParameterOverride<Vector4> param)
        {
            if (node.HasValue(name)) { param.Override(ConfigNode.ParseVector4(node.GetValue(name))); }
        }

        internal void loadSplineParameter(ConfigNode node, string name, ParameterOverride<Spline> param)
        {
            Log.debug("Loading spline for: " + GetType() + " with name: " + name);
            ConfigNode splineNode = node.GetNode("SPLINE", "name", name);
            Log.debug("Node: " + splineNode);
            if (splineNode == null)
            {
                Log.debug("Node was null...");
                return;
            }
            string[] keys = splineNode.GetValues("key");
            List<Keyframe> frames = new List<Keyframe>();
            int len = keys.Length;
            for (int i = 0; i < len; i++)
            {
                float[] vals = Utils.safeParseFloatArray(keys[i]);
                Keyframe frame = new Keyframe(vals[0], vals[1], vals[2], vals[3], vals[4], vals[5]);
                frames.Add(frame);
            }
            frames.Sort((a, b) => { return a.time.CompareTo(b.time); });
            float zero = splineNode.GetFloatValue("zero");
            bool loop = splineNode.GetBoolValue("loop");
            Spline spline = new Spline(new AnimationCurve(frames.ToArray()), zero, loop, new Vector2(0f, 1f));
            param.Override(spline);
        }

        internal void saveBoolParameter(ConfigNode node, string name, ParameterOverride<bool> param)
        {
            if (param.overrideState) { node.SetValue(name, param.value, true); }
        }

        internal void saveIntParameter(ConfigNode node, string name, ParameterOverride<int> param)
        {
            if (param.overrideState) { node.SetValue(name, param.value, true); }
        }

        internal void saveFloatParameter(ConfigNode node, string name, ParameterOverride<float> param)
        {
            if (param.overrideState) { node.SetValue(name, param.value, true); }
        }

        internal void saveTextureParameter(ConfigNode node, string name, ParameterOverride<Texture> param)
        {
            if (!param.overrideState || param.value == null ) { return; }
            Texture2D tex = (Texture2D)param.value;
            string texName = string.Empty;
            if (TexturesUnlimitedFXLoader.INSTANCE.isBuiltinTexture(tex))
            {
                texName = "BUILTIN:" + tex.name;
            }
            else
            {
                GameDatabase.TextureInfo info = GameDatabase.Instance.databaseTexture.FirstOrDefault(m => m.texture == tex);
                texName = info.file.url;
            }
            node.SetValue(name, texName, true);
        }

        internal void saveColorParameter(ConfigNode node, string name, ParameterOverride<Color> param)
        {
            if (param.overrideState) { node.SetValue(name, param.value, true); }
        }

        internal void saveEnumParameter<T>(ConfigNode node, string name, ParameterOverride<T> param)
        {
            if (param.overrideState) { node.SetValue(name, param.value.ToString(), true); }
        }

        internal void saveSplineParameter(ConfigNode node, string name, ParameterOverride<Spline> param)
        {
            if (!param.overrideState) { return; }
            ConfigNode splineNode = new ConfigNode("SPLINE");
            splineNode.SetValue("name", name, true);
            splineNode.SetValue("zero", param.value.ZeroValue);
            splineNode.SetValue("range", param.value.Range);
            splineNode.SetValue("loop", param.value.Loop);
            Spline spline = param.value;
            AnimationCurve animCurve = spline.curve;
            Keyframe[] keys = animCurve.keys;
            int len = keys.Length;
            string val;
            for (int i = 0; i < len; i++)
            {
                val = keys[i].time + "," + keys[i].value + "," + keys[i].inTangent + "," + keys[i].outTangent + "," + keys[i].inWeight + "," + keys[i].outWeight;
                splineNode.AddValue("key", val);
            }
            node.AddNode(splineNode);
        }

        internal void saveVector2Parameter(ConfigNode node, string name, ParameterOverride<Vector2> param)
        {
            if (param.overrideState) { node.SetValue(name, ConfigNode.WriteVector(param.value), true); }
        }

        internal void saveVector4Parameter(ConfigNode node, string name, ParameterOverride<Vector4> param)
        {
            if (param.overrideState) { node.SetValue(name, ConfigNode.WriteVector(param.value), true); }
        }

        #endregion

    }

}
