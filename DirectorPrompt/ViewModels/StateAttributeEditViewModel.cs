using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using DirectorPrompt.Domain.Enums;

namespace DirectorPrompt.ViewModels;

public sealed partial class StateAttributeEditViewModel : ObservableObject
{
    public long ID { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNumericConfig))]
    [NotifyPropertyChangedFor(nameof(IsEnumConfig))]
    [NotifyPropertyChangedFor(nameof(IsDriverVisible))]
    public partial StateValueType ValueType { get; set; } = StateValueType.Numeric;

    [ObservableProperty]
    public partial Driver Driver { get; set; } = Driver.Narrative;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCategoryScope))]
    public partial StateScope Scope { get; set; } = StateScope.Global;

    [ObservableProperty]
    public partial long? CategoryID { get; set; }

    public bool IsCategoryScope => Scope == StateScope.Category;

    [ObservableProperty]
    public partial string CurrentValue { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial float? MinValue { get; set; }

    [ObservableProperty]
    public partial float? MaxValue { get; set; }

    [ObservableProperty]
    public partial string Unit { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ChangeRules { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Options { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SystemTrigger Trigger { get; set; } = SystemTrigger.SceneChange;

    public ObservableCollection<PhaseEditViewModel> Phases { get; } = [];

    public ObservableCollection<EnumTransitionEditViewModel> Transitions { get; } = [];

    public ObservableCollection<string> AvailableNumericAttributes { get; } = [];

    public bool IsNumericConfig => ValueType == StateValueType.Numeric;

    public bool IsEnumConfig => ValueType == StateValueType.Enum;

    public bool IsDriverVisible => ValueType == StateValueType.Numeric;

    partial void OnOptionsChanged(string value) =>
        SyncTransitions();

    private void SyncTransitions()
    {
        var options = Options.Split
        (
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        var optionSet = new HashSet<string>(options);

        for (var i = Transitions.Count - 1; i >= 0; i--)
            if (!optionSet.Contains(Transitions[i].Option))
                Transitions.RemoveAt(i);

        var existing = Transitions.Select(t => t.Option).ToHashSet();

        foreach (var opt in options)
        {
            if (!existing.Contains(opt))
                Transitions.Add(new EnumTransitionEditViewModel { Option = opt });
        }
    }

    private object BuildPhasesPayload() =>
        Phases.Select
        (p => new
            {
                name              = p.Name,
                expression        = p.Expression,
                knowledgeIds      = p.GetKnowledgeIDs(),
                knowledgeGroupIds = p.GetKnowledgeGroupIDs(),
                enterDirectives = p.EnterDirectiveInput.Directives.Select
                (d => new
                    {
                        type    = d.Type.ToString(),
                        content = d.Content,
                        ttl     = d.TTL
                    }
                ),
                exitDirectives = p.ExitDirectiveInput.Directives.Select
                (d => new
                    {
                        type    = d.Type.ToString(),
                        content = d.Content,
                        ttl     = d.TTL
                    }
                )
            }
        );

    private object BuildTransitionsPayload() =>
        Transitions.Select
        (t => new
            {
                option        = t.Option,
                method        = t.Method.ToString(),
                weight        = t.Weight,
                attributeName = t.AttributeName,
                expression    = t.Expression,
                switchMode    = t.SwitchMode.ToString()
            }
        );

    public string BuildConfig() =>
        (ValueType, Driver) switch
        {
            (StateValueType.Numeric, Driver.Narrative) => JsonSerializer.Serialize
            (
                new
                {
                    min         = MinValue,
                    max         = MaxValue,
                    unit        = Unit,
                    changeRules = ChangeRules,
                    phases      = BuildPhasesPayload()
                }
            ),
            (StateValueType.Enum, _) => JsonSerializer.Serialize
            (
                new
                {
                    options     = Options.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    trigger     = Trigger.ToString(),
                    transitions = BuildTransitionsPayload(),
                    phases      = BuildPhasesPayload()
                }
            ),
            _ => JsonSerializer.Serialize(new { phases = BuildPhasesPayload() })
        };
}
