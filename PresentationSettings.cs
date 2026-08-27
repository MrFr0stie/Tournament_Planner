using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DartLeague;

public static class ThemeManager
{
    public static void Apply(bool darkMode)
    {
        var palette = darkMode
            ? new Dictionary<string, string>
            {
                ["CanvasBrush"] = "#10131B", ["SurfaceBrush"] = "#171C26", ["InputSurfaceBrush"] = "#1B2130",
                ["InkBrush"] = "#F4F6FB", ["MutedBrush"] = "#A8B2C3", ["LineBrush"] = "#2C3444",
                ["AccentBrush"] = "#8D90F4", ["SidebarBrush"] = "#0B0E14", ["SoftSurfaceBrush"] = "#232A37",
                ["SoftAccentBrush"] = "#30315A", ["TableHeaderBrush"] = "#1C2230", ["TableHoverBrush"] = "#202737",
                ["SuccessSurfaceBrush"] = "#143A2D"
            }
            : new Dictionary<string, string>
            {
                ["CanvasBrush"] = "#F5F7FB", ["SurfaceBrush"] = "#FFFFFF", ["InputSurfaceBrush"] = "#FFFFFF",
                ["InkBrush"] = "#172033", ["MutedBrush"] = "#718096", ["LineBrush"] = "#E6EAF0",
                ["AccentBrush"] = "#5B5CE2", ["SidebarBrush"] = "#111827", ["SoftSurfaceBrush"] = "#EEF1F7",
                ["SoftAccentBrush"] = "#E9EAFF", ["TableHeaderBrush"] = "#FAFBFC", ["TableHoverBrush"] = "#F8F9FC",
                ["SuccessSurfaceBrush"] = "#E8F7F0"
            };

        foreach (var (key, color) in palette)
            if (Application.Current.Resources[key] is SolidColorBrush brush) brush.Color = (Color)ColorConverter.ConvertFromString(color)!;
    }
}

public static class UiLocalizer
{
    private static readonly Dictionary<string, string> EnglishToGerman = new()
    {
        ["Tournament workspace"] = "Turnierverwaltung", ["WORKSPACE"] = "ARBEITSBEREICH", ["COMPETITION"] = "WETTBEWERB",
        ["Overview"] = "Übersicht", ["Dashboard"] = "Übersicht", ["Matches"] = "Begegnungen", ["Standings"] = "Tabelle", ["Leaderboard"] = "Rangliste", ["Statistics"] = "Statistiken", ["Options"] = "Einstellungen",
        ["New competition"] = "Neuer Wettbewerb", ["ACTIVE COMPETITION"] = "AKTUELLER WETTBEWERB", ["Enter results"] = "Ergebnisse eintragen", ["Table"] = "Tabelle",
        ["Players"] = "Spieler", ["Player"] = "Spieler", ["Completed"] = "Abgeschlossen", ["Next matches"] = "Nächste Begegnungen", ["Select to record"] = "Zum Eintragen auswählen", ["Table leaders"] = "Tabellenführer", ["No competition selected"] = "Kein Wettbewerb ausgewählt",
        ["Competition details"] = "Wettbewerbsdetails", ["Name"] = "Name", ["Type"] = "Typ", ["Meetings"] = "Begegnungen", ["Match format"] = "Spielformat", ["First matchday"] = "Erster Spieltag", ["Weekly matchday"] = "Wöchentlicher Spieltag", ["Update date"] = "Datum ändern", ["Matchday date"] = "Spieltagstermin", ["Select a matchday date first."] = "Wähle zuerst ein Spieltagsdatum aus.", ["Monday"] = "Montag", ["Tuesday"] = "Dienstag", ["Wednesday"] = "Mittwoch", ["Thursday"] = "Donnerstag", ["Friday"] = "Freitag", ["Saturday"] = "Samstag", ["Sunday"] = "Sonntag", ["Set up once, then manage every match from the workspace."] = "Einmal einrichten, danach alle Begegnungen hier verwalten.", ["Select match  •  Record result  •  Add optional statistics"] = "Begegnung auswählen  •  Ergebnis eintragen  •  Statistiken ergänzen",
        ["Add player"] = "Spieler hinzufügen", ["Remove selected"] = "Auswahl entfernen", ["Create competition"] = "Wettbewerb erstellen", ["PLAYER NAME"] = "SPIELERNAME",
        ["Season"] = "Saison", ["Tournament"] = "Turnier", ["Match list"] = "Spielplan", ["All matches"] = "Alle Begegnungen", ["Open matches"] = "Offene Begegnungen",
        ["1. RECORD RESULT"] = "1. ERGEBNIS EINTRAGEN", ["Select a match"] = "Begegnung auswählen", ["Clear"] = "Zurücksetzen", ["Save result"] = "Ergebnis speichern",
        ["2. MATCH STATISTICS"] = "2. MATCHSTATISTIK", ["Select a played match"] = "Gespielte Begegnung auswählen", ["Home player"] = "Heimspieler", ["Away player"] = "Gastspieler", ["Save statistics"] = "Statistiken speichern",
        ["League table"] = "Ligatabelle", ["Export CSV"] = "CSV exportieren", ["Player statistics"] = "Spielerstatistiken", ["Season totals"] = "Saisonwerte",
        ["Language"] = "Sprache", ["Appearance"] = "Darstellung", ["English"] = "Englisch", ["German"] = "Deutsch", ["Light"] = "Hell", ["Dark"] = "Dunkel",
        ["Application settings"] = "App-Einstellungen", ["Choose how Dartboard looks and which language it uses."] = "Lege Sprache und Darstellung von Dartboard fest.", ["English or German"] = "Englisch oder Deutsch", ["Light or dark mode"] = "Heller oder dunkler Modus",
        ["1 time"] = "1 Mal", ["2 times"] = "2 Mal", ["3 times"] = "3 Mal", ["4 times"] = "4 Mal", ["5 times"] = "5 Mal", ["6 times"] = "6 Mal", ["7 times"] = "7 Mal", ["8 times"] = "8 Mal", ["9 times"] = "9 Mal", ["10 times"] = "10 Mal",
        ["Best of 2 (draw possible)"] = "Best of 2 (Unentschieden möglich)", ["Best of 4 (draw possible)"] = "Best of 4 (Unentschieden möglich)", ["Best of 6 (draw possible)"] = "Best of 6 (Unentschieden möglich)", ["Best of 8 (draw possible)"] = "Best of 8 (Unentschieden möglich)", ["Best of 10 (draw possible)"] = "Best of 10 (Unentschieden möglich)", ["First to 1 leg"] = "Erster auf 1 Leg", ["First to 2 legs"] = "Erster auf 2 Legs", ["First to 3 legs"] = "Erster auf 3 Legs", ["First to 4 legs"] = "Erster auf 4 Legs", ["First to 5 legs"] = "Erster auf 5 Legs", ["First to 6 legs"] = "Erster auf 6 Legs", ["First to 7 legs"] = "Erster auf 7 Legs", ["First to 8 legs"] = "Erster auf 8 Legs", ["First to 9 legs"] = "Erster auf 9 Legs", ["First to 10 legs"] = "Erster auf 10 Legs",
        ["High finish"] = "Höchstes Finish", ["Short leg"] = "Kürzestes Leg", ["ROUND"] = "RUNDE", ["DATE"] = "DATUM", ["HOME"] = "HEIM", ["AWAY"] = "GAST", ["RESULT"] = "ERGEBNIS", ["STATUS"] = "STATUS", ["PLAYER"] = "SPIELER", ["POINTS"] = "PUNKTE", ["GAMES"] = "SPIELE", ["LEGS"] = "LEGS"
        , ["Round"] = "Runde", ["vs"] = "gegen", ["New season"] = "Neue Saison", ["New tournament"] = "Neues Turnier", ["match"] = "Begegnung", ["Enter a valid"] = "Gib ein gültiges", ["result."] = "Ergebnis ein.",
        ["Create a season first by adding players on the League setup page."] = "Erstelle zuerst eine Saison und füge Spieler hinzu.", ["No active season"] = "Keine aktive Saison", ["A league needs at least two players."] = "Eine Liga benötigt mindestens zwei Spieler.", ["Check player names"] = "Spielernamen prüfen", ["Every player needs a unique name."] = "Jeder Spieler benötigt einen eindeutigen Namen.", ["Select a match first."] = "Wähle zuerst eine Begegnung aus.", ["No match selected"] = "Keine Begegnung ausgewählt", ["Invalid result"] = "Ungültiges Ergebnis", ["Result saved. The leaderboard has been recalculated."] = "Ergebnis gespeichert. Die Tabelle wurde neu berechnet.", ["Result recorded"] = "Ergebnis erfasst", ["Select a saved result first."] = "Wähle zuerst ein gespeichertes Ergebnis aus.", ["No saved result"] = "Kein gespeichertes Ergebnis", ["Clear this result and its saved match statistics?"] = "Dieses Ergebnis und die gespeicherten Matchstatistiken zurücksetzen?", ["Clear result"] = "Ergebnis zurücksetzen", ["Select a match with a recorded result before saving statistics."] = "Wähle vor dem Speichern der Statistiken eine Begegnung mit Ergebnis aus.", ["Result required"] = "Ergebnis erforderlich", ["Use valid non-negative whole numbers. The 3-dart average can be a decimal."] = "Verwende gültige nicht-negative ganze Zahlen. Der 3-Dart-Average darf Dezimalstellen haben.", ["Check statistics"] = "Statistiken prüfen", ["Player statistics saved."] = "Spielerstatistiken gespeichert.", ["Statistics recorded"] = "Statistiken erfasst"
    };
    private static readonly Dictionary<string, string> GermanToEnglish = EnglishToGerman
        .GroupBy(pair => pair.Value)
        .ToDictionary(group => group.Key, group => group.First().Key);

    public static string Translate(string value, bool german) => german
        ? EnglishToGerman.GetValueOrDefault(value, value)
        : GermanToEnglish.GetValueOrDefault(value, value);

    public static void Apply(DependencyObject root, bool german)
    {
        TranslateElement(root, german);
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>()) Apply(child, german);
    }

    public static void ApplyHeaders(bool german, params DataGrid[] grids)
    {
        foreach (var grid in grids)
            foreach (var column in grid.Columns)
                if (column.Header is string header) column.Header = Translate(header, german);
    }

    private static void TranslateElement(DependencyObject element, bool german)
    {
        if (element is TextBlock textBlock) textBlock.Text = Translate(textBlock.Text, german);
        if (element is Button { Content: string content } button) button.Content = Translate(content, german);
        if (element is ComboBoxItem { Content: string itemContent } item) item.Content = Translate(itemContent, german);
    }
}
