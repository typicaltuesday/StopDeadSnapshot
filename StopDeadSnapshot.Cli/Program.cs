using Steamworks;
using Steamworks.Data;
using System.Text;

// Stop Dead
const int APP_ID = 2164030;

SteamClient.Init(APP_ID);

var maps = new[]
{
    "01_00",
    "01_01",
    "01_02",
    "01_03",
    "01_04",
    "01_05",
    "01_06",
    "01_07",
    "01_08",
    "01_09",
    "01_10",
    "01_11",
    "02_01",
    "02_02",
    "02_03",
    "02_04",
    "02_05",
    "02_06",
    "02_07",
    "02-08", // for whatever reason, this one uses a hyphen
    "02_09",
    "02_10",
    "02_11",
    "03_01",
    "03_02",
    "03_03",
    "03_04",
    "03_05",
    "03_06",
    "03_07",
    "03_08",
    "03_09",
    "03_10",
    "03_11",
    "04_01",
    "04_02",
    "04_03",
    "04_04",
    "04_05",
    "04_06",
    "04_07",
    "04_08",
    "04_09",
    "04_10"
};

var times = (await Task.WhenAll(maps.Select((map) => GetLeaderboardScoresAsync(map, LeaderboardType.Time, 50))))
    .SelectMany((runs) => runs)
    .OrderBy((run) => run.Map)
        .ThenBy((run) => run.Score);

var points = (await Task.WhenAll(maps.Select((map) => GetLeaderboardScoresAsync(map, LeaderboardType.Points, 50))))
    .SelectMany((runs) => runs)
    .OrderBy((run) => run.Map)
        .ThenByDescending((run) => run.Score);

var now = $"{DateTimeOffset.Now:u}".Replace(':', '-');

await WriteRunsToFileAsync($"./Stop Dead {now} Times.csv", LeaderboardType.Time, times);
File.Copy($"./Stop Dead {now} Times.csv", "./Stop Dead Latest Times.csv", true);

await WriteRunsToFileAsync($"./Stop Dead {now} Points.csv", LeaderboardType.Points, points);
File.Copy($"./Stop Dead {now} Points.csv", "./Stop Dead Latest Points.csv", true);

SteamClient.Shutdown();

Console.WriteLine("Finished");

async Task<IEnumerable<StopDeadRun>> GetLeaderboardScoresAsync(string map, LeaderboardType leaderboardType, int scoreCount)
{
    var leaderboardName = leaderboardType switch
    {
        LeaderboardType.Time => $"{map}_Times",
        LeaderboardType.Points => $"{map}_Points",
        _ => throw new NotImplementedException()
    };

    var leaderboardResult = await SteamUserStats.FindLeaderboardAsync(leaderboardName);
    if (leaderboardResult is not Leaderboard leaderboard)
    {
        Console.Error.WriteLine($"Failed to find leaderboard \"{leaderboardName}\"");
        return [];
    }

    var scoresResult = await leaderboard.GetScoresAsync(scoreCount);
    if (scoresResult is not LeaderboardEntry[] scores)
    {
        Console.Error.WriteLine($"Failed to get scores for \"{leaderboardName}\"");
        return [];
    }

    return scores.Select((score) => new StopDeadRun(map.Replace('-', '_'), score.Score, score.User.Id, score.User.Name));
}

async Task WriteRunsToFileAsync(string path, LeaderboardType leaderboardType, IEnumerable<StopDeadRun> runs)
{
    var builder = new StringBuilder();

    var header = leaderboardType switch
    {
        LeaderboardType.Time => "time_ms",
        LeaderboardType.Points => "points",
        _ => throw new NotImplementedException()
    };

    builder.AppendLine($"map,{header},steam_id_64,name");
    foreach (var run in runs)
    {
        builder.AppendLine($"{run.Map},{run.Score},{run.SteamID64},\"{run.Name}\"");
    }

    await File.WriteAllTextAsync(path, builder.ToString());
}

enum LeaderboardType
{
    Time,
    Points
}

record StopDeadRun(string Map, int Score, ulong SteamID64, string Name);
