using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DartLeague;

public sealed class Player : INotifyPropertyChanged
{
    private string _name = string.Empty;
    public long Id { get; set; }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class Competition
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Season";
    public int Meetings { get; set; }
    public int LegsToWin { get; set; } = 3;
    public DateTime CreatedAt { get; set; }
    public string DisplayName => $"{Name}  ·  {Type}";
}

public sealed class LeagueMatch : INotifyPropertyChanged
{
    private int? _homeScore;
    private int? _awayScore;
    public long Id { get; set; }
    public int RoundNumber { get; set; }
    public long HomePlayerId { get; set; }
    public long AwayPlayerId { get; set; }
    public string HomePlayer { get; set; } = string.Empty;
    public string AwayPlayer { get; set; } = string.Empty;
    public int? HomeScore { get => _homeScore; set { _homeScore = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScoreText)); OnPropertyChanged(nameof(IsPlayed)); OnPropertyChanged(nameof(Status)); } }
    public int? AwayScore { get => _awayScore; set { _awayScore = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScoreText)); OnPropertyChanged(nameof(IsPlayed)); OnPropertyChanged(nameof(Status)); } }
    public bool IsPlayed => HomeScore.HasValue && AwayScore.HasValue;
    public string ScoreText => IsPlayed ? $"{HomeScore} – {AwayScore}" : "Pending";
    public string Status => IsPlayed ? "Final" : "Scheduled";
    public string HomeInitial => string.IsNullOrWhiteSpace(HomePlayer) ? "?" : HomePlayer[..1].ToUpperInvariant();
    public string AwayInitial => string.IsNullOrWhiteSpace(AwayPlayer) ? "?" : AwayPlayer[..1].ToUpperInvariant();
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class Standing
{
    public int Position { get; set; }
    public string Player { get; set; } = string.Empty;
    public int Played { get; set; }
    public int Won { get; set; }
    public int Lost { get; set; }
    public int LegsFor { get; set; }
    public int LegsAgainst { get; set; }
    public int LegDifference => LegsFor - LegsAgainst;
    public int Points { get; set; }
    public string Initial => string.IsNullOrWhiteSpace(Player) ? "?" : Player[..1].ToUpperInvariant();
}

public sealed class PlayerStatistics
{
    public string Player { get; set; } = string.Empty;
    public double? Average { get; set; }
    public int Games { get; set; }
    public int Legs { get; set; }
    public int? HighFinish { get; set; }
    public int? ShortLeg { get; set; }
    public int Scores80 { get; set; }
    public int Scores100 { get; set; }
    public int Scores140 { get; set; }
    public string Initial => string.IsNullOrWhiteSpace(Player) ? "?" : Player[..1].ToUpperInvariant();
}

public sealed class MatchStats
{
    public double? ThreeDartAverage { get; set; }
    public int LegsPlayed { get; set; }
    public int? HighFinish { get; set; }
    public int? ShortLeg { get; set; }
    public int Scores80 { get; set; }
    public int Scores100 { get; set; }
    public int Scores140 { get; set; }
}
