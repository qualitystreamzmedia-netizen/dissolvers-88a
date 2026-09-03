using Dissolvers88A.R;

namespace Dissolvers88A.ViewModels;

/// <summary>One line in the R console transcript.</summary>
public sealed record RConsoleEntry(string Text, RStreamKind Kind);
