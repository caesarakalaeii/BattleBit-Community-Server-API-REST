using System.Net;
using System.Net.Sockets;
using System.Numerics;
using BattleBitAPI.Common;
using BattleBitAPI.Common.Extentions;
using BattleBitAPI.Networking;
using BattleBitAPI.Pooling;
using Stream = BattleBitAPI.Common.Serialization.Stream;

namespace BattleBitAPI.Server;

public class ServerListener<TPlayer, TGameServer> : IDisposable where TPlayer : Player<TPlayer>
    where TGameServer : GameServer<TPlayer>
{
    private readonly Dictionary<ulong, (TGameServer server, GameServer<TPlayer>.Internal resources)> mActiveConnections;
    private readonly ItemPooling<GameServer<TPlayer>> mGameServerPool;
    private readonly mInstances<TPlayer, TGameServer> mInstanceDatabase;

    // --- Private --- 
    private TcpListener mSocket;

    // --- Construction --- 
    public ServerListener()
    {
        mActiveConnections = new Dictionary<ulong, (TGameServer, GameServer<TPlayer>.Internal)>(16);
        mInstanceDatabase = new mInstances<TPlayer, TGameServer>();
        mGameServerPool = new ItemPooling<GameServer<TPlayer>>(64);
    }

    // --- Public --- 
    public bool IsListening { get; private set; }
    public bool IsDisposed { get; private set; }
    public int ListeningPort { get; private set; }
    public LogLevel LogLevel { get; set; } = LogLevel.None;

    // --- Events --- 
    /// <summary>
    ///     Fired when an attempt made to connect to the server.<br />
    ///     Default, any connection attempt will be accepted
    /// </summary>
    /// <remarks>
    ///     IPAddress: IP of incoming connection <br />
    /// </remarks>
    /// <value>
    ///     Returns: true if allow connection, false if deny the connection.
    /// </value>
    public Func<IPAddress, Task<bool>> OnGameServerConnecting { get; set; }

    /// <summary>
    ///     Fired when server needs to validate token from incoming connection.<br />
    ///     Default, any connection attempt will be accepted
    /// </summary>
    /// <remarks>
    ///     IPAddress: IP of incoming connection <br />
    ///     ushort: Game Port of the connection <br />
    ///     string: Token of connection<br />
    /// </remarks>
    /// <value>
    ///     Returns: true if allow connection, false if deny the connection.
    /// </value>
    public Func<IPAddress, ushort, string, Task<bool>> OnValidateGameServerToken { get; set; }

    /// <summary>
    ///     Fired when a game server connects.
    /// </summary>
    /// <remarks>
    ///     GameServer: Game server that is connecting.<br />
    /// </remarks>
    public Func<GameServer<TPlayer>, Task> OnGameServerConnected { get; set; }

    /// <summary>
    ///     Fired when a game server disconnects. Check (GameServer.TerminationReason) to see the reason.
    /// </summary>
    /// <remarks>
    ///     GameServer: Game server that disconnected.<br />
    /// </remarks>
    public Func<GameServer<TPlayer>, Task> OnGameServerDisconnected { get; set; }

    /// <summary>
    ///     Fired when a new instance of game server created.
    /// </summary>
    /// <remarks>
    ///     IPAddrt: Game server's Port.<br />ess: Game server's IP.<br />
    ///     ushor
    /// </remarks>
    public Func<IPAddress, ushort, TGameServer> OnCreatingGameServerInstance { get; set; }

    /// <summary>
    ///     Fired when a new instance of player instance created.
    /// </summary>
    /// <remarks>
    ///     TPlayer: The player instance that was created<br />
    ///     ulong: The steamID of the player<br />
    /// </remarks>
    public Func<ulong, TPlayer> OnCreatingPlayerInstance { get; set; }

    /// <summary>
    ///     Fired on log
    /// </summary>
    /// <remarks>
    ///     LogLevel: The level of log<br />
    ///     string: The message<br />
    ///     object: The object that will be carried on log<br />
    /// </remarks>
    public Action<LogLevel, string, object?> OnLog { get; set; }

    // --- Public ---
    public IEnumerable<TGameServer> ConnectedGameServers
    {
        get
        {
            using (var list = mGameServerPool.Get())
            {
                //Get a copy
                lock (mActiveConnections)
                {
                    foreach (var item in mActiveConnections.Values)
                        list.ListItems.Add(item.server);
                }

                //Iterate
                for (var i = 0; i < list.ListItems.Count; i++)
                    yield return (TGameServer)list.ListItems[i];
            }
        }
    }

    // --- Disposing --- 
    public void Dispose()
    {
        //Already disposed?
        if (IsDisposed)
            return;
        IsDisposed = true;

        if (IsListening)
            Stop();
    }

    // --- Starting ---
    public void Start(IPAddress bindIP, int port)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(GetType().FullName);
        if (bindIP == null)
            throw new ArgumentNullException(nameof(bindIP));
        if (IsListening)
            throw new Exception("Server is already listening.");

        mSocket = new TcpListener(bindIP, port);
        mSocket.Start();

        ListeningPort = port;
        IsListening = true;

        if (LogLevel.HasFlag(LogLevel.Sockets))
            OnLog(LogLevel.Sockets, "Listening TCP connections on port " + port, null);

        mMainLoop();
    }

    public void Start(int port)
    {
        Start(IPAddress.Any, port);
    }

    // --- Stopping ---
    public void Stop()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(GetType().FullName);
        if (!IsListening)
            throw new Exception("Already not running.");

        try
        {
            mSocket.Stop();
        }
        catch
        {
        }

        if (LogLevel.HasFlag(LogLevel.Sockets))
            OnLog(LogLevel.Sockets, "Stopped listening TCP connection.", null);

        mSocket = null;
        ListeningPort = 0;
        IsListening = true;
    }

    // --- Main Loop ---
    private async Task mMainLoop()
    {
        while (IsListening)
        {
            var client = await mSocket.AcceptTcpClientAsync();
            mInternalOnClientConnecting(client);
        }
    }

    private async Task mInternalOnClientConnecting(TcpClient client)
    {
        var ip = (client.Client.RemoteEndPoint as IPEndPoint).Address;

        if (LogLevel.HasFlag(LogLevel.Sockets))
            OnLog(LogLevel.Sockets, $"Incoming TCP connection from {ip}", client);

        //Is this IP allowed?
        var allow = true;
        if (OnGameServerConnecting != null)
            allow = await OnGameServerConnecting(ip);

        //Close connection if it was not allowed.
        if (!allow)
        {
            if (LogLevel.HasFlag(LogLevel.Sockets))
                OnLog(LogLevel.Sockets, $"Incoming connection from {ip} was denied", client);

            //Connection is not allowed from this IP.
            client.SafeClose();
            return;
        }

        //Read port,token,version
        string token;
        string version;
        int gamePort;
        try
        {
            using (var source = new CancellationTokenSource(2000))
            {
                using (var readStream = Stream.Get())
                {
                    var networkStream = client.GetStream();

                    //Read package type
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 1, source.Token))
                            throw new Exception("Unable to read the package type");

                        var type = (NetworkCommuncation)readStream.ReadInt8();
                        if (type != NetworkCommuncation.Hail)
                            throw new Exception("Incoming package wasn't hail.");
                    }

                    //Read the server token
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 2, source.Token))
                            throw new Exception("Unable to read the Token Size");

                        int stringSize = readStream.ReadUInt16();
                        if (stringSize > Const.MaxTokenSize)
                            throw new Exception("Invalid token size");

                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, stringSize, source.Token))
                            throw new Exception("Unable to read the token");

                        token = readStream.ReadString(stringSize);
                    }

                    //Read the server version
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 2, source.Token))
                            throw new Exception("Unable to read the version size");

                        int stringSize = readStream.ReadUInt16();
                        if (stringSize > 32)
                            throw new Exception("Invalid version size");

                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, stringSize, source.Token))
                            throw new Exception("Unable to read the version");

                        version = readStream.ReadString(stringSize);
                    }

                    //Read port
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 2, source.Token))
                            throw new Exception("Unable to read the Port");
                        gamePort = readStream.ReadUInt16();
                    }
                }
            }
        }
        catch (Exception e)
        {
            if (LogLevel.HasFlag(LogLevel.Sockets))
                OnLog(LogLevel.Sockets, $"{ip} failed to connected because " + e.Message, client);

            client.SafeClose();
            return;
        }

        var hash = ((ulong)gamePort << 32) | ip.ToUInt();
        TGameServer server = null;
        GameServer<TPlayer>.Internal resources = null;
        try
        {
            //Does versions match?
            if (version != Const.Version)
                throw new Exception("Incoming server's version `" + version +
                                    "` does not match with current API version `" + Const.Version + "`");

            //Is valid token?
            if (OnValidateGameServerToken != null)
                if (!await OnValidateGameServerToken(ip, (ushort)gamePort, token))
                    throw new Exception("Token was not valid!");

            //Are there any connections with same IP and port?
            {
                var sessionExist = false;
                (TGameServer server, GameServer<TPlayer>.Internal resources) oldSession;

                //Any sessions with this IP:Port?
                lock (mActiveConnections)
                {
                    sessionExist = mActiveConnections.TryGetValue(hash, out oldSession);
                }

                if (sessionExist)
                {
                    //Close old session.
                    oldSession.server.CloseConnection("Reconnecting.");

                    //Wait until session is fully closed.
                    while (oldSession.resources.HasActiveConnectionSession)
                        await Task.Delay(1);
                }
            }

            using (var source = new CancellationTokenSource(Const.HailConnectTimeout))
            {
                using (var readStream = Stream.Get())
                {
                    var networkStream = client.GetStream();

                    //Read is server protected
                    bool isPasswordProtected;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 1, source.Token))
                            throw new Exception("Unable to read the IsPasswordProtected");
                        isPasswordProtected = readStream.ReadBool();
                    }

                    //Read the server name
                    string serverName;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 2, source.Token))
                            throw new Exception("Unable to read the ServerName Size");

                        int stringSize = readStream.ReadUInt16();
                        if (stringSize < Const.MinServerNameLength || stringSize > Const.MaxServerNameLength)
                            throw new Exception("Invalid server name size");

                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, stringSize, source.Token))
                            throw new Exception("Unable to read the ServerName");

                        serverName = readStream.ReadString(stringSize);
                    }

                    //Read the gamemode
                    string gameMode;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 2, source.Token))
                            throw new Exception("Unable to read the gamemode Size");

                        int stringSize = readStream.ReadUInt16();
                        if (stringSize < Const.MinGamemodeNameLength || stringSize > Const.MaxGamemodeNameLength)
                            throw new Exception("Invalid gamemode size");

                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, stringSize, source.Token))
                            throw new Exception("Unable to read the gamemode");

                        gameMode = readStream.ReadString(stringSize);
                    }

                    //Read the gamemap
                    string gamemap;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 2, source.Token))
                            throw new Exception("Unable to read the map size");

                        int stringSize = readStream.ReadUInt16();
                        if (stringSize < Const.MinMapNameLength || stringSize > Const.MaxMapNameLength)
                            throw new Exception("Invalid map size");

                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, stringSize, source.Token))
                            throw new Exception("Unable to read the map");

                        gamemap = readStream.ReadString(stringSize);
                    }

                    //Read the mapSize
                    MapSize size;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 1, source.Token))
                            throw new Exception("Unable to read the MapSize");
                        size = (MapSize)readStream.ReadInt8();
                    }

                    //Read the day night
                    MapDayNight dayNight;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 1, source.Token))
                            throw new Exception("Unable to read the MapDayNight");
                        dayNight = (MapDayNight)readStream.ReadInt8();
                    }

                    //Current Players
                    int currentPlayers;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 1, source.Token))
                            throw new Exception("Unable to read the Current Players");
                        currentPlayers = readStream.ReadInt8();
                    }

                    //Queue Players
                    int queuePlayers;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 1, source.Token))
                            throw new Exception("Unable to read the Queue Players");
                        queuePlayers = readStream.ReadInt8();
                    }

                    //Max Players
                    int maxPlayers;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 1, source.Token))
                            throw new Exception("Unable to read the Max Players");
                        maxPlayers = readStream.ReadInt8();
                    }

                    //Read Loading Screen Text
                    string loadingScreenText;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 2, source.Token))
                            throw new Exception("Unable to read the Loading Screen Text Size");

                        int stringSize = readStream.ReadUInt16();
                        if (stringSize < Const.MinLoadingScreenTextLength ||
                            stringSize > Const.MaxLoadingScreenTextLength)
                            throw new Exception("Invalid server Loading Screen Text Size");

                        if (stringSize > 0)
                        {
                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, stringSize, source.Token))
                                throw new Exception("Unable to read the Loading Screen Text");

                            loadingScreenText = readStream.ReadString(stringSize);
                        }
                        else
                        {
                            loadingScreenText = string.Empty;
                        }
                    }

                    //Read Server Rules Text
                    string serverRulesText;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 2, source.Token))
                            throw new Exception("Unable to read the Server Rules Text Size");

                        int stringSize = readStream.ReadUInt16();
                        if (stringSize < Const.MinServerRulesTextLength || stringSize > Const.MaxServerRulesTextLength)
                            throw new Exception("Invalid server Server Rules Text Size");

                        if (stringSize > 0)
                        {
                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, stringSize, source.Token))
                                throw new Exception("Unable to read the Server Rules Text");

                            serverRulesText = readStream.ReadString(stringSize);
                        }
                        else
                        {
                            serverRulesText = string.Empty;
                        }
                    }

                    //Round index
                    uint roundIndex;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 4, source.Token))
                            throw new Exception("Unable to read the Server Round Index");
                        roundIndex = readStream.ReadUInt32();
                    }

                    //Round index
                    long sessionID;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 8, source.Token))
                            throw new Exception("Unable to read the Server Round ID");
                        sessionID = readStream.ReadInt64();
                    }


                    server = mInstanceDatabase.GetServerInstance(hash, out resources, OnCreatingGameServerInstance, ip,
                        (ushort)gamePort);
                    resources.Set(
                        mExecutePackage,
                        mGetPlayerInternals,
                        client,
                        ip,
                        gamePort,
                        isPasswordProtected,
                        serverName,
                        gameMode,
                        gamemap,
                        size,
                        dayNight,
                        currentPlayers,
                        queuePlayers,
                        maxPlayers,
                        loadingScreenText,
                        serverRulesText,
                        roundIndex,
                        sessionID
                    );

                    //Room settings
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 4, source.Token))
                            throw new Exception("Unable to read the room size");
                        var roomSize = (int)readStream.ReadUInt32();

                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, roomSize, source.Token))
                            throw new Exception("Unable to read the room");
                        resources._RoomSettings.Read(readStream);
                    }

                    //Map&gamemode rotation
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 4, source.Token))
                            throw new Exception("Unable to read the map&gamemode rotation size");
                        var rotationSize = (int)readStream.ReadUInt32();

                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, rotationSize, source.Token))
                            throw new Exception("Unable to read the map&gamemode");

                        var count = readStream.ReadUInt32();
                        while (count > 0)
                        {
                            count--;
                            if (readStream.TryReadString(out var item))
                                resources._MapRotation.Add(item.ToUpperInvariant());
                        }

                        count = readStream.ReadUInt32();
                        while (count > 0)
                        {
                            count--;
                            if (readStream.TryReadString(out var item))
                                resources._GamemodeRotation.Add(item);
                        }
                    }

                    //Round Settings
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, RoundSettings<TPlayer>.mRoundSettings.Size,
                                source.Token))
                            throw new Exception("Unable to read the round settings");
                        resources._RoundSettings.Read(readStream);
                    }

                    //Client Count
                    var clientCount = 0;
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 1, source.Token))
                            throw new Exception("Unable to read the Client Count Players");
                        clientCount = readStream.ReadInt8();
                    }

                    //Get each client.
                    while (clientCount > 0)
                    {
                        clientCount--;

                        ulong steamid = 0;
                        {
                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, 8, source.Token))
                                throw new Exception("Unable to read the SteamId");
                            steamid = readStream.ReadUInt64();
                        }

                        string username;
                        {
                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, 2, source.Token))
                                throw new Exception("Unable to read the Username Size");

                            int stringSize = readStream.ReadUInt16();
                            if (stringSize > 0)
                            {
                                readStream.Reset();
                                if (!await networkStream.TryRead(readStream, stringSize, source.Token))
                                    throw new Exception("Unable to read the Username");

                                username = readStream.ReadString(stringSize);
                            }
                            else
                            {
                                username = string.Empty;
                            }
                        }

                        uint ipHash = 0;
                        {
                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, 4, source.Token))
                                throw new Exception("Unable to read the ip");
                            ipHash = readStream.ReadUInt32();
                        }

                        //Team
                        Team team;
                        {
                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, 1, source.Token))
                                throw new Exception("Unable to read the Team");
                            team = (Team)readStream.ReadInt8();
                        }

                        //Squad
                        Squads squad;
                        {
                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, 1, source.Token))
                                throw new Exception("Unable to read the Squad");
                            squad = (Squads)readStream.ReadInt8();
                        }

                        //Role
                        GameRole role;
                        {
                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, 1, source.Token))
                                throw new Exception("Unable to read the Role");
                            role = (GameRole)readStream.ReadInt8();
                        }

                        var loadout = new PlayerLoadout();
                        var wearings = new PlayerWearings();

                        //IsAlive
                        bool isAlive;
                        {
                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, 1, source.Token))
                                throw new Exception("Unable to read the isAlive");
                            isAlive = readStream.ReadBool();
                        }

                        //Loadout + Wearings
                        if (isAlive)
                        {
                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, 4, source.Token))
                                throw new Exception("Unable to read the LoadoutSize");
                            var loadoutSize = (int)readStream.ReadUInt32();

                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, loadoutSize, source.Token))
                                throw new Exception("Unable to read the Loadout + Wearings");
                            loadout.Read(readStream);
                            wearings.Read(readStream);
                        }


                        var player = mInstanceDatabase.GetPlayerInstance(steamid, out var playerInternal,
                            OnCreatingPlayerInstance);
                        playerInternal.SteamID = steamid;
                        playerInternal.Name = username;
                        playerInternal.IP = new IPAddress(ipHash);
                        playerInternal.Team = team;
                        playerInternal.SquadName = squad;
                        playerInternal.Role = role;
                        playerInternal.IsAlive = isAlive;
                        playerInternal.CurrentLoadout = loadout;
                        playerInternal.CurrentWearings = wearings;

                        //Modifications
                        {
                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, 4, source.Token))
                                throw new Exception("Unable to read the Modifications Size");
                            var modificationSize = (int)readStream.ReadUInt32();

                            readStream.Reset();
                            if (!await networkStream.TryRead(readStream, modificationSize, source.Token))
                                throw new Exception("Unable to read the Modifications");
                            playerInternal._Modifications.Read(readStream);
                        }

                        playerInternal.GameServer = server;
                        playerInternal.SessionID = server.SessionID;

                        resources.AddPlayer(player);
                    }

                    //Squads
                    {
                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, 4, source.Token))
                            throw new Exception("Unable to read the Squad size");
                        var squadsDataSize = (int)readStream.ReadUInt32();

                        readStream.Reset();
                        if (!await networkStream.TryRead(readStream, squadsDataSize, source.Token))
                            throw new Exception("Unable to read the Squads");

                        for (var i = 1; i < resources.TeamASquadInternals.Length; i++)
                        {
                            var item = resources.TeamASquadInternals[i];
                            item.SquadPoints = readStream.ReadInt32();
                        }

                        for (var i = 1; i < resources.TeamBSquadInternals.Length; i++)
                        {
                            var item = resources.TeamBSquadInternals[i];
                            item.SquadPoints = readStream.ReadInt32();
                        }
                    }

                    // --- Finished Reading --- 

                    //Assing each player to their squad.
                    foreach (var item in resources.Players.Values)
                        if (item.InSquad)
                        {
                            var squad = resources.GetSquadInternal(item.Squad);
                            lock (squad.Members)
                            {
                                squad.Members.Add((TPlayer)item);
                            }
                        }

                    //Send accepted notification.
                    networkStream.WriteByte((byte)NetworkCommuncation.Accepted);
                }
            }
        }
        catch (Exception e)
        {
            try
            {
                var networkStream = client.GetStream();
                using (var pck = Stream.Get())
                {
                    pck.Write((byte)NetworkCommuncation.Denied);
                    pck.Write(e.Message);

                    //Send denied notification.
                    networkStream.Write(pck.Buffer, 0, pck.WritePosition);
                }

                await networkStream.FlushAsync();
            }
            catch
            {
            }

            if (LogLevel.HasFlag(LogLevel.Sockets))
                OnLog(LogLevel.Sockets, $"{ip} failed to connected because " + e.Message, client);

            client.SafeClose();
            return;
        }

        //Set the buffer sizes.
        client.ReceiveBufferSize = Const.MaxNetworkPackageSize;
        client.SendBufferSize = Const.MaxNetworkPackageSize;

        if (LogLevel.HasFlag(LogLevel.Sockets))
            OnLog(LogLevel.Sockets, $"Incoming game server from {ip}:{gamePort} accepted.", client);

        //Join to main server loop.
        await mHandleGameServer(server, resources);
    }

    private async Task mHandleGameServer(TGameServer server, GameServer<TPlayer>.Internal @internal)
    {
        @internal.HasActiveConnectionSession = true;
        {
            // ---- Connected ---- 
            {
                lock (mActiveConnections)
                {
                    mActiveConnections.Replace(server.ServerHash, (server, @internal));
                }

                server.OnConnected();
                if (OnGameServerConnected != null)
                    OnGameServerConnected(server);
            }

            //Update sessions
            {
                if (@internal.mPreviousSessionID != @internal.SessionID)
                {
                    var oldSession = @internal.mPreviousSessionID;
                    @internal.mPreviousSessionID = @internal.SessionID;

                    if (oldSession != 0)
                        server.OnSessionChanged(oldSession, @internal.SessionID);
                }

                foreach (var item in @internal.Players)
                {
                    var player_internal = mInstanceDatabase.GetPlayerInternals(item.Key);
                    if (player_internal.PreviousSessionID != player_internal.SessionID)
                    {
                        var previousID = player_internal.PreviousSessionID;
                        player_internal.PreviousSessionID = player_internal.SessionID;

                        if (previousID != 0)
                            item.Value.OnSessionChanged(previousID, player_internal.SessionID);
                    }
                }
            }

            if (LogLevel.HasFlag(LogLevel.GameServers))
                OnLog(LogLevel.GameServers, $"{server} has connected", server);

            // ---- Ticking ---- 
            using (server)
            {
                var isTicking = false;

                async Task mTickAsync()
                {
                    isTicking = true;
                    await server.OnTick();
                    isTicking = false;
                }

                while (server.IsConnected)
                {
                    if (!isTicking)
                        mTickAsync();

                    await server.Tick();
                    await Task.Delay(10);
                }
            }

            // ---- Disconnected ---- 
            {
                mCleanup(server, @internal);

                lock (mActiveConnections)
                {
                    mActiveConnections.Remove(server.ServerHash);
                }

                server.OnDisconnected();
                if (OnGameServerDisconnected != null)
                    OnGameServerDisconnected(server);
            }

            if (LogLevel.HasFlag(LogLevel.GameServers))
                OnLog(LogLevel.GameServers, $"{server} has disconnected", server);
        }
        @internal.HasActiveConnectionSession = false;
    }

    // --- Logic Executing ---
    private async Task mExecutePackage(GameServer<TPlayer> server, GameServer<TPlayer>.Internal resources,
        Stream stream)
    {
        var communcation = (NetworkCommuncation)stream.ReadInt8();
        switch (communcation)
        {
            case NetworkCommuncation.PlayerConnected:
            {
                if (stream.CanRead(8 + 2 + 4 + 1 + 1 + 1))
                {
                    var steamID = stream.ReadUInt64();
                    if (stream.TryReadString(out var username))
                    {
                        var ip = stream.ReadUInt32();
                        var team = (Team)stream.ReadInt8();
                        var squad = (Squads)stream.ReadInt8();
                        var role = (GameRole)stream.ReadInt8();

                        var player = mInstanceDatabase.GetPlayerInstance(steamID, out var playerInternal,
                            OnCreatingPlayerInstance);
                        playerInternal.SteamID = steamID;
                        playerInternal.Name = username;
                        playerInternal.IP = new IPAddress(ip);
                        playerInternal.Team = team;
                        playerInternal.SquadName = squad;
                        playerInternal.Role = role;

                        //Start from default.
                        playerInternal._Modifications.Reset();

                        playerInternal.GameServer = server;
                        playerInternal.SessionID = server.SessionID;

                        resources.AddPlayer(player);
                        player.OnConnected();
                        server.OnPlayerConnected(player);

                        if (playerInternal.PreviousSessionID != playerInternal.SessionID)
                        {
                            var previousID = playerInternal.PreviousSessionID;
                            playerInternal.PreviousSessionID = playerInternal.SessionID;

                            if (previousID != 0)
                                player.OnSessionChanged(previousID, playerInternal.SessionID);
                        }

                        if (LogLevel.HasFlag(LogLevel.Players))
                            OnLog(LogLevel.Players, $"{player} has connected", player);
                    }
                }

                break;
            }
            case NetworkCommuncation.PlayerDisconnected:
            {
                if (stream.CanRead(8))
                {
                    var steamID = stream.ReadUInt64();
                    bool exist;
                    Player<TPlayer> player;
                    lock (resources.Players)
                    {
                        exist = resources.Players.Remove(steamID, out player);
                    }

                    if (exist)
                    {
                        var @internal = mInstanceDatabase.GetPlayerInternals(steamID);
                        if (@internal.HP > -1f)
                        {
                            @internal.OnDie();

                            player.OnDied();
                            server.OnPlayerDied((TPlayer)player);
                        }

                        if (@internal.SquadName != Squads.NoSquad)
                        {
                            var msquad = server.GetSquad(@internal.Team, @internal.SquadName);
                            var rsquad = resources.GetSquadInternal(msquad);

                            @internal.SquadName = Squads.NoSquad;
                            lock (rsquad.Members)
                            {
                                rsquad.Members.Remove((TPlayer)player);
                            }

                            player.OnLeftSquad(msquad);
                            server.OnPlayerLeftSquad((TPlayer)player, msquad);
                        }

                        @internal.SessionID = 0;
                        @internal.GameServer = null;

                        player.OnDisconnected();
                        server.OnPlayerDisconnected((TPlayer)player);

                        if (LogLevel.HasFlag(LogLevel.Players))
                            OnLog(LogLevel.Players, $"{player} has disconnected", player);
                    }
                }

                break;
            }
            case NetworkCommuncation.OnPlayerTypedMessage:
            {
                if (stream.CanRead(2 + 8 + 1 + 2))
                {
                    var messageID = stream.ReadUInt16();
                    var steamID = stream.ReadUInt64();

                    if (resources.TryGetPlayer(steamID, out var player))
                    {
                        var chat = (ChatChannel)stream.ReadInt8();
                        if (stream.TryReadString(out var msg))
                        {
                            async Task Handle()
                            {
                                var pass = await server.OnPlayerTypedMessage((TPlayer)player, chat, msg);

                                //Respond back.
                                using (var response = Stream.Get())
                                {
                                    response.Write((byte)NetworkCommuncation.RespondPlayerMessage);
                                    response.Write(messageID);
                                    response.Write(pass);
                                    server.WriteToSocket(response);
                                }
                            }

                            Handle();
                        }
                    }
                }

                break;
            }
            case NetworkCommuncation.OnAPlayerDownedAnotherPlayer:
            {
                if (stream.CanRead(8 + 12 + 8 + 12 + 2 + 1 + 1))
                {
                    var killer = stream.ReadUInt64();
                    var killerPos = new Vector3(stream.ReadFloat(), stream.ReadFloat(), stream.ReadFloat());

                    var victim = stream.ReadUInt64();
                    var victimPos = new Vector3(stream.ReadFloat(), stream.ReadFloat(), stream.ReadFloat());

                    if (stream.TryReadString(out var tool))
                    {
                        var body = (PlayerBody)stream.ReadInt8();
                        var source = (ReasonOfDamage)stream.ReadInt8();

                        if (resources.TryGetPlayer(killer, out var killerClient))
                            if (resources.TryGetPlayer(victim, out var victimClient))
                            {
                                var args = new OnPlayerKillArguments<TPlayer>
                                {
                                    Killer = (TPlayer)killerClient,
                                    KillerPosition = killerPos,
                                    Victim = (TPlayer)victimClient,
                                    VictimPosition = victimPos,
                                    BodyPart = body,
                                    SourceOfDamage = source,
                                    KillerTool = tool
                                };

                                victimClient.OnDowned();
                                server.OnAPlayerDownedAnotherPlayer(args);

                                if (LogLevel.HasFlag(LogLevel.KillsAndSpawns))
                                    OnLog(LogLevel.KillsAndSpawns,
                                        $"{killer} downed {victim} in {Vector3.Distance(killerPos, victimPos)} meters",
                                        null);
                            }
                    }
                }

                break;
            }
            case NetworkCommuncation.OnPlayerJoining:
            {
                if (stream.CanRead(8 + 2))
                {
                    var steamID = stream.ReadUInt64();
                    var stats = new PlayerStats();
                    stats.Read(stream);


                    async Task mHandle()
                    {
                        var args = new PlayerJoiningArguments
                        {
                            Stats = stats,
                            Squad = Squads.NoSquad,
                            Team = Team.None
                        };

                        await server.OnPlayerJoiningToServer(steamID, args);
                        using (var response = Stream.Get())
                        {
                            response.Write((byte)NetworkCommuncation.SendPlayerStats);
                            response.Write(steamID);
                            args.Write(response);
                            server.WriteToSocket(response);
                        }
                    }

                    mHandle();
                }

                break;
            }
            case NetworkCommuncation.SavePlayerStats:
            {
                if (stream.CanRead(8 + 4))
                {
                    var steamID = stream.ReadUInt64();
                    var stats = new PlayerStats();
                    stats.Read(stream);

                    server.OnSavePlayerStats(steamID, stats);
                }

                break;
            }
            case NetworkCommuncation.OnPlayerAskingToChangeRole:
            {
                if (stream.CanRead(8 + 1))
                {
                    var steamID = stream.ReadUInt64();
                    var role = (GameRole)stream.ReadInt8();

                    if (resources.TryGetPlayer(steamID, out var player))
                    {
                        async Task mHandle()
                        {
                            if (LogLevel.HasFlag(LogLevel.Roles))
                                OnLog(LogLevel.Roles, $"{player} asking to change role to {role}", player);

                            var accepted = await server.OnPlayerRequestingToChangeRole((TPlayer)player, role);
                            if (accepted)
                                server.SetRoleTo(steamID, role);

                            if (LogLevel.HasFlag(LogLevel.Roles))
                                OnLog(LogLevel.Roles,
                                    $"{player}'s request to change role to {role} was {(accepted ? "accepted" : "denied")}",
                                    player);
                        }

                        mHandle();
                    }
                }

                break;
            }
            case NetworkCommuncation.OnPlayerChangedRole:
            {
                if (stream.CanRead(8 + 1))
                {
                    var steamID = stream.ReadUInt64();
                    var role = (GameRole)stream.ReadInt8();

                    if (resources.TryGetPlayer(steamID, out var player))
                    {
                        var @internal = mInstanceDatabase.GetPlayerInternals(steamID);
                        @internal.Role = role;

                        player.OnChangedRole(role);
                        server.OnPlayerChangedRole((TPlayer)player, role);

                        if (LogLevel.HasFlag(LogLevel.Roles))
                            OnLog(LogLevel.Roles, $"{player} changed role to {role}", player);
                    }
                }

                break;
            }
            case NetworkCommuncation.OnPlayerJoinedASquad:
            {
                if (stream.CanRead(8 + 1))
                {
                    var steamID = stream.ReadUInt64();
                    var squad = (Squads)stream.ReadInt8();

                    if (resources.TryGetPlayer(steamID, out var player))
                    {
                        var @internal = mInstanceDatabase.GetPlayerInternals(steamID);
                        @internal.SquadName = squad;

                        var msquad = server.GetSquad(player.Team, squad);
                        var rsquad = resources.GetSquadInternal(msquad);
                        lock (rsquad.Members)
                        {
                            rsquad.Members.Add((TPlayer)player);
                        }

                        player.OnJoinedSquad(msquad);
                        server.OnPlayerJoinedSquad((TPlayer)player, msquad);

                        if (LogLevel.HasFlag(LogLevel.Squads))
                            OnLog(LogLevel.Squads, $"{player} has joined to {msquad}", msquad);
                    }
                }

                break;
            }
            case NetworkCommuncation.OnPlayerLeftSquad:
            {
                if (stream.CanRead(8))
                {
                    var steamID = stream.ReadUInt64();

                    if (resources.TryGetPlayer(steamID, out var player))
                    {
                        var @internal = mInstanceDatabase.GetPlayerInternals(steamID);

                        var oldSquad = player.SquadName;
                        var oldRole = player.Role;
                        @internal.SquadName = Squads.NoSquad;
                        @internal.Role = GameRole.Assault;

                        var msquad = server.GetSquad(player.Team, oldSquad);
                        var rsquad = resources.GetSquadInternal(msquad);

                        @internal.SquadName = Squads.NoSquad;
                        lock (rsquad.Members)
                        {
                            rsquad.Members.Remove((TPlayer)player);
                        }

                        player.OnLeftSquad(msquad);
                        server.OnPlayerLeftSquad((TPlayer)player, msquad);

                        if (oldRole != GameRole.Assault)
                        {
                            player.OnChangedRole(GameRole.Assault);
                            server.OnPlayerChangedRole((TPlayer)player, GameRole.Assault);
                        }

                        if (LogLevel.HasFlag(LogLevel.Squads))
                            OnLog(LogLevel.Squads, $"{player} has left the {msquad}", msquad);
                    }
                }

                break;
            }
            case NetworkCommuncation.OnPlayerChangedTeam:
            {
                if (stream.CanRead(8 + 1))
                {
                    var steamID = stream.ReadUInt64();
                    var team = (Team)stream.ReadInt8();

                    if (resources.TryGetPlayer(steamID, out var client))
                    {
                        var @internal = mInstanceDatabase.GetPlayerInternals(steamID);

                        @internal.Team = team;

                        client.OnChangedTeam();
                        server.OnPlayerChangeTeam((TPlayer)client, team);
                    }
                }

                break;
            }
            case NetworkCommuncation.OnPlayerRequestingToSpawn:
            {
                if (stream.CanRead(2))
                {
                    var steamID = stream.ReadUInt64();

                    var request = new OnPlayerSpawnArguments();
                    request.Read(stream);
                    var vehicleID = stream.ReadUInt16();

                    if (resources.TryGetPlayer(steamID, out var player))
                    {
                        async Task mHandle()
                        {
                            if (LogLevel.HasFlag(LogLevel.KillsAndSpawns))
                                OnLog(LogLevel.KillsAndSpawns,
                                    $"{player} asking to spawn at {request.SpawnPosition} ({request.RequestedPoint})",
                                    player);

                            var responseSpawn = await server.OnPlayerSpawning((TPlayer)player, request);

                            //Respond back.
                            using (var response = Stream.Get())
                            {
                                response.Write((byte)NetworkCommuncation.SpawnPlayer);
                                response.Write(steamID);

                                if (responseSpawn != null)
                                {
                                    response.Write(true);
                                    responseSpawn.Value.Write(response);
                                    response.Write(vehicleID);
                                }
                                else
                                {
                                    response.Write(false);
                                }

                                server.WriteToSocket(response);
                            }

                            if (LogLevel.HasFlag(LogLevel.KillsAndSpawns))
                            {
                                if (responseSpawn == null)
                                    OnLog(LogLevel.KillsAndSpawns, $"{player}'s spawn request was denied", player);
                                else
                                    OnLog(LogLevel.KillsAndSpawns,
                                        $"{player}'s spawn request was accepted at {responseSpawn.Value.SpawnPosition}",
                                        player);
                            }
                        }

                        mHandle();
                    }
                }

                break;
            }
            case NetworkCommuncation.OnPlayerReport:
            {
                if (stream.CanRead(8 + 8 + 1 + 2))
                {
                    var reporter = stream.ReadUInt64();
                    var reported = stream.ReadUInt64();
                    var reason = (ReportReason)stream.ReadInt8();
                    stream.TryReadString(out var additionalInfo);

                    if (resources.TryGetPlayer(reporter, out var reporterClient))
                        if (resources.TryGetPlayer(reported, out var reportedClient))
                            server.OnPlayerReported((TPlayer)reporterClient, (TPlayer)reportedClient, reason,
                                additionalInfo);
                }

                break;
            }
            case NetworkCommuncation.OnPlayerSpawn:
            {
                if (stream.CanRead(8 + 2))
                {
                    var steamID = stream.ReadUInt64();
                    if (resources.TryGetPlayer(steamID, out var player))
                    {
                        var @internal = mInstanceDatabase.GetPlayerInternals(steamID);

                        var loadout = new PlayerLoadout();
                        loadout.Read(stream);
                        @internal.CurrentLoadout = loadout;

                        var wearings = new PlayerWearings();
                        wearings.Read(stream);
                        @internal.CurrentWearings = wearings;

                        var position = new Vector3
                        {
                            X = stream.ReadFloat(),
                            Y = stream.ReadFloat(),
                            Z = stream.ReadFloat()
                        };

                        @internal.Position = position;
                        @internal.IsAlive = true;

                        player.OnSpawned();
                        server.OnPlayerSpawned((TPlayer)player);

                        if (LogLevel.HasFlag(LogLevel.KillsAndSpawns))
                            OnLog(LogLevel.KillsAndSpawns, $"{player} has spawned at {player.Position}", player);
                    }
                }

                break;
            }
            case NetworkCommuncation.OnPlayerDie:
            {
                if (stream.CanRead(8))
                {
                    var steamid = stream.ReadUInt64();
                    if (resources.TryGetPlayer(steamid, out var player))
                    {
                        var @internal = mInstanceDatabase.GetPlayerInternals(steamid);
                        @internal.OnDie();

                        player.OnDied();
                        server.OnPlayerDied((TPlayer)player);

                        if (LogLevel.HasFlag(LogLevel.KillsAndSpawns))
                            OnLog(LogLevel.KillsAndSpawns, $"{player} has died", player);
                    }
                }

                break;
            }
            case NetworkCommuncation.NotifyNewMapRotation:
            {
                if (stream.CanRead(4))
                {
                    var count = stream.ReadUInt32();
                    lock (resources._MapRotation)
                    {
                        resources._MapRotation.Clear();
                        while (count > 0)
                        {
                            count--;
                            if (stream.TryReadString(out var map))
                                resources._MapRotation.Add(map.ToUpperInvariant());
                        }
                    }
                }

                break;
            }
            case NetworkCommuncation.NotifyNewGamemodeRotation:
            {
                if (stream.CanRead(4))
                {
                    var count = stream.ReadUInt32();
                    lock (resources._GamemodeRotation)
                    {
                        resources._GamemodeRotation.Clear();
                        while (count > 0)
                        {
                            count--;
                            if (stream.TryReadString(out var map))
                                resources._GamemodeRotation.Add(map);
                        }
                    }
                }

                break;
            }
            case NetworkCommuncation.NotifyNewRoundState:
            {
                if (stream.CanRead(RoundSettings<TPlayer>.mRoundSettings.Size))
                {
                    var oldState = resources._RoundSettings.State;
                    resources._RoundSettings.Read(stream);
                    var newState = resources._RoundSettings.State;

                    if (newState != oldState)
                    {
                        server.OnGameStateChanged(oldState, newState);

                        if (newState == GameState.Playing)
                            server.OnRoundStarted();
                        else if (newState == GameState.EndingGame)
                            server.OnRoundEnded();
                    }
                }

                break;
            }
            case NetworkCommuncation.OnPlayerAskingToChangeTeam:
            {
                if (stream.CanRead(8 + 1))
                {
                    var steamID = stream.ReadUInt64();
                    var team = (Team)stream.ReadInt8();

                    if (resources.TryGetPlayer(steamID, out var client))
                    {
                        async Task mHandle()
                        {
                            var accepted = await server.OnPlayerRequestingToChangeTeam((TPlayer)client, team);
                            if (accepted)
                                server.ChangeTeam(steamID, team);
                        }

                        mHandle();
                    }
                }

                break;
            }
            case NetworkCommuncation.GameTick:
            {
                if (stream.CanRead(4 + 4 + 4))
                {
                    var decompressX = stream.ReadFloat();
                    var decompressY = stream.ReadFloat();
                    var decompressZ = stream.ReadFloat();

                    int playerCount = stream.ReadInt8();
                    while (playerCount > 0)
                    {
                        playerCount--;
                        var steamID = stream.ReadUInt64();

                        //TODO, can compressed further later.
                        var com_posX = stream.ReadUInt16();
                        var com_posY = stream.ReadUInt16();
                        var com_posZ = stream.ReadUInt16();
                        var com_healt = stream.ReadInt8();
                        var standing = (PlayerStand)stream.ReadInt8();
                        var side = (LeaningSide)stream.ReadInt8();
                        var loadoutIndex = (LoadoutIndex)stream.ReadInt8();
                        var inSeat = stream.ReadBool();
                        var isBleeding = stream.ReadBool();
                        var ping = stream.ReadUInt16();

                        var @internal = mInstanceDatabase.GetPlayerInternals(steamID);
                        if (@internal.IsAlive)
                        {
                            var newHP = com_healt * 0.5f - 1f;

                            if (LogLevel.HasFlag(LogLevel.HealtChanges))
                            {
                                var player = resources.Players[steamID];
                                var dtHP = newHP - @internal.HP;
                                if (dtHP > 0)
                                    //Heal
                                    OnLog(LogLevel.HealtChanges,
                                        $"{player} was healed by {dtHP} HP (new HP is {newHP} HP)", player);
                                else if (dtHP < 0)
                                    //Damage
                                    OnLog(LogLevel.HealtChanges,
                                        $"{player} was damaged by {-dtHP} HP (new HP is {newHP} HP)", player);
                            }

                            @internal.Position = new Vector3
                            {
                                X = com_posX * decompressX,
                                Y = com_posY * decompressY,
                                Z = com_posZ * decompressZ
                            };
                            @internal.HP = newHP;
                            @internal.Standing = standing;
                            @internal.Leaning = side;
                            @internal.CurrentLoadoutIndex = loadoutIndex;
                            @internal.InVehicle = inSeat;
                            @internal.IsBleeding = isBleeding;
                            @internal.PingMs = ping;
                        }
                    }
                }

                break;
            }
            case NetworkCommuncation.OnPlayerGivenUp:
            {
                if (stream.CanRead(8))
                {
                    var steamID = stream.ReadUInt64();
                    if (resources.TryGetPlayer(steamID, out var player))
                    {
                        player.OnGivenUp();
                        server.OnPlayerGivenUp((TPlayer)player);

                        if (LogLevel.HasFlag(LogLevel.KillsAndSpawns))
                            OnLog(LogLevel.KillsAndSpawns, $"{player} has givenup", player);
                    }
                }

                break;
            }
            case NetworkCommuncation.OnPlayerRevivedAnother:
            {
                if (stream.CanRead(8 + 8))
                {
                    var from = stream.ReadUInt64();
                    var to = stream.ReadUInt64();
                    if (resources.TryGetPlayer(to, out var toClient))
                    {
                        toClient.OnRevivedByAnotherPlayer();

                        if (resources.TryGetPlayer(from, out var fromClient))
                        {
                            fromClient.OnRevivedAnotherPlayer();
                            server.OnAPlayerRevivedAnotherPlayer((TPlayer)fromClient, (TPlayer)toClient);

                            if (LogLevel.HasFlag(LogLevel.KillsAndSpawns))
                                OnLog(LogLevel.KillsAndSpawns, $"{fromClient} revived {toClient}", null);
                        }
                    }
                }

                break;
            }
            case NetworkCommuncation.OnSquadPointsChanged:
            {
                if (stream.CanRead(1 + 1 + 4))
                {
                    var team = (Team)stream.ReadInt8();
                    var squad = (Squads)stream.ReadInt8();
                    var points = stream.ReadInt32();

                    var msquad = server.GetSquad(team, squad);
                    var rsquad = resources.GetSquadInternal(msquad);

                    if (rsquad.SquadPoints != points)
                    {
                        rsquad.SquadPoints = points;
                        server.OnSquadPointsChanged(msquad, points);
                    }

                    if (LogLevel.HasFlag(LogLevel.Squads))
                        OnLog(LogLevel.Squads, $"{msquad} now has {points} points", msquad);
                }

                break;
            }
            case NetworkCommuncation.NotifyNewRoundID:
            {
                if (stream.CanRead(4 + 8))
                {
                    resources.RoundIndex = stream.ReadUInt32();
                    resources.SessionID = stream.ReadInt64();

                    if (resources.mPreviousSessionID != resources.SessionID)
                    {
                        var oldSession = resources.mPreviousSessionID;
                        resources.mPreviousSessionID = resources.SessionID;

                        if (oldSession != 0)
                            server.OnSessionChanged(oldSession, resources.SessionID);
                    }

                    foreach (var item in resources.Players)
                    {
                        var player_internal = mInstanceDatabase.GetPlayerInternals(item.Key);
                        player_internal.SessionID = resources.SessionID;

                        if (player_internal.PreviousSessionID != player_internal.SessionID)
                        {
                            var previousID = player_internal.PreviousSessionID;
                            player_internal.PreviousSessionID = player_internal.SessionID;

                            if (previousID != 0)
                                item.Value.OnSessionChanged(previousID, player_internal.SessionID);
                        }
                    }
                }

                break;
            }
            case NetworkCommuncation.Log:
            {
                if (LogLevel.HasFlag(LogLevel.GameServerErrors))
                    if (stream.TryReadString(out var log))
                        OnLog(LogLevel.GameServerErrors, log, server);
                break;
            }
        }
    }

    // --- Private ---
    private void mCleanup(GameServer<TPlayer> server, GameServer<TPlayer>.Internal @internal)
    {
        lock (@internal.Players)
        {
            foreach (var item in @internal.Players)
            {
                var player_internal = mInstanceDatabase.GetPlayerInternals(item.Key);
                player_internal.SessionID = 0;
                player_internal.GameServer = null;
            }
        }
    }

    private Player<TPlayer>.Internal mGetPlayerInternals(ulong steamID)
    {
        return mInstanceDatabase.GetPlayerInternals(steamID);
    }

    public bool TryGetGameServer(IPAddress ip, ushort port, out TGameServer server)
    {
        var hash = ((ulong)port << 32) | ip.ToUInt();
        lock (mActiveConnections)
        {
            if (mActiveConnections.TryGetValue(hash, out var _server))
            {
                server = _server.server;
                return true;
            }
        }

        server = default;
        return false;
    }

    // --- Classes --- 
    private class mInstances<TPlayer, TGameServer> where TPlayer : Player<TPlayer>
        where TGameServer : GameServer<TPlayer>
    {
        private readonly Dictionary<ulong, (TGameServer, GameServer<TPlayer>.Internal)> mGameServerInstances;
        private readonly Dictionary<ulong, (TPlayer, Player<TPlayer>.Internal)> mPlayerInstances;

        public mInstances()
        {
            mGameServerInstances = new Dictionary<ulong, (TGameServer, GameServer<TPlayer>.Internal)>(64);
            mPlayerInstances = new Dictionary<ulong, (TPlayer, Player<TPlayer>.Internal)>(1024 * 16);
        }

        public TGameServer GetServerInstance(ulong hash, out GameServer<TPlayer>.Internal @internal,
            Func<IPAddress, ushort, TGameServer> createFunc, IPAddress ip, ushort port)
        {
            lock (mGameServerInstances)
            {
                if (mGameServerInstances.TryGetValue(hash, out var data))
                {
                    @internal = data.Item2;
                    return data.Item1;
                }

                GameServer<TPlayer> server;
                if (createFunc != null)
                    server = createFunc(ip, port);
                else
                    server = Activator.CreateInstance<TGameServer>();

                @internal = new GameServer<TPlayer>.Internal(server);

                GameServer<TPlayer>.SetInstance(server, @internal);

                mGameServerInstances.Add(hash, ((TGameServer)server, @internal));
                return (TGameServer)server;
            }
        }

        public TPlayer GetPlayerInstance(ulong steamID, out Player<TPlayer>.Internal @internal,
            Func<ulong, TPlayer> createFunc)
        {
            lock (mPlayerInstances)
            {
                if (mPlayerInstances.TryGetValue(steamID, out var player))
                {
                    @internal = player.Item2;
                    return player.Item1;
                }

                @internal = new Player<TPlayer>.Internal();

                Player<TPlayer> pplayer;

                if (createFunc != null)
                    pplayer = createFunc(steamID);
                else
                    pplayer = Activator.CreateInstance<TPlayer>();
                Player<TPlayer>.SetInstance((TPlayer)pplayer, @internal);

                mPlayerInstances.Add(steamID, ((TPlayer)pplayer, @internal));
                return (TPlayer)pplayer;
            }
        }

        public Player<TPlayer>.Internal GetPlayerInternals(ulong steamID)
        {
            lock (mPlayerInstances)
            {
                return mPlayerInstances[steamID].Item2;
            }
        }
    }
}