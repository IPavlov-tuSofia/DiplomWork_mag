using System.IO;

namespace DiplomWork_Ivan_2026.Services
{
    public static class ExportDirectoryProvider
    {
        public static string GetExportDirectory()
        {
            string? projectExportDirectory = FindProjectExportDirectory(
                AppContext.BaseDirectory) ??
                FindProjectExportDirectory(Environment.CurrentDirectory);

            return projectExportDirectory ??
                Path.Combine(AppContext.BaseDirectory, "Exports");
        }

        private static string? FindProjectExportDirectory(string startPath)
        {
            DirectoryInfo? directory = new DirectoryInfo(startPath);

            while (directory != null)
            {
                if (File.Exists(Path.Combine(
                    directory.FullName,
                    "DiplomWork_Ivan_2026.csproj")))
                {
                    return Path.Combine(directory.FullName, "Exports");
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}
