using Digitalroot.Modding.Framework.Logging;
using Digitalroot.Modding.Framework.Utils;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Reflection;

namespace Digitalroot.Valheim.BetterWisps
{
  [UsedImplicitly]
  public class Patch
  {
    [UsedImplicitly]
    [HarmonyPatch(typeof(Demister))]
    public static class PatchDemisterOnEnable
    {
      [HarmonyPostfix, HarmonyPriority(Priority.Normal)]
      [HarmonyPatch(typeof(Demister), nameof(Demister.OnEnable))]
      // ReSharper disable once InconsistentNaming
      public static void OnEnablePostfix([NotNull] Demister __instance)
      {
        try
        {
          if (Main.Instance.EnableTrace) Log.Trace(Main.Instance, $"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
          if (ValheimUtils.IsHeadless()
              || !ValheimUtils.IsZNetSceneReady()
              || !ValheimUtils.IsPlayerReady()
             )
            return;

          Main.DemisterInstance = __instance;
          Main.DemisterOnEnable();
        }
        catch (Exception ex)
        {
          Log.Error(Main.Instance, ex);
        }
      }

      [HarmonyPostfix, HarmonyPriority(Priority.Normal)]
      [HarmonyPatch(typeof(Demister), nameof(Demister.OnDisable))]
      // ReSharper disable once InconsistentNaming
      public static void GetMovedDistancePostfix([NotNull] Demister __instance)
      {
        try
        {
          if (Main.Instance.EnableTrace) Log.Trace(Main.Instance, $"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
          if (ValheimUtils.IsHeadless()
              || !ValheimUtils.IsZNetSceneReady()
              || !ValheimUtils.IsPlayerReady()
             )
            return;

          Main.DemisterInstance = null;
        }
        catch (Exception ex)
        {
          Log.Error(Main.Instance, ex);
        }
      }
    }
  }
}
