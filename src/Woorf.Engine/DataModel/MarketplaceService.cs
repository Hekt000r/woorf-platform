using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

internal sealed class MarketplaceService : Instance
{
    internal MarketplaceService() : base("MarketplaceService") =>
        GetSignal("Destroying").Connect(_ => PromptGamePassPurchaseFinished.Close());

    internal ScriptSignal PromptGamePassPurchaseFinished { get; } = new();

    internal void PromptGamePassPurchase(Player player, double gamePassId) =>
        PromptGamePassPurchaseFinished.Fire(player, gamePassId, false);
}
