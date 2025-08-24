# Comment Effects

## Overview
The Comment effect is a non-visual effect that allows users to add descriptive text and notes to AVS presets without affecting the visual output. It's essential for documentation, organization, and collaboration in AVS preset development.

## C++ Source Analysis
**File:** `r_comment.cpp`  
**Class:** `C_THISCLASS : public C_RBASE`

### Key Properties
- **Comment Text**: The actual comment content
- **Comment Type**: Different comment styles and behaviors
- **Visibility**: Whether comments are visible in the editor
- **Metadata**: Additional comment information
- **Grouping**: Comment organization and categorization

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE
{
    char comment[256];
    int type;
    int visible;
    int group;
    int metadata;
    
    virtual int render(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
    virtual HWND conf(HINSTANCE hInstance, HWND hwndParent);
    virtual char *get_desc();
};
```

## C# Implementation

### CommentEffectsNode Class
```csharp
public class CommentEffectsNode : BaseEffectNode
{
    public string CommentText { get; set; } = "";
    public int CommentType { get; set; } = 0;
    public bool IsVisible { get; set; } = true;
    public int GroupId { get; set; } = 0;
    public string GroupName { get; set; } = "";
    public DateTime CreationDate { get; set; } = DateTime.Now;
    public string Author { get; set; } = "";
    public int Priority { get; set; } = 0;
    public bool IsCollapsible { get; set; } = true;
    public bool IsCollapsed { get; set; } = false;
    public string[] Tags { get; set; } = new string[0];
}
```

### Key Features
1. **Multiple Comment Types**: Different comment styles and behaviors
2. **Text Support**: Rich text and formatting options
3. **Grouping System**: Comment organization and categorization
4. **Metadata Support**: Creation date, author, priority, tags
5. **Collapsible Comments**: Expandable/collapsible comment blocks
6. **Search and Filter**: Comment search and filtering capabilities
7. **Export Support**: Comment export for documentation

### Comment Types
- **0**: Standard Comment (Basic text comment)
- **1**: Section Header (Major section divider)
- **2**: Subsection Header (Minor section divider)
- **3**: Code Comment (Code-specific documentation)
- **4**: Warning Comment (Important warnings/notes)
- **5**: TODO Comment (Action items and reminders)
- **6**: Bug Report (Bug descriptions and reports)
- **7**: Feature Request (Feature suggestions and ideas)

### Comment Priorities
- **0**: Low (Informational)
- **1**: Normal (Standard)
- **2**: High (Important)
- **3**: Critical (Urgent)
- **4**: Blocking (Must address)

## Usage Examples

### Basic Comment
```csharp
var commentNode = new CommentEffectsNode
{
    CommentText = "This effect creates a rotating starfield with beat-reactive colors",
    CommentType = 0, // Standard comment
    IsVisible = true,
    GroupId = 1,
    GroupName = "Starfield Effects",
    Author = "Justin",
    Priority = 1
};
```

### Section Header
```csharp
var commentNode = new CommentEffectsNode
{
    CommentText = "=== AUDIO REACTIVE EFFECTS ===",
    CommentType = 1, // Section header
    IsVisible = true,
    GroupId = 2,
    GroupName = "Audio Effects",
    Author = "Justin",
    Priority = 2,
    IsCollapsible = false
};
```

### Code Documentation
```csharp
var commentNode = new CommentEffectsNode
{
    CommentText = @"// TODO: Optimize this algorithm for better performance
// Current implementation uses O(n²) complexity
// Target: Reduce to O(n log n) using spatial partitioning",
    CommentType = 3, // Code comment
    IsVisible = true,
    GroupId = 3,
    GroupName = "Optimization",
    Author = "Justin",
    Priority = 3,
    Tags = new string[] { "performance", "optimization", "algorithm" }
};
```

### Warning Comment
```csharp
var commentNode = new CommentEffectsNode
{
    CommentText = "⚠️ WARNING: This effect may cause performance issues on older hardware. Consider reducing particle count for better frame rates.",
    CommentType = 4, // Warning comment
    IsVisible = true,
    GroupId = 4,
    GroupName = "Performance Warnings",
    Author = "Justin",
    Priority = 4,
    Tags = new string[] { "warning", "performance", "hardware" }
};
```

## Technical Implementation

### Core Comment Processing
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    // Comments don't modify the visual output, so just pass through the input
    if (inputs.TryGetValue("Image", out var input))
    {
        return input;
    }
    
    return GetDefaultOutput();
}
```

### Comment Rendering (Editor Only)
```csharp
public override void RenderComment(Graphics graphics, Rectangle bounds)
{
    if (!IsVisible || string.IsNullOrEmpty(CommentText))
        return;

    // Apply comment styling based on type
    var style = GetCommentStyle();
    
    // Render comment background
    using (var backgroundBrush = new SolidBrush(style.BackgroundColor))
    {
        graphics.FillRectangle(backgroundBrush, bounds);
    }
    
    // Render comment border
    using (var borderPen = new Pen(style.BorderColor, style.BorderWidth))
    {
        graphics.DrawRectangle(borderPen, bounds);
    }
    
    // Render comment text
    using (var textBrush = new SolidBrush(style.TextColor))
    using (var font = new Font(style.FontFamily, style.FontSize, style.FontStyle))
    {
        var textBounds = new Rectangle(
            bounds.X + style.TextPadding,
            bounds.Y + style.TextPadding,
            bounds.Width - (style.TextPadding * 2),
            bounds.Height - (style.TextPadding * 2)
        );
        
        graphics.DrawString(CommentText, font, textBrush, textBounds, style.TextFormat);
    }
}
```

### Comment Style Management
```csharp
private CommentStyle GetCommentStyle()
{
    return CommentType switch
    {
        0 => new CommentStyle // Standard
        {
            BackgroundColor = Color.FromArgb(240, 240, 240),
            BorderColor = Color.Gray,
            BorderWidth = 1,
            TextColor = Color.Black,
            FontFamily = "Segoe UI",
            FontSize = 9,
            FontStyle = FontStyle.Regular,
            TextPadding = 4,
            TextFormat = StringFormat.GenericDefault
        },
        1 => new CommentStyle // Section Header
        {
            BackgroundColor = Color.FromArgb(200, 220, 255),
            BorderColor = Color.Blue,
            BorderWidth = 2,
            TextColor = Color.DarkBlue,
            FontFamily = "Segoe UI",
            FontSize = 12,
            FontStyle = FontStyle.Bold,
            TextPadding = 6,
            TextFormat = new StringFormat { Alignment = StringAlignment.Center }
        },
        2 => new CommentStyle // Subsection Header
        {
            BackgroundColor = Color.FromArgb(220, 240, 255),
            BorderColor = Color.LightBlue,
            BorderWidth = 1,
            TextColor = Color.DarkBlue,
            FontFamily = "Segoe UI",
            FontSize = 10,
            FontStyle = FontStyle.Bold,
            TextPadding = 5,
            TextFormat = new StringFormat { Alignment = StringAlignment.Center }
        },
        3 => new CommentStyle // Code Comment
        {
            BackgroundColor = Color.FromArgb(255, 255, 200),
            BorderColor = Color.Orange,
            BorderWidth = 1,
            TextColor = Color.DarkGreen,
            FontFamily = "Consolas",
            FontSize = 8,
            FontStyle = FontStyle.Regular,
            TextPadding = 4,
            TextFormat = StringFormat.GenericDefault
        },
        4 => new CommentStyle // Warning
        {
            BackgroundColor = Color.FromArgb(255, 240, 200),
            BorderColor = Color.Orange,
            BorderWidth = 2,
            TextColor = Color.DarkOrange,
            FontFamily = "Segoe UI",
            FontSize = 9,
            FontStyle = FontStyle.Bold,
            TextPadding = 5,
            TextFormat = StringFormat.GenericDefault
        },
        5 => new CommentStyle // TODO
        {
            BackgroundColor = Color.FromArgb(255, 200, 200),
            BorderColor = Color.Red,
            BorderWidth = 1,
            TextColor = Color.DarkRed,
            FontFamily = "Segoe UI",
            FontSize = 9,
            FontStyle = FontStyle.Italic,
            TextPadding = 4,
            TextFormat = StringFormat.GenericDefault
        },
        _ => new CommentStyle // Default
        {
            BackgroundColor = Color.FromArgb(240, 240, 240),
            BorderColor = Color.Gray,
            BorderWidth = 1,
            TextColor = Color.Black,
            FontFamily = "Segoe UI",
            FontSize = 9,
            FontStyle = FontStyle.Regular,
            TextPadding = 4,
            TextFormat = StringFormat.GenericDefault
        }
    };
}
```

## Comment Management System

### Comment Grouping
```csharp
public class CommentGroup
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Color GroupColor { get; set; }
    public List<CommentEffectsNode> Comments { get; set; } = new List<CommentEffectsNode>();
    public bool IsExpanded { get; set; } = true;
    public int SortOrder { get; set; }
}
```

### Comment Search and Filter
```csharp
public class CommentSearchEngine
{
    public List<CommentEffectsNode> SearchComments(string searchTerm, List<CommentEffectsNode> allComments)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return allComments;

        var results = new List<CommentEffectsNode>();
        var term = searchTerm.ToLowerInvariant();

        foreach (var comment in allComments)
        {
            if (comment.CommentText.ToLowerInvariant().Contains(term) ||
                comment.Author.ToLowerInvariant().Contains(term) ||
                comment.GroupName.ToLowerInvariant().Contains(term) ||
                comment.Tags.Any(tag => tag.ToLowerInvariant().Contains(term)))
            {
                results.Add(comment);
            }
        }

        return results.OrderBy(c => c.Priority).ThenBy(c => c.CreationDate).ToList();
    }

    public List<CommentEffectsNode> FilterByPriority(int priority, List<CommentEffectsNode> allComments)
    {
        return allComments.Where(c => c.Priority >= priority).ToList();
    }

    public List<CommentEffectsNode> FilterByGroup(int groupId, List<CommentEffectsNode> allComments)
    {
        return allComments.Where(c => c.GroupId == groupId).ToList();
    }

    public List<CommentEffectsNode> FilterByType(int commentType, List<CommentEffectsNode> allComments)
    {
        return allComments.Where(c => c.CommentType == commentType).ToList();
    }
}
```

### Comment Export
```csharp
public class CommentExporter
{
    public string ExportToMarkdown(List<CommentEffectsNode> comments)
    {
        var markdown = new StringBuilder();
        
        // Group comments by group
        var groupedComments = comments.GroupBy(c => c.GroupId).OrderBy(g => g.Key);
        
        foreach (var group in groupedComments)
        {
            var groupName = group.First().GroupName;
            markdown.AppendLine($"## {groupName}");
            markdown.AppendLine();
            
            foreach (var comment in group.OrderBy(c => c.Priority).ThenBy(c => c.CreationDate))
            {
                var priorityIcon = GetPriorityIcon(comment.Priority);
                var typeIcon = GetTypeIcon(comment.CommentType);
                
                markdown.AppendLine($"### {priorityIcon} {typeIcon} {comment.CommentText}");
                markdown.AppendLine();
                markdown.AppendLine($"**Author:** {comment.Author} | **Date:** {comment.CreationDate:yyyy-MM-dd} | **Priority:** {comment.Priority}");
                
                if (comment.Tags.Length > 0)
                {
                    markdown.AppendLine($"**Tags:** {string.Join(", ", comment.Tags)}");
                }
                
                markdown.AppendLine();
            }
        }
        
        return markdown.ToString();
    }

    private string GetPriorityIcon(int priority)
    {
        return priority switch
        {
            0 => "ℹ️",
            1 => "📝",
            2 => "⚠️",
            3 => "🚨",
            4 => "🛑",
            _ => "📝"
        };
    }

    private string GetTypeIcon(int commentType)
    {
        return commentType switch
        {
            0 => "💬",
            1 => "📋",
            2 => "📑",
            3 => "💻",
            4 => "⚠️",
            5 => "✅",
            6 => "🐛",
            7 => "💡",
            _ => "💬"
        };
    }
}
```

## Advanced Comment Features

### Rich Text Support
```csharp
public class RichTextComment : CommentEffectsNode
{
    public RichTextFormatting[] Formatting { get; set; } = new RichTextFormatting[0];
    public string[] Links { get; set; } = new string[0];
    public CommentAttachment[] Attachments { get; set; } = new CommentAttachment[0];
}

public class RichTextFormatting
{
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public bool IsUnderlined { get; set; }
    public Color TextColor { get; set; }
    public Color BackgroundColor { get; set; }
}
```

### Comment Attachments
```csharp
public class CommentAttachment
{
    public string FileName { get; set; }
    public byte[] FileData { get; set; }
    public string FileType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadDate { get; set; }
    public string Description { get; set; }
}
```

### Comment Collaboration
```csharp
public class CommentCollaboration
{
    public string CommentId { get; set; }
    public List<CommentReply> Replies { get; set; } = new List<CommentReply>();
    public List<CommentReaction> Reactions { get; set; } = new List<CommentReaction>();
    public bool IsResolved { get; set; }
    public string ResolvedBy { get; set; }
    public DateTime ResolvedDate { get; set; }
}

public class CommentReply
{
    public string Id { get; set; }
    public string Author { get; set; }
    public string Text { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsEdited { get; set; }
    public DateTime EditTimestamp { get; set; }
}

public class CommentReaction
{
    public string Emoji { get; set; }
    public string User { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## Performance Optimization

### Optimization Techniques
1. **Lazy Rendering**: Only render visible comments
2. **Text Caching**: Cache rendered text for performance
3. **Virtual Scrolling**: Handle large numbers of comments efficiently
4. **Search Indexing**: Optimize search performance
5. **Memory Management**: Efficient comment storage

### Memory Management
- Efficient text storage
- Minimize string allocations
- Use value types for metadata
- Optimize comment grouping

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image (pass-through)"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Output image (unchanged)"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("CommentType", CommentType);
    metadata.Add("CommentLength", CommentText.Length);
    metadata.Add("Group", GroupName);
    metadata.Add("Author", Author);
    metadata.Add("Priority", Priority);
    metadata.Add("Tags", string.Join(", ", Tags));
    metadata.Add("CreationDate", CreationDate.ToString("yyyy-MM-dd"));
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Functionality**: Verify comment creation and storage
2. **Text Processing**: Test comment text handling
3. **Grouping System**: Validate comment organization
4. **Search and Filter**: Test search functionality
5. **Export Features**: Verify export capabilities
6. **Performance**: Measure rendering speed

### Validation Methods
- Text content verification
- Performance benchmarking
- Memory usage analysis
- Export format validation

## Future Enhancements

### Planned Features
1. **Rich Text Editor**: Advanced text formatting
2. **File Attachments**: Support for file uploads
3. **Real-time Collaboration**: Live comment editing
4. **Version Control**: Comment history tracking
5. **AI Integration**: Smart comment suggestions

### Compatibility
- Full AVS preset compatibility
- Support for legacy comment formats
- Performance parity with original
- Extended functionality

## Conclusion

The Comment effect provides essential documentation capabilities for AVS preset development. This C# implementation maintains full compatibility with the original while adding modern features like rich text support, advanced organization, and collaboration tools. Complete documentation ensures reliable operation in production environments.
