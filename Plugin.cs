﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using LethalLib.Modules;
using static LethalLib.Modules.Levels;
using static LethalLib.Modules.Enemies;
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
        private const string VersionString = "2.0.3";

        private static readonly Harmony Harmony = new Harmony(MyGuid);

        public static ManualLogSource Log;

        public static EnemyType SirenEnemy;
        
        public static ConfigEntry<string> AIStart;

        public static ConfigEntry<float> walkSpeed;

        public static ConfigEntry<float> runSpeed;

        public static ConfigEntry<string> Levels;

        public static AudioClip[] spotSound;

        public static AudioClip[] stepSound;

        public static AudioClip[] walkieChatter;

        public static AudioClip OhMyGodIts;

        public void Awake()
        {
            AIStart = Config.Bind("General", "AI Start Animation", "random", "Which animation Siren Head will spawn in");
            walkSpeed = Config.Bind("General", "Siren Head Walk Speed", 3.5f, "Walking speed of Siren Head");
            runSpeed = Config.Bind("General", "Siren Head Run Speed", 7.0f, "Running speed of Siren Head");
            Levels = Config.Bind("General", "Moons", "VowLevel:100;MarchLevel:100", "Moons that Siren Head will spawn on. Format as: \"MoonName:SpawnWeight\". Check README for more info.");



            Assets.PopulateAssets();

            SirenEnemy = Assets.MainAssetBundle.LoadAsset<EnemyType>("SirenHead");
            var Node = Assets.MainAssetBundle.LoadAsset<TerminalNode>("SirenHeadTN");
            var Keyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("SirenHeadKW");

            spotSound = Utils.LoadSounds(Assets.MainAssetBundle, "sirenheadspot");
            stepSound = Utils.LoadSounds(Assets.MainAssetBundle, "sirenfoot");
            walkieChatter = Utils.LoadSounds(Assets.MainAssetBundle, "sirenchatter");

            OhMyGodIts = Assets.MainAssetBundle.LoadAsset<AudioClip>("oh_my_god_its.wav");

            foreach (var x in spotSound)
            {
                Debug.Log(x.ToString());
            }
            foreach (var x in stepSound)
            {
                Debug.Log(x.ToString());
            }
            foreach (var x in walkieChatter)
            {
                Debug.Log(x.ToString());
            }

            (Dictionary<LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = SolveLevels(Levels.Value);

            NetworkPrefabs.RegisterNetworkPrefab(SirenEnemy.enemyPrefab);
            Harmony.PatchAll();
            Logger.LogInfo(PluginName + " " + VersionString + " " + "loaded.");
            RegisterEnemy(SirenEnemy, spawnRateByLevelType, spawnRateByCustomLevelType, Node, Keyword);
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

        (Dictionary<LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) SolveLevels(string config)
        {
            Dictionary<LevelTypes, int> spawnRateByLevelType = new Dictionary<LevelTypes, int>();
            Dictionary<string, int> spawnRateByCustomLevelType = new Dictionary<string, int>();

            string[] configSplit = config.Split(';');

            foreach (string entry in configSplit)
            {
                string[] levelDef = entry.Trim().Split(':');

                if (levelDef.Length != 2)
                {
                    continue;
                }

                int spawnrate = 0;

                if (!int.TryParse(levelDef[1], out spawnrate))
                {
                    continue;
                }

                if (Enum.TryParse<LevelTypes>(levelDef[0], true, out LevelTypes levelType))
                {
                    spawnRateByLevelType[levelType] = spawnrate;
                    Logger.LogInfo($"Registered spawn rate for level type {levelType} to {spawnrate}");
                }
                else
                {
                    spawnRateByCustomLevelType[levelDef[0]] = spawnrate;
                    Logger.LogInfo($"Registered spawn rate for custom level type {levelDef[0]} to {spawnrate}");
                }
            }


            return (spawnRateByLevelType, spawnRateByCustomLevelType);
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
