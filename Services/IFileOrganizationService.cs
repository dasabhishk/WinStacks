using DesktopStacksService.Models;

namespace DesktopStacksService.Services;

public interface IFileOrganizationService
{
    Task<IEnumerable<FileItem>> GetDesktopFilesAsync(string desktopPath, CancellationToken cancellationToken = default);
    Task<IEnumerable<StackGroup>> OrganizeFilesAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default);
    Task CreateStacksAsync(IEnumerable<StackGroup> stacks, CancellationToken cancellationToken = default);
}