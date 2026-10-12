using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Utils.Cloners;

namespace Painter;

/// <summary>
/// Injected into 'AddTraderWithAssortJson' to help register the Painter trader with the server
/// </summary>
[Injectable(InjectionType.Singleton)]
public class AddCustomTraderHelper(
    ISptLogger<AddCustomTraderHelper> logger,
    ICloner cloner,
    TradersTable tradersTable,
    LocaleTable localeTable)
{
    /// <summary>
    /// Add the traders update time for when their offers refresh
    /// </summary>
    public void SetTraderUpdateTime(TraderConfig traderConfig, TraderBase traderBase, int refreshTimeSecondsMin, int refreshTimeSecondsMax)
    {
        traderConfig.UpdateTime.Add(new UpdateTime
        {
            TraderId = traderBase.Id,
            Seconds = new MinMax<int>(refreshTimeSecondsMin, refreshTimeSecondsMax)
        });
    }

    /// <summary>
    /// Add a traders base data to the server, no assort items
    /// </summary>
    public void AddTraderWithEmptyAssortToDb(TraderBase traderBase)
    {
        var trader = new Trader
        {
            Assort = new TraderAssort
            {
                Items = [],
                BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                LoyalLevelItems = new Dictionary<MongoId, int>()
            },
            Base = cloner.Clone(traderBase),
            QuestAssort = new Dictionary<string, Dictionary<MongoId, MongoId>>
            {
                ["Started"] = new(),
                ["Success"] = new(),
                ["Fail"] = new()
            },
            Dialogue = new Dictionary<string, List<string>>()
        };

        if (!tradersTable.TryAdd(traderBase.Id, trader))
        {
            logger.Warning($"Painter trader {traderBase.Id} is already registered; keeping the existing entry.");
        }
    }

    /// <summary>
    /// Add traders name/location/description to all locales
    /// </summary>
    public void AddTraderToLocales(TraderBase traderBase, string firstName, string description)
    {
        var traderId = traderBase.Id;
        foreach (var locale in localeTable.Global.Values)
        {
            // Locales are lazy loaded, so a transformer is needed to apply our entries whenever they are requested
            locale.AddTransformer(localeData =>
            {
                localeData![$"{traderId} FullName"] = traderBase.Name;
                localeData[$"{traderId} FirstName"] = firstName;
                localeData[$"{traderId} Nickname"] = traderBase.Nickname;
                localeData[$"{traderId} Location"] = traderBase.Location;
                localeData[$"{traderId} Description"] = description;
                return localeData;
            });
        }
    }

    /// <summary>
    /// Overwrite the desired traders assorts with the ones provided
    /// </summary>
    public void OverwriteTraderAssort(MongoId traderId, TraderAssort newAssort)
    {
        if (!tradersTable.TryGetValue(traderId, out var trader))
        {
            logger.Warning($"Unable to update Painter assort because trader {traderId} was not registered.");
            return;
        }

        trader.Assort = newAssort;
    }
}
