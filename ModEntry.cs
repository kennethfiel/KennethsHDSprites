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
            Monitor.Log($"Kenneth's HD Sprites loaded! UniqueID: {this.ModManifest.UniqueID}", LogLevel.Info);
        }

        private void OnAssetRequested(object? sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("KennethsHDSprites/Assets"))
            {
                e.LoadFrom(() => new Dictionary<string, AssetEntry>(), StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
            }
        }

        public static int GetSize(string textureName)
        {
            if (RegisteredAssets.TryGetValue(textureName, out int s)) return s;
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
        public int SpriteSize { get; set; } = 32;
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
                if (!ModEntry.RegisteredAssets.ContainsKey(name)) return true;
                float scale = (16f / size) * scaleSize;
                Rectangle source = new Rectangle(0, 0, size, size);
                spriteBatch.Draw(__instance.Texture, location + new Vector2(32,32) * scaleSize / 2f, source, color * transparency, 0f, new Vector2(size/2f, size/2f), scale, SpriteEffects.None, layerDepth);
                __result = false;
                return false;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"HD Sprites error: {ex.Message}", LogLevel.Trace);
                return true;
            }
        }
    }
}
