using IOF_Team_Score.Util;
using System.Text.Json;

namespace IOF_Team_Score.Services
{
    public static class OptionsService
    {
        private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "options.json");

        public static Options Load()
        {
            if (!File.Exists(FilePath))
                return new Options(); // Returnér defaults

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<Options>(json) ?? new Options();
        }

        public static void Save(Options settings)
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

    }
}
