namespace DirectorPrompt.Agents;

public record DirectiveBatch
(
    long                         ProjectID,
    IReadOnlyList<DirectiveItem> Directives
);
