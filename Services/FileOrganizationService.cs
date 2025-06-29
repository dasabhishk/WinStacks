using System.Runtime.CompilerServices;
using DesktopStacksService.Configuration;
using DesktopStacksService.Models;
using Microsoft.Extensions.Options;

namespace DesktopStacksService.Services;

public class FileOrganizationService : IFileOrganizationService
{
    private readonly ILogger<FileOrganizationService> _logger;
    private readonly DesktopStacksOptions _options;

    private readonly Dictionary<string, FileCategory> _extensionMapping = new()
    {
        // Documents
        [".pdf"] = FileCategory.Document,
        [".doc"] = FileCategory.Document,
        [".docx"] = FileCategory.Document,
        [".txt"] = FileCategory.Document,
        [".rtf"] = FileCategory.Document,
        [".xls"] = FileCategory.Document,
        [".xlsx"] = FileCategory.Document,
        [".ppt"] = FileCategory.Document,
        [".pptx"] = FileCategory.Document,

        // Images
        [".jpg"] = FileCategory.Image,
        [".jpeg"] = FileCategory.Image,
        [".png"] = FileCategory.Image,
        [".gif"] = FileCategory.Image,
        [".bmp"] = FileCategory.Image,
        [".tiff"] = FileCategory.Image,
        [".webp"] = FileCategory.Image,

        // Videos
        [".mp4"] = FileCategory.Video,
        [".avi"] = FileCategory.Video,
        [".mkv"] = FileCategory.Video,
        [".mov"] = FileCategory.Video,
        [".wmv"] = FileCategory.Video,
        [".flv"] = FileCategory.Video,

        // Audio
        [".mp3"] = FileCategory.Audio,
        [".wav"] = FileCategory.Audio,
        [".flac"] = FileCategory.Audio,
        [".aac"] = FileCategory.Audio,
        [".ogg"] = FileCategory.Audio,

        // Archives
        [".zip"] = FileCategory.Archive,
        [".rar"] = FileCategory.Archive,
        [".7z"] = FileCategory.Archive,
        [".tar"] = FileCategory.Archive,
        [".gz"] = FileCategory.Archive,

        // Executables
        [".exe"] = FileCategory.Executable,
        [".msi"] = FileCategory.Executable,
        [".bat"] = FileCategory.Executable,
        [".cmd"] = FileCategory.Executable,

        // Code
        [".cs"] = FileCategory.Code,
        [".js"] = FileCategory.Code,
        [".html"] = FileCategory.Code,
        [".css"] = FileCategory.Code,
        [".py"] = FileCategory.Code,
        [".json"] = FileCategory.Code,
        [".xml"] = FileCategory.Code,
    };

    public FileOrganizationService(ILogger<FileOrganizationService> logger, IOptions<DesktopStacksOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IEnumerable<FileItem>> GetDesktopFilesAsync(string desktopPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(desktopPath);

            // More efficient enumeration
            var fileInfos = directoryInfo.EnumerateFiles()
                .Where(f => !_options.ExcludedExtensions.Contains(f.Extension.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase))
                .Where(f => DateTime.Now - f.LastWriteTime > _options.MinFileAge)
                .Take(_options.MaxBatchSize);

            var files = new List<FileItem>(_options.MaxBatchSize);

            await foreach (var fileInfo in ProcessFilesAsync(fileInfos, cancellationToken))
            {
                files.Add(fileInfo);
            }

            _logger.LogInformation("Found {FileCount} files to organize", files.Count);
            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning desktop files at {Path}", desktopPath);
            return Array.Empty<FileItem>();
        }
    }

    private async IAsyncEnumerable<FileItem> ProcessFilesAsync(
        IEnumerable<System.IO.FileInfo> fileInfos,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var fileInfo in fileInfos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Yield periodically to prevent blocking
            if (DateTime.Now.Millisecond % 10 == 0)
                await Task.Yield();

            var extension = fileInfo.Extension.ToLowerInvariant();
            var category = _extensionMapping.GetValueOrDefault(extension, FileCategory.Other);

            yield return new FileItem
            {
                FullPath = fileInfo.FullName,
                Name = fileInfo.Name,
                Extension = extension,
                CreatedAt = fileInfo.CreationTime,
                ModifiedAt = fileInfo.LastWriteTime,
                Size = fileInfo.Length,
                Category = category
            };
        }
    }

    public Task<IEnumerable<StackGroup>> OrganizeFilesAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default)
    {
        var stacks = files
            .GroupBy(f => f.Category)
            .Where(g => g.Any())
            .Select(g => new StackGroup
            {
                Name = GetStackName(g.Key),
                Category = g.Key,
                Files = g.ToList(),
                TargetPath = Path.Combine(_options.MonitorPath, "Stacks", GetStackName(g.Key))
            })
            .ToList();

        _logger.LogInformation("Organized {FileCount} files into {StackCount} stacks",
            files.Count(), stacks.Count);

        return Task.FromResult<IEnumerable<StackGroup>>(stacks);
    }

    public async Task CreateStacksAsync(IEnumerable<StackGroup> stacks, CancellationToken cancellationToken = default)
    {
        foreach (var stack in stacks)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                Directory.CreateDirectory(stack.TargetPath);

                foreach (var file in stack.Files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await MoveFileWithRetryAsync(file, stack.TargetPath, cancellationToken);
                }

                _logger.LogInformation("Created stack '{StackName}' with {FileCount} files",
                    stack.Name, stack.Files.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stack '{StackName}'", stack.Name);
            }
        }
    }

    private async Task MoveFileWithRetryAsync(FileItem file, string stackPath, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                var targetPath = GenerateUniqueFileName(stackPath, file.Name);

                // Use FileStream for atomic operation
                using var source = new FileStream(file.FullPath, FileMode.Open, FileAccess.Read, FileShare.None);
                using var destination = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

                await source.CopyToAsync(destination, cancellationToken);
                destination.Close();
                source.Close();

                // Only delete source after successful copy
                File.Delete(file.FullPath);

                _logger.LogDebug("Moved file {Source} to {Target}", file.FullPath, targetPath);
                return;
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                attempt++;
                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException($"Failed to move file {file.FullPath} after {maxRetries} attempts");
    }

    private static string GenerateUniqueFileName(string directory, string fileName)
    {
        var targetPath = Path.Combine(directory, fileName);

        if (!File.Exists(targetPath))
            return targetPath;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);

        for (int i = 1; i < 1000; i++)
        {
            targetPath = Path.Combine(directory, $"{nameWithoutExt}_{i}{ext}");
            if (!File.Exists(targetPath))
                return targetPath;
        }

        throw new InvalidOperationException($"Cannot generate unique filename for {fileName}");
    }

    private static string GetStackName(FileCategory category) => category switch
    {
        FileCategory.Document => "Documents",
        FileCategory.Image => "Images",
        FileCategory.Video => "Videos",
        FileCategory.Audio => "Audio",
        FileCategory.Archive => "Archives",
        FileCategory.Executable => "Applications",
        FileCategory.Code => "Code Files",
        FileCategory.Other => "Other Files",
        _ => "Unknown"
    };
}