using Dissolvers88A.Engine;

namespace Dissolvers88A;

/// <summary>App-wide shared state — currently just the six statistics lists,
/// used by the Stats screen and the graph screen's stat plots.</summary>
public static class AppState
{
    public static readonly StatData Stats = new();
}
