using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Block = Markdig.Syntax.Block;
using Inline = Markdig.Syntax.Inlines.Inline;
using MarkdownTable = Markdig.Extensions.Tables.Table;
using MarkdownTableRow = Markdig.Extensions.Tables.TableRow;
using MarkdownTableCell = Markdig.Extensions.Tables.TableCell;

namespace DirectorPrompt.Markdown;

internal sealed class FlowDocumentRenderer
{
    private FlowDocument? document;

    internal void Render(FlowDocument doc, MarkdownDocument parsed)
    {
        document = doc;

        foreach (var block in parsed)
            RenderBlock(block);
    }

    private void RenderBlock(Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                RenderHeading(heading);
                break;
            case ParagraphBlock paragraph:
                RenderParagraph(paragraph);
                break;
            case QuoteBlock quote:
                RenderQuote(quote);
                break;
            case ListBlock list:
                RenderList(list);
                break;
            case ThematicBreakBlock:
                document!.Blocks.Add(new Paragraph(new Run("────────────────") { Foreground = Brushes.Gray }));
                break;
            case MarkdownTable table:
                RenderTable(table);
                break;
            default:
                if (block is ParagraphBlock p)
                    RenderParagraph(p);

                break;
        }
    }

    private void RenderHeading(HeadingBlock heading)
    {
        var fontSize = heading.Level switch
        {
            1 => 22.0,
            2 => 18.0,
            3 => 16.0,
            _ => 14.0
        };

        var paragraph = new Paragraph
        {
            FontSize   = fontSize,
            FontWeight = FontWeights.Bold,
            Margin     = new Thickness(0, 8, 0, 4)
        };

        if (heading.Inline is not null)
            RenderInlines(paragraph, heading.Inline);

        document!.Blocks.Add(paragraph);
    }

    private void RenderParagraph(ParagraphBlock paragraph)
    {
        var p = new Paragraph
        {
            Margin     = new Thickness(0, 4, 0, 4),
            LineHeight = 22
        };

        if (paragraph.Inline is not null)
            RenderInlines(p, paragraph.Inline);

        document!.Blocks.Add(p);
    }

    private void RenderQuote(QuoteBlock quote)
    {
        var borderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 120));

        foreach (var subBlock in quote)
        {
            if (subBlock is ParagraphBlock p)
            {
                var paragraph = new Paragraph
                {
                    Margin          = new Thickness(16, 2, 0, 2),
                    BorderBrush     = borderBrush,
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    Padding         = new Thickness(8, 0, 0, 0),
                    Foreground      = new SolidColorBrush(Color.FromRgb(180, 180, 190))
                };

                if (p.Inline is not null)
                    RenderInlines(paragraph, p.Inline);

                document!.Blocks.Add(paragraph);
            }
        }
    }

    private void RenderList(ListBlock list)
    {
        var index = 0;

        foreach (var item in list)
        {
            if (item is not ListItemBlock listItem)
                continue;

            index++;

            var prefix = list.IsOrdered ?
                             $"{index}. " :
                             "• ";

            var paragraph = new Paragraph
            {
                Margin = new Thickness(16, 2, 0, 2)
            };

            paragraph.Inlines.Add(new Run(prefix) { Foreground = Brushes.Gray });

            foreach (var subBlock in listItem)
            {
                if (subBlock is ParagraphBlock { Inline: not null } p)
                    RenderInlines(paragraph, p.Inline);
            }

            document!.Blocks.Add(paragraph);
        }
    }

    private void RenderTable(MarkdownTable table)
    {
        var borderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
        var headerBg    = new SolidColorBrush(Color.FromRgb(45, 45, 48));

        var flowTable = new Table
        {
            BorderBrush     = borderBrush,
            BorderThickness = new Thickness(1),
            CellSpacing     = 0,
            Margin          = new Thickness(0, 4, 0, 4)
        };

        var columnCount = 0;

        foreach (var item in table)
        {
            if (item is not MarkdownTableRow row)
                continue;

            var isHeaderRow = row.IsHeader;
            var flowRow     = new TableRow();

            if (isHeaderRow)
                flowRow.Background = headerBg;

            var cellCount = 0;

            foreach (var cell in row)
            {
                if (cell is not MarkdownTableCell tableCell)
                    continue;

                cellCount++;

                var cellContent = new Paragraph { Margin = new Thickness(2) };

                foreach (var subBlock in tableCell)
                {
                    if (subBlock is ParagraphBlock { Inline: not null } p)
                        RenderInlines(cellContent, p.Inline);
                }

                var flowCell = new TableCell(cellContent)
                {
                    BorderBrush     = borderBrush,
                    BorderThickness = new Thickness(1),
                    Padding         = new Thickness(4, 2, 4, 2)
                };

                if (isHeaderRow)
                    flowCell.FontWeight = FontWeights.Bold;

                flowRow.Cells.Add(flowCell);
            }

            if (cellCount > columnCount)
                columnCount = cellCount;

            flowTable.RowGroups.Add(new TableRowGroup { Rows = { flowRow } });
        }

        for (var i = 0; i < columnCount; i++)
            flowTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Auto) });

        document!.Blocks.Add(flowTable);
    }

    private void RenderInlines(Paragraph paragraph, ContainerInline container)
    {
        foreach (var inline in container)
            RenderInline(paragraph, inline);
    }

    private void RenderInline(Paragraph paragraph, Inline inline)
    {
        switch (inline)
        {
            case LiteralInline literal:
                paragraph.Inlines.Add(new Run(literal.ToString()));
                break;
            case EmphasisInline emphasis:
                var run = new Run();

                if (emphasis.DelimiterCount == 2)
                    run.FontWeight = FontWeights.Bold;
                else
                    run.FontStyle = FontStyles.Italic;

                foreach (var child in emphasis)
                {
                    if (child is LiteralInline li)
                        run.Text += li.ToString();
                }

                paragraph.Inlines.Add(run);
                break;
            case CodeInline code:
                var codeRun = new Run(code.Content)
                {
                    FontFamily = new FontFamily("Consolas"),
                    Background = new SolidColorBrush(Color.FromRgb(45,  45,  48)),
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 180, 100))
                };

                paragraph.Inlines.Add(codeRun);
                break;
            case LinkInline link:
                var linkRun = new Run(link.Url ?? string.Empty)
                {
                    Foreground      = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                    TextDecorations = TextDecorations.Underline
                };

                paragraph.Inlines.Add(linkRun);
                break;
            case LineBreakInline:
                paragraph.Inlines.Add(new LineBreak());
                break;
            case ContainerInline container:
                RenderInlines(paragraph, container);
                break;
        }
    }
}
