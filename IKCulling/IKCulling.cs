using System;
using System.Collections.Generic;
using HarmonyLib; // HarmonyLib comes included with a NeosModLoader install
using NeosModLoader;
using BaseX;
using FrooxEngine;
using FrooxEngine.FinalIK;
using Leap.Unity;


namespace IkCulling
{
    public class IkCulling : NeosMod
    {
        public override string Name => "IkCulling";
        public override string Author => "KyuubiYoru";
        public override string Version => "1.2.0";
        public override string Link => "https://github.com/KyuubiYoru/IkCulling";

        public static ModConfiguration Config;

        public static readonly ModConfigurationKey<bool> ConfigFileExist =
            new ModConfigurationKey<bool>("ConfigFileExist",
                "Value to check if the config file need to be initialized.", () => false, true);

        public static readonly ModConfigurationKey<bool> Enabled =
            new ModConfigurationKey<bool>("Enabled", "IkCulling Enabled.", () => true);

        public static readonly ModConfigurationKey<bool> AutoSaveConfig =
            new ModConfigurationKey<bool>("AutoSaveConfig", "If true the Config gets saved after every change.", () => true);

        public static readonly ModConfigurationKey<bool> UseUserScale =
            new ModConfigurationKey<bool>("UseUserScale", "Should user scale be used for Distance check.", () => false);

        public static readonly ModConfigurationKey<float> Fov = new ModConfigurationKey<float>("Fov",
            "Field of view used for IkCulling, can be between 1 and -1.",
            () => 0.5f, false, v => v <= 1f && v >= -1f);

        public static readonly ModConfigurationKey<float> MinCullingRange =
            new ModConfigurationKey<float>("MinCullingRange",
                "Minimal range for IkCulling, useful in front of a mirror.",
                () => 4);

        public static readonly ModConfigurationKey<float> MaxViewRange =
            new ModConfigurationKey<float>("MaxViewRange", "Maximal view range where IkCulling is always enabled.",
                () => 30);

        private static bool _enabled = true;
        private static bool _useUserScale = false;
        private static float _fov = 0.7f;
        private static float _minCullingRange = 4;
        private static float _maxViewRange = 30;


        public override ModConfigurationDefinition GetConfigurationDefinition()
        {
            try
            {
                List<ModConfigurationKey> keys = new List<ModConfigurationKey>();
                keys.Add(ConfigFileExist);
                keys.Add(Enabled);
                keys.Add(AutoSaveConfig);
                keys.Add(UseUserScale);
                keys.Add(Fov);
                keys.Add(MinCullingRange);
                keys.Add(MaxViewRange);
                

                return DefineConfiguration(new Version(1, 0, 0), keys);
            }
            catch (Exception e)
            {
                Error(e.Message);
                Error(e.StackTrace);
                throw;
            }
        }

        public override void OnEngineInit()
        {
            try
            {
                Harmony harmony = new Harmony("net.KyuubiYoru.IkCulling");
                harmony.PatchAll();

                Config = GetConfiguration();
                Config.OnThisConfigurationChanged += RefreshConfigState;

                if (!Config.GetValue(ConfigFileExist))
                {
                    Config.Set(ConfigFileExist, true);
                    Config.Save(true);
                }

                RefreshConfigState();
            }
            catch (Exception e)
            {
                Error(e.Message);
                Error(e.ToString());
                throw;
            }
        }

        private void RefreshConfigState(ConfigurationChangedEvent configurationChangedEvent = null)
        {
            _enabled = Config.GetValue(Enabled);
            _useUserScale = Config.GetValue(UseUserScale);
            _fov = Config.GetValue(Fov);
            _minCullingRange = Config.GetValue(MinCullingRange);
            _maxViewRange = Config.GetValue(MaxViewRange);
            if (Config.GetValue(AutoSaveConfig)||Equals(configurationChangedEvent?.Key, AutoSaveConfig))
            {
                Config.Save(true);
            }
        }

        [HarmonyPatch(typeof(VRIKAvatar))]
        public class IkCullingPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("OnCommonUpdate")]
            private static bool OnCommonUpdatePrefix(VRIKAvatar __instance)
            {
                try
                {
                    if (!_enabled)
                    {
                        return true; //IkCulling is Disabled
                    }

                    if (!__instance.Enabled)
                    {
                        return false; //Ik is Disabled
                    }


                    if (__instance.IsUnderLocalUser)
                    {
                        return true; //Always Update local Ik
                    }

                    float3 playerPos = __instance.Slot.World.LocalUserGlobalPosition;
                    floatQ playerViewRot = __instance.Slot.World.LocalUserViewRotation;
                    float3 ikPos = __instance.ChestNode.Slot.GlobalPosition;
                    

                    float3 dirToIk = (ikPos - playerPos).Normalized;
                    float3 viewDir = playerViewRot * float3.Forward;

                    float dist = MathX.Distance(playerPos, ikPos);

                    if (_useUserScale)
                    {
                        dist = dist / __instance.LocalUserRoot.GlobalScale;
                    }

                    float dot = MathX.Dot(dirToIk, viewDir);


                    if (dist > _maxViewRange)
                    {
                        return false;
                    }

                    if (dist > _minCullingRange && dot < _fov)
                    {
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    Debug("Error in OnCommonUpdatePrefix");
                    Debug(e.Message);
                    Debug(e.StackTrace);
                    return true;
                }
            }
        }
    }
}