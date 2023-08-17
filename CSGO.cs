using BattleBitAPI.Common;

namespace CommunityServerAPI;

public class Csgo : GameMode
{
    public Csgo(MyGameServer r) : base(r)
    {
        Name = "CSGO";
    }

    public override void Init()
    {
        ServerSettings.PlayerCollision = true;
        ServerSettings.FriendlyFireEnabled = true;
        if (Gamemode != "Rush")
        {
            // dunno
        }
    }
    //Buy system maybe

    public override Task OnPlayerSpawned(MyPlayer player)
    {
        return base.OnPlayerSpawned(player);
    }

    public override Task<OnPlayerSpawnArguments> OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        return base.OnPlayerSpawning(player, request);
    }

    public override Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<MyPlayer> args)
    {
        var victim = args.Victim;
        var killer = args.Killer;
        victim.Modifications.CanDeploy = false;
        victim.Modifications.CanSpectate = false;
        victim.Kill();
        return base.OnAPlayerDownedAnotherPlayer(args);
    }

    public override void Reset()
    {
        R.ServerSettings.PlayerCollision = false;
        R.ServerSettings.FriendlyFireEnabled = false;
        base.Reset();
    }
}