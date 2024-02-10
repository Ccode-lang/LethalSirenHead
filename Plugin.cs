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
using LethalLib.Modules;
using static LethalLib.Modules.Levels;
using static LethalLib.Modules.Enemies;
using LethalSirenHead.Enemy;
using System.IO;
using System.Reflection;

namespace LethalSirenHead
{
    [BepInPlugin(Plugin.MyGuid, Plugin.PluginName, Plugin.VersionString)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        private const string MyGuid = "Ccode.SirenHead";
        private const string PluginName = "SirenHead";
        private const string VersionString = "0.0.1";

        private static readonly Harmony Harmony = new Harmony(MyGuid);

        public static ManualLogSource Log;

        public static EnemyType SirenEnemy;
        public void Awake()
        {
            Assets.PopulateAssets();
            SirenEnemy = Assets.MainAssetBundle.LoadAsset<EnemyType>("SirenHead");
            var Node = Assets.MainAssetBundle.LoadAsset<TerminalNode>("SirenHeadTN");
            var Keyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("SirenHeadKW");
            NetworkPrefabs.RegisterNetworkPrefab(SirenEnemy.enemyPrefab);
            Harmony.PatchAll();
            Logger.LogInfo(PluginName + " " + VersionString + " " + "loaded.");
            RegisterEnemy(SirenEnemy, 100, LevelTypes.VowLevel, SpawnType.Outside, Node, Keyword);
            Log = Logger;
        }
    }

    public static class Assets
    {
        public static AssetBundle MainAssetBundle = null;
        public static void PopulateAssets()
        {
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "modassets"));
            if (MainAssetBundle == null)
            {
                Plugin.Log.LogError("Failed to load custom assets.");
                return;
            }
        }
    }
}
