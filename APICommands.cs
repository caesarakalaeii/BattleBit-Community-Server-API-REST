using System.Numerics;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using CommunityServerAPI;

public abstract class ApiCommand
{
    public string CommandPrefix;
    public string Help;

    public virtual Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        return null;
    }
}

public class HealCommand : ApiCommand
{
    public HealCommand()
    {
        CommandPrefix = "!heal";

        Help =
            "'steamid' 'amount': Heals specific player the specified amount";
    }


    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.Heal,
            Amount = int.Parse(splits[2]),
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class KillCommand : ApiCommand
{
    public KillCommand()
    {
        CommandPrefix = "!kill";
        Help = "'steamid': Kills specific player";
    }


    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.Kill,
            Amount = 0,
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class GrenadeCommand : ApiCommand
{
    public GrenadeCommand()
    {
        CommandPrefix = "!grenade";
        Help = "'steamid': spawns live grenade on specific player";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.Grenade,
            Amount = 0,
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class TeleportCommand : ApiCommand
{
    public TeleportCommand()
    {
        CommandPrefix = "!teleport";
        Help = "'steamid' 'vector': Teleports specific player to vector location";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var vectorStr = splits[2].Split(",");
        var vector = new Vector3
        {
            X = Convert.ToSingle(vectorStr[0]),
            Y = Convert.ToSingle(vectorStr[1]),
            Z = Convert.ToSingle(vectorStr[2])
        };

        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.Teleport,
            Amount = 0,
            Location = vector,
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class SpeedCommand : ApiCommand
{
    public SpeedCommand()
    {
        CommandPrefix = "!speed";
        Help = "'steamid' 'amount': Sets speed multiplier of specific player to the specified amount";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.Speed,
            Amount = int.Parse(splits[2]),
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class ChangeAttachmentCommand : ApiCommand
{
    public ChangeAttachmentCommand()
    {
        CommandPrefix = "!changeAttachment";
        Help = "'steamid' 'pri=Attachment' 'sec=Attachment': Change attachments of specific player";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.ChangeAttachment,
            Amount = 0, // Not sure what this value represents, please adjust accordingly
            AttachmentChange = Utility.ParseAttachments(splits),
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class ChangeWeaponCommand : ApiCommand
{
    public ChangeWeaponCommand()
    {
        CommandPrefix = "!changeWeapon";
        Help = "'steamid' 'pri=Weapon' 'sec=Weapon': Change weapons of specific player";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.ChangeWeapon,
            Amount = 0, // Not sure what this value represents, please adjust accordingly
            AttachmentChange = Utility.ParseAttachments(splits),
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class ForceStartCommand : ApiCommand
{
    public ForceStartCommand()
    {
        CommandPrefix = "!forceStart";
        Help = ": Forces the game to start";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var c = new Command
        {
            Action = ActionType.Start,
            StreamerId = player.SteamID,
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class HelpCommand : ApiCommand
{
    public HelpCommand()
    {
        CommandPrefix = "!help";
        Help = ": Lists all commands";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var c = new Command
        {
            Action = ActionType.Help,
            StreamerId = player.SteamID,
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class RevealCommand : ApiCommand
{
    public RevealCommand()
    {
        CommandPrefix = "!reveal";
        Help = "'steamid': Reveal information about the specified player";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.Reveal,
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class ChangeDamageCommand : ApiCommand
{
    public ChangeDamageCommand()
    {
        CommandPrefix = "!changeDamage";
        Help = "'steamid' 'amount': Change the damage of the specified player";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.ChangeDamage,
            Amount = int.Parse(splits[2]),
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class ChangeReceivedDamageCommand : ApiCommand
{
    public ChangeReceivedDamageCommand()
    {
        CommandPrefix = "!changeReceivedDamage";
        Help = "'steamid' 'amount': Change the received damage of the specified player";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.ChangeReceivedDamage,
            Amount = int.Parse(splits[2]),
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class ChangeAmmoCommand : ApiCommand
{
    public ChangeAmmoCommand()
    {
        CommandPrefix = "!changeAmmo";
        Help = "'steamid' 'amount': Change the ammo of the specified player";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.ChangeAmmo,
            Amount = int.Parse(splits[2]),
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class SetStreamerCommand : ApiCommand
{
    public SetStreamerCommand()
    {
        CommandPrefix = "!setStreamer";
        Help = "'steamid': Set the specified player as the streamer";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]), // needs fixing, will check if that is already a streamer
            Action = ActionType.SetStreamer,
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class RemoveStreamerCommand : ApiCommand
{
    public RemoveStreamerCommand()
    {
        CommandPrefix = "!rmStreamer";
        Help = "'steamid': Remove the streamer status from the specified player";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.RemoveStreamer,
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class OpCommand : ApiCommand
{
    public OpCommand()
    {
        CommandPrefix = "!op";
        Help = "'steamid': Grant operator privileges to the specified player";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.GrantOp,
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class DeopCommand : ApiCommand
{
    public DeopCommand()
    {
        CommandPrefix = "!deop";
        Help = "'steamid': Revoke operator privileges from the specified player";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var splits = msg.Split(" ");
        var c = new Command
        {
            StreamerId = Convert.ToUInt64(splits[1]),
            Action = ActionType.RevokeOp,
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class NextGameModeCommand : ApiCommand
{
    public NextGameModeCommand()
    {
        CommandPrefix = "!nextGM";
        Help = ": switches to the next gamemode in the playlist";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var c = new Command
        {
            StreamerId = player.SteamID,
            Action = ActionType.NextGameMode,
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class SetGameModeCommand : ApiCommand
{
    public SetGameModeCommand()
    {
        CommandPrefix = "!setGM";
        Help = "'GameMode': switches to the specified gamemode if it's in the playlist";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var split = msg.Split();
        var c = new Command
        {
            StreamerId = player.SteamID,
            Action = ActionType.SetGameMode,
            ExecutorName = split[1]
        };
        return c;
    }
}

public class TogglePlaylistCommand : ApiCommand
{
    public TogglePlaylistCommand()
    {
        CommandPrefix = "!togglePlaylist";
        Help = ": toggles switching GameModes on round end";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var split = msg.Split();
        var c = new Command
        {
            StreamerId = player.SteamID,
            Action = ActionType.TogglePlaylist,
            ExecutorName = "Chat Test"
        };
        return c;
    }
}

public class GetGameModeCommand : ApiCommand
{
    public GetGameModeCommand()
    {
        CommandPrefix = "!getGM";
        Help = ": returns the current gamemode";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var c = new Command
        {
            StreamerId = player.SteamID,
            Action = ActionType.GetGameMode,
            ExecutorName = player.Name
        };
        return c;
    }
}

public class AddDebugLineCommand : ApiCommand
{
    public AddDebugLineCommand()
    {
        CommandPrefix = "!addDebud";
        Help = ": Adds Debug info to the menu";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var split = msg.Split();
        var c = new Command
        {
            StreamerId = player.SteamID,
            Action = ActionType.AddLine,
            ExecutorName = split[1]
        };
        return c;
    }
}

public class DelDebugLineCommand : ApiCommand
{
    public DelDebugLineCommand()
    {
        CommandPrefix = "!addDebud";
        Help = ": Adds Debug info to the menu";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var split = msg.Split();
        var c = new Command
        {
            StreamerId = player.SteamID,
            Action = ActionType.DelLine,
            ExecutorName = split[1]
        };
        return c;
    }
}

public class ToggleDebugCommand : ApiCommand
{
    public ToggleDebugCommand()
    {
        CommandPrefix = "!toggleDebug";
        Help = ": en/disables the debug messages";
    }

    public override Command ChatCommand(MyPlayer player, ChatChannel channel, string msg)
    {
        var c = new Command
        {
            StreamerId = player.SteamID,
            Action = ActionType.Debug,
            ExecutorName = player.Name
        };
        return c;
    }
}