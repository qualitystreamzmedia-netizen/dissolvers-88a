using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dissolvers88A.Engine;
using Dissolvers88A.Mvvm;
using Dissolvers88A.R;

namespace Dissolvers88A.ViewModels;

public sealed class RViewModel : ObservableObject
{
    private readonly StatData _stats = AppState.Stats;
    private RSession? _session;
    private bool _starting;

    public RInstall? Install { get; } = RInstall.Discover();
    public bool IsAvailable => Install is not null;
    public string DownloadUrl => RInstall.DownloadUrl;

    public string HeaderText => IsAvailable
        ? $"native R {Install!.Version}"
        : "R not found on this PC";

    public ObservableCollection<RConsoleEntry> Console { get; } = new();
    public ObservableCollection<ImageSource> Plots { get; } = new();

    public RViewModel()
    {
        RunCommand      = new RelayCommand(_ => _ = SubmitAsync(), _ => CanRun);
        ClearLogCommand = new RelayCommand(_ => Console.Clear());
        PrevPlotCommand = new RelayCommand(_ => PlotIndex--, _ => PlotIndex > 0);
        NextPlotCommand = new RelayCommand(_ => PlotIndex++, _ => PlotIndex < Plots.Count - 1);
        PushListsCommand = new RelayCommand(_ => _ = PushListsAsync(), _ => Ready);
        PullCommand      = new RelayCommand(_ => _ = PullAsync(), _ => Ready && !string.IsNullOrWhiteSpace(PullExpression));
    }

    // ---- lifecycle ------------------------------------------------------

    /// <summary>Called when the R tab is shown; boots R on first use.</summary>
    public async void Activate()
    {
        if (!IsAvailable || _session is not null || _starting) return;
        _starting = true;
        AddLine($"Starting R {Install!.Version}…", RStreamKind.System);
        try
        {
            var session = new RSession(Install);
            session.Line += (text, kind) => OnUi(() => AddLine(text, kind));
            session.Plot += path => OnUi(() => AddPlot(path));
            await session.Ready;
            _session = session;
            Ready = true;
            AddLine("R is ready. Lists L1–L6 are synced from the Stats screen.", RStreamKind.System);
            await PushListsAsync();
        }
        catch (Exception ex)
        {
            AddLine("Could not start R: " + ex.Message, RStreamKind.System);
        }
        _starting = false;
    }

    public void Shutdown()
    {
        _session?.Dispose();
        _session = null;
        Ready = false;
    }

    // ---- console ------------------------------------------------------

    public string InputText { get => _input; set { if (Set(ref _input, value)) Raise(nameof(CanRun)); } }
    private string _input = "";

    private bool _ready;
    public bool Ready { get => _ready; private set { if (Set(ref _ready, value)) { Raise(nameof(CanRun)); Raise(nameof(StatusText)); } } }

    private bool _busy;
    public bool IsBusy { get => _busy; private set { if (Set(ref _busy, value)) { Raise(nameof(CanRun)); Raise(nameof(StatusText)); } } }

    public bool CanRun => Ready && !IsBusy && !string.IsNullOrWhiteSpace(_input);

    public string StatusText => !IsAvailable ? "unavailable"
                              : !Ready ? "starting…"
                              : IsBusy ? "running…"
                              : "idle";

    public RelayCommand RunCommand { get; }
    public RelayCommand ClearLogCommand { get; }

    private async System.Threading.Tasks.Task SubmitAsync()
    {
        if (_session is null || IsBusy || string.IsNullOrWhiteSpace(_input)) return;
        var code = _input.TrimEnd();
        InputText = "";
        foreach (var l in code.Split('\n')) AddLine("> " + l, RStreamKind.Echo);

        IsBusy = true;
        var t0 = DateTime.UtcNow;
        try { await _session.SubmitAsync(code); }
        catch (Exception ex) { AddLine(ex.Message, RStreamKind.Error); }
        AddLine($"({(DateTime.UtcNow - t0).TotalSeconds:0.00}s)", RStreamKind.System);
        IsBusy = false;
    }

    private void AddLine(string text, RStreamKind kind)
    {
        Console.Add(new RConsoleEntry(text, kind));
        while (Console.Count > 600) Console.RemoveAt(0);
    }

    // ---- plots ------------------------------------------------------

    private int _plotIndex = -1;
    public int PlotIndex
    {
        get => _plotIndex;
        set
        {
            var clamped = Plots.Count == 0 ? -1 : Math.Clamp(value, 0, Plots.Count - 1);
            if (Set(ref _plotIndex, clamped)) { Raise(nameof(CurrentPlot)); Raise(nameof(PlotCounter)); Raise(nameof(HasPlots)); }
        }
    }

    public ImageSource? CurrentPlot => _plotIndex >= 0 && _plotIndex < Plots.Count ? Plots[_plotIndex] : null;
    public bool HasPlots => Plots.Count > 0;
    public bool NoPlots => Plots.Count == 0;
    public string PlotCounter => Plots.Count > 1 ? $"plot {_plotIndex + 1} / {Plots.Count}" : "";

    public RelayCommand PrevPlotCommand { get; }
    public RelayCommand NextPlotCommand { get; }

    private void AddPlot(string path)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.UriSource = new Uri(path);
            bmp.EndInit();
            bmp.Freeze();
            Plots.Add(bmp);
            Raise(nameof(HasPlots));
            Raise(nameof(NoPlots));
            PlotIndex = Plots.Count - 1;
        }
        catch (Exception ex) { AddLine("plot load failed: " + ex.Message, RStreamKind.System); }
    }

    // ---- data bridge ------------------------------------------------------

    public RelayCommand PushListsCommand { get; }
    public RelayCommand PullCommand { get; }

    public string PullExpression { get => _pullExpr; set { if (Set(ref _pullExpr, value)) Raise(nameof(CanPull)); } }
    private string _pullExpr = "";

    public int PullTargetIndex { get => _pullTarget; set => Set(ref _pullTarget, value); }
    private int _pullTarget;

    public bool CanPull => Ready && !string.IsNullOrWhiteSpace(_pullExpr);

    private async System.Threading.Tasks.Task PushListsAsync()
    {
        if (_session is null) return;
        int pushed = 0;
        for (int i = 0; i < StatData.ListCount; i++)
        {
            await _session.SetVectorAsync(_stats.Name(i), _stats[i]);
            if (_stats[i].Count > 0) pushed++;
        }
        AddLine($"Synced {pushed} non-empty list(s) → R (L1–L6).", RStreamKind.System);
    }

    private async System.Threading.Tasks.Task PullAsync()
    {
        if (_session is null || string.IsNullOrWhiteSpace(_pullExpr)) return;
        var raw = await _session.QueryAsync(_pullExpr.Trim());
        if (string.IsNullOrWhiteSpace(raw)) { AddLine($"'{_pullExpr}' is empty or not numeric.", RStreamKind.Error); return; }

        var nums = new List<double>();
        foreach (var tok in raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            if (double.TryParse(tok, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var d))
                nums.Add(d);

        if (nums.Count == 0) { AddLine($"'{_pullExpr}' did not yield numbers.", RStreamKind.Error); return; }
        _stats.Set(_pullTarget, nums);
        AddLine($"{_pullExpr} → {_stats.Name(_pullTarget)}  ({nums.Count} values)", RStreamKind.System);
    }

    // ---- helpers ------------------------------------------------------

    private static void OnUi(Action a)
    {
        var app = Application.Current;
        if (app is null) a();
        else app.Dispatcher.Invoke(a);
    }
}
