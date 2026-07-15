using DirectorPrompt.Domain.Enums;
using DirectorPrompt.Domain.Models;

namespace DirectorPrompt.Agents;

public record DirectiveItem
(
    DirectiveType Type,
    string        Content,
    int           Order,
    int?          TTL      = null,
    bool          IsSystem = false
)
{
    public static List<DirectiveItem> FromConfigs(IReadOnlyList<DirectiveConfig> directives)
    {
        var result = new List<DirectiveItem>();

        var order = 1;

        foreach (var d in directives)
            result.Add(new DirectiveItem(d.Type, d.Content, order++, d.TTL));

        return result;
    }
}
