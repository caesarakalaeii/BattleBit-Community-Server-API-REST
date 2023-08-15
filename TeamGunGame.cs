using BattleBitAPI.Common;

namespace CommunityServerAPI;

public class TeamGunGame : GameMode
{
    public int LevelA;
    public int LevelB;

    public List<WeaponItem> ProgressionList = new()
    {
        new WeaponItem
        {
            Tool = Weapons.FAL,
            MainSight = Attachments.RedDot,
            TopSight = null,
            CantedSight = null,
            Barrel = null,
            SideRail = null,
            UnderRail = null,
            BoltAction = null
        },

        new WeaponItem
        {
            Tool = Weapons.M249,
            MainSight = Attachments.Acog,
            TopSight = null,
            CantedSight = null,
            Barrel = null,
            SideRail = null,
            UnderRail = null,
            BoltAction = null
        },
        new WeaponItem
        {
            Tool = Weapons.M4A1,
            MainSight = Attachments.Holographic,
            TopSight = null,
            CantedSight = Attachments.CantedRedDot,
            Barrel = Attachments.Compensator,
            SideRail = Attachments.Flashlight,
            UnderRail = Attachments.VerticalGrip,
            BoltAction = null
        },
        new WeaponItem
        {
            Tool = Weapons.AK74,
            MainSight = Attachments.RedDot,
            TopSight = Attachments.DeltaSightTop,
            CantedSight = Attachments.CantedRedDot,
            Barrel = Attachments.Ranger,
            SideRail = Attachments.TacticalFlashlight,
            UnderRail = Attachments.Bipod,
            BoltAction = null
        },
        new WeaponItem
        {
            Tool = Weapons.SCARH,
            MainSight = Attachments.Acog,
            TopSight = Attachments.RedDotTop,
            CantedSight = Attachments.Ironsight,
            Barrel = Attachments.MuzzleBreak,
            SideRail = Attachments.TacticalFlashlight,
            UnderRail = Attachments.AngledGrip,
            BoltAction = null
        },
        new WeaponItem
        {
            Tool = Weapons.SSG69,
            MainSight = Attachments._6xScope,
            TopSight = null,
            CantedSight = Attachments.HoloDot,
            Barrel = Attachments.LongBarrel,
            SideRail = Attachments.Greenlaser,
            UnderRail = Attachments.VerticalSkeletonGrip,
            BoltAction = null
        },
        new WeaponItem
        {
            Tool = Weapons.M110,
            MainSight = Attachments.Acog,
            TopSight = Attachments.PistolRedDot,
            CantedSight = Attachments.FYouCanted,
            Barrel = Attachments.Heavy,
            SideRail = Attachments.TacticalFlashlight,
            UnderRail = Attachments.StubbyGrip,
            BoltAction = null
        },
        new WeaponItem
        {
            Tool = Weapons.PP2000,
            MainSight = Attachments.Kobra,
            TopSight = null,
            CantedSight = Attachments.Ironsight,
            Barrel = Attachments.MuzzleBreak,
            SideRail = Attachments.Flashlight,
            UnderRail = Attachments.AngledGrip,
            BoltAction = null
        }
    };

    public TeamGunGame()
    {
        Name = "TeamGunGame";
    }

    public override Task<OnPlayerSpawnArguments> OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        var level = LevelB;
        if (player.Team == Team.TeamA) level = LevelA;

        request.Loadout.PrimaryWeapon = ProgressionList[level];
        request.Loadout.HeavyGadget = new Gadget("Sledge Hammer");
        return base.OnPlayerSpawning(player, request);
    }

    public override Task OnPlayerSpawned(MyPlayer player)
    {
        player.SetRunningSpeedMultiplier(1.25f);
        player.SetFallDamageMultiplier(0f);
        player.SetJumpMultiplier(1.5f);
        return base.OnPlayerSpawned(player);
    }

    public override Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<MyPlayer> args)
    {
        args.Victim.Kill();
        var level = LevelB;
        if (args.Killer.Team == Team.TeamA)
        {
            LevelA++;
            level = LevelA;
        }
        else
        {
            LevelB++;
        }

        if (level == ProgressionList.Count)
        {
            AnnounceShort($"{args.Killer.Team.ToString()} only needs 1 more Kill");
        }
        else if (level > ProgressionList.Count)
        {
            AnnounceLong($"{args.Killer.Team.ToString()} won the Game");
            ForceEndGame();
        }

        return base.OnAPlayerDownedAnotherPlayer(args);
    }
}