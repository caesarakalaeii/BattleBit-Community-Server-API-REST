﻿using BattleBitAPI.Common;

namespace CommunityServerAPI;

public class MeleeOnly : GameMode
{
    public MeleeOnly(MyGameServer r) : base(r)
    {
        Name = "MeleeOnly";
    }

    public override Returner OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        player.SetLightGadget("Pickaxe", 0, true);
        player.Modifications.RunningSpeedMultiplier = 1.25f;
        player.Modifications.FallDamageMultiplier = 0f;
        player.Modifications.JumpHeightMultiplier = 1.5f;
        return base.OnPlayerSpawning(player, request);
    }
}