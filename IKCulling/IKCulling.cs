using System;
using System.Collections.Generic;
using HarmonyLib; // HarmonyLib comes included with a NeosModLoader install
using NeosModLoader;
using BaseX;
using FrooxEngine;
using FrooxEngine.FinalIK;


namespace IkCulling
{
    public class IkCulling : NeosMod
    {
        public override string Name => "IkCulling";
        public override string Author => "KyuubiYoru";
        public override string Version => "1.1.0";
        public override string Link => "https://github.com/KyuubiYoru/IkCulling";

        internal static ModConfiguration Config;

        internal static readonly ModConfigurationKey<bool> Enabled =
            new ModConfigurationKey<bool>("Enabled", "IkCulling Enabled.", () => true);

        internal static readonly ModConfigurationKey<bool> UseUserScale =
            new ModConfigurationKey<bool>("UseUserScale", "Should user scale be used for Distance check.", () => false);

        internal static readonly ModConfigurationKey<float> Fov = new ModConfigurationKey<float>("Fov",
            "Field of view used for IkCulling, can be between 1 and -1.",
            () => 0.7f, false, v => v <= 1f && v >= -1f);

        internal static readonly ModConfigurationKey<float> MinRange =
            new ModConfigurationKey<float>("_minRange", "Minimal range for IkCulling, useful in front of a mirror.",
                () => 4);

        internal static readonly ModConfigurationKey<float> MaxViewRange =
            new ModConfigurationKey<float>("_maxViewRange", "Maximal view range where IkCulling is always enabled.",
                () => 30);


        public override ModConfigurationDefinition GetConfigurationDefinition()
        {
            List<ModConfigurationKey> keys = new List<ModConfigurationKey>();
            keys.Add(Enabled);
            keys.Add(Fov);
            keys.Add(MinRange);
            keys.Add(MaxViewRange);

            return DefineConfiguration(new Version(1, 0, 0), keys);
        }

        public override void OnEngineInit()
        {
            Config = GetConfiguration();

            Harmony harmony = new Harmony("net.KyuubiYoru.IkCulling");
            harmony.PatchAll();
        }


        [HarmonyPatch(typeof(VRIKAvatar))]
        public class IkCullingPatch
        {
            public static int IkCount = 0;
            public static DynamicVariableSpace UserSpaceWorld;

            [HarmonyPrefix]
            [HarmonyPatch("OnCommonUpdate")]
            private static bool OnCommonUpdatePrefix(VRIKAvatar __instance)
            {
                try
                {
                    if (!Config.GetValue(Enabled))
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
                    float3 ikPos = __instance.Slot.GlobalPosition;

                    float3 dirToIk = (ikPos - playerPos).Normalized;
                    float3 viewDir = playerViewRot * float3.Forward;

                    float dist = MathX.Distance(playerPos, ikPos);

                    if (Config.GetValue(UseUserScale))
                    {
                        dist = dist / __instance.LocalUserRoot.GlobalScale;
                    }

                    float dot = MathX.Dot(dirToIk, viewDir);


                    if (dist > Config.GetValue(MaxViewRange))
                    {
                        return false;
                    }

                    if (dist > 4 && dot < Config.GetValue(Fov))
                    {
                        return false;
                    }

                    return true;


                    //IkThrottleData data = __instance.GetThrottleData();
                    //if (data.SkippedFrames > 4)
                    //{
                    //    data.CurrentDeltaTime = __instance.Time.Delta;
                    //    Traverse.Create(__instance.Time).Property("Delta").SetValue(__instance.Time.Delta + data.DeltaTimeOffset);
                    //    data.Reset = true;
                    //    return true;
                    //}
                    //else
                    //{
                    //    data.DeltaTimeOffset += __instance.Time.Delta;
                    //    data.SkippedFrames++;
                    //    return false; //Skip update
                    //}
                }
                catch (Exception e)
                {
                    Debug("Error in OnCommonUpdatePrefix");
                    Debug(e.Message);
                    Debug(e.StackTrace);
                    return true;
                }
            }


            //[HarmonyPostfix]
            //[HarmonyPatch("SolveIK")]
            //private static void SolveIkPostfix(VRIK __instance)
            //{
            //    try
            //    {
            //        IkThrottleData data = __instance.GetThrottleData();
            //        if (data.Reset)
            //        {
            //            Traverse.Create(__instance.Time).Property("Delta").SetValue(data.CurrentDeltaTime);
            //            data.DeltaTimeOffset = 0;
            //            data.SkippedFrames = 0;
            //            data.Reset = false;
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        Error("Error in SolveIkPostfix");
            //        Error(e.Message);
            //        Error(e.StackTrace);
            //        throw;
            //    }
            //}

            //[HarmonyPostfix]
            //[HarmonyPatch("Initiate")]
            //static void InitiatePatch(VRIK __instance, ref Action ____solveIK)
            //{
            //    try
            //    {
            //        IkThrottleData data = __instance.AddThrottleData();
            //        data.DeltaTimeProperty = Traverse.Create(__instance.Time).Property("Delta");
            //        IkCount++;
            //        data.IkIndex = IkCount;

            //        data.SkippedFrames = data.IkIndex % 5; //Spread IK updates over frames
            //        Msg("InitiatePatch IkCount:" + IkCount);
            //    }
            //    catch (Exception e)
            //    {
            //        Error(e.Message);
            //        Error(e.StackTrace);
            //        throw;
            //    }
            //}
        }

    }
}