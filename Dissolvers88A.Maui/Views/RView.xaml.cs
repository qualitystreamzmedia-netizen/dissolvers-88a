using System.Globalization;
using Dissolvers88A.Engine;
using Dissolvers88A.Maui.R;

namespace Dissolvers88A.Maui.Views;

public partial class RView : ContentView
{
    private readonly StatData _stats = AppState.Stats;
    private WebrServer? _server;
    private bool _started;

    public RView()
    {
        InitializeComponent();
        Web.Navigated += (_, _) => Hint.IsVisible = false;
    }

    /// <summary>Called when the R tab is shown; boots WebR on first use.</summary>
    public void OnShown()
    {
        if (!_started)
        {
            _started = true;
            _server = new WebrServer();
            Web.Source = _server.BaseUrl + "r-console.html";
        }
        else
        {
            _ = PushListsAsync();
        }
    }

    public void Shutdown()
    {
        _server?.Dispose();
        _server = null;
    }

    private void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("dissolvers://", StringComparison.OrdinalIgnoreCase)) return;
        e.Cancel = true;

        var uri = new Uri(e.Url);
        switch (uri.Host)
        {
            case "sync":
                _ = PushListsAsync();
                break;
            case "pull":
                HandlePull(uri);
                break;
        }
    }

    private void HandlePull(Uri uri)
    {
        var query = ParseQuery(uri.Query);
        if (!query.TryGetValue("target", out var t) || !int.TryParse(t, out var target)) return;
        query.TryGetValue("data", out var raw);

        var nums = new List<double>();
        foreach (var tok in (raw ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            if (double.TryParse(tok, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                nums.Add(d);

        if (nums.Count == 0)
        {
            Toast($"L{target + 1}: nothing numeric to pull");
            return;
        }

        target = Math.Clamp(target, 0, StatData.ListCount - 1);
        _stats.Set(target, nums);
        Toast($"Pulled {nums.Count} values → L{target + 1}");
    }

    private async Task PushListsAsync()
    {
        var payload = new System.Text.StringBuilder("{");
        for (int i = 0; i < StatData.ListCount; i++)
        {
            if (i > 0) payload.Append(',');
            payload.Append('"').Append(_stats.Name(i)).Append("\":[");
            payload.Append(string.Join(",", _stats[i].Select(v =>
                v.ToString("R", CultureInfo.InvariantCulture))));
            payload.Append(']');
        }
        payload.Append('}');

        try
        {
            await Web.EvaluateJavaScriptAsync($"window.d88a && window.d88a.setLists({payload})");
        }
        catch { /* page not ready yet */ }
    }

    private void Toast(string message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
                await page.DisplayAlert("R", message, "OK");
        });
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            result[Uri.UnescapeDataString(kv[0])] = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
        }
        return result;
    }
}
