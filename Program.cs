using System.Text.Json;
using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using CommunityServerAPI;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Starting API");
        var listener = new ServerListener<MyPlayer, MyGameServer>();
        listener.Start(55669);
        Thread.Sleep(-1);
    }
}

public class GameMode : GameServer<MyPlayer>
{
    public string Name = string.Empty;
}

public class MyPlayer : Player<MyPlayer>
{
    public bool IsAdmin;
    public bool IsStreamer;
    public int Level;
}

public class MyGameServer : GameServer<MyPlayer>
{
    private readonly List<APICommand> ChatCommands = new()
    {
        new HealCommand(),
        new KillCommand(),
        new TeleportCommand(),
        new GrenadeCommand(),
        new ForceStartCommand(),
        new SpeedCommand(),
        new HelpCommand(),
        new RevealCommand(),
        new ChangeDamageCommand(),
        new ChangeReceivedDamageCommand(),
        new ChangeAmmoCommand(),
        new SetStreamerCommand(),
        new RemoveStreamerCommand(),
        new OpCommand(),
        new DeopCommand(),
        new NextGameModeCommand()
    };


    private readonly string mAdminJson = "./config/admins.json";
    private readonly List<ulong> mAdmins = new();

    private readonly List<GameMode> mGameModes = new()
    {
        new LifeSteal(),
        new Swap(),
        new MeleeOnly(),
        new GunGame()
    };

    //public CommandQueue queue = new();
    private readonly List<ulong> mListedStreamers = new();
    private readonly string mSteamIdJson = "./config/streamer_steamids.json";

    private GameMode mCurrentGameMode = new GunGame();
    private int mGameModeIndex;

    //modular GameModes: CHECK if new Gamemodes need more passthrough


    public override Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<MyPlayer> args)
    {
        return mCurrentGameMode.OnAPlayerDownedAnotherPlayer(args);
    }

    public override Task OnPlayerGivenUp(MyPlayer player)
    {
        return mCurrentGameMode.OnPlayerGivenUp(player);
    }


    public override Task OnPlayerSpawned(MyPlayer player)
    {
        return mCurrentGameMode.OnPlayerSpawned(player);
    }

    public override Task<OnPlayerSpawnArguments> OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        return mCurrentGameMode.OnPlayerSpawning(player, request);
    }

    public override Task OnRoundEnded()
    {
        mGameModeIndex = (mGameModeIndex + 1) % mGameModes.Count;
        return mCurrentGameMode.OnRoundEnded();
    }

    public override Task OnRoundStarted()
    {
        mCurrentGameMode = mGameModes[mGameModeIndex];
        return mCurrentGameMode.OnRoundStarted();
    }

    //basic Functionality

    public override Task OnDisconnected()
    {
        Console.WriteLine("Server disconnected");
        return base.OnDisconnected();
    }


    public override Task OnPlayerDisconnected(MyPlayer player)
    {
        Console.WriteLine($"{player.Name} disconnected");
        return mCurrentGameMode.OnPlayerDisconnected(player);
    }


    public override Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args)
    {
        args.Stats.Progress.Rank = 200;
        args.Stats.Progress.Prestige = 10;
        return mCurrentGameMode.OnPlayerJoiningToServer(steamID, args);
    }

    public override async Task<bool> OnPlayerTypedMessage(MyPlayer player, ChatChannel channel, string msg)
    {
        if (!player.IsAdmin) return true;
        var splits = msg.Split(" ");
        foreach (var command in ChatCommands)
            if (splits[0] == command.CommandPrefix)
            {
                var c = command.ChatCommand(player, channel, msg);
                await HandleCommand(c);
                return false;
            }

        return await mCurrentGameMode.OnPlayerTypedMessage(player, channel, msg);
    }


    public void SaveStreamers()
    {
        try
        {
            var newJson =
                JsonSerializer.Serialize(mListedStreamers, new JsonSerializerOptions { WriteIndented = true });

            // Write the JSON to the file, overwriting its content
            File.WriteAllText(mSteamIdJson, newJson);

            Console.WriteLine("Steam IDs updated and saved to the file.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Steam IDs couldn't be updated and saved to the file." + ex);
        }
    }

    public void SaveAdmins()
    {
        try
        {
            var newJson =
                JsonSerializer.Serialize(mAdmins, new JsonSerializerOptions { WriteIndented = true });

            // Write the JSON to the file, overwriting its content
            File.WriteAllText(mAdminJson, newJson);

            Console.WriteLine("Admins updated and saved to the file.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Admins couldn't be updated and saved to the file." + ex);
        }
    }

    public override async Task OnConnected()
    {
        mCurrentGameMode = mGameModes[0];
        await Console.Out.WriteLineAsync(GameIP + " Connected");
        await Console.Out.WriteLineAsync("Fetching configs");
        try
        {
            // Read the entire JSON file as a string
            var jsonFilePath = mSteamIdJson;
            var jsonString = File.ReadAllText(jsonFilePath);

            // Parse the JSON array using System.Text.Json
            var steamIds = JsonSerializer.Deserialize<ulong[]>(jsonString);
            foreach (var steamId in steamIds) mListedStreamers.Add(steamId);
            await Console.Out.WriteLineAsync("Fetching streamers succeeded");
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync("Fetching streamers failed: " + ex);
        }

        try
        {
            // Read the entire JSON file as a string
            var jsonFilePath = mAdminJson;
            var jsonString = File.ReadAllText(jsonFilePath);

            // Parse the JSON array using System.Text.Json
            var steamIds = JsonSerializer.Deserialize<ulong[]>(jsonString);
            foreach (var steamId in steamIds) mAdmins.Add(steamId);
            await Console.Out.WriteLineAsync("Fetching admins succeeded");
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync("Fetching admins failed: " + ex);
        }
    }


    public override async Task<bool> OnPlayerConnected(MyPlayer player)
    {
        await Console.Out.WriteLineAsync(player.Name + " Connected");
        if (!mListedStreamers.Contains(player.SteamID)) return true;

        player.IsStreamer = true;
        if (!mAdmins.Contains(player.SteamID)) return true;

        player.IsAdmin = true;
        return true;
    }

    public override async Task OnTick()
    {
        foreach (var player in AllPlayers) SayToChat($"{player.Name} HP: {player.HP}");
    }

    public async Task HandleCommand(Command c)
    {
        // need testing if blocking
        foreach (var player in AllPlayers)
        {
            if (!player.IsStreamer) continue;
            if (player.SteamID != c.StreamerId) continue;
            switch (c.Action)
            {
                case ActionType.Heal:
                {
                    player.Heal(c.Amount);
                    player.Message($"{c.ExecutorName} has healed you for {c.Amount}");
                    break;
                }
                case ActionType.Kill:
                {
                    player.Kill();
                    player.Message($"{c.ExecutorName} has killed you");
                    break;
                }
                case ActionType.Grenade:
                {
                    //can't get player pos right now   
                    player.Message($"{c.ExecutorName} has spawned a grenade on you");
                    break;
                }
                case ActionType.Teleport:
                {
                    //relative teleport????
                    player.Message($"{c.ExecutorName} has teleported you {c.Data}");
                    break;
                }
                case ActionType.Speed:
                {
                    player.SetRunningSpeedMultiplier(c.Amount);
                    player.Message($"{c.ExecutorName} has set your speed to {c.Amount}x");
                    break;
                }
                case ActionType.Reveal:
                {
                    //set marker on Map
                    player.Message($"{c.ExecutorName} has revealed your Position");
                    break;
                }
                case ActionType.ChangeAmmo:
                {
                    player.Message($"{c.ExecutorName} has set your Ammo to {c.Amount}");
                    break;
                }
                case ActionType.Start:
                {
                    player.Message("Forcing start");
                    ForceStartGame();
                    break;
                }
                case ActionType.Help:
                {
                    foreach (var command in ChatCommands) SayToChat($"{command.CommandPrefix} {command.Help}");
                    break;
                }
                case ActionType.ChangeDamage:
                {
                    player.SetGiveDamageMultiplier(c.Amount);
                    player.Message($"{c.ExecutorName} has set your Dmg Multiplier to {c.Amount}");
                    break;
                }
                case ActionType.ChangeReceivedDamage:
                {
                    player.SetReceiveDamageMultiplier(c.Amount);
                    player.Message($"{c.ExecutorName} has set your recieve Dmg Multiplier to {c.Amount}");
                    break;
                }
                case ActionType.SetStreamer:
                {
                    player.Message("You are now a streamer", 2f);
                    player.IsStreamer = true;
                    SaveStreamers();
                    break;
                }
                case ActionType.RemoveStreamer:
                {
                    player.Message("You are no longer a streamer", 2f);
                    player.IsStreamer = false;
                    SaveStreamers();
                    break;
                }
                case ActionType.GrantOP:
                {
                    player.Message("You are now an admin", 2f);
                    player.IsAdmin = true;
                    SaveAdmins();
                    break;
                }
                case ActionType.RevokeOP:
                {
                    player.Message("You are no longer an Andmin", 2f);
                    player.IsAdmin = false;
                    SaveAdmins();
                    break;
                }
                case ActionType.NextGameMode:
                {
                    mGameModeIndex = (mGameModeIndex + 1) % mGameModes.Count;
                    mCurrentGameMode = mGameModes[mGameModeIndex];
                    AnnounceShort($"GameMode is now {mCurrentGameMode.Name}");
                    Console.WriteLine($"GameMode is now {mCurrentGameMode.Name}");
                    break;
                }
                // Add more cases for other ActionType values as needed
            }
        }
    }

/*
 set admin role
    if (steamID == ID)
    {
        stats.Roles = Roles.Admin;
    }
    */
    public override Task OnSavePlayerStats(ulong steamID, PlayerStats stats)
    {
        return base.OnSavePlayerStats(steamID, stats);
    }
}