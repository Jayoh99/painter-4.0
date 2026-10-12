using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace Painter;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "jayoh99.painter.4.0";
    public string Name { get; init; } = "Painter";
    public string Author { get; init; } = "MoxoPixel & Jayoh";
    public List<string>? Contributors { get; init; } = ["Jayoh"];
    public Version Version { get; init; } = new("2.0.0");
    public Range SptVersion { get; init; } = new("~4.1.5");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; } = new() { ["com.wtt.commonlib"] = new Range("~3.0.0") };
    public string? Url { get; init; } = "https://github.com/emilanderss0n/Painter";
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; }
}

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.Preload + 1)]
public class AddTraderWithAssortJson(
    ISptLogger<AddTraderWithAssortJson> logger,
    ModHelper modHelper,
    ImageRouter imageRouter,
    TraderConfig traderConfig,
    RagfairConfig ragfairConfig,
    TimeUtil timeUtil,
    WTTServerCommonLib.WTTServerCommonLib wttCommon,
    AddCustomTraderHelper traderHelper
) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var assembly = Assembly.GetExecutingAssembly();
        var pathToMod = modHelper.GetAbsolutePathToModFolder(assembly);
        var traderImagePath = Path.Combine(pathToMod, "data", "painter.png");
        var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, Path.Combine("data", "base.json"));

        // Register the trader icon and stock refresh time
        imageRouter.AddRoute(traderBase.Avatar.Replace(".png", ""), traderImagePath);
        traderHelper.SetTraderUpdateTime(traderConfig, traderBase, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));

        // Lets the trader's offers be seen on the flea market
        ragfairConfig.Traders.TryAdd(traderBase.Id, true);

        traderHelper.AddTraderWithEmptyAssortToDb(traderBase);
        traderHelper.AddTraderToLocales(traderBase, "Painter", "Painter is a trader who sells unique weapon mods and gear. Also painted weapon parts (incl. whole weapons).");

        var assort = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, Path.Combine("data", "assort.json"));
        traderHelper.OverwriteTraderAssort(traderBase.Id, assort);

        // Custom content is loaded through WTT-CommonLib
        await wttCommon.CustomQuestService.CreateCustomQuests(assembly);
        cancellationToken.ThrowIfCancellationRequested();
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        cancellationToken.ThrowIfCancellationRequested();
        await wttCommon.CustomWeaponPresetService.CreateCustomWeaponPresets(assembly);
        cancellationToken.ThrowIfCancellationRequested();
        await wttCommon.CustomClothingService.CreateCustomClothing(assembly);

        logger.Success("[Painter] SPT 4.1.5 port loaded successfully.");
    }
}
