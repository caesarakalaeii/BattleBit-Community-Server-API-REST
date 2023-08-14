namespace CommunityServerAPI.server_utilities;

public class Hardcore : GameMode
{
    public Hardcore()
    {
        Name = "Hardcore";
    }

    public override Task OnPlayerSpawned(MyPlayer player)
    {
        ServerSettings.HitMarkersEnabled = false;
        player.SetRunningSpeedMultiplier(1.25f);
        player.SetFallDamageMultiplier(2f);
        player.SetHP(50);
        player.SetGiveDamageMultiplier(2f);

        return base.OnPlayerSpawned(player);
    }
}