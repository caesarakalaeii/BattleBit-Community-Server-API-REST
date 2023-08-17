using BattleBitAPI.Server;

namespace CommunityServerAPI.GameModes;

public class Hardcore : GameMode
{
    public Hardcore(GameServer<MyPlayer> reference) : base(reference)
    {
        Name = "Hardcore";
    }

    public override Task OnPlayerSpawned(MyPlayer player)
    {
        player.Modifications.HitMarkersEnabled = false;
        player.Modifications.RunningSpeedMultiplier = 1.25f;
        player.Modifications.FallDamageMultiplier = 2f;
        player.Modifications.GiveDamageMultiplier = 2f;
        player.SetHP(50);
        return base.OnPlayerSpawned(player);
    }
}