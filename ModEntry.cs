using StardewModdingAPI;
using System.Collections.Generic;

namespace KennethsHDSprites
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            Monitor.Log("Kenneth's HD Sprites 1.0.0 loaded! Supports 32,48,64", LogLevel.Info);
            helper.Events.Content.AssetRequested += OnAssetRequested;
        }
        private void OnAssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("KennethsHDSprites/Assets"))
            {
                e.LoadFrom(() => new Dictionary<string, object>(), StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
            }
        }
    }
    public class AssetEntry
    {
        public string Asset { get; set; } = "";
        public int SpriteSize { get; set; } = 32;
    }
}
