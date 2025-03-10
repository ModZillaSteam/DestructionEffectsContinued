using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DestructionEffects
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class DESettings : MonoBehaviour
    {
        public static string settingsConfigURL = "GameData/DestructionEffectsContinued/settings.cfg";
        //=======configurable settings

        public static bool LegacyEffect = false;
        public static string[] PartIgnoreList;

        void Start()
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            try
            {
                Debug.Log("== DestructionEffectsContinued: Loading settings.cfg ==");

                ConfigNode fileNode = ConfigNode.Load(settingsConfigURL);
                if (!fileNode.HasNode("DESettings")) return;

                ConfigNode settings = fileNode.GetNode("DESettings");

                LegacyEffect = bool.Parse(settings.GetValue("LegacyEffect"));
                PartIgnoreList = settings.GetValues("PartIgnoreList");
            }
            catch (NullReferenceException)
            {
                Debug.Log("== DestructionEffectsContinued : Failed to load settings config==");
            }
        }
    }
}
