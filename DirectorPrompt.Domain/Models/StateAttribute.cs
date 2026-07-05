using DirectorPrompt.Domain.Enums;

namespace DirectorPrompt.Domain.Models;

public record StateAttribute
{
    public long ID { get; init; }

    public long ProjectID { get; init; }

    public string Name { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public StateScope Scope { get; init; }

    public long? CategoryID { get; init; }

    public StateValueType ValueType { get; init; }

    public Driver Driver { get; init; }

    /// <summary>
    ///     JSON 格式的配置, 结构由 ValueType + Driver 组合决定
    /// </summary>
    public string Config { get; init; } = string.Empty;
}
