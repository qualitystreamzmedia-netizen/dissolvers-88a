namespace Dissolvers88A.Engine;

/// <summary>
/// The six statistics lists L1–L6, shared across the app (list editor, STAT
/// calculations, and the stat plots on the graph screen). In-memory only,
/// like a TI-84's RAM.
/// </summary>
public sealed class StatData
{
    public const int ListCount = 6;

    private readonly List<double>[] _lists =
        Enumerable.Range(0, ListCount).Select(_ => new List<double>()).ToArray();

    /// <summary>Raised whenever any list changes, so views / plots can refresh.</summary>
    public event Action? Changed;

    /// <param name="index">0 = L1 … 5 = L6.</param>
    public IReadOnlyList<double> this[int index] => _lists[index];

    public string Name(int index) => "L" + (index + 1);

    public void Set(int index, IEnumerable<double> values)
    {
        _lists[index].Clear();
        _lists[index].AddRange(values);
        Changed?.Invoke();
    }

    public void Clear(int index)
    {
        _lists[index].Clear();
        Changed?.Invoke();
    }

    public void ClearAll()
    {
        foreach (var l in _lists) l.Clear();
        Changed?.Invoke();
    }

    public bool AnyData => _lists.Any(l => l.Count > 0);

    /// <summary>Parse a "L3" / "l3" reference to its 0-based index, or -1.</summary>
    public static int IndexOf(string name)
    {
        name = name.Trim();
        if (name.Length == 2 && (name[0] is 'L' or 'l') && char.IsDigit(name[1]))
        {
            int n = name[1] - '0';
            if (n is >= 1 and <= ListCount) return n - 1;
        }
        return -1;
    }
}
