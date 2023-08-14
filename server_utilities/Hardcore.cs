namespace CommunityServerAPI.server_utilities;

public class Hardcore : GameMode
{
    public Hardcore()
    {
        Name = "Hardcore";
    }

    public override Task OnPlayerSpawned(MyPlayer player)
    {
        player.SetRunningSpeedMultiplier(1.25f);
        player.SetFallDamageMultiplier(2f);
        player.SetHP(50);
        player.SetGiveDamageMultiplier(2f);
        if (player.SteamID == 76561198053896127) player.SetReceiveDamageMultiplier(0f);
        return base.OnPlayerSpawned(player);
    }
}