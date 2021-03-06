using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BaseX;
using FrooxEngine;
using FrooxEngine.FinalIK;
using HarmonyLib;
using NeosModLoader;

namespace IkCulling
{
    public class IkCulling : NeosMod
    {
        public static ModConfiguration Config;

        [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> Enabled =
            new ModConfigurationKey<bool>("Enabled", "IkCulling Enabled.", () => true);

        [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> DisableAfkUser =
            new ModConfigurationKey<bool>("DisableAfkUser", "Disable User not in the World.", () => true);

        [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> DisableIkWithoutUser =
            new ModConfigurationKey<bool>("DisableIkWithoutUser", "Disable Ik's without active user.", () => true);

        [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> AutoSaveConfig =
            new ModConfigurationKey<bool>("AutoSaveConfig", "If true the Config gets saved after every change.",
                () => true);

        [AutoRegisterConfigKey] public static readonly ModConfigurationKey<int> MinUserCount =
            new ModConfigurationKey<int>("MinUserCount", "Min amount of active users in the world to enable ik culling.",
                () => 3);

        [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> UseUserScale =
            new ModConfigurationKey<bool>("UseUserScale", "Should user scale be used for Distance check.", () => false);

        [AutoRegisterConfigKey] public static readonly ModConfigurationKey<bool> UseOtherUserScale =
            new ModConfigurationKey<bool>("UseOtherUserScale",
                "Should the other user's scale be used for Distance check.", () => false);

        [AutoRegisterConfigKey] public static readonly ModConfigurationKey<float> Fov = new ModConfigurationKey<float>(
            "Fov",
            "Field of view used for IkCulling, can be between 1 and -1.",
            () => 0.5f, false, v => v <= 1f && v >= -1f);

        [AutoRegisterConfigKey] public static readonly ModConfigurationKey<float> MinCullingRange =
            new ModConfigurationKey<float>("MinCullingRange",
                "Minimal range for IkCulling, useful in front of a mirror.",
                () => 4);

        [AutoRegisterConfigKey] public static readonly ModConfigurationKey<float> MaxViewRange =
            new ModConfigurationKey<float>("MaxViewRange", "Maximal view range where IkCulling is always enabled.",
                () => 30);


        private static bool _enabled = true;
        private static bool _disableAfkUser = true;
        private static bool _disableIkWithoutUser = true;
        private static int _minUserCount = 1;
        private static bool _useUserScale;
        private static bool _useOtherUserScale;
        private static float _fov = 0.7f;
        private static float _minCullingRange = 4;
        private static float _maxViewRange = 30;

        private static ConditionalWeakTable<VRIKAvatar, FullBodyCalibrator> _calibrators =
            new ConditionalWeakTable<VRIKAvatar, FullBodyCalibrator>();

        public override string Name => "IkCulling";
        public override string Author => "KyuubiYoru";
        public override string Version => "1.4.0";
        public override string Link => "https://github.com/KyuubiYoru/IkCulling";

        public override void OnEngineInit()
        {
            try
            {
                Harmony harmony = new Harmony("net.KyuubiYoru.IkCulling");
                harmony.PatchAll();

                Config = GetConfiguration();
                Config.OnThisConfigurationChanged += RefreshConfigState;

                Config.Save(true);

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
            _disableAfkUser = Config.GetValue(DisableAfkUser);
            _disableIkWithoutUser = Config.GetValue(DisableIkWithoutUser);
            _minUserCount = Config.GetValue(MinUserCount);
            _useUserScale = Config.GetValue(UseUserScale);
            _useOtherUserScale = Config.GetValue(UseOtherUserScale);
            _fov = Config.GetValue(Fov);
            _minCullingRange = Config.GetValue(MinCullingRange);
            _maxViewRange = Config.GetValue(MaxViewRange);

            if (Config.GetValue(AutoSaveConfig) || Equals(configurationChangedEvent?.Key, AutoSaveConfig))
                Config.Save(true);
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
                    if (!_enabled) return true; //IkCulling is Disabled

                    if (_calibrators.TryGetValue(__instance, out _)) return true;
                    
                    if (__instance.LocalUser.HeadDevice == HeadOutputDevice.Headless) return false;

                    if (!__instance.Enabled) return false; //Ik is Disabled

                    if (__instance.IsUnderLocalUser) return true; //Always Update local Ik

                    if (_disableIkWithoutUser && !__instance.IsEquipped) return false;

                    if (__instance.Slot.ActiveUser != null && _disableAfkUser &&
                        !__instance.Slot.ActiveUser.IsPresentInWorld) return false;

                    if (__instance.Slot.World.UserCount < _minUserCount) return true;


                    float3 playerPos = __instance.Slot.World.LocalUserViewPosition;
                    floatQ playerViewRot = __instance.Slot.World.LocalUserViewRotation;
                    float3 ikPos = __instance.HeadProxy.GlobalPosition;


                    float3 dirToIk = (ikPos - playerPos).Normalized;
                    float3 viewDir = playerViewRot * float3.Forward;

                    float dist = MathX.Distance(playerPos, ikPos);

                    if (_useUserScale) dist = dist / __instance.LocalUserRoot.GlobalScale;

                    if (_useOtherUserScale)
                        if (__instance.Slot.ActiveUser != null)
                            dist = dist / __instance.Slot.ActiveUser.Root.GlobalScale;

                    float dot = MathX.Dot(dirToIk, viewDir);


                    if (dist > _maxViewRange) return false;

                    if (dist > _minCullingRange && dot < _fov) return false;

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

        [HarmonyPatch(typeof(FullBodyCalibrator))]
        public class FullBodyCalibratorPath
        {
            [HarmonyPostfix]
            [HarmonyPatch("OnAttach")]
            private static void OnAttachPostfix(FullBodyCalibrator __instance)
            {
                try
                {
                    Traverse traverse = Traverse.Create(__instance).Field("_platformBody").Field("_vrIkAvatar");
                    SyncRef<VRIKAvatar> vrikAvatar = traverse.GetValue<SyncRef<VRIKAvatar>>();
                    vrikAvatar.OnTargetChange += reference => _calibrators.Add(reference, null);
                }
                catch (Exception e)
                {
                    Debug("Error in OnAttachPostfix");
                    Debug(e.Message);
                    Debug(e.StackTrace);
                }
            }
        }
    }
}