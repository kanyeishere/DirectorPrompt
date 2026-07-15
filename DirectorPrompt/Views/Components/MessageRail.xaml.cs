using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DirectorPrompt.ViewModels;

namespace DirectorPrompt.Views.Components;

public partial class MessageRail
{
    private ScrollViewer? railScrollViewer;
    private ScrollViewer? targetScrollViewer;

    public static readonly DependencyProperty EntriesProperty =
        DependencyProperty.Register
        (
            nameof(Entries),
            typeof(IEnumerable),
            typeof(MessageRail),
            new PropertyMetadata(null)
        );

    public static readonly DependencyProperty TargetListBoxProperty =
        DependencyProperty.Register
        (
            nameof(TargetListBox),
            typeof(ListBox),
            typeof(MessageRail),
            new PropertyMetadata(null, OnTargetListBoxChanged)
        );

    public IEnumerable? Entries
    {
        get => (IEnumerable?)GetValue(EntriesProperty);
        set => SetValue(EntriesProperty, value);
    }

    public ListBox? TargetListBox
    {
        get => (ListBox?)GetValue(TargetListBoxProperty);
        set => SetValue(TargetListBoxProperty, value);
    }

    public MessageRail()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        railScrollViewer = FindVisualChild<ScrollViewer>(RailListBox);

        if (railScrollViewer is not null)
            railScrollViewer.PreviewMouseWheel += OnRailPreviewMouseWheel;

        HookTargetScrollViewer();
    }

    private static void OnTargetListBoxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MessageRail rail)
            rail.HookTargetScrollViewer();
    }

    private void HookTargetScrollViewer()
    {
        if (TargetListBox is null)
            return;

        TargetListBox.Loaded -= OnTargetListBoxLoaded;
        TargetListBox.Loaded += OnTargetListBoxLoaded;
        OnTargetListBoxLoaded(TargetListBox, new RoutedEventArgs());
    }

    private void OnTargetListBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (TargetListBox is null)
            return;

        var sv = FindVisualChild<ScrollViewer>(TargetListBox);

        if (sv is null)
            return;

        if (targetScrollViewer is not null)
            targetScrollViewer.ScrollChanged -= OnTargetScrollChanged;

        targetScrollViewer               =  sv;
        targetScrollViewer.ScrollChanged += OnTargetScrollChanged;
        SyncRailScroll();
        UpdateCurrentItem();
    }

    private void OnTargetScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        SyncRailScroll();
        UpdateCurrentItem();
    }

    private void SyncRailScroll()
    {
        if (railScrollViewer is null || targetScrollViewer is null)
            return;

        var targetMax = targetScrollViewer.ScrollableHeight;

        if (targetMax <= 0)
        {
            railScrollViewer.ScrollToTop();
            return;
        }

        var railMax = railScrollViewer.ScrollableHeight;

        if (railMax <= 0)
            return;

        var ratio = targetScrollViewer.VerticalOffset / targetMax;
        railScrollViewer.ScrollToVerticalOffset(ratio * railMax);
    }

    private void UpdateCurrentItem()
    {
        if (TargetListBox is null || targetScrollViewer is null)
            return;

        var bestIndex = -1;
        var bestTop   = double.MaxValue;

        for (var i = 0; i < TargetListBox.Items.Count; i++)
        {
            if (TargetListBox.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container)
                continue;

            try
            {
                var transform = container.TransformToVisual(targetScrollViewer);
                var top       = transform.Transform(new Point(0, 0)).Y;

                if (top >= 0 && top < bestTop)
                {
                    bestTop   = top;
                    bestIndex = i;
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        if (bestIndex < 0)
        {
            bestTop = double.MaxValue;

            for (var i = 0; i < TargetListBox.Items.Count; i++)
            {
                if (TargetListBox.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container)
                    continue;

                try
                {
                    var transform = container.TransformToVisual(targetScrollViewer);
                    var top       = transform.Transform(new Point(0, 0)).Y;

                    if (top < 0 && -top < bestTop)
                    {
                        bestTop   = -top;
                        bestIndex = i;
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        if (bestIndex >= 0 && RailListBox.SelectedIndex != bestIndex)
            RailListBox.SelectedIndex = bestIndex;
    }

    private void OnRailPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (targetScrollViewer is null)
            return;

        targetScrollViewer.ScrollToVerticalOffset(targetScrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private void OnRailItemMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: DialogEntryViewModel entry })
            return;

        ForceScrollToEntry(entry);
    }

    private void ForceScrollToEntry(DialogEntryViewModel entry)
    {
        if (TargetListBox is null || targetScrollViewer is null)
            return;

        var index = FindEntryIndex(entry);

        if (index < 0)
            return;

        TargetListBox.ScrollIntoView(entry);

        Dispatcher.BeginInvoke
        (
            DispatcherPriority.Background,
            () =>
            {
                if (TargetListBox.ItemContainerGenerator.ContainerFromIndex(index) is not FrameworkElement container)
                    return;

                try
                {
                    var transform = container.TransformToVisual(targetScrollViewer);
                    var position  = transform.Transform(new Point(0, 0));
                    targetScrollViewer.ScrollToVerticalOffset(targetScrollViewer.VerticalOffset + position.Y - 8);
                }
                catch (InvalidOperationException)
                {
                }
            }
        );
    }

    private int FindEntryIndex(DialogEntryViewModel entry)
    {
        if (TargetListBox?.ItemsSource is not IEnumerable items)
            return -1;

        var i = 0;

        foreach (var item in items)
        {
            if (ReferenceEquals(item, entry))
                return i;

            i++;
        }

        return -1;
    }

    private void OnRailMouseEnter(object sender, MouseEventArgs e)
    {
        Panel.SetZIndex(RailListBox, 100);
        AnimateScale(1.6);
    }

    private void OnRailMouseLeave(object sender, MouseEventArgs e)
    {
        AnimateScale(1);
        Panel.SetZIndex(RailListBox, 0);
    }

    private void AnimateScale(double target)
    {
        var animation = new DoubleAnimation
        {
            To             = target,
            Duration       = TimeSpan.FromMilliseconds(120),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        RailScale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        RailScale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);

        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T result)
                return result;

            var found = FindVisualChild<T>(child);

            if (found is not null)
                return found;
        }

        return null;
    }
}
