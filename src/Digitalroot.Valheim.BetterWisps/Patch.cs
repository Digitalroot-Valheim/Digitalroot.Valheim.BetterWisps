using Digitalroot.Valheim.Common;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Linq;
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
      public static void Postfix([NotNull] ref Demister __instance)
      {
        try
        {
          Log.Trace(Main.Instance, $"{Main.Namespace}.{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
          if (Common.Utils.IsHeadless()) return;
          if (!Common.Utils.IsZNetSceneReady()) return;
          if (!Common.Utils.IsPlayerReady()) return;

          var itemData = Common.Utils.GetLocalPlayer()
                               .GetInventory()
                               .GetEquippedItems()
                               .FirstOrDefault(i => i.m_dropPrefab.name == Common.Names.Vanilla.ItemDropNames.Demister);

          if (!__instance.isActiveAndEnabled || itemData == null) return;
          __instance.m_forceField.endRange = Main.BaseRange.Value + (Main.IncreasedRangePerLevel.Value * (itemData.m_quality - 1));

          Log.Trace(Main.Instance, $"[{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}] Updated {itemData.m_dropPrefab.name} range to {__instance.m_forceField.endRange}.");
          // __instance.m_forceField.gravity = 1f + itemDrop.m_itemData.m_quality;
        }
        catch (Exception ex)
        {
          Log.Error(Main.Instance, ex);
        }
      }
    }
  }
}
