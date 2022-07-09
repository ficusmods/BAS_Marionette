using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace Marionette
{
    public class Logger
    {
        public enum Level
        {
            None = 0,
            Basic = 1,
            Detailed = 2
        }

        private static string modname = "Marionette";
        private static string mod_version = String.Format("{0}.{1}", Config.versionMajor, Config.versionMinor);
        private static Level level = Level.Basic;

        public static void init(string _name, string _level)
        {
            Logger.modname = _name;
            if (!Enum.TryParse(_level, out Logger.level))
            {
                Logger.level = Level.Basic;
            }
        }

        public static void Basic(object msg, params object[] values)
        {
            if (level >= Level.Basic)
                Debug.Log(String.Format($"{modname} v{mod_version} (Basic) | " + msg, values));
        }
        public static void Detailed(object msg, params object[] values)
        {
            if (level >= Level.Detailed)
                Debug.Log(String.Format($"{modname} v{mod_version} (Detailed) | " + msg, values));
        }
    }
}
