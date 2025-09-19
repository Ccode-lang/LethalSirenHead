using System.Runtime.CompilerServices;
using GameNetcodeStuff;
using Coroner;

namespace LethalSirenHead
{
    public class CoronerCompatibility
    {
        private static bool? _enabled;
        static string SIREN_HEAD_LANGUAGE_KEY = "DeathEnemySirenHead";

        private static AdvancedCauseOfDeath SIREN_HEAD;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.elitemastereric.coroner");
                }

                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void CoronerRegister()
        {
            if (Coroner.API.IsRegistered(SIREN_HEAD_LANGUAGE_KEY))
                return;
            SIREN_HEAD = Coroner.API.Register(SIREN_HEAD_LANGUAGE_KEY);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void CoronerSetCauseOfDeathSirenHead(PlayerControllerB player)
        {
            Coroner.API.SetCauseOfDeath(player, SIREN_HEAD);
        }

    }
}