using BepInEx;
using BepInEx.Configuration;
using Digitalroot.Valheim.Common;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Digitalroot.Valheim.BetterWisps
{
  [BepInPlugin(Guid, Name, Version)]
  [BepInDependency(Jotunn.Main.ModGuid, "2.10.0")]
  [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  public partial class Main : BaseUnityPlugin, ITraceableLogging
  {
    private Harmony _harmony;

    [UsedImplicitly]
    public static ConfigEntry<int> NexusId;

    public static Main Instance;

    public Main()
    {
      Instance = this;
      #if DEBUG
      EnableTrace = true;
      Log.RegisterSource(Instance);
      #else
      EnableTrace = false;
      #endif
      Log.Trace(Instance, $"{Namespace}.{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [UsedImplicitly]
    private void Awake()
    {
      try
      {
        Log.Trace(Instance, $"{Namespace}.{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        NexusId = Config.Bind("General", "NexusID", 0000, new ConfigDescription("Nexus mod ID for updates", null, new ConfigurationManagerAttributes { Browsable = false, ReadOnly = true }));
        _harmony = Harmony.CreateAndPatchAll(typeof(Main).Assembly, Guid);
        ItemManager.OnItemsRegisteredFejd  += UpdateWisp;
      }
      catch (Exception e)
      {
        Log.Error(Instance, e);
      }
    }
    
    [UsedImplicitly]
    private void OnDestroy()
    {
      try
      {
        Log.Trace(Instance, $"{Namespace}.{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        _harmony?.UnpatchSelf();
      }
      catch (Exception e)
      {
        Log.Error(Instance, e);
      }
    }

    private static void UpdateWisp()
    {
      foreach (var recipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name == Common.Names.Vanilla.ItemDropNames.Demister))
      {
        // wispLightRecipe.m_resources.FirstOrDefault(r => r.m_resItem)
        var wispRequirement = recipe.m_resources.FirstOrDefault(r => r.m_resItem.name == Common.Names.Vanilla.ItemDropNames.Wisp);
        if (wispRequirement != null) wispRequirement.m_amountPerLevel = 5;
        Log.Debug(Instance, $"Updated {recipe.m_item.name} of {recipe.name}, set {wispRequirement?.m_resItem.name} m_amountPerLevel to {wispRequirement?.m_amountPerLevel}");

        var silverRequirement = recipe.m_resources.FirstOrDefault(r => r.m_resItem = PrefabManager.Cache.GetPrefab<ItemDrop>(Common.Names.Vanilla.ItemDropNames.Silver));
        if (silverRequirement != null) silverRequirement.m_amountPerLevel = 10;
        Log.Debug(Instance, $"Updated {recipe.m_item.name} of {recipe.name}, set {silverRequirement?.m_resItem.name} m_amountPerLevel to {silverRequirement?.m_amountPerLevel}");
      }

      var wispLight = ObjectDB.instance.m_items.FirstOrDefault(i => i.name == Common.Names.Vanilla.ItemDropNames.Demister);
      var itemDrop = wispLight?.GetComponent<ItemDrop>();

      if (itemDrop != null)
      {
        itemDrop.m_itemData.m_shared.m_maxQuality = 10;
        Log.Debug(Instance, $"Updated {wispLight?.name} set m_maxQuality to {itemDrop.m_itemData.m_shared.m_maxQuality}");
      }

      ItemManager.OnItemsRegisteredFejd  -= UpdateWisp;
    }

    #region Implementation of ITraceableLogging

    /// <inheritdoc />
    public string Source => Namespace;

    /// <inheritdoc />
    public bool EnableTrace { get; }

    #endregion
  }
}
