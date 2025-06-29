using System.ComponentModel.DataAnnotations;

namespace DesktopStacksService.Configuration
{
    public class DesktopStacksOptions
    {
        public const string SectionName = "DesktopStacks";

        /// <summary>
        /// Path to monitor for file changes (default: user's desktop)
        /// </summary>
        private string _monitorPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        [Required]
        public string MonitorPath
        {
            get => _monitorPath;
            set
            {
                // Handle null or empty during configuration binding
                if (string.IsNullOrWhiteSpace(value))
                {
                    // Use default path if not specified
                    _monitorPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    return;
                }

                try
                {
                    var fullPath = Path.GetFullPath(value);

                    // Ensure path is within allowed directories
                    var allowedPaths = new[]
                    {
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    };

                    var isPathAllowed = allowedPaths.Any(allowed =>
                    {
                        try
                        {
                            var allowedFullPath = Path.GetFullPath(allowed);
                            return fullPath.StartsWith(allowedFullPath, StringComparison.OrdinalIgnoreCase);
                        }
                        catch
                        {
                            return false;
                        }
                    });

                    if (!isPathAllowed)
                        throw new UnauthorizedAccessException($"Path '{fullPath}' is not in allowed directories");

                    _monitorPath = fullPath;
                }
                catch (Exception ex) when (!(ex is UnauthorizedAccessException))
                {
                    throw new ArgumentException($"Invalid path '{value}': {ex.Message}", nameof(value), ex);
                }
            }
        }

        /// <summary>
        /// Strategy for organizing files (FileType, Date, Size, Custom)
        /// </summary>
        public OrganizationStrategy OrganizationStrategy { get; set; } = OrganizationStrategy.FileType;

        /// <summary>
        /// Interval between organization checks
        /// </summary>
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Enable automatic organization of files
        /// </summary>
        public bool EnableAutoOrganization { get; set; } = true;

        /// <summary>
        /// Minimum file age before organization (prevents organizing files being actively used)
        /// </summary>
        public TimeSpan MinFileAge { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// File extensions to exclude from organization
        /// </summary>
        public string[] ExcludedExtensions { get; set; } = { ".lnk", ".url", ".ini" };

        /// <summary>
        /// Maximum number of files to process in a single batch
        /// </summary>
        public int MaxBatchSize { get; set; } = 50;
    }

    public enum OrganizationStrategy
    {
        FileType,
        Date,
        Size,
        Custom
    }
}
