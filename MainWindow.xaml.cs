using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace DartLeague;

public partial class MainWindow : Window
{
    private readonly LeagueDatabase _database;
    private long? _seasonId;
    private LeagueMatch? _selectedMatch;
    private Competition? _activeCompetition;
    private bool _isSelectingCompetition;
    private List<LeagueMatch> _allMatches = new();

    public ObservableCollection<Player> Players { get; } = new();
    public ObservableCollection<Competition> Competitions { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        _database = new LeagueDatabase();
        PopulateDefaultPlayers();
        LoadLatestSeason();
        ShowView(DashboardView, "Dashboard", string.Empty);
    }

    private void LoadLatestSeason()
    {
        var season = _database.GetLatestSeason();
        if (season is not null) _seasonId = season.Id;
        RefreshCompetitionSelector(_seasonId);
        LoadSeasonData();
        RefreshDashboard();
    }

    private void RefreshCompetitionSelector(long? selectedId)
    {
        _isSelectingCompetition = true;
        Competitions.Clear();
        foreach (var competition in _database.GetCompetitions()) Competitions.Add(competition);
        CompetitionSelector.ItemsSource = Competitions;
        CompetitionSelector.SelectedItem = Competitions.FirstOrDefault(c => c.Id == selectedId);
        _activeCompetition = CompetitionSelector.SelectedItem as Competition;
        _isSelectingCompetition = false;
    }

    private void CompetitionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSelectingCompetition || CompetitionSelector.SelectedItem is not Competition competition) return;
        _seasonId = competition.Id;
        _activeCompetition = competition;
        _selectedMatch = null;
        SelectedMatchText.Text = "Select match";
        ClearStatsInputs();
        LoadSeasonData();
        RefreshDashboard();
    }

    private void NewCompetition_Click(object sender, RoutedEventArgs e) => PrepareNewCompetition();

    private void PrepareNewCompetition()
    {
        _selectedMatch = null;
        SeasonNameTextBox.Text = "New competition";
        CompetitionTypeComboBox.SelectedIndex = 0;
        MeetingsComboBox.SelectedIndex = 0;
        LegsToWinComboBox.SelectedIndex = 1;
        PopulateDefaultPlayers();
        ShowView(SetupView, "New competition", "Set up once, then manage every match from the workspace.");
    }

    private void PopulateDefaultPlayers()
    {
        Players.Clear();
        for (var i = 1; i <= 4; i++) Players.Add(new Player { Id = i, Name = $"Player {i}" });
    }

    private void LoadSeasonData()
    {
        if (!_seasonId.HasValue) return;
        _allMatches = _database.GetMatches(_seasonId.Value);
        ApplyMatchFilter();
        StandingsGrid.ItemsSource = _database.GetStandings(_seasonId.Value);
        StatisticsGrid.ItemsSource = _database.GetPlayerStatistics(_seasonId.Value);
    }

    private void MatchFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyMatchFilter();

    private void ApplyMatchFilter()
    {
        // The ComboBox raises SelectionChanged while its visual tree is being
        // constructed, before MatchesGrid exists. Loading the season below
        // will apply this same filter once initialization is complete.
        if (MatchesGrid is null) return;

        if (MatchFilterComboBox?.SelectedItem is not ComboBoxItem filter) return;
        var matches = filter.Tag?.ToString() switch
        {
            "Open" => _allMatches.Where(match => !match.IsPlayed),
            "Completed" => _allMatches.Where(match => match.IsPlayed),
            _ => _allMatches
        };
        MatchesGrid.ItemsSource = matches.ToList();
    }

    private void RefreshDashboard()
    {
        if (!_seasonId.HasValue || _activeCompetition is null)
        {
            DashboardCompetitionText.Text = "No competition selected";
            DashboardFormatText.Text = string.Empty;
            PlayerCountText.Text = MatchCountText.Text = PlayedCountText.Text = "0";
            NextMatchesGrid.ItemsSource = null;
            TopStandingsGrid.ItemsSource = null;
            return;
        }
        var matches = _database.GetMatches(_seasonId.Value);
        var standings = _database.GetStandings(_seasonId.Value);
        DashboardCompetitionText.Text = _activeCompetition.Name;
        DashboardFormatText.Text = $"{_activeCompetition.Type}  ·  First to {_activeCompetition.LegsToWin} legs";
        PlayerCountText.Text = standings.Count.ToString();
        MatchCountText.Text = matches.Count.ToString();
        PlayedCountText.Text = matches.Count(match => match.IsPlayed).ToString();
        NextMatchesGrid.ItemsSource = matches.Where(match => !match.IsPlayed).Take(6).ToList();
        TopStandingsGrid.ItemsSource = standings.Take(5).ToList();
    }

    private void ShowDashboard_Click(object sender, RoutedEventArgs e)
    {
        RefreshDashboard();
        ShowView(DashboardView, "Dashboard", string.Empty);
    }
    private void ShowSetup_Click(object sender, RoutedEventArgs e) => PrepareNewCompetition();
    private void ShowMatches_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSeason()) return;
        LoadSeasonData();
        ShowView(MatchesView, "Matches", "Select match  •  Record result  •  Add optional statistics");
    }
    private void ShowStandings_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSeason()) return;
        StandingsGrid.ItemsSource = _database.GetStandings(_seasonId!.Value);
        ShowView(StandingsView, "Leaderboard", string.Empty);
    }
    private void ShowStats_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSeason()) return;
        StatisticsGrid.ItemsSource = _database.GetPlayerStatistics(_seasonId!.Value);
        ShowView(StatsView, "Statistics", "Season totals");
    }
    private void OpenMatches_Click(object sender, RoutedEventArgs e)
    {
        ShowMatches_Click(sender, e);
        MatchFilterComboBox.SelectedIndex = 0;
        var next = _allMatches.FirstOrDefault(match => !match.IsPlayed);
        if (next is null) return;
        MatchesGrid.SelectedItem = next;
        MatchesGrid.ScrollIntoView(next);
    }
    private void OpenStandings_Click(object sender, RoutedEventArgs e) => ShowStandings_Click(sender, e);

    private void NextMatchesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NextMatchesGrid.SelectedItem is not LeagueMatch nextMatch) return;
        ShowMatches_Click(sender, e);
        MatchFilterComboBox.SelectedIndex = 0;
        if (MatchesGrid.ItemsSource is IEnumerable<LeagueMatch> matches)
        {
            var match = matches.FirstOrDefault(item => item.Id == nextMatch.Id);
            if (match is not null)
            {
                MatchesGrid.SelectedItem = match;
                MatchesGrid.ScrollIntoView(match);
            }
        }
        NextMatchesGrid.SelectedItem = null;
    }

    private void ShowView(UIElement target, string title, string subtitle)
    {
        SetupView.Visibility = Visibility.Collapsed;
        DashboardView.Visibility = Visibility.Collapsed;
        MatchesView.Visibility = Visibility.Collapsed;
        StandingsView.Visibility = Visibility.Collapsed;
        StatsView.Visibility = Visibility.Collapsed;
        target.Visibility = Visibility.Visible;
        PageTitle.Text = title;
        PageSubtitle.Text = subtitle;
        PageSubtitle.Visibility = string.IsNullOrWhiteSpace(subtitle) ? Visibility.Collapsed : Visibility.Visible;
        SetActiveNavigation(target);
    }

    private void SetActiveNavigation(UIElement target)
    {
        var buttons = new[] { NavOverview, NavMatches, NavStandings, NavStatistics };
        foreach (var button in buttons)
        {
            button.Background = Brushes.Transparent;
            button.Foreground = new SolidColorBrush(Color.FromRgb(185, 194, 211));
        }
        if (target == SetupView) return;
        var active = target == DashboardView ? NavOverview : target == MatchesView ? NavMatches : target == StandingsView ? NavStandings : NavStatistics;
        active.Background = new SolidColorBrush(Color.FromRgb(32, 43, 63));
        active.Foreground = Brushes.White;
    }

    private bool RequireSeason()
    {
        if (_seasonId.HasValue) return true;
        MessageBox.Show("Create a season first by adding players on the League setup page.", "No active season", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }

    private void AddPlayer_Click(object sender, RoutedEventArgs e) => Players.Add(new Player { Id = Players.Count + 1, Name = $"Player {Players.Count + 1}" });

    private void RemovePlayer_Click(object sender, RoutedEventArgs e)
    {
        if (PlayersGrid.SelectedItem is not Player player) return;
        Players.Remove(player);
        for (var i = 0; i < Players.Count; i++) Players[i].Id = i + 1;
    }

    private void StartSeason_Click(object sender, RoutedEventArgs e)
    {
        PlayersGrid.CommitEdit(DataGridEditingUnit.Row, true);
        var names = Players.Select(p => p.Name.Trim()).ToList();
        if (names.Count < 2)
        {
            MessageBox.Show("A league needs at least two players.", "Add players", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (names.Any(string.IsNullOrWhiteSpace) || names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count)
        {
            MessageBox.Show("Every player needs a unique name.", "Check player names", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var name = SeasonNameTextBox.Text.Trim();
        var type = ((ComboBoxItem)CompetitionTypeComboBox.SelectedItem).Tag.ToString()!;
        if (string.IsNullOrWhiteSpace(name)) name = type == "Season" ? "New season" : "New tournament";
        var meetings = int.Parse(((ComboBoxItem)MeetingsComboBox.SelectedItem).Tag.ToString()!);
        var legsToWin = int.Parse(((ComboBoxItem)LegsToWinComboBox.SelectedItem).Tag.ToString()!);
        _seasonId = _database.CreateSeason(name, type, meetings, legsToWin, Players);
        RefreshCompetitionSelector(_seasonId);
        _selectedMatch = null;
        SelectedMatchText.Text = "Select match";
        ClearStatsInputs();
        LoadSeasonData();
        RefreshDashboard();
        ShowView(MatchesView, "Matches", "Select match  •  Record result  •  Add optional statistics");
    }

    private void MatchesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedMatch = MatchesGrid.SelectedItem as LeagueMatch;
        if (_selectedMatch is null) return;
        SelectedMatchText.Text = $"Round {_selectedMatch.RoundNumber}: {_selectedMatch.HomePlayer} vs {_selectedMatch.AwayPlayer}";
        HomeScoreTextBox.Text = _selectedMatch.HomeScore?.ToString() ?? string.Empty;
        AwayScoreTextBox.Text = _selectedMatch.AwayScore?.ToString() ?? string.Empty;
        StatsMatchText.Text = $"Round {_selectedMatch.RoundNumber}: {_selectedMatch.HomePlayer} vs {_selectedMatch.AwayPlayer}";
        StatsPlayerComboBox.SelectedIndex = 0;
        LoadStatsForSelectedPlayer();
    }

    private void SaveResult_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMatch is null)
        {
            MessageBox.Show("Select a match first.", "No match selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var legsToWin = _activeCompetition?.LegsToWin ?? 3;
        if (!int.TryParse(HomeScoreTextBox.Text, out var home) || !int.TryParse(AwayScoreTextBox.Text, out var away) || home < 0 || away < 0 || home == away || Math.Max(home, away) != legsToWin)
        {
            MessageBox.Show($"One player must reach {legsToWin} legs.", "Invalid result", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _selectedMatch.HomeScore = home;
        _selectedMatch.AwayScore = away;
        _database.SaveResult(_selectedMatch);
        ApplyMatchFilter();
        StandingsGrid.ItemsSource = _database.GetStandings(_seasonId!.Value);
        StatisticsGrid.ItemsSource = _database.GetPlayerStatistics(_seasonId!.Value);
        RefreshDashboard();
        MessageBox.Show("Result saved. The leaderboard has been recalculated.", "Result recorded", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AdjustScore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag }) return;
        var parts = tag.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var adjustment)) return;
        var target = parts[0] == "Home" ? HomeScoreTextBox : AwayScoreTextBox;
        var score = int.TryParse(target.Text, out var parsed) ? parsed : 0;
        target.Text = Math.Max(0, score + adjustment).ToString();
    }

    private void ClearResult_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMatch is null || !_selectedMatch.IsPlayed)
        {
            MessageBox.Show("Select a saved result first.", "No saved result", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var confirmation = MessageBox.Show("Clear this result and its saved match statistics?", "Clear result", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes) return;
        _database.ClearResult(_selectedMatch.Id);
        _selectedMatch.HomeScore = null;
        _selectedMatch.AwayScore = null;
        HomeScoreTextBox.Clear();
        AwayScoreTextBox.Clear();
        ClearStatsInputs();
        ApplyMatchFilter();
        StandingsGrid.ItemsSource = _database.GetStandings(_seasonId!.Value);
        StatisticsGrid.ItemsSource = _database.GetPlayerStatistics(_seasonId.Value);
        RefreshDashboard();
    }

    private void ExportStandings_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSeason()) return;
        var dialog = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = $"{_activeCompetition?.Name ?? "dartboard"}-standings.csv" };
        if (dialog.ShowDialog() != true) return;
        var rows = _database.GetStandings(_seasonId!.Value);
        StandingsExporter.ExportCsv(dialog.FileName, rows);
    }

    private void StatsPlayerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadStatsForSelectedPlayer();

    private void LoadStatsForSelectedPlayer()
    {
        if (_selectedMatch is null || StatsPlayerComboBox.SelectedItem is not ComboBoxItem selection) return;
        var isHome = Equals(selection.Tag, "Home");
        var playerName = isHome ? _selectedMatch.HomePlayer : _selectedMatch.AwayPlayer;
        StatsPlayerText.Text = playerName;
        var stats = _database.GetStats(_selectedMatch.Id, isHome ? _selectedMatch.HomePlayerId : _selectedMatch.AwayPlayerId);
        AverageTextBox.Text = stats.ThreeDartAverage?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
        LegsTextBox.Text = stats.LegsPlayed == 0 ? string.Empty : stats.LegsPlayed.ToString();
        HighFinishTextBox.Text = stats.HighFinish?.ToString() ?? string.Empty;
        ShortLegTextBox.Text = stats.ShortLeg?.ToString() ?? string.Empty;
        Scores80TextBox.Text = stats.Scores80 == 0 ? string.Empty : stats.Scores80.ToString();
        Scores100TextBox.Text = stats.Scores100 == 0 ? string.Empty : stats.Scores100.ToString();
        Scores140TextBox.Text = stats.Scores140 == 0 ? string.Empty : stats.Scores140.ToString();
    }

    private void SaveStats_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMatch is null || !_selectedMatch.IsPlayed || StatsPlayerComboBox.SelectedItem is not ComboBoxItem selection)
        {
            MessageBox.Show("Select a match with a recorded result before saving statistics.", "Result required", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!TryGetStats(out var stats))
        {
            MessageBox.Show("Use valid non-negative whole numbers. The 3-dart average can be a decimal.", "Check statistics", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var isHome = Equals(selection.Tag, "Home");
        _database.SaveStats(_selectedMatch.Id, isHome ? _selectedMatch.HomePlayerId : _selectedMatch.AwayPlayerId, stats);
        StatisticsGrid.ItemsSource = _database.GetPlayerStatistics(_seasonId!.Value);
        MessageBox.Show("Player statistics saved.", "Statistics recorded", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private bool TryGetStats(out MatchStats stats)
    {
        stats = new MatchStats
        {
            ThreeDartAverage = ParseNullableDouble(AverageTextBox.Text),
            LegsPlayed = ParseIntOrZero(LegsTextBox.Text),
            HighFinish = ParseNullableInt(HighFinishTextBox.Text),
            ShortLeg = ParseNullableInt(ShortLegTextBox.Text),
            Scores80 = ParseIntOrZero(Scores80TextBox.Text),
            Scores100 = ParseIntOrZero(Scores100TextBox.Text),
            Scores140 = ParseIntOrZero(Scores140TextBox.Text)
        };
        return (string.IsNullOrWhiteSpace(AverageTextBox.Text) || stats.ThreeDartAverage is >= 0)
            && FieldsAreNonNegativeIntegers(LegsTextBox, HighFinishTextBox, ShortLegTextBox, Scores80TextBox, Scores100TextBox, Scores140TextBox);
    }

    private static bool FieldsAreNonNegativeIntegers(params TextBox[] boxes) => boxes.All(box => string.IsNullOrWhiteSpace(box.Text) || int.TryParse(box.Text, out var value) && value >= 0);
    private static int ParseIntOrZero(string value) => int.TryParse(value, out var result) ? result : 0;
    private static int? ParseNullableInt(string value) => int.TryParse(value, out var result) ? result : null;
    private static double? ParseNullableDouble(string value) => double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var result) ? result : null;
    private void ClearStatsInputs()
    {
        AverageTextBox.Clear(); LegsTextBox.Clear(); HighFinishTextBox.Clear(); ShortLegTextBox.Clear(); Scores80TextBox.Clear(); Scores100TextBox.Clear(); Scores140TextBox.Clear();
    }
}
