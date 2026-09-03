using Dissolvers88A.Mvvm;

namespace Dissolvers88A.ViewModels;

/// <summary>One row of the Stats list editor — six nullable cells (L1–L6).</summary>
public sealed class StatRow : ObservableObject
{
    private readonly double?[] _cells = new double?[6];

    public double? C1 { get => _cells[0]; set => SetCell(0, value); }
    public double? C2 { get => _cells[1]; set => SetCell(1, value); }
    public double? C3 { get => _cells[2]; set => SetCell(2, value); }
    public double? C4 { get => _cells[3]; set => SetCell(3, value); }
    public double? C5 { get => _cells[4]; set => SetCell(4, value); }
    public double? C6 { get => _cells[5]; set => SetCell(5, value); }

    public double? this[int i] => _cells[i];

    /// <summary>Raised when any cell changes so the editor can rebuild the lists.</summary>
    public event Action? CellChanged;

    private void SetCell(int i, double? v)
    {
        if (Nullable.Equals(_cells[i], v)) return;
        _cells[i] = v;
        Raise(i switch { 0 => nameof(C1), 1 => nameof(C2), 2 => nameof(C3), 3 => nameof(C4), 4 => nameof(C5), _ => nameof(C6) });
        CellChanged?.Invoke();
    }

    public bool IsEmpty => _cells.All(c => c == null);
}
