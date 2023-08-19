namespace CommunityServerAPI;

public class DebugInfo
{
    private readonly List<InfoLine> mAvailableInfoLines = new()
    {
        new InfoLineName(),
        new InfoLineBleeding(),
        new InfoLineLeaning(),
        new InfoLinePing(),
        new InfoLinePos(),
        new InfoLineStanding(),
        new InfoLineHP(),
        new InfoLineRespawnTime(),
        new InfoLineRunningSpeedMulti()
    };

    private readonly List<InfoLine> mInfoLines;

    private readonly MyPlayer mPlayer;

    public DebugInfo(MyPlayer player)
    {
        mPlayer = player;
        mInfoLines = new List<InfoLine>
        {
            new InfoLineDebugTitle()
        };
    }


    public string GetInfo()
    {
        var re = string.Empty;
        foreach (var line in mInfoLines) re += $"{line.Line(mPlayer)}{RichText.LineBreak}";
        return re;
    }

    public bool AddLine(string name)
    {
        var found = false;
        foreach (var line in mAvailableInfoLines)
            if (line.Name == name)
            {
                found = true;
                mInfoLines.Add(line);
            }

        return found;
    }

    public bool DelLine(string name)
    {
        var found = false;
        foreach (var line in mInfoLines)
            if (line.Name == name)
            {
                found = true;
                mInfoLines.Remove(line);
            }

        return found;
    }
}

public class InfoLine
{
    public string Name;

    protected InfoLine()
    {
    }

    public virtual string Line(MyPlayer myPlayer)
    {
        return string.Empty;
    }
}

public class InfoLineDebugTitle : InfoLine
{
    public InfoLineDebugTitle()
    {
        Name = "DebugTitle";
    }

    public override string Line(MyPlayer myPlayer)
    {
        return $"{RichText.AlignCenter($"{RichText.StyleH1(RichText.Underline("Debug Info:"))}")}";
    }
}

public class InfoLineName : InfoLine
{
    public InfoLineName()
    {
        Name = "Name";
    }

    public override string Line(MyPlayer myPlayer)
    {
        return $"{RichText.AlignLeft("Name:")}{RichText.AlignRight(myPlayer.Name)}";
    }
}

public class InfoLinePos : InfoLine
{
    public InfoLinePos()
    {
        Name = "Position";
    }

    public override string Line(MyPlayer myPlayer)
    {
        return
            $"{RichText.AlignLeft("Position:")}{RichText.AlignRight($"X:{myPlayer.Position.X} Y:{myPlayer.Position.Y} Z:{myPlayer.Position.Z}")}";
    }
}

public class InfoLinePing : InfoLine
{
    public InfoLinePing()
    {
        Name = "Ping";
    }

    public override string Line(MyPlayer myPlayer)
    {
        return $"{RichText.AlignLeft("Ping:")}{RichText.AlignRight($"{myPlayer.PingMs}")}";
    }
}

public class InfoLineHP : InfoLine
{
    public InfoLineHP()
    {
        Name = "HP";
    }

    public override string Line(MyPlayer myPlayer)
    {
        return $"{RichText.AlignLeft("Health:")}{RichText.AlignRight($"{myPlayer.HP}")}";
    }
}

public class InfoLineStanding : InfoLine
{
    public InfoLineStanding()
    {
        Name = "Standing";
    }

    public override string Line(MyPlayer myPlayer)
    {
        return $"{RichText.AlignLeft("Standing State:")}{RichText.AlignRight($"{myPlayer.StandingState}")}";
    }
}

public class InfoLineLeaning : InfoLine
{
    public InfoLineLeaning()
    {
        Name = "Leaning";
    }

    public override string Line(MyPlayer myPlayer)
    {
        return $"{RichText.AlignLeft("Leaning State:")}{RichText.AlignRight($"{myPlayer.LeaningState}")}";
    }
}

public class InfoLineBleeding : InfoLine
{
    public InfoLineBleeding()
    {
        Name = "Bleeding";
    }

    public override string Line(MyPlayer myPlayer)
    {
        return $"{RichText.AlignLeft("IsBleeding:")}{RichText.AlignRight($"{myPlayer.IsBleeding}")}";
    }
}

public class InfoLineRespawnTime : InfoLine
{
    public InfoLineRespawnTime()
    {
        Name = "RespawnTime";
    }

    public override string Line(MyPlayer myPlayer)
    {
        return $"{RichText.AlignLeft("RespawnTime:")}{RichText.AlignRight($"{myPlayer.Modifications.RespawnTime}")}";
    }
}

public class InfoLineRunningSpeedMulti : InfoLine
{
    public InfoLineRunningSpeedMulti()
    {
        Name = "RunningSpeed";
    }

    public override string Line(MyPlayer myPlayer)
    {
        return
            $"{RichText.AlignLeft("RunningSpeedMuliplyer:")}{RichText.AlignRight($"{myPlayer.Modifications.RunningSpeedMultiplier}")}";
    }
}