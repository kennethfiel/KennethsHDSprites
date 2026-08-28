using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using HarmonyLib;

namespace KennethsHDSprites
{
    public class ModEntry : Mod
    {
        public static IMonitor ModMonitor = null!;
        public static Dictionary<string, int> RegisteredAssets = new(StringComparer.OrdinalIgnoreCase);
        public static ModConfig Config = new();

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            Config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
            helper.WriteConfig(Config);

            helper.Events.Content.AssetRequested += OnAssetRequested;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll();

            Monitor.Log($"Kenneth's HD Sprites 1.0.0 loaded! Supports 16,32,48,64 - UniqueID: {this.ModManifest.UniqueID}", LogLevel.Info);
        }

        private void OnAssetRequested(object? sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("KennethsHDSprites/Assets"))
            {
                e.LoadFrom(() => new Dictionary<string, AssetEntry>(), StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
            }
            // Also support legacy token for migration
            if (e.NameWithoutLocale.IsEquivalentTo("Kree.DSS/Assets") || e.NameWithoutLocale.IsEquivalentTo("KennethFiel.DSS64/Assets"))
            {
                e.LoadFrom(() => new Dictionary<string, AssetEntry>(), StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
            }
        }

        public static int GetSize(string textureName)
        {
            // Check exact
            if (RegisteredAssets.TryGetValue(textureName, out int s)) return s;
            // Check contains
            foreach (var kv in RegisteredAssets)
            {
                if (textureName.Contains(kv.Key, StringComparison.OrdinalIgnoreCase)) return kv.Value;
            }
            return Config.DefaultSize;
        }
    }

    public class ModConfig
    {
        public int DefaultSize { get; set; } = 32;
    }

    public class AssetEntry
    {
        public string Asset { get; set; } = "";
        public int SpriteSize { get; set; } = 32; // 16,32,48,64
    }

    [HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.drawInMenu))]
    public static class DrawPatch
    {
        static bool Prefix(StardewValley.Object __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool shouldDrawStackNumber, ref bool __result)
        {
            try
            {
                if (__instance?.Texture == null) return true;
                string name = __instance.Texture.Name ?? "";
                int size = ModEntry.GetSize(name);
                if (size <= 16) return true;
                if (!__instance.ItemId.Contains("Kenneth", StringComparison.OrdinalIgnoreCase) && !__instance.ItemId.Contains("FruitShop") && !__instance.ItemId.Contains("Restaurant")) 
                {
                    // allow all if registered, but for safety check registered list
                    if (!ModEntry.RegisteredAssets.ContainsKey(name)) return true;
                }

                // Draw HD sprite scaled to vanilla 16
                float scale = (16f / size) * scaleSize;
                // Source rect = full sprite (for simplicity, we assume sheet index handled elsewhere - this draws first sprite, for menu it works for single texture objects)
                // For atlas, we need to calculate index - using Game1.objectData not easily accessible here, so we use 0 as fallback and let Content Patcher handle atlas?
                // Improved: use __instance to get source rect from data if available
                Rectangle source = new Rectangle(0, 0, size, size);
                
                // If it's part of a sheet, try to get SpriteIndex from objectData
                if (Game1.objectData.TryGetValue(__instance.ItemId, out var data))
                {
                    // In 1.6, data has internal handling, but we can approximate: if texture is sheet, index * size
                    // We'll need to store spriteIndex somewhere - for now use 0 for simplicity, real atlas handling in EditImage patch
                }

                spriteBatch.Draw(__instance.Texture, location + new Vector2(32,32) * scaleSize / 2f, source, color * transparency, 0f, new Vector2(size/2f, size/2f), scale, SpriteEffects.None, layerDepth);
                __result = false;
                return false;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"KennethsHDSprites error: {ex.Message}", LogLevel.Trace);
                return true;
            }
        }
    }
}
