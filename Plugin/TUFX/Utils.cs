using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace TUFX
{
    public static class Utils
    {

        #region REGION - Config Node Extensions
        public static string[] GetStringValues(this ConfigNode node, string name, bool reverse = false)
        {
            string[] values = node.GetValues(name);
            int l = values.Length;
            if (reverse)
            {
                int len = values.Length;
                string[] returnValues = new string[len];
                for (int i = 0, k = len - 1; i < len; i++, k--)
                {
                    returnValues[i] = values[k];
                }
                return returnValues;
            }
            return values;
        }

        public static string[] GetStringValues(this ConfigNode node, string name, string[] defaults, bool reverse = false)
        {
            if (node.HasValue(name)) { return node.GetStringValues(name, reverse); }
            return defaults;
        }

        public static string GetStringValue(this ConfigNode node, string name, string defaultValue)
        {
            String value = node.GetValue(name);
            return value == null ? defaultValue : value;
        }

        public static string GetStringValue(this ConfigNode node, string name)
        {
            return GetStringValue(node, name, "");
        }

        public static bool[] GetBoolValues(this ConfigNode node, string name)
        {
            String[] values = node.GetValues(name);
            int len = values.Length;
            bool[] vals = new bool[len];
            for (int i = 0; i < len; i++)
            {
                vals[i] = safeParseBool(values[i]);
            }
            return vals;
        }

        public static bool GetBoolValue(this ConfigNode node, string name, bool defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return bool.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static bool GetBoolValue(this ConfigNode node, string name)
        {
            return GetBoolValue(node, name, false);
        }

        public static float[] GetFloatValues(this ConfigNode node, string name, float[] defaults)
        {
            String baseVal = node.GetStringValue(name);
            if (!String.IsNullOrEmpty(baseVal))
            {
                String[] split = baseVal.Split(new char[] { ',' });
                float[] vals = new float[split.Length];
                for (int i = 0; i < split.Length; i++) { vals[i] = safeParseFloat(split[i]); }
                return vals;
            }
            return defaults;
        }

        public static float[] GetFloatValues(this ConfigNode node, string name)
        {
            return GetFloatValues(node, name, new float[] { });
        }

        public static float[] GetFloatValuesCSV(this ConfigNode node, string name)
        {
            return GetFloatValuesCSV(node, name, new float[] { });
        }

        public static float[] GetFloatValuesCSV(this ConfigNode node, string name, float[] defaults)
        {
            float[] values = defaults;
            if (node.HasValue(name))
            {
                string strVal = node.GetStringValue(name);
                string[] splits = strVal.Split(',');
                values = new float[splits.Length];
                for (int i = 0; i < splits.Length; i++)
                {
                    values[i] = float.Parse(splits[i]);
                }
            }
            return values;
        }

        public static float GetFloatValue(this ConfigNode node, string name, float defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return float.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static float GetFloatValue(this ConfigNode node, string name)
        {
            return GetFloatValue(node, name, 0);
        }

        public static double GetDoubleValue(this ConfigNode node, string name, double defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return double.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static double GetDoubleValue(this ConfigNode node, string name)
        {
            return GetDoubleValue(node, name, 0);
        }

        public static int GetIntValue(this ConfigNode node, string name, int defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return int.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static int GetIntValue(this ConfigNode node, string name)
        {
            return GetIntValue(node, name, 0);
        }

        public static int[] GetIntValues(this ConfigNode node, string name, int[] defaultValues = null)
        {
            int[] values = defaultValues;
            string[] stringValues = node.GetValues(name);
            if (stringValues == null || stringValues.Length == 0) { return values; }
            int len = stringValues.Length;
            values = new int[len];
            for (int i = 0; i < len; i++)
            {
                values[i] = safeParseInt(stringValues[i]);
            }
            return values;
        }

        public static Vector3 GetVector3(this ConfigNode node, string name, Vector3 defaultValue)
        {
            string value = node.GetValue(name);
            if (value == null)
            {
                return defaultValue;
            }
            string[] vals = value.Split(',');
            if (vals.Length < 3)
            {
                MonoBehaviour.print("ERROR parsing values for Vector3 from input: " + value + ". found less than 3 values, cannot create Vector3");
                return defaultValue;
            }
            return new Vector3(safeParseFloat(vals[0]), safeParseFloat(vals[1]), safeParseFloat(vals[2]));
        }

        public static Vector3 GetVector3(this ConfigNode node, string name)
        {
            string value = node.GetValue(name);
            if (value == null)
            {
                MonoBehaviour.print("ERROR: No value for name: " + name + " found in config node: " + node);
                return Vector3.zero;
            }
            string[] vals = value.Split(',');
            if (vals.Length < 3)
            {
                MonoBehaviour.print("ERROR parsing values for Vector3 from input: " + value + ". found less than 3 values, cannot create Vector3");
                return Vector3.zero;
            }
            return new Vector3(safeParseFloat(vals[0]), safeParseFloat(vals[1]), safeParseFloat(vals[2]));
        }

        public static FloatCurve GetFloatCurve(this ConfigNode node, string name, FloatCurve defaultValue = null)
        {
            FloatCurve curve = new FloatCurve();
            if (node.HasNode(name))
            {
                ConfigNode curveNode = node.GetNode(name);
                String[] values = curveNode.GetValues("key");
                int len = values.Length;
                String[] splitValue;
                float a, b, c, d;
                for (int i = 0; i < len; i++)
                {
                    splitValue = Regex.Replace(values[i], @"\s+", " ").Split(' ');
                    if (splitValue.Length > 2)
                    {
                        a = safeParseFloat(splitValue[0]);
                        b = safeParseFloat(splitValue[1]);
                        c = safeParseFloat(splitValue[2]);
                        d = safeParseFloat(splitValue[3]);
                        curve.Add(a, b, c, d);
                    }
                    else
                    {
                        a = safeParseFloat(splitValue[0]);
                        b = safeParseFloat(splitValue[1]);
                        curve.Add(a, b);
                    }
                }
            }
            else if (defaultValue != null)
            {
                foreach (Keyframe f in defaultValue.Curve.keys)
                {
                    curve.Add(f.time, f.value, f.inTangent, f.outTangent);
                }
            }
            else
            {
                curve.Add(0, 0);
                curve.Add(1, 1);
            }
            return curve;
        }

        public static ConfigNode getNode(this FloatCurve curve, string name)
        {
            ConfigNode node = new ConfigNode(name);
            int len = curve.Curve.length;
            Keyframe[] keys = curve.Curve.keys;
            for (int i = 0; i < len; i++)
            {
                Keyframe key = keys[i];
                node.AddValue("key", key.time + " " + key.value + " " + key.inTangent + " " + key.outTangent);
            }
            return node;
        }

        public static Color getColor(this ConfigNode node, string name)
        {
            Color color = new Color();
            float[] vals = node.GetFloatValuesCSV(name);
            color.r = vals[0];
            color.g = vals[1];
            color.b = vals[2];
            color.a = vals[3];
            return color;
        }

        public static Color getColorFromByteValues(this ConfigNode node, string name)
        {
            Color color = new Color();
            float[] vals = node.GetFloatValuesCSV(name);
            color.r = vals[0] / 255f;
            color.g = vals[1] / 255f;
            color.b = vals[2] / 255f;
            color.a = vals[3] / 255f;
            return color;
        }

        public static T GetEnumValue<T>(this ConfigNode node, string name, T defaultValue)
        {
            string value = node.GetStringValue(name);
            if (string.IsNullOrEmpty(value)) { return defaultValue; }
            try
            {
                return (T)Enum.Parse(defaultValue.GetType(), value);
            }
            catch (Exception e)
            {
                Log.debug(e.ToString());
                return defaultValue;
            }
        }
        #endregion

        public static float safeParseFloat(string val)
        {
            return float.TryParse(val, out float v) ? v : 0;
        }

        public static int safeParseInt(string val)
        {
            return int.TryParse(val, out int v) ? v : 0;
        }

        public static bool safeParseBool(string val)
        {
            return bool.TryParse(val, out bool v) ? v : false;
        }

        public static float[] safeParseFloatArray(string val)
        {
            string[] vals = val.Split(',');
            int len = vals.Length;
            float[] fVals = new float[len];
            for (int i = 0; i < len; i++)
            {
                if (!float.TryParse(vals[i], out float v)) { v = 0; }
                fVals[i] = v;
            }
            return fVals;
        }

    }

    public static class Log
    {
        public static void debug(string msg)
        {
#if DEBUG
            MonoBehaviour.print("[TUFX-DEBUG] " + msg);
#endif
        }
        public static void log(string msg) { MonoBehaviour.print("[TUFX] " + msg); }
        public static void exception(string msg) { MonoBehaviour.print("[TUFX-ERROR] " + msg); }
    }
}
