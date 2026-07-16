using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using DirectorPrompt.ViewModels;

namespace DirectorPrompt.Views.Components;

public partial class MessageRail : UserControl
{
    public static readonly StyledProperty<IEnumerable?> EntriesProperty =
        AvaloniaProperty.Register<MessageRail, IEnumerable?>(nameof(Entries));

    public static readonly StyledProperty<ListBox?> TargetListBoxProperty =
        AvaloniaProperty.Register<MessageRail, ListBox?>(nameof(TargetListBox));

    private ScrollViewer? railScrollViewer;
    private ScrollViewer? targetScrollViewer;

    private ListBox Rail =>
        this.GetLogicalDescendants().OfType<ListBox>().First(control => control.Name == "RailListBox");

    public IEnumerable? Entries
    {
        get => GetValue(EntriesProperty);
        set => SetValue(EntriesProperty, value);
    }

    public ListBox? TargetListBox
    {
        get => GetValue(TargetListBoxProperty);
        set => SetValue(TargetListBoxProperty, value);
    }

    static MessageRail() =>
        TargetListBoxProperty.Changed.AddClassHandler<MessageRail>(static (rail, _) => rail.HookTargetScrollViewer());

    public MessageRail()
    {
        AvaloniaXamlLoader.Load(this);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        railScrollViewer = Rail.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        HookTargetScrollViewer();
    }

    private void HookTargetScrollViewer()
    {
        var scrollViewer = TargetListBox?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

        if (ReferenceEquals(scrollViewer, targetScrollViewer))
            return;

        if (targetScrollViewer is not null)
            targetScrollViewer.ScrollChanged -= OnTargetScrollChanged;

        targetScrollViewer = scrollViewer;

        if (targetScrollViewer is not null)
            targetScrollViewer.ScrollChanged += OnTargetScrollChanged;
    }

    private void OnTargetScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (railScrollViewer is null || targetScrollViewer is null)
            return;

        var targetMaximum = Math.Max(0, targetScrollViewer.Extent.Height - targetScrollViewer.Viewport.Height);
        var railMaximum = Math.Max(0, railScrollViewer.Extent.Height - railScrollViewer.Viewport.Height);

        if (targetMaximum <= 0 || railMaximum <= 0)
            return;

        var ratio = targetScrollViewer.Offset.Y / targetMaximum;
        railScrollViewer.Offset = railScrollViewer.Offset.WithY(ratio * railMaximum);
    }

    private void OnRailPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (targetScrollViewer is null)
            return;

        var maximum = Math.Max(0, targetScrollViewer.Extent.Height - targetScrollViewer.Viewport.Height);
        var offset = Math.Clamp(targetScrollViewer.Offset.Y - e.Delta.Y * 48, 0, maximum);
        targetScrollViewer.Offset = targetScrollViewer.Offset.WithY(offset);
        e.Handled = true;
    }

    private void OnRailItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: DialogEntryViewModel entry } || TargetListBox is null)
            return;

        TargetListBox.ScrollIntoView(entry);
        Dispatcher.UIThread.Post(() => TargetListBox.ScrollIntoView(entry), DispatcherPriority.Background);
        e.Handled = true;
    }

    private void OnRailPointerEntered(object? sender, PointerEventArgs e)
    {
        if (Rail.RenderTransform is Avalonia.Media.ScaleTransform scale)
        {
            scale.ScaleX = 1.6;
            scale.ScaleY = 1.6;
        }

        Rail.SetValue(Panel.ZIndexProperty, 100);
    }

    private void OnRailPointerExited(object? sender, PointerEventArgs e)
    {
        if (Rail.RenderTransform is Avalonia.Media.ScaleTransform scale)
        {
            scale.ScaleX = 1;
            scale.ScaleY = 1;
        }

        Rail.SetValue(Panel.ZIndexProperty, 0);
    }
}
