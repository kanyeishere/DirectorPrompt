using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Headless.XUnit;
using DirectorPrompt.Views.Components;

namespace DirectorPrompt.Tests;

public sealed class MultiSelectComboBoxTests
{
    [AvaloniaFact]
    public void SelectionSynchronizesWithSourceItems()
    {
        var items = new ObservableCollection<TestOption>
        {
            new() { Name = "Alpha" },
            new() { Name = "Beta", IsSelected = true }
        };
        var control = new MultiSelectComboBox
        {
            DisplayMemberPath = nameof(TestOption.Name),
            SelectedMemberPath = nameof(TestOption.IsSelected),
            Watermark = "None",
            ItemsSource = items
        };

        Assert.Equal(2, control.Options.Count);
        Assert.True(control.Options[1].IsSelected);

        control.Options[0].IsSelected = true;
        Assert.True(items[0].IsSelected);

        items.Add(new TestOption { Name = "Gamma" });
        Assert.Equal(3, control.Options.Count);
    }

    private sealed class TestOption : INotifyPropertyChanged
    {
        private bool isSelected;

        public string Name { get; init; } = string.Empty;

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected == value)
                    return;

                isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
