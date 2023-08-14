﻿using BattleBitAPI.Common;

namespace CommunityServerAPI;

public class LifeSteal : GameMode
{
    public LifeSteal()
    {
        Name = "LifeSteal";
    }

    public override Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<MyPlayer> args)
    {
        args.Killer.SetHP(100);
        args.Victim.Kill();
        return base.OnAPlayerDownedAnotherPlayer(args);
    }

    public override Task OnPlayerSpawned(MyPlayer player)
    {
        player.SetRunningSpeedMultiplier(1.25f);
        player.SetFallDamageMultiplier(0f);
        player.SetJumpMultiplier(1.5f);
        if (player.SteamID == 76561198053896127) player.SetReceiveDamageMultiplier(0f);
        return base.OnPlayerSpawned(player);
    }
}