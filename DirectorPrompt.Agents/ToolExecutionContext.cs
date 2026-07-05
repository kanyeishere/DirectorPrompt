namespace DirectorPrompt.Agents;

public record ToolExecutionContext
(
    long  ProjectID,
    long? SceneID,
    long  TimelinePosition,
    long  RoundID
);
