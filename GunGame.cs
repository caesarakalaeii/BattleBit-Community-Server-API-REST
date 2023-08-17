using BattleBitAPI.Common;

namespace CommunityServerAPI;

public class GunGame : GameMode
{
    private readonly List<Weapon> mGunGame = new()
    {
        Weapons.Glock18,
        Weapons.Groza,
        Weapons.ACR,
        Weapons.AK15,
        Weapons.AK74,
        Weapons.G36C,
        Weapons.HoneyBadger,
        Weapons.KrissVector,
        Weapons.L86A1,
        Weapons.L96,
        Weapons.M4A1,
        Weapons.M9,
        Weapons.M110,
        Weapons.M249,
        Weapons.MK14EBR,
        Weapons.MK20,
        Weapons.MP7,
        Weapons.PP2000,
        Weapons.SCARH,
        Weapons.SSG69
    };

    public GunGame()
    {
        Name = "GunGame";
    }

    // Gun Game
    public override async Task<OnPlayerSpawnArguments> OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        await Task.Run(() => { UpdateWeapon(player); });
        return request;
    }

    public override Task OnPlayerSpawned(MyPlayer player)
    {
        player.Modifications.RespawnTime = 0f;
        player.Modifications.RunningSpeedMultiplier = 1.25f;
        player.Modifications.FallDamageMultiplier = 0f;
        player.Modifications.JumpHeightMultiplier = 1.5f;
        return base.OnPlayerSpawned(player);
    }

    public int GetGameLenght()
    {
        return mGunGame.Count;
    }

    public void UpdateWeapon(MyPlayer player)
    {
        var w = new WeaponItem
        {
            ToolName = mGunGame[player.Level].Name,
            MainSight = Attachments.RedDot
        };


        player.SetPrimaryWeapon(w, 0, true); //currently buggy everything will crash
    }

    public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<MyPlayer> onPlayerKillArguments)
    {
        await Task.Run(() =>
        {
            var killer = onPlayerKillArguments.Killer;
            var victim = onPlayerKillArguments.Victim;
            killer.Level++;
            if (killer.Level == GetGameLenght()) AnnounceShort($"{killer.Name} only needs 1 more Kill");
            if (killer.Level > GetGameLenght())
            {
                AnnounceShort($"{killer.Name} won the Game");
                ForceEndGame();
            }

            killer.SetHP(100);
            victim.Kill();
            if (onPlayerKillArguments.KillerTool == "Sledge Hammer" && victim.Level != 0) victim.Level--;
            UpdateWeapon(killer);
        });
    }

    public override Task OnRoundEnded()
    {
        Reset();
        return base.OnRoundEnded();
    }

    public override void Reset()
    {
        SayToChat("Resetting GameMode");
        foreach (var player in AllPlayers)
        {
            player.Level = 0;
            player.Kill();
        }
    }
}