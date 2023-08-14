using BattleBitAPI.Common;

namespace CommunityServerAPI;

public class Swap : GameMode
{
    public Swap()
    {
        Name = "Swappers";
    }

    public override Task<OnPlayerSpawnArguments> OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        player.SetLightGadget("Pickaxe", 0, true);
        player.SetRunningSpeedMultiplier(1.25f);
        player.SetFallDamageMultiplier(0f);
        player.SetJumpMultiplier(1.5f);
        return base.OnPlayerSpawning(player, request);
    }

    public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<MyPlayer> onPlayerKillArguments)
    {
        await Task.Run(() =>
        {
            var victimPos = onPlayerKillArguments.VictimPosition;
            onPlayerKillArguments.Killer.Teleport(victimPos);
            onPlayerKillArguments.Victim.Kill();
        });
    }
}