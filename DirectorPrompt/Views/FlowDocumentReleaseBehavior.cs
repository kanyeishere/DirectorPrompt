using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace DirectorPrompt.Views;

public static class FlowDocumentReleaseBehavior
{
    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.RegisterAttached
        (
            "Document",
            typeof(FlowDocument),
            typeof(FlowDocumentReleaseBehavior),
            new FrameworkPropertyMetadata(null, OnDocumentChanged)
        );

    public static FlowDocument? GetDocument(DependencyObject obj) =>
        (FlowDocument?)obj.GetValue(DocumentProperty);

    public static void SetDocument(DependencyObject obj, FlowDocument? value) =>
        obj.SetValue(DocumentProperty, value);

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FlowDocumentScrollViewer viewer)
            return;

        viewer.Unloaded -= OnUnloaded;
        viewer.Unloaded += OnUnloaded;

        viewer.Document = null;

        if (e.NewValue is not FlowDocument newDoc)
            return;

        ClearExistingParent(newDoc);

        try
        {
            viewer.Document = newDoc;
        }
        catch (InvalidOperationException)
        {
            viewer.Document = CloneDocument(newDoc);
        }
    }

    private static void ClearExistingParent(FlowDocument doc)
    {
        var parent = LogicalTreeHelper.GetParent(doc);

        while (parent is not null)
        {
            if (parent is FlowDocumentScrollViewer parentViewer)
            {
                parentViewer.Document = null;
                return;
            }

            parent = LogicalTreeHelper.GetParent(parent);
        }
    }

    private static FlowDocument CloneDocument(FlowDocument doc)
    {
        using var stream = new MemoryStream();
        XamlWriter.Save(doc, stream);
        stream.Position = 0;
        return (FlowDocument)XamlReader.Load(stream);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FlowDocumentScrollViewer viewer)
            viewer.Document = null;
    }
}
