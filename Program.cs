﻿using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;

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

public class MyPlayer : Player<MyPlayer>
{
    public int Level;
}

internal class MyGameServer : GameServer<MyPlayer>
{
    private readonly List<Weapon> mGunGame = new()
    {
        Weapons.Glock18,
        Weapons.Groza,
        Weapons.ACR,
        Weapons.AK15,
        Weapons.AK74,
        Weapons.G36C,
        Weapons.HoneyBadger,
        Weapons.KrissVector,
        Weapons.L86A1,
        Weapons.L96,
        Weapons.M4A1,
        Weapons.M9,
        Weapons.M110,
        Weapons.M249,
        Weapons.MK14EBR,
        Weapons.MK20,
        Weapons.MP7,
        Weapons.PP2000,
        Weapons.SCARH,
        Weapons.SSG69
    };

    // Gun Game
//    public override async Task OnPlayerSpawned(MyPlayer player)
//    {
//        UpdateWeapon(player);
//        player.SetRunningSpeedMultiplier(1.25f);
//        player.SetFallDamageMultiplier(0f);
//        player.SetJumpMultiplier(1.5f);
//    }

    public int GetGameLenght()
    {
        return mGunGame.Count;
    }

    public void UpdateWeapon(MyPlayer player)
    {
        if (player.Level < mGunGame.Count)
        {
            var w = new WeaponItem
            {
                ToolName = mGunGame[player.Level].Name,
                MainSight = Attachments.RedDot
            };
            player.SetPrimaryWeapon(w, 10, true);
        }
    }

    public override async Task OnAPlayerKilledAnotherPlayer(OnPlayerKillArguments<MyPlayer> onPlayerKillArguments)
    {
        var killer = onPlayerKillArguments.Killer;
        var victim = onPlayerKillArguments.Victim;
        killer.Level++;
        if (killer.Level == GetGameLenght()) AnnounceShort($"{killer.Name} only needs 1 more Kill");
        if (killer.Level > GetGameLenght())
        {
            AnnounceShort($"{killer.Name} won the Game");
            ForceEndGame();
        }

        if (onPlayerKillArguments.KillerTool == "Sledge Hammer" && victim.Level != 0) victim.Level--;
        UpdateWeapon(killer);
    }

    public override Task OnRoundEnded()
    {
        foreach (var player in AllPlayers) player.Level = 0;
        return base.OnRoundEnded();
    }


//basic Functionality
    public override Task OnConnected()
    {
        Console.WriteLine("Server connected");
        ServerSettings.BleedingEnabled = false;
        return base.OnConnected();
    }

    public override Task OnDisconnected()
    {
        Console.WriteLine("Server disconnected");
        return base.OnDisconnected();
    }

    public override Task OnPlayerConnected(MyPlayer player)
    {
        Console.WriteLine($"{player.Name} connected");

        return base.OnPlayerConnected(player);
    }

    public override Task OnPlayerDisconnected(MyPlayer player)
    {
        Console.WriteLine($"{player.Name} disconnected");
        return base.OnPlayerDisconnected(player);
    }


    public override Task<bool> OnPlayerTypedMessage(MyPlayer player, ChatChannel channel, string msg)
    {
        if (msg.StartsWith("start")) ForceStartGame();

        return base.OnPlayerTypedMessage(player, channel, msg);
    }

    public override Task<PlayerStats> OnGetPlayerStats(ulong steamID, PlayerStats officialStats)
    {
        officialStats.Progress.Rank = 200;
        officialStats.Progress.Prestige = 10;
        return base.OnGetPlayerStats(steamID, officialStats);
    }

    public override Task OnSavePlayerStats(ulong steamID, PlayerStats stats)
    {
        return base.OnSavePlayerStats(steamID, stats);
    }
}