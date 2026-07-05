namespace DirectorPrompt.Domain.Models;

public record CharacterScenePresence
{
    public long CharacterID { get; init; }

    public long SceneID { get; init; }
}
