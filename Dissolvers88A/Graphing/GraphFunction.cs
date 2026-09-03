using System.Windows.Media;
using Dissolvers88A.Engine;
using Dissolvers88A.Mvvm;

namespace Dissolvers88A.Graphing;

/// <summary>One slot in the Y= editor: an expression in X, a colour, and an on/off switch.</summary>
public sealed class GraphFunction : ObservableObject
{
    public GraphFunction(string label, Color color)
    {
        Label = label;
        Color = color;
        Brush = new SolidColorBrush(color);
        Brush.Freeze();
    }

    public string Label { get; }
    public Color Color { get; }
    public Brush Brush { get; }

    private string _text = "";
    public string Text
    {
        get => _text;
        set { if (Set(ref _text, value)) Recompile(); }
    }

    private bool _enabled = true;
    public bool Enabled
    {
        get => _enabled;
        set { if (Set(ref _enabled, value)) Changed?.Invoke(); }
    }

    public Node? Compiled { get; private set; }

    private string? _error;
    public string? Error
    {
        get => _error;
        private set { if (Set(ref _error, value)) Raise(nameof(HasError)); }
    }

    public bool HasError => _error != null;

    public bool IsPlottable => Enabled && Compiled != null;

    /// <summary>Raised when the plotted result of this row changes.</summary>
    public event Action? Changed;

    private void Recompile()
    {
        if (string.IsNullOrWhiteSpace(_text))
        {
            Compiled = null;
            Error = null;
        }
        else
        {
            try
            {
                Compiled = Calculator.Compile(_text);
                Error = null;
            }
            catch (CalcException ex)
            {
                Compiled = null;
                Error = ex.Message;
            }
            catch (Exception)
            {
                Compiled = null;
                Error = "SYNTAX ERROR";
            }
        }
        Changed?.Invoke();
    }
}
