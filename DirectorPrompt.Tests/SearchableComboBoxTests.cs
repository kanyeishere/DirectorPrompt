using Avalonia.Headless.XUnit;
using DirectorPrompt.Views.Components;

namespace DirectorPrompt.Tests;

public sealed class SearchableComboBoxTests
{
    [AvaloniaFact]
    public void TextFiltersItemsAndSelectionUpdatesValue()
    {
        var control = new SearchableComboBox
        {
            DisplayMemberPath = nameof(TestOption.Name),
            SelectedValuePath = nameof(TestOption.ID),
            ItemsSource = new[]
            {
                new TestOption(1, "Alpha"),
                new TestOption(2, "Beta")
            }
        };

        control.Text = "bet";

        Assert.Single(control.FilteredItems);
        Assert.Equal("Beta", ((TestOption)control.FilteredItems[0]).Name);
    }

    private sealed record TestOption(int ID, string Name);
}
