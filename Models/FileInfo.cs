namespace DesktopStacksService.Models;

public record FileItem
{
    public required string FullPath { get; init; }
    public required string Name { get; init; }
    public required string Extension { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ModifiedAt { get; init; }
    public required long Size { get; init; }
    public required FileCategory Category { get; init; }
}

public enum FileCategory
{
    Document,
    Image,
    Video,
    Audio,
    Archive,
    Executable,
    Code,
    Other
}

public record StackGroup
{
    public required string Name { get; init; }
    public required FileCategory Category { get; init; }
    public required List<FileItem> Files { get; init; } = new();
    public required string TargetPath { get; init; }
}