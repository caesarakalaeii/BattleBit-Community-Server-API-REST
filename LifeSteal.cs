using BattleBitAPI.Common;

namespace CommunityServerAPI;

public class LifeSteal : GameMode
{
    public LifeSteal(MyGameServer r) : base(r)
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
        player.Modifications.RunningSpeedMultiplier = 1.25f;
        player.Modifications.FallDamageMultiplier = 0f;
        player.Modifications.JumpHeightMultiplier = 1.5f;
        player.Modifications.DisableBleeding();
        return base.OnPlayerSpawned(player);
    }


}