using Microsoft.Data.Sqlite;
using System.IO;

namespace DartLeague;

public sealed class LeagueDatabase
{
    private readonly string _connectionString;

    public LeagueDatabase()
    {
        var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DartLeague");
        Directory.CreateDirectory(dataDirectory);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(dataDirectory, "dart-league.db"), ForeignKeys = true }.ToString();
        Initialize();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public string? GetSetting(string key)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT SettingValue FROM ApplicationSettings WHERE SettingKey=$key;";
        command.Parameters.AddWithValue("$key", key);
        return command.ExecuteScalar() as string;
    }

    public void SaveSetting(string key, string value)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO ApplicationSettings (SettingKey, SettingValue) VALUES ($key, $value) ON CONFLICT(SettingKey) DO UPDATE SET SettingValue=$value;";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Seasons (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Type TEXT NOT NULL DEFAULT 'Season', Meetings INTEGER NOT NULL, LegsToWin INTEGER NOT NULL DEFAULT 3, FormatType TEXT NOT NULL DEFAULT 'FirstTo', CreatedAt TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS ApplicationSettings (SettingKey TEXT PRIMARY KEY, SettingValue TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Players (Id INTEGER PRIMARY KEY, SeasonId INTEGER NOT NULL REFERENCES Seasons(Id) ON DELETE CASCADE, Name TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Matches (Id INTEGER PRIMARY KEY, SeasonId INTEGER NOT NULL REFERENCES Seasons(Id) ON DELETE CASCADE, RoundNumber INTEGER NOT NULL, HomePlayerId INTEGER NOT NULL REFERENCES Players(Id), AwayPlayerId INTEGER NOT NULL REFERENCES Players(Id), MatchDate TEXT NULL, HomeScore INTEGER NULL, AwayScore INTEGER NULL, RecordedAt TEXT NULL);
            CREATE TABLE IF NOT EXISTS PlayerMatchStats (MatchId INTEGER NOT NULL REFERENCES Matches(Id) ON DELETE CASCADE, PlayerId INTEGER NOT NULL REFERENCES Players(Id), ThreeDartAverage REAL NULL, LegsPlayed INTEGER NOT NULL DEFAULT 0, HighFinish INTEGER NULL, ShortLeg INTEGER NULL, Scores80 INTEGER NOT NULL DEFAULT 0, Scores100 INTEGER NOT NULL DEFAULT 0, Scores140 INTEGER NOT NULL DEFAULT 0, Scores180 INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (MatchId, PlayerId));
            """;
        command.ExecuteNonQuery();

        using var columnCheck = connection.CreateCommand();
        columnCheck.CommandText = "PRAGMA table_info(Seasons);";
        using var reader = columnCheck.ExecuteReader();
        var hasTypeColumn = false;
        var hasLegsToWinColumn = false;
        var hasFormatTypeColumn = false;
        while (reader.Read())
        {
            hasTypeColumn |= reader.GetString(1).Equals("Type", StringComparison.OrdinalIgnoreCase);
            hasLegsToWinColumn |= reader.GetString(1).Equals("LegsToWin", StringComparison.OrdinalIgnoreCase);
            hasFormatTypeColumn |= reader.GetString(1).Equals("FormatType", StringComparison.OrdinalIgnoreCase);
        }
        reader.Dispose();
        if (!hasTypeColumn)
        {
            using var migration = connection.CreateCommand();
            migration.CommandText = "ALTER TABLE Seasons ADD COLUMN Type TEXT NOT NULL DEFAULT 'Season';";
            migration.ExecuteNonQuery();
        }
        if (!hasLegsToWinColumn)
        {
            using var migration = connection.CreateCommand();
            migration.CommandText = "ALTER TABLE Seasons ADD COLUMN LegsToWin INTEGER NOT NULL DEFAULT 3;";
            migration.ExecuteNonQuery();
        }
        if (!hasFormatTypeColumn)
        {
            using var migration = connection.CreateCommand();
            migration.CommandText = "ALTER TABLE Seasons ADD COLUMN FormatType TEXT NOT NULL DEFAULT 'FirstTo';";
            migration.ExecuteNonQuery();
        }

        using var statsColumnCheck = connection.CreateCommand();
        statsColumnCheck.CommandText = "PRAGMA table_info(PlayerMatchStats);";
        using var statsReader = statsColumnCheck.ExecuteReader();
        var hasScores180Column = false;
        while (statsReader.Read()) hasScores180Column |= statsReader.GetString(1).Equals("Scores180", StringComparison.OrdinalIgnoreCase);
        statsReader.Dispose();
        if (!hasScores180Column)
        {
            using var migration = connection.CreateCommand();
            migration.CommandText = "ALTER TABLE PlayerMatchStats ADD COLUMN Scores180 INTEGER NOT NULL DEFAULT 0;";
            migration.ExecuteNonQuery();
        }

        using var matchColumnCheck = connection.CreateCommand();
        matchColumnCheck.CommandText = "PRAGMA table_info(Matches);";
        using var matchReader = matchColumnCheck.ExecuteReader();
        var hasMatchDateColumn = false;
        while (matchReader.Read()) hasMatchDateColumn |= matchReader.GetString(1).Equals("MatchDate", StringComparison.OrdinalIgnoreCase);
        matchReader.Dispose();
        if (!hasMatchDateColumn)
        {
            using var migration = connection.CreateCommand();
            migration.CommandText = "ALTER TABLE Matches ADD COLUMN MatchDate TEXT NULL;";
            migration.ExecuteNonQuery();
        }
    }

    public long CreateSeason(string name, string type, int meetings, string formatType, int formatValue, DateTime firstMatchday, DayOfWeek matchday, IReadOnlyCollection<Player> players)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        long seasonId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Seasons (Name, Type, Meetings, LegsToWin, FormatType, CreatedAt) VALUES ($name, $type, $meetings, $formatValue, $formatType, $created); SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("$name", name);
            command.Parameters.AddWithValue("$type", type);
            command.Parameters.AddWithValue("$meetings", meetings);
            command.Parameters.AddWithValue("$formatValue", formatValue);
            command.Parameters.AddWithValue("$formatType", formatType);
            command.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("O"));
            seasonId = (long)command.ExecuteScalar()!;
        }
        var registered = new List<Player>();
        foreach (var player in players)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Players (SeasonId, Name) VALUES ($season, $name); SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("$season", seasonId);
            command.Parameters.AddWithValue("$name", player.Name.Trim());
            registered.Add(new Player { Id = (long)command.ExecuteScalar()!, Name = player.Name.Trim() });
        }
        foreach (var fixture in LeagueScheduler.Build(registered, meetings, firstMatchday, matchday))
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Matches (SeasonId, RoundNumber, HomePlayerId, AwayPlayerId, MatchDate) VALUES ($season, $round, $home, $away, $date);";
            command.Parameters.AddWithValue("$season", seasonId);
            command.Parameters.AddWithValue("$round", fixture.RoundNumber);
            command.Parameters.AddWithValue("$home", fixture.HomePlayerId);
            command.Parameters.AddWithValue("$away", fixture.AwayPlayerId);
            command.Parameters.AddWithValue("$date", fixture.MatchDate!.Value.ToString("O"));
            command.ExecuteNonQuery();
        }
        transaction.Commit();
        return seasonId;
    }

    public Competition? GetLatestSeason()
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Type, Meetings, LegsToWin, FormatType, CreatedAt FROM Seasons ORDER BY Id DESC LIMIT 1;";
        using var reader = command.ExecuteReader();
        return reader.Read() ? new Competition { Id = reader.GetInt64(0), Name = reader.GetString(1), Type = reader.GetString(2), Meetings = reader.GetInt32(3), LegsToWin = reader.GetInt32(4), FormatType = reader.GetString(5), CreatedAt = DateTime.Parse(reader.GetString(6)) } : null;
    }

    public List<Competition> GetCompetitions()
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Type, Meetings, LegsToWin, FormatType, CreatedAt FROM Seasons ORDER BY Id DESC;";
        using var reader = command.ExecuteReader();
        var competitions = new List<Competition>();
        while (reader.Read()) competitions.Add(new Competition { Id = reader.GetInt64(0), Name = reader.GetString(1), Type = reader.GetString(2), Meetings = reader.GetInt32(3), LegsToWin = reader.GetInt32(4), FormatType = reader.GetString(5), CreatedAt = DateTime.Parse(reader.GetString(6)) });
        return competitions;
    }

    public List<LeagueMatch> GetMatches(long seasonId)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT m.Id, m.RoundNumber, m.HomePlayerId, m.AwayPlayerId, home.Name, away.Name, m.MatchDate, m.HomeScore, m.AwayScore
            FROM Matches m JOIN Players home ON home.Id=m.HomePlayerId JOIN Players away ON away.Id=m.AwayPlayerId
            WHERE m.SeasonId=$season ORDER BY m.RoundNumber, m.Id;
            """;
        command.Parameters.AddWithValue("$season", seasonId);
        using var reader = command.ExecuteReader(); var results = new List<LeagueMatch>();
        while (reader.Read()) results.Add(new LeagueMatch { Id = reader.GetInt64(0), RoundNumber = reader.GetInt32(1), HomePlayerId = reader.GetInt64(2), AwayPlayerId = reader.GetInt64(3), HomePlayer = reader.GetString(4), AwayPlayer = reader.GetString(5), MatchDate = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)), HomeScore = reader.IsDBNull(7) ? null : reader.GetInt32(7), AwayScore = reader.IsDBNull(8) ? null : reader.GetInt32(8) });
        return results;
    }

    public void UpdateMatchdayDate(long seasonId, int roundNumber, DateTime matchDate)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Matches SET MatchDate=$date WHERE SeasonId=$season AND RoundNumber=$round;";
        command.Parameters.AddWithValue("$date", matchDate.Date.ToString("O"));
        command.Parameters.AddWithValue("$season", seasonId);
        command.Parameters.AddWithValue("$round", roundNumber);
        command.ExecuteNonQuery();
    }

    public void SaveResult(LeagueMatch match)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Matches SET HomeScore=$home, AwayScore=$away, RecordedAt=$recorded WHERE Id=$id;";
        command.Parameters.AddWithValue("$home", match.HomeScore!); command.Parameters.AddWithValue("$away", match.AwayScore!); command.Parameters.AddWithValue("$recorded", DateTime.UtcNow.ToString("O")); command.Parameters.AddWithValue("$id", match.Id); command.ExecuteNonQuery();
    }

    public void ClearResult(long matchId)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using (var resultCommand = connection.CreateCommand())
        {
            resultCommand.Transaction = transaction;
            resultCommand.CommandText = "UPDATE Matches SET HomeScore=NULL, AwayScore=NULL, RecordedAt=NULL WHERE Id=$id;";
            resultCommand.Parameters.AddWithValue("$id", matchId);
            resultCommand.ExecuteNonQuery();
        }
        using (var statsCommand = connection.CreateCommand())
        {
            statsCommand.Transaction = transaction;
            statsCommand.CommandText = "DELETE FROM PlayerMatchStats WHERE MatchId=$id;";
            statsCommand.Parameters.AddWithValue("$id", matchId);
            statsCommand.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public MatchStats GetStats(long matchId, long playerId)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT ThreeDartAverage, LegsPlayed, HighFinish, ShortLeg, Scores80, Scores100, Scores140, Scores180 FROM PlayerMatchStats WHERE MatchId=$match AND PlayerId=$player;";
        command.Parameters.AddWithValue("$match", matchId); command.Parameters.AddWithValue("$player", playerId); using var reader = command.ExecuteReader();
        return reader.Read() ? new MatchStats { ThreeDartAverage = reader.IsDBNull(0) ? null : reader.GetDouble(0), LegsPlayed = reader.GetInt32(1), HighFinish = reader.IsDBNull(2) ? null : reader.GetInt32(2), ShortLeg = reader.IsDBNull(3) ? null : reader.GetInt32(3), Scores80 = reader.GetInt32(4), Scores100 = reader.GetInt32(5), Scores140 = reader.GetInt32(6), Scores180 = reader.GetInt32(7) } : new MatchStats();
    }

    public void SaveStats(long matchId, long playerId, MatchStats stats)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO PlayerMatchStats (MatchId, PlayerId, ThreeDartAverage, LegsPlayed, HighFinish, ShortLeg, Scores80, Scores100, Scores140, Scores180)
            VALUES ($match, $player, $avg, $legs, $finish, $short, $eighty, $hundred, $oneforty, $oneeighty)
            ON CONFLICT(MatchId, PlayerId) DO UPDATE SET ThreeDartAverage=$avg, LegsPlayed=$legs, HighFinish=$finish, ShortLeg=$short, Scores80=$eighty, Scores100=$hundred, Scores140=$oneforty, Scores180=$oneeighty;
            """;
        command.Parameters.AddWithValue("$match", matchId); command.Parameters.AddWithValue("$player", playerId); command.Parameters.AddWithValue("$avg", (object?)stats.ThreeDartAverage ?? DBNull.Value); command.Parameters.AddWithValue("$legs", stats.LegsPlayed); command.Parameters.AddWithValue("$finish", (object?)stats.HighFinish ?? DBNull.Value); command.Parameters.AddWithValue("$short", (object?)stats.ShortLeg ?? DBNull.Value); command.Parameters.AddWithValue("$eighty", stats.Scores80); command.Parameters.AddWithValue("$hundred", stats.Scores100); command.Parameters.AddWithValue("$oneforty", stats.Scores140); command.Parameters.AddWithValue("$oneeighty", stats.Scores180); command.ExecuteNonQuery();
    }

    public List<Standing> GetStandings(long seasonId)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = """
            WITH entries AS (
              SELECT HomePlayerId PlayerId, HomeScore ForLegs, AwayScore AgainstLegs FROM Matches WHERE SeasonId=$season AND HomeScore IS NOT NULL AND AwayScore IS NOT NULL
              UNION ALL SELECT AwayPlayerId, AwayScore, HomeScore FROM Matches WHERE SeasonId=$season AND HomeScore IS NOT NULL AND AwayScore IS NOT NULL
            )
            SELECT p.Name, COUNT(e.PlayerId), COALESCE(SUM(CASE WHEN e.ForLegs>e.AgainstLegs THEN 1 ELSE 0 END),0), COALESCE(SUM(CASE WHEN e.ForLegs<e.AgainstLegs THEN 1 ELSE 0 END),0), COALESCE(SUM(e.ForLegs),0), COALESCE(SUM(e.AgainstLegs),0), COALESCE(SUM(CASE WHEN e.ForLegs>e.AgainstLegs THEN 2 WHEN e.ForLegs=e.AgainstLegs THEN 1 ELSE 0 END),0)
            FROM Players p LEFT JOIN entries e ON e.PlayerId=p.Id WHERE p.SeasonId=$season GROUP BY p.Id, p.Name ORDER BY 7 DESC, (5-6) DESC, 5 DESC, p.Name;
            """;
        command.Parameters.AddWithValue("$season", seasonId); using var reader = command.ExecuteReader(); var standings = new List<Standing>(); var position = 1;
        while (reader.Read()) standings.Add(new Standing { Position = position++, Player = reader.GetString(0), Played = reader.GetInt32(1), Won = reader.GetInt32(2), Lost = reader.GetInt32(3), LegsFor = reader.GetInt32(4), LegsAgainst = reader.GetInt32(5), Points = reader.GetInt32(6) });
        return standings;
    }

    public List<PlayerStatistics> GetPlayerStatistics(long seasonId)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.Name, AVG(s.ThreeDartAverage), COUNT(s.MatchId), COALESCE(SUM(s.LegsPlayed),0), MAX(s.HighFinish), MIN(s.ShortLeg), COALESCE(SUM(s.Scores80),0), COALESCE(SUM(s.Scores100),0), COALESCE(SUM(s.Scores140),0), COALESCE(SUM(s.Scores180),0)
            FROM Players p LEFT JOIN PlayerMatchStats s ON s.PlayerId=p.Id WHERE p.SeasonId=$season GROUP BY p.Id, p.Name ORDER BY p.Name;
            """;
        command.Parameters.AddWithValue("$season", seasonId); using var reader = command.ExecuteReader(); var stats = new List<PlayerStatistics>();
        while (reader.Read()) stats.Add(new PlayerStatistics { Player = reader.GetString(0), Average = reader.IsDBNull(1) ? null : reader.GetDouble(1), Games = reader.GetInt32(2), Legs = reader.GetInt32(3), HighFinish = reader.IsDBNull(4) ? null : reader.GetInt32(4), ShortLeg = reader.IsDBNull(5) ? null : reader.GetInt32(5), Scores80 = reader.GetInt32(6), Scores100 = reader.GetInt32(7), Scores140 = reader.GetInt32(8), Scores180 = reader.GetInt32(9) });
        return stats;
    }
}

public static class LeagueScheduler
{
    public static List<LeagueMatch> Build(IReadOnlyCollection<Player> players, int meetings, DateTime firstMatchday, DayOfWeek matchday)
    {
        var fixtures = new List<LeagueMatch>(); var nextRound = 1;
        var firstDate = firstMatchday.Date;
        while (firstDate.DayOfWeek != matchday) firstDate = firstDate.AddDays(1);
        for (var cycle = 0; cycle < meetings; cycle++)
        {
            var rotation = players.OrderBy(_ => Random.Shared.Next()).ToList();
            if (rotation.Count % 2 != 0) rotation.Add(new Player { Id = -1, Name = "BYE" });
            for (var round = 0; round < rotation.Count - 1; round++)
            {
                for (var i = 0; i < rotation.Count / 2; i++)
                {
                    var left = rotation[i]; var right = rotation[rotation.Count - 1 - i];
                    if (left.Id >= 0 && right.Id >= 0)
                    {
                        var reverse = (round + cycle) % 2 == 1;
                        fixtures.Add(new LeagueMatch { RoundNumber = nextRound, HomePlayerId = reverse ? right.Id : left.Id, AwayPlayerId = reverse ? left.Id : right.Id, MatchDate = firstDate.AddDays((nextRound - 1) * 7) });
                    }
                }
                var last = rotation[^1]; rotation.RemoveAt(rotation.Count - 1); rotation.Insert(1, last); nextRound++;
            }
        }
        return fixtures;
    }
}
