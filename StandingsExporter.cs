using System.Text;
using System.IO;

namespace DartLeague;

public static class StandingsExporter
{
    public static void ExportCsv(string filePath, IEnumerable<Standing> standings)
    {
        var csv = new StringBuilder("Position,Player,Played,Won,Lost,Legs For,Legs Against,Leg Difference,Points" + Environment.NewLine);
        foreach (var row in standings)
        {
            csv.AppendLine(string.Join(',', row.Position, Escape(row.Player), row.Played, row.Won, row.Lost, row.LegsFor, row.LegsAgainst, row.LegDifference, row.Points));
        }
        File.WriteAllText(filePath, csv.ToString(), new UTF8Encoding(true));
    }

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
