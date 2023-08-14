using BattleBitAPI.Common;
using BattleBitAPI.Server;

namespace CommunityServerAPI;

public class MeleeOnly : GameServer<MyPlayer>
{
    public override Task<OnPlayerSpawnArguments> OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        player.SetLightGadget("Pickaxe", 0, true);
        player.SetRunningSpeedMultiplier(1.25f);
        player.SetFallDamageMultiplier(0f);
        player.SetJumpMultiplier(1.5f);
        return base.OnPlayerSpawning(player, request);
    }
}