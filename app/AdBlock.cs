using System.Text.Json.Nodes;
using Microsoft.Web.WebView2.Core;

namespace CabinPlay;

/// <summary>
/// The ad blocker: uBlock Origin Lite (GPLv3, github.com/uBlockOrigin/uBOL-home), which
/// comes with the app in the "adblock" folder and is loaded into the one browser profile all
/// apps share, so it works on YouTube, YouTube Music, Twitch and the browser alike. It is on
/// unless switched off in the settings; switched off it stays installed but disabled.
/// </summary>
internal static class AdBlock
{
    public static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "adblock");
    private const string Name = "uBlock Origin Lite";

    /// <summary>The version that came with the app, or null when the folder is missing.</summary>
    public static readonly string? BundledVersion = ReadVersion();

    private static string? ReadVersion()
    {
        try
        {
            string manifest = Path.Combine(Folder, "manifest.json");
            return File.Exists(manifest) ? (string?)JsonNode.Parse(File.ReadAllText(manifest))?["version"] : null;
        }
        catch (Exception ex)
        {
            Log.Write("ad blocker: cannot read its manifest: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Makes the profile match the setting. <paramref name="settings"/> remembers which
    /// version went into the profile ("adBlockVersion"): a different one that came with an
    /// app update is put in afresh, because the browser keeps its own copy of what it read.
    /// Never throws; the apps work without it.
    /// </summary>
    public static async Task ApplyAsync(CoreWebView2Profile profile, bool on, JsonObject settings)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            CoreWebView2BrowserExtension? ours = null;
            foreach (CoreWebView2BrowserExtension extension in await profile.GetBrowserExtensionsAsync())
            {
                if (extension.Name == Name || extension.Id == (string?)settings["adBlockId"])
                    ours = extension;
            }

            if (BundledVersion is null)
            {
                Log.Write($"ad blocker: not shipped with this build (no {Folder})");
                if (ours is not null && ours.IsEnabled)
                    await ours.EnableAsync(false);
                return;
            }

            if (ours is not null && (string?)settings["adBlockVersion"] != BundledVersion)
            {
                Log.Write($"ad blocker: replacing {(string?)settings["adBlockVersion"] ?? "?"} with {BundledVersion}");
                await ours.RemoveAsync();
                ours = null;
            }

            if (ours is null)
            {
                if (!on)
                {
                    Log.Write("ad blocker: off");
                    return;
                }
                ours = await profile.AddBrowserExtensionAsync(Folder);
                settings["adBlockId"] = ours.Id;
                settings["adBlockVersion"] = BundledVersion;
            }

            if (ours.IsEnabled != on)
                await ours.EnableAsync(on);
            Log.Write($"ad blocker: {(on ? "on" : "off")} ({Name} {BundledVersion}, {ours.Id}, {clock.ElapsedMilliseconds} ms)");
        }
        catch (Exception ex)
        {
            // An old WebView2 runtime, or one that turns extensions down.
            Log.Write("ad blocker: failed: " + ex);
        }
    }
}
