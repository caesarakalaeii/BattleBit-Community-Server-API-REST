using System.Text.Json;
using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using CommunityServerAPI;
using CommunityServerAPI.server_utilities;

internal class MyProgram
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Starting API");
        var listener = new ServerListener<MyPlayer, MyGameServer>();
        listener.Start(55669);
        Thread.Sleep(-1);
    }
}


public class MyPlayer : Player<MyPlayer>
{
    public int Level;
}

public class MyGameServer : GameServer<MyPlayer>
{
    private const string AdminJson = "./config/admins.json";
    private const string SteamIdJson = "./config/streamer_steamids.json";
    private readonly List<ulong> mAdmins = new();

    private readonly List<APICommand> mChatCommands = new()
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
        new NextGameModeCommand(),
        new SetGameModeCommand(),
        new GetGameModeCommand(),
        new TogglePlaylistCommand()
    };

    private readonly List<ulong> mListedStreamers = new();

    private GameMode mCurrentGameMode;

    private bool mCyclePlaylist;
    private int mGameModeIndex;

    private readonly List<GameMode> mGameModes;


    //public CommandQueue queue = new();
    public MyGameServer()
    {
        mGameModes = new List<GameMode>
        {
            new GunGame(this),
            new TeamGunGame(this),
            new LifeSteal(this),
            new Swap(this),
            new Hardcore(this),
            new MeleeOnly(this),
            new Csgo(this)
        };
        mGameModeIndex = 0;
        mCurrentGameMode = mGameModes[mGameModeIndex];
        FetchStreamers();
        FetchAdmins();
    }

    public override async Task OnConnected()
    {
        await Console.Out.WriteLineAsync(GameIP + " Connected");
    }

    public override Task OnReconnected()
    {
        Console.Out.WriteLine($"Current GameMode: {mCurrentGameMode.Name}");
        return base.OnReconnected();
    }


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
        if (mCyclePlaylist) mGameModeIndex = (mGameModeIndex + 1) % mGameModes.Count;
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


    public override Task OnPlayerJoiningToServer(ulong steamId, PlayerJoiningArguments args)
    {
        args.Stats.Progress.Rank = 200;
        args.Stats.Progress.Prestige = 10;

        return mCurrentGameMode.OnPlayerJoiningToServer(steamId, args);
    }


    public override async Task<bool> OnPlayerTypedMessage(MyPlayer player, ChatChannel channel, string msg)
    {
        if (msg.StartsWith("!fetch"))
        {
            player.Message("Fetching Admins and Streamers", 2f);
            FetchStreamers();
            FetchAdmins();
        }

        if (!mAdmins.Contains(player.SteamID)) return true;
        var splits = msg.Split(" ");
        foreach (var command in mChatCommands)
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
            File.WriteAllText(SteamIdJson, newJson);

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
            File.WriteAllText(AdminJson, newJson);

            Console.WriteLine("Admins updated and saved to the file.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Admins couldn't be updated and saved to the file." + ex);
        }
    }

    public void FetchStreamers()
    {
        Console.Out.WriteLine("Fetching configs");
        try
        {
            // Read the entire JSON file as a string
            var jsonFilePath = SteamIdJson;
            var jsonString = File.ReadAllText(jsonFilePath);

            // Parse the JSON array using System.Text.Json
            var steamIds = JsonSerializer.Deserialize<ulong[]>(jsonString);
            foreach (var steamId in steamIds) mListedStreamers.Add(steamId);
            Console.Out.WriteLine("Fetching streamers succeeded");
        }
        catch (Exception ex)
        {
            Console.Out.WriteLine("Fetching streamers failed: " + ex);
        }
    }

    public void FetchAdmins()
    {
        try
        {
            // Read the entire JSON file as a string
            var jsonFilePath = AdminJson;
            var jsonString = File.ReadAllText(jsonFilePath);

            // Parse the JSON array using System.Text.Json
            var steamIds = JsonSerializer.Deserialize<ulong[]>(jsonString);
            foreach (var steamId in steamIds) mAdmins.Add(steamId);
            Console.Out.WriteLine("Fetching admins succeeded");
        }
        catch (Exception ex)
        {
            Console.Out.WriteLine("Fetching admins failed: " + ex);
        }
    }


    public override async Task<bool> OnPlayerConnected(MyPlayer player)
    {
        await Console.Out.WriteLineAsync(player.Name + " Connected");
        if (!mListedStreamers.Contains(player.SteamID)) return true;
        player.Message($"Current GameMode is: {mCurrentGameMode.Name}", 4f);

        return true;
    }


    public async Task HandleCommand(Command c)
    {
        // need testing if blocking
        foreach (var player in AllPlayers)
        {
            if (!mListedStreamers.Contains(player.SteamID)) continue;
            if (player.SteamID != c.StreamerId) continue;
            switch (c.Action)
            {
                case ActionType.Heal:
                {
                    player.Heal(c.Amount);
                    player.Message($"{c.ExecutorName} has healed you for {c.Amount}", 2f);
                    break;
                }
                case ActionType.Kill:
                {
                    player.Kill();
                    player.Message($"{c.ExecutorName} has killed you", 2f);
                    break;
                }
                case ActionType.Grenade:
                {
                    //can't spawn stuff, also no playerPOS 
                    player.Message($"{c.ExecutorName} has spawned a grenade on you", 2f);
                    break;
                }
                case ActionType.Teleport:
                {
                    //relative teleport????
                    player.Message($"{c.ExecutorName} has teleported you {c.Data}", 2f);
                    break;
                }
                case ActionType.Speed:
                {
                    player.Modifications.RunningSpeedMultiplier = c.Amount;
                    player.Message($"{c.ExecutorName} has set your speed to {c.Amount}x", 2f);
                    break;
                }
                case ActionType.Reveal:
                {
                    //set marker on Map
                    player.Message($"{c.ExecutorName} has revealed your Position", 2f);
                    break;
                }
                case ActionType.ChangeAmmo:
                {
                    player.Message($"{c.ExecutorName} has set your Ammo to {c.Amount}", 2f);
                    break;
                }
                case ActionType.Start:
                {
                    player.Message("Forcing start", 2f);
                    ForceStartGame();
                    break;
                }
                case ActionType.Help:
                {
                    foreach (var command in mChatCommands) SayToChat($"{command.CommandPrefix} {command.Help}");
                    break;
                }
                case ActionType.ChangeDamage:
                {
                    player.Modifications.GiveDamageMultiplier = c.Amount;
                    player.Message($"{c.ExecutorName} has set your Dmg Multiplier to {c.Amount}", 2f);
                    break;
                }
                case ActionType.ChangeReceivedDamage:
                {
                    player.Modifications.ReceiveDamageMultiplier = c.Amount;
                    player.Message($"{c.ExecutorName} has set your recieve Dmg Multiplier to {c.Amount}", 2f);
                    break;
                }
                case ActionType.SetStreamer:
                {
                    player.Message("You are now a streamer", 2f);
                    mListedStreamers.Add(player.SteamID);
                    SaveStreamers();
                    break;
                }
                case ActionType.RemoveStreamer:
                {
                    player.Message("You are no longer a streamer", 2f);
                    mListedStreamers.Remove(player.SteamID);
                    SaveStreamers();
                    break;
                }
                case ActionType.GrantOp:
                {
                    player.Message("You are now an admin", 2f);
                    mAdmins.Add(player.SteamID);
                    SaveAdmins();
                    break;
                }
                case ActionType.RevokeOp:
                {
                    player.Message("You are no longer an Admin", 2f);
                    mAdmins.Remove(player.SteamID);
                    SaveAdmins();
                    break;
                }
                case ActionType.NextGameMode:
                {
                    mCurrentGameMode.Reset();
                    mGameModeIndex = (mGameModeIndex + 1) % mGameModes.Count;
                    mCurrentGameMode = mGameModes[mGameModeIndex];
                    mCurrentGameMode.Init();
                    AnnounceShort($"GameMode is now {mCurrentGameMode.Name}");
                    Console.WriteLine($"GameMode is now {mCurrentGameMode.Name}");
                    break;
                }
                case ActionType.SetGameMode:
                {
                    try
                    {
                        mCurrentGameMode.Reset();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR resetting GM: {ex}");
                    }

                    foreach (var gameMode in mGameModes)
                        if (gameMode.Name == c.ExecutorName)
                        {
                            mCurrentGameMode = gameMode;
                            mGameModeIndex = mGameModes.IndexOf(gameMode);
                        }

                    try
                    {
                        mCurrentGameMode.Init();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR initializing GM: {ex}");
                    }

                    AnnounceShort($"GameMode is now {mCurrentGameMode.Name}");
                    break;
                }
                case ActionType.GetGameMode:
                {
                    AnnounceShort($"GameMode is {mCurrentGameMode.Name}");
                    break;
                }
                case ActionType.TogglePlaylist:
                {
                    mCyclePlaylist = !mCyclePlaylist;
                    AnnounceShort($"GameModePlaylist is now {mCyclePlaylist}");
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
}