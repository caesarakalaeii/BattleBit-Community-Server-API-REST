﻿using BattleBitAPI.Common;

namespace CommunityServerAPI;

public class Swap : GameMode
{
    public Swap()
    {
        Name = "Swappers";
    }

    public override Task<OnPlayerSpawnArguments> OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        player.Modifications.RunningSpeedMultiplier = 1.25f;
        player.Modifications.FallDamageMultiplier = 0f;
        player.Modifications.JumpHeightMultiplier = 1.5f;
        return base.OnPlayerSpawning(player, request);
    }

    public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<MyPlayer> onPlayerKillArguments)
    {
        await Task.Run(() =>
        {
            var victimPos = onPlayerKillArguments.VictimPosition;
            onPlayerKillArguments.Killer.Teleport(victimPos); // Non functional for now
            onPlayerKillArguments.Victim.Kill();
        });
    }

    public override void Reset()
    {
        foreach (var player in AllPlayers) player.Kill();
    }
}