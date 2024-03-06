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
        
        public static ConfigEntry<string> AIStart;

        public static ConfigEntry<float> walkSpeed;

        public static ConfigEntry<float> runSpeed;

        public static AudioClip spotSound;

        public static AudioClip stepSound;

        public static AudioClip walkieChatter;

        public void Awake()
        {
            AIStart = Config.Bind("General", "AI Start", "random", "The AI option to use. (tree, standard, or random)");
            walkSpeed = Config.Bind("General", "Walk Speed", 3.5f, "Walking speed.");
            runSpeed = Config.Bind("General", "Run Speed", 7.0f, "Running speed.");




            Assets.PopulateAssets();
            SirenEnemy = Assets.MainAssetBundle.LoadAsset<EnemyType>("SirenHead");
            var Node = Assets.MainAssetBundle.LoadAsset<TerminalNode>("SirenHeadTN");
            var Keyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("SirenHeadKW");
            spotSound = Assets.MainAssetBundle.LoadAsset<AudioClip>("sirenheadspot");
            stepSound = Assets.MainAssetBundle.LoadAsset<AudioClip>("Sirenfoot1");
            walkieChatter = Assets.MainAssetBundle.LoadAsset<AudioClip>("Sirenchatter");
            NetworkPrefabs.RegisterNetworkPrefab(SirenEnemy.enemyPrefab);
            Harmony.PatchAll();
            Logger.LogInfo(PluginName + " " + VersionString + " " + "loaded.");
            RegisterEnemy(SirenEnemy, 100, LevelTypes.VowLevel, SpawnType.Outside, Node, Keyword);
            Log = Logger;

            // netcode stuff
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }

    public static class Assets
    {
        public static AssetBundle MainAssetBundle = null;
        public static void PopulateAssets()
        {
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "sirenheadassets"));
            if (MainAssetBundle == null)
            {
                Plugin.Log.LogError("Failed to load custom assets.");
                return;
            }
        }
    }
}
