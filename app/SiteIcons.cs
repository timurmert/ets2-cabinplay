using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;

namespace CabinPlay;

/// <summary>
/// Logos for the browser's start page: each site's own icon, the largest one it offers,
/// kept in %LocalAppData%\CabinPlay\icons as "&lt;host&gt;.&lt;ext&gt;" and served to the shell
/// from <see cref="Host"/>. The shell asks for the sites it shows; whatever is missing is
/// fetched in the background, from the site's page (its icon links) or from the usual
/// places (/apple-touch-icon.png, /favicon.ico). A site that turns that away gets the icon
/// the browser shows for it, once it is opened there.
/// </summary>
internal sealed partial class SiteIcons
{
    public const string Host = "icons.cabinplay.local";
    private const int MaxBytes = 1 << 20;
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);
    private static readonly string[] Extensions = { ".png", ".ico", ".svg", ".webp", ".jpg", ".gif", ".bmp" };

    private readonly HttpClient _http;
    private readonly object _gate = new();
    private readonly Dictionary<string, string> _files = new();  // host -> file name
    private readonly HashSet<string> _tried = new();             // this run, fetched or failed
    private readonly HashSet<string> _failed = new();            // nothing found: the browser's icon will do
    private readonly Queue<(string Host, Uri Page)> _queue = new();
    private bool _working;

    /// <summary>An icon was added or replaced. Raised on a worker thread.</summary>
    public event Action? Changed;

    public string Folder { get; } = Path.Combine(Program.DataDir, "icons");

    public SiteIcons(string userAgent)
    {
        _http = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            MaxAutomaticRedirections = 5,
        })
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en;q=0.8");
        try
        {
            Directory.CreateDirectory(Folder);
            foreach (string file in Directory.EnumerateFiles(Folder))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (Extensions.Contains(ext))
                    _files[Path.GetFileNameWithoutExtension(file)] = Path.GetFileName(file);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Write("site icons folder: " + ex.Message);
        }
    }

    /// <summary>The start page's name for a site: its host without "www.", as in ui/shell.js.</summary>
    public static string? HostOf(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
            return null;
        string host = uri.IdnHost.ToLowerInvariant();
        return host.StartsWith("www.") ? host[4..] : host;
    }

    /// <summary>Host -> address of its icon, for the shell. The address changes with the file.</summary>
    public Dictionary<string, string> Addresses()
    {
        lock (_gate)
        {
            var all = new Dictionary<string, string>();
            foreach ((string host, string file) in _files)
            {
                long stamp = 0;
                try
                {
                    stamp = File.GetLastWriteTimeUtc(Path.Combine(Folder, file)).Ticks / TimeSpan.TicksPerSecond;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                }
                all[host] = $"https://{Host}/{Uri.EscapeDataString(file)}?v={stamp}";
            }
            return all;
        }
    }

    /// <summary>The sites on the start page: fetches the icons missing, or a month old.</summary>
    public void Want(IEnumerable<string> urls)
    {
        lock (_gate)
        {
            foreach (string url in urls)
            {
                string? host = HostOf(url);
                if (host is null || !Uri.TryCreate(url, UriKind.Absolute, out Uri? page) || !_tried.Add(host))
                    continue;
                if (_files.TryGetValue(host, out string? file) && !Old(file))
                    continue;
                _queue.Enqueue((host, page));
            }
            if (_working || _queue.Count == 0)
                return;
            _working = true;
        }
        _ = Task.Run(WorkAsync);
    }

    /// <summary>
    /// A page the browser has open: if its site is one whose icon could not be fetched, the
    /// browser's own icon of it is kept instead.
    /// </summary>
    public async Task TakeFromBrowserAsync(CoreWebView2 core)
    {
        string? host = HostOf(core.Source);
        lock (_gate)
        {
            if (host is null || !_failed.Contains(host) || _files.ContainsKey(host))
                return;
        }
        try
        {
            using Stream image = await core.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
            using var copy = new MemoryStream();
            await image.CopyToAsync(copy);
            if (copy.Length > 0 && Save(host!, copy.ToArray(), ".png"))
            {
                lock (_gate)
                    _failed.Remove(host!);
                Log.Write($"site icon for {host}: the browser's");
                Changed?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Log.Write($"site icon for {host} from the browser failed: {ex.Message}");
        }
    }

    private bool Old(string file)
    {
        try
        {
            return DateTime.UtcNow - File.GetLastWriteTimeUtc(Path.Combine(Folder, file)) > MaxAge;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private async Task WorkAsync()
    {
        while (true)
        {
            (string Host, Uri Page) next;
            lock (_gate)
            {
                if (_queue.Count == 0)
                {
                    _working = false;
                    return;
                }
                next = _queue.Dequeue();
            }
            bool found = false;
            try
            {
                found = await FetchAsync(next.Host, next.Page);
            }
            catch (Exception ex)
            {
                Log.Write($"site icon for {next.Host} failed: {ex.Message}");
            }
            if (found)
            {
                Changed?.Invoke();
            }
            else
            {
                lock (_gate)
                {
                    if (!_files.ContainsKey(next.Host))
                        _failed.Add(next.Host);
                }
            }
        }
    }

    /// <summary>Tries the site's icons, best first, and keeps the first that is a picture.</summary>
    private async Task<bool> FetchAsync(string host, Uri page)
    {
        var links = new List<(Uri Url, int Size)>();
        Uri origin = new(page.GetLeftPart(UriPartial.Authority));
        try
        {
            using HttpResponseMessage response = await _http.GetAsync(page, HttpCompletionOption.ResponseHeadersRead);
            Uri final = response.RequestMessage?.RequestUri ?? page;
            origin = new Uri(final.GetLeftPart(UriPartial.Authority));
            if (response.IsSuccessStatusCode)
                links = IconLinks(await ReadAsync(response, 2 * MaxBytes) is byte[] html
                    ? System.Text.Encoding.UTF8.GetString(html) : "", final);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            Log.Write($"site icon for {host}: page not read ({ex.Message})");
        }

        // Big ones from the page, then the usual places, then the small ones from the page.
        var tries = new List<Uri>();
        tries.AddRange(links.Where(l => l.Size >= 64).Select(l => l.Url));
        tries.Add(new Uri(origin, "/apple-touch-icon.png"));
        tries.AddRange(links.Where(l => l.Size < 64).Select(l => l.Url));
        tries.Add(new Uri(origin, "/favicon.ico"));
        foreach (Uri url in tries.Distinct())
        {
            try
            {
                using HttpResponseMessage response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                    continue;
                byte[]? data = await ReadAsync(response, MaxBytes);
                string? ext = data is null ? null : Sniff(data);
                if (ext is not null && Save(host, data!, ext))
                {
                    Log.Write($"site icon for {host}: {url}");
                    return true;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            {
            }
        }
        Log.Write($"site icon for {host}: none found");
        return false;
    }

    /// <summary>The body, or null if it is longer than <paramref name="limit"/>.</summary>
    private static async Task<byte[]?> ReadAsync(HttpResponseMessage response, int limit)
    {
        if (response.Content.Headers.ContentLength > limit)
            return null;
        using Stream body = await response.Content.ReadAsStreamAsync();
        using var copy = new MemoryStream();
        var buffer = new byte[16384];
        int n;
        while ((n = await body.ReadAsync(buffer)) > 0)
        {
            if (copy.Length + n > limit)
                return null;
            copy.Write(buffer, 0, n);
        }
        return copy.ToArray();
    }

    [GeneratedRegex(@"<link\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex LinkTag();

    [GeneratedRegex(@"([\w-]+)\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s>""']+))")]
    private static partial Regex Attribute();

    /// <summary>The page's icon links, each with the size it says it is (a guess if it does not say).</summary>
    private static List<(Uri Url, int Size)> IconLinks(string html, Uri page)
    {
        var found = new List<(Uri Url, int Size)>();
        foreach (Match tag in LinkTag().Matches(html))
        {
            var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match a in Attribute().Matches(tag.Value))
                attrs[a.Groups[1].Value] = WebUtility.HtmlDecode(a.Groups[2].Success ? a.Groups[2].Value
                    : a.Groups[3].Success ? a.Groups[3].Value : a.Groups[4].Value);
            string[] rel = attrs.GetValueOrDefault("rel", "").ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool touch = rel.Any(r => r.StartsWith("apple-touch-icon"));
            if (!touch && !rel.Contains("icon"))
                continue; // mask-icon is a one-colour outline
            string href = attrs.GetValueOrDefault("href", "").Trim();
            if (href.Length == 0 || href.StartsWith("data:") || !Uri.TryCreate(page, href, out Uri? url) ||
                (url.Scheme != "https" && url.Scheme != "http"))
                continue;
            int size = 0;
            string sizes = attrs.GetValueOrDefault("sizes", "").ToLowerInvariant();
            foreach (string s in sizes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (s == "any")
                    size = Math.Max(size, 512);
                else if (int.TryParse(s.Split('x')[0], out int n))
                    size = Math.Max(size, n);
            }
            if (size == 0)
                size = touch ? 180 : url.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ? 512 : 32;
            found.Add((url, size));
        }
        return found.OrderByDescending(f => f.Size).ToList();
    }

    /// <summary>What kind of picture the bytes are, as a file extension, or null if none.</summary>
    private static string? Sniff(byte[] d)
    {
        if (d.Length < 12)
            return null;
        if (d[0] == 0x89 && d[1] == 'P' && d[2] == 'N' && d[3] == 'G') return ".png";
        if (d[0] == 0 && d[1] == 0 && d[2] == 1 && d[3] == 0) return ".ico";
        if (d[0] == 0xFF && d[1] == 0xD8) return ".jpg";
        if (d[0] == 'G' && d[1] == 'I' && d[2] == 'F') return ".gif";
        if (d[0] == 'B' && d[1] == 'M') return ".bmp";
        if (d[0] == 'R' && d[1] == 'I' && d[2] == 'F' && d[3] == 'F' && d[8] == 'W' && d[9] == 'E' && d[10] == 'B' && d[11] == 'P')
            return ".webp";
        string start = System.Text.Encoding.UTF8.GetString(d, 0, Math.Min(d.Length, 512)).TrimStart('﻿', ' ', '\r', '\n', '\t');
        if (start.StartsWith("<svg") || (start.StartsWith("<?xml") && start.Contains("<svg")))
            return ".svg";
        return null;
    }

    private bool Save(string host, byte[] data, string ext)
    {
        if (host.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;
        try
        {
            string file = host + ext;
            string path = Path.Combine(Folder, file);
            string temp = path + ".part";
            File.WriteAllBytes(temp, data);
            File.Move(temp, path, overwrite: true);
            lock (_gate)
            {
                // the same site's picture of another kind is out of date now
                if (_files.TryGetValue(host, out string? before) && before != file)
                    File.Delete(Path.Combine(Folder, before));
                _files[host] = file;
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Write($"site icon for {host} not saved: {ex.Message}");
            return false;
        }
    }
}
