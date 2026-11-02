using BepInEx;
using BepInEx.Configuration;
using Digitalroot.Modding.Framework.Logging;
using Digitalroot.Modding.Framework.Utils;
using Digitalroot.Modding.Framework.Names.Vanilla;
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
  [BepInDependency(Jotunn.Main.ModGuid)]
  [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Minor)]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  public partial class Main : BaseUnityPlugin, ITraceableLogging
  {
    private Harmony _harmony;
    public static Main Instance;

    [UsedImplicitly]
    public static ConfigEntry<int> NexusId;

    public static ConfigEntry<float> BaseRange;
    public static ConfigEntry<float> IncreasedRangePerLevel;
    public static ConfigEntry<int> MaxLevel;
    public static ConfigEntry<int> WispsPerLevel;
    public static ConfigEntry<int> SilverPerLevel;
    public static ConfigEntry<int> EitrPerLevel;
    public static Demister DemisterInstance;

    public Main()
    {
      try
      {
        Instance = this;
        #if DEBUG
        EnableTrace = true;
        Log.RegisterSource(Instance);
        Log.Trace(Instance, $"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        #else
        EnableTrace = false;
        #endif
        // ReSharper disable once ReplaceWithSingleAssignment.False
        var needToReload = false;
        // ReSharper disable once ConvertIfToOrExpression
        if (ValheimUtils.HandleConfigFileRename(Config.ConfigFilePath, "digitalroot.mods.betterWisps.cfg", Instance)) needToReload = true;
        // ReSharper disable once StringLiteralTypo
        if (ValheimUtils.HandleConfigFileRename(Config.ConfigFilePath, "digitalroot.mods.betterwisps.cfg", Instance)) needToReload = true;
        // ReSharper disable once InvertIf
        if (needToReload)
        {
          Log.Info(Instance, "Reloading Config");
          Config.Reload();
        }
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
        if (Instance.EnableTrace) Log.Trace(Instance, $"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        NexusId = Config.Bind("General", "NexusID", 2150, new ConfigDescription("Nexus mod ID for updates", null, new ConfigurationManagerAttributes { Browsable = false, ReadOnly = true }));
        BaseRange = Config.Bind("General", "Base Range", 15f, new ConfigDescription("Base clear range of the Wisp Light.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { IsAdminOnly = true, Order = 11 }));
        IncreasedRangePerLevel = Config.Bind("General", "Increased Range Per Level", 5f, new ConfigDescription("How much the clear range is Increased per level of the Wisp Light.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { IsAdminOnly = true, Order = 10 }));
        MaxLevel = Config.Bind("Advanced", "Max Level", 5, new ConfigDescription("Max level of the Wisp Light.", new AcceptableValueRange<int>(1, 25), new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = true, Order = 4 }));
        WispsPerLevel = Config.Bind("Advanced", "Wisps Per Level", 5, new ConfigDescription("Amount of Wisps needed per level.", new AcceptableValueRange<int>(1, 50), new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = true, Order = 3 }));
        SilverPerLevel = Config.Bind("Advanced", "Silver Per Level", 10, new ConfigDescription("Amount of Silver needed per level.", new AcceptableValueRange<int>(1, 50), new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = true, Order = 2 }));
        EitrPerLevel = Config.Bind("Advanced", "Eitr Per Level", 0, new ConfigDescription("Amount of Eitr needed per level (only for upgrade).", new AcceptableValueRange<int>(0, 50), new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = true, Order = 1 }));
        _harmony = Harmony.CreateAndPatchAll(typeof(Main).Assembly, Guid);
        ItemManager.OnItemsRegisteredFejd += OnItemsRegisteredFejd;
        Config.SettingChanged += Config_SettingChanged;
        Config.ConfigReloaded += Config_ConfigReloaded;
      }
      catch (Exception e)
      {
        Log.Error(Instance, e);
      }
    }

    private void Config_ConfigReloaded(object sender, EventArgs e)
    {
      DemisterUpdateRangeConfig(DemisterInstance);
      UpdateWispRecipe();
    }

    private void Config_SettingChanged(object sender, SettingChangedEventArgs e)
    {
      try
      {
        if (Instance.EnableTrace) Log.Trace(Instance, $"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        if (Game.instance.IsShuttingDown()) return;

        switch (e.ChangedSetting.Definition.Key)
        {
          case "Base Range":
          case "Increased Range Per Level":
            if (DemisterInstance != null)
            {
              DemisterUpdateRangeConfig(DemisterInstance);
            }

            break;

          default:
            UpdateWispRecipe();
            break;
        }
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
        if (Instance.EnableTrace) Log.Trace(Instance, $"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        ItemManager.OnItemsRegisteredFejd -= OnItemsRegisteredFejd;
        Config.SettingChanged -= Config_SettingChanged;
        Config.ConfigReloaded -= Config_ConfigReloaded;
        _harmony?.UnpatchSelf();
      }
      catch (Exception e)
      {
        Log.Error(Instance, e);
      }
    }

    public static void DemisterOnEnable() => DemisterUpdateRangeConfig(DemisterInstance);

    public static void DemisterUpdateRangeConfig(Demister demister)
    {
      try
      {
        if (Instance.EnableTrace) Log.Trace(Instance, $"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        var itemData = ValheimUtils.GetLocalPlayer()
                                   .GetInventory()
                                   .GetEquippedItems()
                                   .FirstOrDefault(i => i.m_dropPrefab.name == ItemDropNames.Demister);

        if (!demister.isActiveAndEnabled || itemData == null) return;
        demister.m_forceField.endRange = BaseRange.Value + IncreasedRangePerLevel.Value * (itemData.m_quality - 1);

        if (Instance.EnableTrace) Log.Trace(Instance, $"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name} Updated {itemData.m_dropPrefab.name} range to {demister.m_forceField.endRange}.");
        // __instance.m_forceField.gravity = 1f + itemDrop.m_itemData.m_quality;
      }
      catch (Exception ex)
      {
        Log.Error(Instance, ex);
      }
    }

    private void OnItemsRegisteredFejd()
    {
      try
      {
        if (Instance.EnableTrace) Log.Trace(Instance, $"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        UpdateWispRecipe();
        ItemManager.OnItemsRegisteredFejd -= OnItemsRegisteredFejd;
      }
      catch (Exception ex)
      {
        Log.Error(Instance, ex);
      }
    }

    private static void UpdateWispRecipe()
    {
      try
      {
        if (Instance.EnableTrace) Log.Trace(Instance, $"{MethodBase.GetCurrentMethod()?.DeclaringType?.Name}.{MethodBase.GetCurrentMethod()?.Name}");
        foreach (var recipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name == ItemDropNames.Demister))
        {
          if (Game.instance != null && Game.instance.IsShuttingDown()) return;

          // wispLightRecipe.m_resources.FirstOrDefault(r => r.m_resItem)
          var wispRequirement = recipe.m_resources.FirstOrDefault(r => r.m_resItem.name == ItemDropNames.Wisp);
          if (wispRequirement != null) wispRequirement.m_amountPerLevel = WispsPerLevel.Value;
          if (Instance.EnableTrace) Log.Trace(Instance, $"Updated {recipe.m_item.name} of {recipe.name}, set {wispRequirement?.m_resItem.name} m_amountPerLevel to {wispRequirement?.m_amountPerLevel}");

          var silverRequirement = recipe.m_resources.FirstOrDefault(r => r.m_resItem.name == ItemDropNames.Silver);
          if (silverRequirement != null) silverRequirement.m_amountPerLevel = SilverPerLevel.Value;
          if (Instance.EnableTrace) Log.Trace(Instance, $"Updated {recipe.m_item.name} of {recipe.name}, set {silverRequirement?.m_resItem.name} m_amountPerLevel to {silverRequirement?.m_amountPerLevel}");

          var eitrRequirement = recipe.m_resources.FirstOrDefault(r => r.m_resItem.name == ItemDropNames.Eitr);
          if (eitrRequirement != null)
          {
            eitrRequirement.m_amountPerLevel = EitrPerLevel.Value;
            if (Instance.EnableTrace) Log.Trace(Instance, $"Updated {recipe.m_item.name} of {recipe.name}, set {eitrRequirement.m_resItem.name} m_amountPerLevel to {eitrRequirement.m_amountPerLevel}");
          }
          else
          {
            eitrRequirement = new Piece.Requirement
            {
              m_amount = 0, m_amountPerLevel = EitrPerLevel.Value, m_resItem = PrefabManager.Cache.GetPrefab<ItemDrop>(ItemDropNames.Eitr)
            };
            recipe.m_resources = recipe.m_resources.AddItem(eitrRequirement).ToArray();
            if (Instance.EnableTrace) Log.Trace(Instance, $"Added {eitrRequirement.m_resItem.name} to {recipe.m_item.name} of {recipe.name}, set {eitrRequirement.m_resItem.name} m_amountPerLevel to {eitrRequirement.m_amountPerLevel}");
          }
        }

        var wispLight = ObjectDB.instance.m_items.FirstOrDefault(i => i.name == ItemDropNames.Demister);
        var itemDrop = wispLight?.GetComponent<ItemDrop>();

        if (itemDrop == null) return;
        itemDrop.m_itemData.m_shared.m_maxQuality = MaxLevel.Value;
        if (Instance.EnableTrace) Log.Trace(Instance, $"Updated {wispLight.name} set m_maxQuality to {itemDrop.m_itemData.m_shared.m_maxQuality}");
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
