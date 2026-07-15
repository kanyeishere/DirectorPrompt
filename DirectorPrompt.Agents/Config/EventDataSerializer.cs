using System.Text.Json;
using DirectorPrompt.Domain.Enums;

namespace DirectorPrompt.Agents.Config;

public static class EventDataSerializer
{
    public static IReadOnlyList<DirectiveItem> ParseDirectives(string jsonData)
    {
        var result = new List<DirectiveItem>();

        using var doc = JsonDocument.Parse(jsonData);

        var order = 1;

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var isSystem = element.TryGetProperty("isSystem", out var sysEl) && sysEl.GetBoolean();

            if (isSystem)
                continue;

            var typeStr = element.GetProperty("type").GetString() ?? "Plot";
            var content = element.GetProperty("content").GetString() ?? string.Empty;

            var type = ParseDirectiveType(typeStr);

            result.Add(new DirectiveItem(type, content, order++));
        }

        return result;
    }

    public static List<(DirectiveType Type, string Content)> ParseDirectiveBlocks(string json)
    {
        var result = new List<(DirectiveType Type, string Content)>();

        try
        {
            using var doc = JsonDocument.Parse(json);

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var isSystem = element.TryGetProperty("isSystem", out var sysEl) && sysEl.GetBoolean();

                if (isSystem)
                    continue;

                var typeStr = element.GetProperty("type").GetString() ?? "Plot";
                var content = element.GetProperty("content").GetString() ?? string.Empty;

                var type = ParseDirectiveType(typeStr);

                result.Add((type, content));
            }
        }
        catch
        {
            return [(DirectiveType.Plot, json)];
        }

        return result;
    }

    private static DirectiveType ParseDirectiveType(string typeStr) =>
        typeStr switch
        {
            "Tone"                => DirectiveType.Tone,
            "TemporaryConstraint" => DirectiveType.TemporaryConstraint,
            "SceneChange"         => DirectiveType.SceneChange,
            _                     => DirectiveType.Plot
        };
}
