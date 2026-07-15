using System.Text.Json;
using System.Text.Json.Serialization;
using DirectorPrompt.Domain.Configurations;
using DirectorPrompt.Domain.Enums;

namespace DirectorPrompt.Agents.Config;

// TODO: 看看用 JsonPropertyName Attribute 替代掉这个类
// TODO: 感觉目前配置都太手动了
public static class AttributeConfigSerializer
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static T? Deserialize<T>(string json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string Serialize(StateAttributeConfig config, StateValueType valueType, Driver driver)
    {
        var phasesPayload = config.Phases.Select
        (
            p => new
            {
                name              = p.Name,
                expression        = p.Expression,
                knowledgeIds      = p.KnowledgeIDs,
                knowledgeGroupIds = p.KnowledgeGroupIDs,
                enterDirectives = p.EnterDirectives.Select
                (
                    d => new
                    {
                        type    = d.Type.ToString(),
                        content = d.Content,
                        ttl     = d.TTL
                    }
                ),
                exitDirectives = p.ExitDirectives.Select
                (
                    d => new
                    {
                        type    = d.Type.ToString(),
                        content = d.Content,
                        ttl     = d.TTL
                    }
                )
            }
        );

        return (valueType, driver) switch
        {
            (StateValueType.Numeric, Driver.Narrative) => JsonSerializer.Serialize
            (
                new
                {
                    min         = config.Min,
                    max         = config.Max,
                    unit        = config.Unit,
                    changeRules = config.ChangeRules,
                    phases      = phasesPayload
                }
            ),
            (StateValueType.Enum, _) => JsonSerializer.Serialize
            (
                new
                {
                    options     = config.Options,
                    trigger     = config.Trigger,
                    transitions = config.Transitions?.Select
                    (
                        t => new
                        {
                            option        = t.Option,
                            method        = t.Method.ToString(),
                            weight        = t.Weight,
                            attributeName = t.AttributeName,
                            expression    = t.Expression,
                            switchMode    = t.SwitchMode.ToString()
                        }
                    ),
                    phases = phasesPayload
                }
            ),
            _ => JsonSerializer.Serialize(new { phases = phasesPayload })
        };
    }
}
