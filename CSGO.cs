using BattleBitAPI.Common;

namespace CommunityServerAPI;

public class CSGO : GameMode
{
    public CSGO(MyGameServer r) : base(r)
    {
        Name = "CSGO";
    }

    public override void Init()
    {
        R.ServerSettings.PlayerCollision = true;
        R.ServerSettings.FriendlyFireEnabled = true;
        if (Gamemode != "Rush")
        {
            // dunno
        }

        base.Init();
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