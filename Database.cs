using System;
using Blaze2SDK.Blaze.GameReporting;
using NLog;
using Npgsql;

namespace Zamboni;

public class Database
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly string connectionString = Program.ZamboniConfig.DatabaseConnectionString;
    public readonly bool isEnabled;

    private uint fallbackGameIdCounter = 1;

    public Database()
    {
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            isEnabled = true;
            Logger.Warn("Database is accessible.");
        }
        catch (Exception ex)
        {
            isEnabled = false;
            Logger.Warn("Database is not accessible. Gamedata wont be saved");
            Logger.Warn(ex.Message);
            return;
        }

        CreateGameIdSequence();
        CreateGamesTable();
        CreateReportTable();
    }

    private void CreateGameIdSequence()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        const string createSequenceQuery = @"
            CREATE SEQUENCE IF NOT EXISTS zamboni_game_id_seq
            START 1
            INCREMENT 1;
        ";

        using var cmd = new NpgsqlCommand(createSequenceQuery, conn);
        cmd.ExecuteNonQuery();
    }

    private void CreateGamesTable()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS games (
                    game_id BIGINT PRIMARY KEY,
                    fnsh BOOLEAN,
                    gtyp INTEGER,
                    venue INTEGER,
                    ""time"" INTEGER,
                    sku INTEGER,
                    skil INTEGER,
                    shootout INTEGER,
                    pnum INTEGER,
                    plen INTEGER,
                    ot INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    private void CreateReportTable()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS reports (
                    game_id BIGINT NOT NULL,
                    user_id BIGINT NOT NULL,
                    weight INTEGER,
                    voipstrt1 INTEGER,
                    voipstrt0 INTEGER,
                    voipend1 INTEGER,
                    voipend0 INTEGER,
                    usersstrt1 INTEGER,
                    usersstrt0 INTEGER,
                    usersend1 INTEGER,
                    usersend0 INTEGER,
                    toa INTEGER,
                    team_name VARCHAR,
                    team INTEGER,
                    shots INTEGER,
                    shg INTEGER,
                    score INTEGER,
                    quit INTEGER,
                    pshgoal INTEGER,
                    pshchance INTEGER,
                    ppo INTEGER,
                    ppg INTEGER,
                    pktloss INTEGER,
                    penmin INTEGER,
                    passcomp INTEGER,
                    passchance INTEGER,
                    onetgoal INTEGER,
                    onetchance INTEGER,
                    latesdevnet INTEGER,
                    latesdevgm INTEGER,
                    latelownet INTEGER,
                    latelowgm INTEGER,
                    latehinet INTEGER,
                    latehigm INTEGER,
                    lateavgnet INTEGER,
                    lateavggm INTEGER,
                    home INTEGER,
                    hits INTEGER,
                    guests1 INTEGER,
                    guests0 INTEGER,
                    grptver VARCHAR,
                    grpttype INTEGER,
                    gresult INTEGER,
                    gendphase INTEGER,
                    gdesyncrsn INTEGER,
                    gdesyncend INTEGER,
                    gamertag VARCHAR,
                    fpslow INTEGER,
                    fpshi INTEGER,
                    fpsdev INTEGER,
                    fpsavg INTEGER,
                    faceoff INTEGER,
                    dtime INTEGER,
                    dscore INTEGER,
                    droppkts INTEGER,
                    disc INTEGER,
                    cheat INTEGER,
                    bytessentnet INTEGER,
                    bytessentgm INTEGER,
                    bytesrcvdnet INTEGER,
                    bytesrcvdgm INTEGER,
                    blkshot INTEGER,
                    bkgoal INTEGER,
                    bkchance INTEGER,
                    bandlownet INTEGER,
                    bandlowgm INTEGER,
                    bandhinet INTEGER,
                    bandhigm INTEGER,
                    bandavgnet INTEGER,
                    bandavggm INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (game_id, user_id)
                );";

        using var cmd = new NpgsqlCommand(createTableQuery, conn);
        cmd.ExecuteNonQuery();
    }

    public void InsertReport(GameReport report)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        const string insertGameQuery = @"
            INSERT INTO games (
                game_id, fnsh, gtyp
            ) VALUES (
                @game_id, @fnsh, @gtyp
            )
            ON CONFLICT (game_id) DO NOTHING;";

        using var cmd = new NpgsqlCommand(insertGameQuery, conn);
        cmd.Parameters.AddWithValue("game_id", (long)report.mGameReportingId);
        cmd.Parameters.AddWithValue("fnsh", report.mFinished);
        cmd.Parameters.AddWithValue("gtyp", (long)report.mGameTypeId);
        cmd.ExecuteNonQuery();

        var gameAttributeMap = report.mAttributeMap;
        foreach (var key in gameAttributeMap.Keys)
        {
            var column = key.ToLower();
            var insertGameAttributeQuery = $@"
                INSERT INTO games (game_id, {column})
                    VALUES (@game_id, @value)
                ON CONFLICT (game_id) DO UPDATE
                    SET {column} = EXCLUDED.{column};";

            using var cmd1 = new NpgsqlCommand(insertGameAttributeQuery, conn);
            cmd1.Parameters.AddWithValue("game_id", (long)report.mGameReportingId);

            if (int.TryParse(gameAttributeMap[key], out var intValue))
                cmd1.Parameters.AddWithValue("value", intValue);
            else
                cmd1.Parameters.AddWithValue("value", gameAttributeMap[key]);
            cmd1.ExecuteNonQuery();
        }

        var mPlayerReportMap = report.mPlayerReportMap;
        foreach (var userId in mPlayerReportMap.Keys)
        {
            const string insertPlayerQuery = @"
                INSERT INTO reports (
                    game_id, user_id
                ) VALUES (
                    @game_id, @user_id
                )
                ON CONFLICT (game_id, user_id) DO NOTHING;";

            using var cmd1 = new NpgsqlCommand(insertPlayerQuery, conn);
            cmd1.Parameters.AddWithValue("game_id", (long)report.mGameReportingId);
            cmd1.Parameters.AddWithValue("user_id", (long)userId);
            cmd1.ExecuteNonQuery();
        }

        foreach (var userId in mPlayerReportMap.Keys)
        {
            var playerAttributeMap = mPlayerReportMap[userId].mAttributeMap;
            foreach (var key in playerAttributeMap.Keys)
            {
                var column = key.ToLower();
                var insertPlayerAttributeQuery = $@"
                    INSERT INTO reports (game_id, user_id, {column})
                        VALUES (@game_id, @user_id, @value)
                    ON CONFLICT (game_id, user_id) DO UPDATE
                        SET {column} = EXCLUDED.{column};";

                using var cmd1 = new NpgsqlCommand(insertPlayerAttributeQuery, conn);
                cmd1.Parameters.AddWithValue("game_id", (long)report.mGameReportingId);
                cmd1.Parameters.AddWithValue("user_id", (long)userId);

                if (int.TryParse(playerAttributeMap[key], out var intValue))
                    cmd1.Parameters.AddWithValue("value", intValue);
                else
                    cmd1.Parameters.AddWithValue("value", playerAttributeMap[key]);
                cmd1.ExecuteNonQuery();
            }
        }
    }

    public uint GetNextGameId()
    {
        if (!isEnabled) return fallbackGameIdCounter++;
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT nextval('zamboni_game_id_seq');", conn);
        var result = cmd.ExecuteScalar() ?? throw new InvalidOperationException("Failed to get next game ID.");
        var nextId = (long)result;
        if (nextId > uint.MaxValue) throw new OverflowException("Over 4 billion games played, what we do now?");
        return (uint)nextId;
    }
}