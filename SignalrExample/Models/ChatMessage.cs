namespace SignalrExample.Models;

public record ChatMessage(
    string User,
    string Text,
    string? Group,
    DateTime Timestamp);
