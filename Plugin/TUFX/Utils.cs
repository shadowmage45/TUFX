using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TUFX
{
    public static class Utils
    {

    }

    public static class Log
    {
        public static void debug(string msg) { MonoBehaviour.print("[TUFX-DEBUG] " + msg); }
        public static void log(string msg) { MonoBehaviour.print("[TUFX] " + msg); }
        public static void exception(string msg) { MonoBehaviour.print("[TUFX-ERROR] " + msg); }
    }
}
