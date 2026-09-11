using System.Globalization;
using System.IO;
using DiplomWork_Ivan_2026.Enums;

namespace DiplomWork_Ivan_2026.Services
{
    public static class RunLabelGenerator
    {
        public static string Create(string materialName, DryingMode dryingMode)
        {
            string prefix = $"{GetMaterialCode(materialName)}-{GetRecipeCode(dryingMode)}";
            string exportDirectory = ExportDirectoryProvider.GetExportDirectory();
            int maximumSequence = 0;

            if (Directory.Exists(exportDirectory))
            {
                foreach (string filePath in Directory.EnumerateFiles(
                    exportDirectory,
                    "*.csv",
                    SearchOption.TopDirectoryOnly))
                {
                    string? runLabel = TryReadRunLabel(filePath);
                    if (runLabel == null ||
                        !runLabel.StartsWith($"{prefix}-", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string sequenceText = runLabel[(prefix.Length + 1)..];
                    if (int.TryParse(
                        sequenceText,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out int sequence))
                    {
                        maximumSequence = Math.Max(maximumSequence, sequence);
                    }
                }
            }

            return $"{prefix}-{maximumSequence + 1:D2}";
        }

        private static string? TryReadRunLabel(string filePath)
        {
            try
            {
                using StreamReader reader = new StreamReader(filePath);

                for (int lineIndex = 0; lineIndex < 100; lineIndex++)
                {
                    string? line = reader.ReadLine();
                    if (line == null || string.Equals(
                        line,
                        "Process Data",
                        StringComparison.Ordinal))
                    {
                        break;
                    }

                    const string prefix = "RunLabel,";
                    if (line.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        string value = line[prefix.Length..];
                        int separatorIndex = value.IndexOf(',');
                        return separatorIndex >= 0
                            ? value[..separatorIndex].Trim('"')
                            : value.Trim('"');
                    }
                }
            }
            catch (IOException)
            {
                // A file being written by another application is ignored.
            }
            catch (UnauthorizedAccessException)
            {
                // An inaccessible export must not block a new experiment.
            }

            return null;
        }

        private static string GetMaterialCode(string materialName) =>
            materialName switch
            {
                "Herbs" => "H",
                "Grain" => "G",
                "Wood" => "W",
                "Fruits" => "F",
                _ => GetFallbackMaterialCode(materialName)
            };

        private static string GetFallbackMaterialCode(string materialName)
        {
            char firstLetter = materialName
                .FirstOrDefault(character => char.IsLetterOrDigit(character));
            return firstLetter == default
                ? "X"
                : char.ToUpperInvariant(firstLetter).ToString();
        }

        private static string GetRecipeCode(DryingMode dryingMode) =>
            dryingMode switch
            {
                DryingMode.Soft => "S",
                DryingMode.Hard => "H",
                _ => "N"
            };
    }
}
