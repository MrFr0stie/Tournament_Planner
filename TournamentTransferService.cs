using System.Text.Json;
using System.IO;

namespace DartLeague;

public static class TournamentTransferService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Export(string filePath, TournamentBackup backup) => File.WriteAllText(filePath, JsonSerializer.Serialize(backup, JsonOptions));

    public static TournamentBackup Import(string filePath)
    {
        var backup = JsonSerializer.Deserialize<TournamentBackup>(File.ReadAllText(filePath), JsonOptions);
        if (backup is null || backup.FormatVersion != 1 || backup.Players.Count < 2 || backup.Matches.Count == 0)
            throw new InvalidDataException("This file is not a valid Dartboard tournament export.");
        return backup;
    }
}
