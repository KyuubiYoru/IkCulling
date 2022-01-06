using System;
using HarmonyLib; // HarmonyLib comes included with a NeosModLoader install
using NeosModLoader;
using BaseX;
using FrooxEngine.FinalIK;


namespace IkCulling
{
    public class IkCulling : NeosMod
    {
        public override string Name => "IkCulling";
        public override string Author => "KyuubiYoru";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/KyuubiYoru/IkCulling";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("IkCulling");
            harmony.PatchAll();
        }


        [HarmonyPatch(typeof(VRIK))]
        class IKCullingPatch
        {
            public static int IkCount = 0;

            [HarmonyPrefix]
            [HarmonyPatch("SolveIK")]
            private static bool SolveIkPrefix(VRIK __instance)
            {
                try
                {
                    if (!__instance.Enabled)
                    {
                        return false;
                    }

                    if (!__instance.IsUnderLocalUser)
                    {
                        float3 playerPos = __instance.Slot.World.LocalUserGlobalPosition;
                        floatQ playerViewRot = __instance.Slot.World.LocalUserViewRotation;
                        float3 ikPos = __instance.Slot.GlobalPosition;

                        float3 dirToIk = (ikPos - playerPos).Normalized;
                        float3 viewDir = playerViewRot*float3.Forward;

                        float dist = MathX.Distance(playerPos, ikPos)/ __instance.LocalUserRoot.GlobalScale;
                        

                        float dot = MathX.Dot(dirToIk, viewDir);

                        if (dist > 30)
                        {
                            return false;
                        }

                        if (dist > 4 && dot < 0.7f)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }


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

                    return true; //Always run the IK update on LocalUser
                }
                catch (Exception e)
                {
                    Error("Error in SolveIkPrefix");
                    Error(e.Message);
                    Error(e.StackTrace);
                    Error(e);
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