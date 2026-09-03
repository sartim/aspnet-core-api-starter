namespace AspNetCoreApiStarter.Models;

public sealed class OutboxMessage : Base
{
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public required DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
