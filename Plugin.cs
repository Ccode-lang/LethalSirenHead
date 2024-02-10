using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;
using UnityEngine;
using LethalSirenHead.Enemy;

namespace LethalSirenHead
{
    public class Plugin : BaseUnityPlugin
    {
        private const string MyGuid = "Ccode.SirenHead";
        private const string PluginName = "DeathSirenHead";
        private const string VersionString = "0.0.1";

        private static readonly Harmony Harmony = new Harmony(MyGuid);

        public static ManualLogSource Log;

        public static GameObject Enemy;
        public void Awake()
        {
            string location = ((BaseUnityPlugin)this).Info.Location;
            string text = "L.dll";
            string text2 = location.TrimEnd(text.ToCharArray());
            string text3 = text2 + "enemymodel";
            AssetBundle val = AssetBundle.LoadFromFile(text3);
            Enemy = val.LoadAsset<GameObject>("SirenHead");
            Harmony.PatchAll();
            Logger.LogInfo(PluginName + " " + VersionString + " " + "loaded.");
            Log = Logger;
        }
    }
}
