using Dissolvers88A.Engine;

namespace Dissolvers88A.Maui;

/// <summary>App-wide shared state — the six statistics lists, shared by the
/// Stats screen and the graph screen's stat plots.</summary>
public static class AppState
{
    public static readonly StatData Stats = new();
}
