using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;

namespace DirectorPrompt.Views.Components;

public sealed partial class MultiSelectComboBox : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MultiSelectComboBox, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<string> DisplayMemberPathProperty =
        AvaloniaProperty.Register<MultiSelectComboBox, string>(nameof(DisplayMemberPath), string.Empty);

    public static readonly StyledProperty<string> SelectedMemberPathProperty =
        AvaloniaProperty.Register<MultiSelectComboBox, string>(nameof(SelectedMemberPath), string.Empty);

    public static readonly StyledProperty<string> DelimiterProperty =
        AvaloniaProperty.Register<MultiSelectComboBox, string>(nameof(Delimiter), ", ");

    public static readonly StyledProperty<object?> WatermarkProperty =
        AvaloniaProperty.Register<MultiSelectComboBox, object?>(nameof(Watermark));

    private INotifyCollectionChanged? observedCollection;

    private TextBlock SummaryControl =>
        this.GetLogicalDescendants().OfType<TextBlock>().First(control => control.Name == "SummaryTextBlock");

    public ObservableCollection<Option> Options { get; } = [];

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string DisplayMemberPath
    {
        get => GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public string SelectedMemberPath
    {
        get => GetValue(SelectedMemberPathProperty);
        set => SetValue(SelectedMemberPathProperty, value);
    }

    public string Delimiter
    {
        get => GetValue(DelimiterProperty);
        set => SetValue(DelimiterProperty, value);
    }

    public object? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    static MultiSelectComboBox()
    {
        ItemsSourceProperty.Changed.AddClassHandler<MultiSelectComboBox>(static (control,        _) => control.ObserveItems());
        DisplayMemberPathProperty.Changed.AddClassHandler<MultiSelectComboBox>(static (control,  _) => control.RefreshOptions());
        SelectedMemberPathProperty.Changed.AddClassHandler<MultiSelectComboBox>(static (control, _) => control.RefreshOptions());
        DelimiterProperty.Changed.AddClassHandler<MultiSelectComboBox>(static (control,          _) => control.UpdateSummary());
        WatermarkProperty.Changed.AddClassHandler<MultiSelectComboBox>(static (control,          _) => control.UpdateSummary());
    }

    public MultiSelectComboBox() =>
        AvaloniaXamlLoader.Load(this);

    private void ObserveItems()
    {
        if (observedCollection is not null)
            observedCollection.CollectionChanged -= OnCollectionChanged;

        observedCollection = ItemsSource as INotifyCollectionChanged;

        if (observedCollection is not null)
            observedCollection.CollectionChanged += OnCollectionChanged;

        RefreshOptions();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        RefreshOptions();

    private void RefreshOptions()
    {
        foreach (var option in Options)
            option.PropertyChanged -= OnOptionPropertyChanged;

        Options.Clear();

        if (ItemsSource is not null)
        {
            foreach (var item in ItemsSource)
            {
                if (item is null)
                    continue;

                var option = new Option(item, DisplayMemberPath, SelectedMemberPath);
                option.PropertyChanged += OnOptionPropertyChanged;
                Options.Add(option);
            }
        }

        UpdateSummary();
    }

    private void OnOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Option.IsSelected))
            UpdateSummary();
    }

    private void UpdateSummary()
    {
        var selected = Options.Where(option => option.IsSelected)
                              .Select(option => option.Display)
                              .ToArray();

        SummaryControl.Text = selected.Length == 0 ?
                                  Watermark?.ToString() ?? string.Empty :
                                  string.Join(Delimiter, selected);
        SummaryControl.Opacity = selected.Length == 0 ?
                                     0.65 :
                                     1;
    }

    public sealed class Option : INotifyPropertyChanged
    {
        private readonly object              item;
        private readonly PropertyDescriptor? selectedProperty;
        private          bool                isSelected;

        public string Display { get; }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected == value)
                    return;

                isSelected = value;
                selectedProperty?.SetValue(item, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Option(object item, string displayMemberPath, string selectedMemberPath)
        {
            this.item = item;
            var properties = TypeDescriptor.GetProperties(item);
            Display = string.IsNullOrEmpty(displayMemberPath) ?
                          item.ToString()                                           ?? string.Empty :
                          properties[displayMemberPath]?.GetValue(item)?.ToString() ?? string.Empty;
            selectedProperty = string.IsNullOrEmpty(selectedMemberPath) ?
                                   null :
                                   properties[selectedMemberPath];
            isSelected = selectedProperty?.GetValue(item) is true;

            if (item is INotifyPropertyChanged observable)
                observable.PropertyChanged += OnItemPropertyChanged;
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (selectedProperty is null || e.PropertyName != selectedProperty.Name)
                return;

            var value = selectedProperty.GetValue(item) is true;

            if (isSelected == value)
                return;

            isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }
}
