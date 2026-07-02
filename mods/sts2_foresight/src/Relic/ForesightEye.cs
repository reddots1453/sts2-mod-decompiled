using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Foresight.Relic;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class ForesightEye : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    // TODO: replace with custom art
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/neows_lament.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/neows_lament.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/neows_lament.tres"
    );
}
