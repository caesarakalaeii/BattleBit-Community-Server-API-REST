using BattleBitAPI.Common;
using Stream = BattleBitAPI.Common.Serialization.Stream;

namespace BattleBitAPI.Server;

public class RoundSettings<TPlayer> where TPlayer : Player<TPlayer>
{
    // ---- Construction ---- 
    private readonly GameServer<TPlayer>.Internal mResources;

    public RoundSettings(GameServer<TPlayer>.Internal resources)
    {
        mResources = resources;
    }

    // ---- Variables ---- 
    public GameState State => mResources._RoundSettings.State;

    public double TeamATickets
    {
        get => mResources._RoundSettings.TeamATickets;
        set
        {
            mResources._RoundSettings.TeamATickets = value;
            mResources.IsDirtyRoundSettings = true;
        }
    }

    public double TeamBTickets
    {
        get => mResources._RoundSettings.TeamBTickets;
        set
        {
            mResources._RoundSettings.TeamBTickets = value;
            mResources.IsDirtyRoundSettings = true;
        }
    }

    public double MaxTickets
    {
        get => mResources._RoundSettings.MaxTickets;
        set
        {
            mResources._RoundSettings.MaxTickets = value;
            mResources.IsDirtyRoundSettings = true;
        }
    }

    public int PlayersToStart
    {
        get => mResources._RoundSettings.PlayersToStart;
        set
        {
            mResources._RoundSettings.PlayersToStart = value;
            mResources.IsDirtyRoundSettings = true;
        }
    }

    public int SecondsLeft
    {
        get => mResources._RoundSettings.SecondsLeft;
        set
        {
            mResources._RoundSettings.SecondsLeft = value;
            mResources.IsDirtyRoundSettings = true;
        }
    }

    // ---- Reset ---- 
    public void Reset()
    {
    }

    // ---- Classes ---- 
    public class mRoundSettings
    {
        public const int Size = 1 + 8 + 8 + 8 + 4 + 4;
        public double MaxTickets = 1;
        public int PlayersToStart = 16;
        public int SecondsLeft = 60;

        public GameState State = GameState.WaitingForPlayers;
        public double TeamATickets;
        public double TeamBTickets;

        public void Write(Stream ser)
        {
            ser.Write((byte)State);
            ser.Write(TeamATickets);
            ser.Write(TeamBTickets);
            ser.Write(MaxTickets);
            ser.Write(PlayersToStart);
            ser.Write(SecondsLeft);
        }

        public void Read(Stream ser)
        {
            State = (GameState)ser.ReadInt8();
            TeamATickets = ser.ReadDouble();
            TeamBTickets = ser.ReadDouble();
            MaxTickets = ser.ReadDouble();
            PlayersToStart = ser.ReadInt32();
            SecondsLeft = ser.ReadInt32();
        }

        public void Reset()
        {
            State = GameState.WaitingForPlayers;
            TeamATickets = 0;
            TeamBTickets = 0;
            MaxTickets = 1;
            PlayersToStart = 16;
            SecondsLeft = 60;
        }
    }
}