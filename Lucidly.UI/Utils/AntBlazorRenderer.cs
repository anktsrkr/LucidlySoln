using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using AntDesign;
using System.Text;
using System.Linq.Expressions;
using Markdown.ColorCode;

public class AntBlazorTypographyRenderer
{
    private readonly MarkdownPipeline _pipeline;

    public AntBlazorTypographyRenderer()
    {
        // Build the pipeline with necessary extensions for parsing inlines, emphasis, links, etc.
        _pipeline = new MarkdownPipelineBuilder()
            .UseEmphasisExtras() // For strong/emphasis
            .UsePipeTables() // Often needed for basic parsing
            .UseAutoLinks() // For automatic link detection
            .UseAdvancedExtensions() // Includes many useful extensions
            .UseEmojiAndSmiley() // For emoji support
            .UseColorCode() // Optional: if you want code syntax highlighting
            .Build();
    }

    /// <summary>
    /// Renders the provided Markdown string into Ant Design Blazor Typography components.
    /// </summary>
    /// <param name="markdown">The Markdown string to render.</param>
    /// <returns>A RenderFragment representing the rendered content.</returns>
    public RenderFragment Render(string markdown) => builder =>
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return; // Render nothing for empty/whitespace content

        // Parse the Markdown into an Abstract Syntax Tree (AST)
        var document = Markdig.Markdown.Parse(markdown, _pipeline);
        int sequence = 0;

        // Iterate through the top-level blocks in the document
        foreach (var block in document)
        {
            RenderBlock(builder, block, ref sequence);
        }
    };

    /// <summary>
    /// Renders a Markdown block element using appropriate Ant Design components.
    /// </summary>
    private void RenderBlock(RenderTreeBuilder builder, MarkdownObject block, ref int sequence)
    {
        switch (block)
        {
            case HeadingBlock heading:
                RenderHeading(builder, heading, ref sequence);
                break;

            case ParagraphBlock paragraph:
                RenderParagraph(builder, paragraph, ref sequence);
                break;

            case CodeBlock codeBlock:
                RenderCodeBlock(builder, codeBlock, ref sequence);
                break;

            case QuoteBlock quote:
                RenderQuoteBlock(builder, quote, ref sequence);
                break;

            case ListBlock list:
                RenderList(builder, list, ref sequence);
                break;

            case Table table:
                RenderTable(builder, table, ref sequence);
                break;

            case ThematicBreakBlock:
                RenderDivider(builder, ref sequence);
                break;

            case HtmlBlock htmlBlock:
                RenderHtmlBlock(builder, htmlBlock, ref sequence);
                break;
            case LinkReferenceDefinitionGroup:
                // Link reference definitions are not rendered - they're used by reference links
                // Example: [1]: https://example.com "Title"
                // These are invisible in the final output
                break;
            default:
                // Fallback for unsupported block types
                RenderUnsupportedBlock(builder, block, ref sequence);
                break;
        }
    }

    private void RenderHeading(RenderTreeBuilder builder, HeadingBlock heading, ref int sequence)
    {
        var level = Math.Max(1, Math.Min(5, heading.Level)); // Title supports levels 1-5

        builder.OpenComponent<Title>(sequence++);
        builder.AddAttribute(sequence++, "Level", level);
        builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(childBuilder =>
        {
            if (heading.Inline != null)
            {
                int childSequence = 0;
                RenderInlines(childBuilder, heading.Inline, ref childSequence);
            }
        }));
        builder.CloseComponent();
    }

    private void RenderParagraph(RenderTreeBuilder builder, ParagraphBlock paragraph, ref int sequence)
    {
        builder.OpenComponent<Paragraph>(sequence++);
        builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(childBuilder =>
        {
            if (paragraph.Inline != null)
            {
                int childSequence = 0;
                RenderInlines(childBuilder, paragraph.Inline, ref childSequence);
            }
        }));
        builder.CloseComponent();
    }

    private void RenderCodeBlock(RenderTreeBuilder builder, CodeBlock codeBlock, ref int sequence)
    {
        // Use Paragraph with Code=true for code blocks
        builder.OpenComponent<Paragraph>(sequence++);
        builder.AddAttribute(sequence++, "Code", true);
        builder.AddAttribute(sequence++, "Style", "background: #f5f5f5; padding: 16px; border-radius: 6px; overflow-x: auto; border: 1px solid #d9d9d9; white-space: pre;");
        builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(childBuilder =>
        {
            childBuilder.AddContent(0, codeBlock.Lines.ToString());
        }));
        builder.CloseComponent();
    }

    private void RenderQuoteBlock(RenderTreeBuilder builder, QuoteBlock quote, ref int sequence)
    {
        builder.OpenComponent<Alert>(sequence++);
        builder.AddAttribute(sequence++, "Type", AlertType.Info); // Default to info type for blockquotes
        builder.AddAttribute(sequence++, "ShowIcon", true);
        builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(childBuilder =>
        {
            int childSequence = 0;
            foreach (var child in quote)
            {
                childSequence++;
                RenderBlock(childBuilder, child,ref childSequence);
            }
        }));
        builder.CloseComponent();
    }

    private void RenderList(RenderTreeBuilder builder, ListBlock list, ref int sequence)
    {
        var tagName = list.IsOrdered ? "ol" : "ul";
        builder.OpenElement(sequence++, tagName);
        builder.AddAttribute(sequence++, "style", "padding-left: 20px; margin: 16px 0;");

        foreach (var item in list)
        {
            if (item is ListItemBlock listItem)
            {
                builder.OpenElement(sequence++, "li");
                builder.AddAttribute(sequence++, "style", "margin: 4px 0;");

                foreach (var itemChild in listItem)
                {
                    RenderBlock(builder, itemChild, ref sequence);
                }

                builder.CloseElement();
            }
        }

        builder.CloseElement();
    }

    private void RenderTable(RenderTreeBuilder builder, Table table, ref int sequence)
    {
        // Extract headers and data from markdown table
        var headers = new List<string>();
        var rows = new List<Dictionary<string, object>>();

        try
        {
            if (table != null && table.Any())
            {
                Markdig.Extensions.Tables.TableRow? headerRow = table.FirstOrDefault() as Markdig.Extensions.Tables.TableRow;
                if (headerRow != null)
                {
                    // Extract headers
                    for (int i = 0; i < headerRow.Count; i++)
                    {
                        var cell = headerRow[i] as Markdig.Extensions.Tables.TableCell;
                        var headerText = GetCellText(cell) ?? $"Column{i + 1}";
                        headers.Add(headerText);
                    }

                    // Extract data rows
                    foreach (var row in table.Skip(1))
                    {
                        if (row is Markdig.Extensions.Tables.TableRow dataRow)
                        {
                            var rowData = new Dictionary<string, object>();
                            for (int i = 0; i < dataRow.Count && i < headers.Count; i++)
                            {
                                var cell = dataRow[i] as Markdig.Extensions.Tables.TableCell;
                                var cellText = GetCellText(cell) ?? "";
                                rowData[headers[i]] = cellText;
                            }
                            // Fill missing columns with empty strings
                            foreach (var header in headers)
                            {
                                if (!rowData.ContainsKey(header))
                                {
                                    rowData[header] = "";
                                }
                            }
                            rows.Add(rowData);
                        }
                    }
                }
            }

            // Handle streaming scenarios - if we have headers but no rows, show table with headers only
            if (!headers.Any())
            {
                // No headers means completely empty or malformed table - render basic HTML table
                if (table != null && table.Any())
                {
                    // Try to render what we have as basic HTML table
                    builder.OpenElement(sequence++, "table");
                    builder.AddAttribute(sequence++, "style", "border-collapse: collapse; width: 100%; margin: 16px 0; border: 1px solid #d9d9d9;");

                    foreach (var row in table)
                    {
                        if (row is Markdig.Extensions.Tables.TableRow tableRow)
                        {
                            builder.OpenElement(sequence++, "tr");
                            for (int i = 0; i < tableRow.Count; i++)
                            {
                                var cell = tableRow[i] as Markdig.Extensions.Tables.TableCell;
                                builder.OpenElement(sequence++, "td");
                                builder.AddAttribute(sequence++, "style", "border: 1px solid #d9d9d9; padding: 8px;");
                                builder.AddContent(sequence++, GetCellText(cell) ?? "");
                                builder.CloseElement();
                            }
                            builder.CloseElement(); // tr
                        }
                    }

                    builder.CloseElement(); // table
                }
                else
                {
                    builder.OpenComponent<Paragraph>(sequence++);
                    builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(childBuilder =>
                    {
                        childBuilder.AddContent(0, "[Empty table]");
                    }));
                    builder.CloseComponent();
                }
                return;
            }

            // If we have headers but no rows (streaming scenario), show table with empty data
            if (!rows.Any())
            {
                // Create empty row to show table structure
                var emptyRow = new Dictionary<string, object>();
                foreach (var header in headers)
                {
                    emptyRow[header] = ""; // Empty cells
                }
                rows.Add(emptyRow);
            }

            // Render Ant Design Table
            builder.OpenComponent<AntDesign.Table<Dictionary<string, object>>>(sequence++);
            builder.AddAttribute(sequence++, "DataSource", rows);
            builder.AddAttribute(sequence++, "Size", TableSize.Small);
            builder.AddAttribute(sequence++, "Bordered", true);
            builder.AddAttribute(sequence++, "HidePagination", true);

            // Add ChildContent with correct RenderFragment<TItem> signature
            builder.AddAttribute(sequence++, "ChildContent", (RenderFragment<Dictionary<string, object>>)(context => childBuilder =>
            {
                int columnSequence = 0;

                foreach (var header in headers)
                {
                    var headerCopy = header; // Capture for closure
                    childBuilder.OpenComponent<PropertyColumn<Dictionary<string, object>, object>>(columnSequence++);
                    childBuilder.AddAttribute(columnSequence++, "Property", (Expression<Func<Dictionary<string, object>, object>>)(c => c[headerCopy]));
                    childBuilder.AddAttribute(columnSequence++, "Title", headerCopy);
                    childBuilder.CloseComponent();
                }
            }));

            builder.CloseComponent();
        }
        catch (Exception ex)
        {
            // Fallback to HTML table if anything goes wrong
            try
            {
                if (table != null && table.Any())
                {
                    // Render as basic HTML table
                    builder.OpenElement(sequence++, "table");
                    builder.AddAttribute(sequence++, "style", "border-collapse: collapse; width: 100%; margin: 16px 0; border: 1px solid #d9d9d9;");

                    foreach (var row in table)
                    {
                        if (row is Markdig.Extensions.Tables.TableRow tableRow)
                        {
                            builder.OpenElement(sequence++, "tr");
                            for (int i = 0; i < tableRow.Count; i++)
                            {
                                var cell = tableRow[i] as Markdig.Extensions.Tables.TableCell;
                                builder.OpenElement(sequence++, "td");
                                builder.AddAttribute(sequence++, "style", "border: 1px solid #d9d9d9; padding: 8px;");
                                builder.AddContent(sequence++, GetCellText(cell) ?? "");
                                builder.CloseElement();
                            }
                            builder.CloseElement(); // tr
                        }
                    }

                    builder.CloseElement(); // table
                }
                else
                {
                    builder.OpenComponent<Paragraph>(sequence++);
                    builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(childBuilder =>
                    {
                        childBuilder.AddContent(0, $"[Error rendering table: {ex.Message}]");
                    }));
                    builder.CloseComponent();
                }
            }
            catch
            {
                // Final fallback if even the HTML conversion fails
                builder.OpenComponent<Paragraph>(sequence++);
                builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    childBuilder.AddContent(0, "[Table rendering failed]");
                }));
                builder.CloseComponent();
            }
        }
    }

    private void RenderDivider(RenderTreeBuilder builder, ref int sequence)
    {
        builder.OpenComponent<Divider>(sequence++);
        builder.CloseComponent();
    }

    private void RenderHtmlBlock(RenderTreeBuilder builder, HtmlBlock htmlBlock, ref int sequence)
    {
        // Render raw HTML - be careful with security implications
        var html = htmlBlock.Lines.ToString();
        builder.AddMarkupContent(sequence++, html);
    }

    private void RenderUnsupportedBlock(RenderTreeBuilder builder, MarkdownObject block, ref int sequence)
    {
        // For unsupported blocks, render as plain text or basic HTML
        if (block is LeafBlock leafBlock)
        {
            // For leaf blocks, try to get the text content
            var content = leafBlock.Lines.ToString();
            if (!string.IsNullOrEmpty(content))
            {
                builder.OpenComponent<Paragraph>(sequence++);
                builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    childBuilder.AddContent(0, content);
                }));
                builder.CloseComponent();
            }
        }
        else
        {
            // For other unsupported blocks, render a placeholder
            builder.OpenComponent<Paragraph>(sequence++);
            builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.AddContent(0, $"[Unsupported block type: {block.GetType().Name}]");
            }));
            builder.CloseComponent();
        }
    }

    /// <summary>
    /// Renders inline elements like emphasis, links, code spans, etc.
    /// </summary>
    private void RenderInlines(RenderTreeBuilder builder, ContainerInline inline, ref int sequence)
    {
        if (inline == null) return;

        foreach (var child in inline)
        {
            RenderInline(builder, child, ref sequence);
        }
    }

    private void RenderInline(RenderTreeBuilder builder, Markdig.Syntax.Inlines.Inline inline, ref int sequence)
    {
        switch (inline)
        {
            case LiteralInline literal:
                builder.AddContent(sequence++, literal.Content.ToString());
                break;

            case EmphasisInline emphasis:
                if (emphasis.DelimiterCount == 2) // Bold (**text**)
                {
                    builder.OpenComponent<Text>(sequence++);
                    builder.AddAttribute(sequence++, "Strong", true);
                    builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(childBuilder =>
                    {
                        int childSequence = 0;
                        RenderInlines(childBuilder, emphasis, ref childSequence);
                    }));
                    builder.CloseComponent();
                }
                else // Italic (*text*)
                {
                    builder.OpenElement(sequence++, "em");
                    int childSequence = 0;
                    RenderInlines(builder, emphasis, ref childSequence);
                    builder.CloseElement();
                }
                break;

            case CodeInline code:
                builder.OpenComponent<Text>(sequence++);
                builder.AddAttribute(sequence++, "Code", true);
                builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    childBuilder.AddContent(0, code.Content);
                }));
                builder.CloseComponent();
                break;

            case LinkInline link:
                builder.OpenElement(sequence++, "a");
                builder.AddAttribute(sequence++, "href", link.Url);
                builder.AddAttribute(sequence++, "target", "_blank");
                builder.AddAttribute(sequence++, "rel", "noopener noreferrer");
                builder.AddAttribute(sequence++, "style", "color: #1890ff; text-decoration: none;");
                int linkChildSequence = 0;
                RenderInlines(builder, link, ref linkChildSequence);
                builder.CloseElement();
                break;

            case LineBreakInline:
                builder.OpenElement(sequence++, "br");
                builder.CloseElement();
                break;

            case AutolinkInline autolink:
                builder.OpenElement(sequence++, "a");
                builder.AddAttribute(sequence++, "href", autolink.Url);
                builder.AddAttribute(sequence++, "target", "_blank");
                builder.AddAttribute(sequence++, "rel", "noopener noreferrer");
                builder.AddAttribute(sequence++, "style", "color: #1890ff; text-decoration: none;");
                builder.AddContent(sequence++, autolink.Url);
                builder.CloseElement();
                break;

            case HtmlInline htmlInline:
                builder.AddMarkupContent(sequence++, htmlInline.Tag);
                break;

            default:
                // Handle other inline types or fallback
                var text = GetInlineText(inline);
                if (!string.IsNullOrEmpty(text))
                {
                    builder.AddContent(sequence++, text);
                }
                break;
        }
    }

    /// <summary>
    /// Extracts plain text from inline elements.
    /// </summary>
    private string GetInlineText(Markdig.Syntax.Inlines.Inline inline)
    {
        return inline switch
        {
            LiteralInline literal => literal.Content.ToString(),
            ContainerInline container => GetContainerInlineText(container),
            CodeInline code => code.Content,
            AutolinkInline autolink => autolink.Url,
            _ => string.Empty
        };
    }

    private string GetContainerInlineText(ContainerInline inline)
    {
        if (inline == null) return string.Empty;

        var text = new StringBuilder();
        foreach (var child in inline)
        {
            text.Append(GetInlineText(child));
        }
        return text.ToString();
    }

    /// <summary>
    /// Extracts text content from table cells.
    /// </summary>
    private string GetCellText(Markdig.Extensions.Tables.TableCell cell)
    {
        if (cell == null) return string.Empty;

        var text = new StringBuilder();
        foreach (var block in cell)
        {
            if (block is ParagraphBlock paragraph && paragraph.Inline != null)
            {
                text.Append(GetContainerInlineText(paragraph.Inline));
            }
            else if (block is LeafBlock leaf)
            {
                text.Append(leaf.Lines.ToString());
            }
        }
        return text.ToString();
    }
}