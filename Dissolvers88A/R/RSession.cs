using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Dissolvers88A.R;

public enum RStreamKind { Output, Error, Echo, Plot, System }

/// <summary>
/// A persistent native-R session driven over stdin/stdout of <c>Rterm.exe</c>.
/// Each submitted command is wrapped so that any plot it draws is snapshotted to
/// a PNG on disk; completion is framed by a sentinel line.
/// </summary>
public sealed class RSession : IDisposable
{
    private const string Eoc     = "<<<D88A_EOC>>>";
    private const string PlotTag = "<<<D88A_PLOT:";
    private const string CapTag  = "<<<D88A_CAP>>>";

    private readonly Process _proc;
    private readonly StreamWriter _stdin;
    private readonly string _sessionDir;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource? _turn;
    private string? _captured;
    private bool _sawFirstEoc;

    /// <summary>text, kind — marshal to the UI thread in the handler.</summary>
    public event Action<string, RStreamKind>? Line;
    /// <summary>Absolute path of a PNG a command produced.</summary>
    public event Action<string>? Plot;

    public string Version { get; }
    public string SessionDirectory => _sessionDir;

    public RSession(RInstall r)
    {
        Version = r.Version;
        _sessionDir = Path.Combine(Path.GetTempPath(), "d88a-r-" + Guid.NewGuid().ToString("N")[..10]);
        Directory.CreateDirectory(_sessionDir);

        _proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = r.RTermExe,
                Arguments = "--vanilla --no-echo",
                WorkingDirectory = _sessionDir,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
            EnableRaisingEvents = true,
        };
        _proc.StartInfo.EnvironmentVariables["R_HOME"] = r.Home;
        _proc.OutputDataReceived += (_, e) => OnStdout(e.Data);
        _proc.ErrorDataReceived  += (_, e) => { if (e.Data is not null) Line?.Invoke(e.Data, RStreamKind.Error); };
        _proc.Exited += (_, _) => Line?.Invoke("R session ended.", RStreamKind.System);

        _proc.Start();
        _proc.BeginOutputReadLine();
        _proc.BeginErrorReadLine();
        _stdin = _proc.StandardInput;

        _stdin.Write(InitScript());
        _stdin.Flush();
    }

    /// <summary>Completes once R has processed the init script.</summary>
    public Task Ready => _ready.Task;

    /// <summary>Run one console submission; completes when R is idle again.</summary>
    public async Task SubmitAsync(string code)
    {
        if (_proc.HasExited) { Line?.Invoke("R session is not running.", RStreamKind.System); return; }

        _turn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _stdin.WriteLineAsync($".d88a_eval({RString(code)})");
        await _stdin.WriteLineAsync($"cat(\"{Eoc}\\n\")");
        await _stdin.FlushAsync();
        await _turn.Task;
    }

    /// <summary>Assign an R numeric vector in the global environment (empty ⇒ NULL).</summary>
    public async Task SetVectorAsync(string name, IReadOnlyList<double> values)
    {
        if (_proc.HasExited) return;
        var body = values.Count == 0
            ? "NULL"
            : "c(" + string.Join(",", values.Select(v => double.IsNaN(v)
                ? "NA_real_"
                : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture))) + ")";
        _turn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _stdin.WriteLineAsync($"{name} <- {body}");
        await _stdin.WriteLineAsync($"cat(\"{Eoc}\\n\")");
        await _stdin.FlushAsync();
        await _turn.Task;
    }

    /// <summary>Evaluate <paramref name="rExpr"/> and return its space-joined text, or null on failure.</summary>
    public async Task<string?> QueryAsync(string rExpr)
    {
        if (_proc.HasExited) return null;
        _captured = null;
        _turn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _stdin.WriteLineAsync(
            $"cat(\"{CapTag}\"); try(cat(paste(format({rExpr}, digits = 12, scientific = FALSE), collapse = \" \")), silent = TRUE); cat(\"\\n\")");
        await _stdin.WriteLineAsync($"cat(\"{Eoc}\\n\")");
        await _stdin.FlushAsync();
        await _turn.Task;
        return _captured;
    }

    private void OnStdout(string? data)
    {
        if (data is null) return;

        if (data.StartsWith(PlotTag))
        {
            var file = data[PlotTag.Length..].TrimEnd('>', '\r', '\n');
            var full = Path.Combine(_sessionDir, file);
            if (File.Exists(full)) Plot?.Invoke(full);
            return;
        }
        if (data.StartsWith(CapTag))
        {
            _captured = data[CapTag.Length..].TrimEnd('\r', '\n');
            return;
        }
        if (data.Contains(Eoc))
        {
            if (!_sawFirstEoc) { _sawFirstEoc = true; _ready.TrySetResult(); }
            else _turn?.TrySetResult();
            return;
        }
        Line?.Invoke(data, RStreamKind.Output);
    }

    private string InitScript() =>
        ".d88a_dir <- " + RString(_sessionDir) + "\n" +
        ".d88a_drew <- FALSE\n" +
        "setHook(\"plot.new\", function(...) .d88a_drew <<- TRUE)\n" +
        "options(device = function(...) png(tempfile(\"canvas\", .d88a_dir, \".png\"), " +
            "width = 1000, height = 760, res = 110, type = \"cairo\", ...))\n" +
        ".d88a_eval <- function(txt) {\n" +
        "  f <- tempfile(\"plot\", .d88a_dir, \".png\")\n" +
        "  grDevices::png(f, width = 1000, height = 760, res = 110, type = \"cairo\")\n" +
        "  .d88a_drew <<- FALSE\n" +
        "  on.exit({\n" +
        "    try(grDevices::dev.off(), silent = TRUE)\n" +
        "    if (isTRUE(.d88a_drew)) cat(sprintf(\"" + PlotTag + "%s>>>\\n\", basename(f))) else unlink(f)\n" +
        "  })\n" +
        "  tryCatch(\n" +
        "    source(textConnection(txt), echo = FALSE, print.eval = TRUE, max.deparse.length = Inf),\n" +
        "    error   = function(e) cat(\"Error:\", conditionMessage(e), \"\\n\"),\n" +
        "    warning = function(w) cat(\"Warning:\", conditionMessage(w), \"\\n\"))\n" +
        "}\n" +
        "cat(\"" + Eoc + "\\n\")\n";

    /// <summary>Quote a string as an R single-quoted literal.</summary>
    private static string RString(string s) =>
        "'" + s.Replace("\\", "\\\\").Replace("'", "\\'") + "'";

    public void Dispose()
    {
        try
        {
            if (!_proc.HasExited)
            {
                _stdin.WriteLine("q('no')");
                _stdin.Flush();
                if (!_proc.WaitForExit(1500)) _proc.Kill(entireProcessTree: true);
            }
        }
        catch { /* best effort */ }
        _proc.Dispose();
        try { Directory.Delete(_sessionDir, recursive: true); } catch { /* leave temp behind */ }
    }
}
