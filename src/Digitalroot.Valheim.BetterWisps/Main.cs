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

    public static ConfigEntry<float> BaseRange;
    public static ConfigEntry<float> IncreasedRangePerLevel;
    public static ConfigEntry<int> MaxLevel;
    public static ConfigEntry<int> WispsPerLevel;
    public static ConfigEntry<int> SilverPerLevel;
    public static ConfigEntry<int> EitrPerLevel;

    public static Main Instance;

    public Main()
    {
      try
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
      catch (Exception ex)
      {
        ZLog.LogError(ex);
      }
    }

    [UsedImplicitly]
    private void Awake()
    {
      try
      {
        Log.Trace(Instance, $"{Namespace}.{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        NexusId = Config.Bind("General", "NexusID", 2150, new ConfigDescription("Nexus mod ID for updates", null, new ConfigurationManagerAttributes { Browsable = false, ReadOnly = true }));
        BaseRange = Config.Bind("General", "Base Range", 15f, new ConfigDescription("Base clear range of the Wisp Light.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { IsAdminOnly = true, Order = 11 }));
        IncreasedRangePerLevel = Config.Bind("General", "Increased Range Per Level", 5f, new ConfigDescription("How much the clear range is Increased per level of the Wisp Light.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { IsAdminOnly = true, Order = 10 }));
        MaxLevel = Config.Bind("Advanced", "Max Level", 5, new ConfigDescription("Max level of the Wisp Light.", new AcceptableValueRange<int>(1, 25), new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = true, Order = 4 }));
        WispsPerLevel = Config.Bind("Advanced", "Wisps Per Level", 5, new ConfigDescription("Amount of Wisps needed per level.", new AcceptableValueRange<int>(1, 50), new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = true, Order = 3 }));
        SilverPerLevel = Config.Bind("Advanced", "Silver Per Level", 10, new ConfigDescription("Amount of Silver needed per level.", new AcceptableValueRange<int>(1, 50), new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = true, Order = 2 }));
        EitrPerLevel = Config.Bind("Advanced", "Eitr Per Level", 0, new ConfigDescription("Amount of Eitr needed per level (only for upgrade).", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = true, Order = 1 }));
        _harmony = Harmony.CreateAndPatchAll(typeof(Main).Assembly, Guid);
        ItemManager.OnItemsRegisteredFejd += OnItemsRegisteredFejd;
      }
      catch (Exception e)
      {
        Log.Error(Instance, e);
      }
    }

    private void OnItemsRegisteredFejd()
    {
      try
      {
        Log.Trace(Instance, $"{Namespace}.{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        UpdateWisp();
        MaxLevel.SettingChanged += UpdateSettings;
        WispsPerLevel.SettingChanged += UpdateSettings;
        SilverPerLevel.SettingChanged += UpdateSettings;
        EitrPerLevel.SettingChanged += UpdateSettings;
        ItemManager.OnItemsRegisteredFejd -= OnItemsRegisteredFejd;
      }
      catch (Exception ex)
      {
        Log.Error(Instance, ex);
      }
    }

    private static void UpdateSettings(object sender, EventArgs e)
    {
      try
      {
        Log.Trace(Instance, $"{Namespace}.{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        UpdateWisp();
      }
      catch (Exception ex)
      {
        Log.Error(Instance, ex);
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
      try
      {
        Log.Trace(Instance, $"{Namespace}.{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        foreach (var recipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name == Common.Names.Vanilla.ItemDropNames.Demister))
        {
          // wispLightRecipe.m_resources.FirstOrDefault(r => r.m_resItem)
          var wispRequirement = recipe.m_resources.FirstOrDefault(r => r.m_resItem.name == Common.Names.Vanilla.ItemDropNames.Wisp);
          if (wispRequirement != null) wispRequirement.m_amountPerLevel = WispsPerLevel.Value;
          Log.Trace(Instance, $"Updated {recipe.m_item.name} of {recipe.name}, set {wispRequirement?.m_resItem.name} m_amountPerLevel to {wispRequirement?.m_amountPerLevel}");

          var silverRequirement = recipe.m_resources.FirstOrDefault(r => r.m_resItem.name == Common.Names.Vanilla.ItemDropNames.Silver);
          if (silverRequirement != null) silverRequirement.m_amountPerLevel = SilverPerLevel.Value;
          Log.Trace(Instance, $"Updated {recipe.m_item.name} of {recipe.name}, set {silverRequirement?.m_resItem.name} m_amountPerLevel to {silverRequirement?.m_amountPerLevel}");

          var eitrRequirement = recipe.m_resources.FirstOrDefault(r => r.m_resItem.name == Common.Names.Vanilla.ItemDropNames.Eitr);
          if (eitrRequirement != null)
          {
            eitrRequirement.m_amountPerLevel = EitrPerLevel.Value;
            Log.Trace(Instance, $"Updated {recipe.m_item.name} of {recipe.name}, set {eitrRequirement.m_resItem.name} m_amountPerLevel to {eitrRequirement.m_amountPerLevel}");
          }
          else
          {
            eitrRequirement = new Piece.Requirement
            {
              m_amount = 0, m_amountPerLevel = EitrPerLevel.Value, m_resItem = PrefabManager.Cache.GetPrefab<ItemDrop>(Common.Names.Vanilla.ItemDropNames.Eitr)
            };
            recipe.m_resources = recipe.m_resources.AddItem(eitrRequirement).ToArray();
            Log.Trace(Instance, $"Added {eitrRequirement.m_resItem.name} to {recipe.m_item.name} of {recipe.name}, set {eitrRequirement.m_resItem.name} m_amountPerLevel to {eitrRequirement.m_amountPerLevel}");
          }
        }

        var wispLight = ObjectDB.instance.m_items.FirstOrDefault(i => i.name == Common.Names.Vanilla.ItemDropNames.Demister);
        var itemDrop = wispLight?.GetComponent<ItemDrop>();

        if (itemDrop == null) return;
        itemDrop.m_itemData.m_shared.m_maxQuality = MaxLevel.Value;
        Log.Trace(Instance, $"Updated {wispLight.name} set m_maxQuality to {itemDrop.m_itemData.m_shared.m_maxQuality}");
      }
      catch (Exception e)
      {
        Log.Error(Instance, e);
      }
    }

    #region Implementation of ITraceableLogging

    /// <inheritdoc />
    public string Source => Namespace;

    /// <inheritdoc />
    public bool EnableTrace { get; }

    #endregion
  }
}
