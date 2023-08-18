using BattleBitAPI.Common;

namespace CommunityServerAPI;

public class Swap : GameMode
{
    public Swap(MyGameServer r) : base(r)
    {
        Name = "Swappers";
    }

    public override Returner OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        player.Modifications.RunningSpeedMultiplier = 1.25f;
        player.Modifications.FallDamageMultiplier = 0f;
        player.Modifications.JumpHeightMultiplier = 1.5f;
        return base.OnPlayerSpawning(player, request);
    }

    public override OnPlayerKillArguments<MyPlayer> OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<MyPlayer> onPlayerKillArguments)
    {
        
            var victimPos = onPlayerKillArguments.VictimPosition;
            onPlayerKillArguments.Killer.Teleport(victimPos); // Non functional for now
            onPlayerKillArguments.Victim.Kill();
            return base.OnAPlayerDownedAnotherPlayer(onPlayerKillArguments);
    }
}