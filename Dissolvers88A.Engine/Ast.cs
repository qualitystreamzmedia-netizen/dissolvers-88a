namespace Dissolvers88A.Engine;

/// <summary>Abstract syntax tree for a parsed expression.</summary>
public abstract record Node;

public sealed record NumberNode(double Value) : Node;

/// <summary>A single-letter variable (A–Z, X, Y, θ, T, N) or a named constant (pi, e).</summary>
public sealed record VariableNode(string Name) : Node;

public sealed record UnaryNode(char Op, Node Operand) : Node;          // '-' or '+'

public sealed record BinaryNode(char Op, Node Left, Node Right) : Node; // + - * / ^

public sealed record CallNode(string Name, IReadOnlyList<Node> Args) : Node;

public sealed record FactorialNode(Node Operand) : Node;

/// <summary><c>expr → VAR</c> — store the value of <see cref="Value"/> into <see cref="Target"/>.</summary>
public sealed record AssignNode(Node Value, string Target) : Node;
