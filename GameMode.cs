using BattleBitAPI.Common;

namespace CommunityServerAPI;

public class Returner
{
    public OnPlayerSpawnArguments SpawnArguments;
    public ChatChannel Channel;
    public PlayerJoiningArguments JoiningArguments;
    public string Msg;
    public MyPlayer Player;
    public ulong SteamId;
}

public class GameMode
{
    public string Name = string.Empty;
    protected MyGameServer R;

    protected GameMode(MyGameServer r)
    {
        R = r;
    }

    public virtual void Init()
    {
    }

    public virtual void Reset()
    {
        foreach (var player in R.AllPlayers) player.Kill();
    }

    public virtual void OnRoundEnded()
    {
        Reset();
    }

    public virtual OnPlayerKillArguments<MyPlayer> OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<MyPlayer> args)
    {
        return args;
    }

    public virtual MyPlayer OnPlayerGivenUp(MyPlayer player)
    {
        throw new NotImplementedException();
    }

    public virtual MyPlayer OnPlayerSpawned(MyPlayer player)
    {
        return player;
    }

    public virtual Returner OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        var re = new Returner
        {
            Player = player,
            SpawnArguments = request
        };
        return re;
    }

    public virtual void OnRoundStarted()
    {
    }

    public MyPlayer OnPlayerDisconnected(MyPlayer player)
    {
        return player;
    }

    public Returner OnPlayerJoiningToServer(ulong steamId, PlayerJoiningArguments args)
    {
        var re = new Returner
        {
            SteamId = steamId,
            JoiningArguments = args
        };
        return re;
    }

    public async Task<Returner> OnPlayerTypedMessage(MyPlayer player, ChatChannel channel, string msg)
    {
        var re = new Returner
        {
            Player = player,
            Channel = channel,
            Msg = msg
        };
        return re;
    }
}