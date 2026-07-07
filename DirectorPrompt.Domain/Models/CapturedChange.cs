namespace DirectorPrompt.Domain.Models;

public sealed record CapturedChange
{
    public required string TableName { get; init; }

    public required long RecordID { get; init; }

    public required string Operation { get; init; }

    public string? OldDataJSON { get; init; }

    public string? NewDataJSON { get; init; }
}

public sealed record StateChangeCapture
{
    public required long AttributeID { get; init; }

    public required long SceneID { get; init; }

    public required string OldValue { get; init; }

    public required string NewValue { get; init; }

    public required string Source { get; init; }

    public required string Reason { get; init; }
}
