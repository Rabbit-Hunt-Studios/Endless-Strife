using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using TbsFramework.Grid;
using UnityEngine;
using System.Collections;
using System.Linq;
using TbsFramework.Units;

namespace TbsFramework.Network
{
    /// <summary>
    /// An abstract class for managing network connections in a multiplayer game.
    /// Implementations of this class can provide different online backends such as Nakama, Photon, etc.
    /// </summary>
    public abstract class NetworkConnection : MonoBehaviour
    {
        [SerializeField] private CellGrid _cellGrid;

        /// <summary>
        /// Event triggered when the server connection is successfully established.
        /// </summary>
        public event EventHandler ServerConnected;

        /// <summary>
        /// Event triggered when a room is successfully joined. Carries room data as event arguments.
        /// </summary>
        public event EventHandler<RoomData> RoomJoined;

        /// <summary>
        /// Event triggered when the local player exits a room.
        /// </summary>
        public event EventHandler RoomExited;

        /// <summary>
        /// Event triggered when a new player enters the room. Carries the player's network user data.
        /// </summary>
        public event EventHandler<NetworkUser> PlayerEnteredRoom;

        /// <summary>
        /// Event triggered when a player leaves the room. Carries the player's network user data.
        /// </summary>
        public event EventHandler<NetworkUser> PlayerLeftRoom;

        /// <summary>
        /// Event triggered if joining a room fails. Carries a message detailing the failure.
        /// </summary>
        public event EventHandler<string> JoinRoomFailed;

        /// <summary>
        /// Event triggered if creating a room fails. Carries a message detailing the failure.
        /// </summary>
        public event EventHandler<string> CreateRoomFailed;

        /// <summary>
        /// Property indicating if the local player is the host of the current room.
        /// </summary>
        public virtual bool IsHost { get; protected set; }

        protected Dictionary<long, Action<Dictionary<string, string>>> Handlers = new Dictionary<long, Action<Dictionary<string, string>>>();
        protected Queue<(Action preAction, Func<IEnumerator> routine)> EventQueue = new Queue<(Action preAction, Func<IEnumerator> routine)>();
        protected bool processingEvents;

        /// <summary>
        /// Connect to the multiplayer game server.
        /// </summary>
        /// <param name="userName">The name of the user connecting to the server.</param>
        /// <param name="customParams">Additional custom parameters for the connection.</param>
        public abstract void ConnectToServer(string userName, Dictionary<string, string> customParams);

        /// <summary>
        /// Join a quick match with the specified maximum number of players.
        /// </summary>
        /// <param name="maxPlayers">Maximum number of players in the match.</param>
        /// <param name="customParams">Additional custom parameters for the match.</param>
        public abstract void JoinQuickMatch(int maxPlayers, Dictionary<string, string> customParams);

        /// <summary>
        /// Create a new room with the specified parameters.
        /// </summary>
        /// <param name="roomName">Name of the room to create.</param>
        /// <param name="maxPlayers">Maximum number of players in the room.</param>
        /// <param name="isPrivate">Whether the room is private. A private room will not be listed by the <see cref="GetRoomList"/> method</param>
        /// <param name="customParams">Additional custom parameters for the room.</param>
        public abstract void CreateRoom(string roomName, int maxPlayers, bool isPrivate, Dictionary<string, string> customParams);

        /// <summary>
        /// Join an existing room by its name.
        /// </summary>
        /// <param name="roomName">The name of the room to join.</param>
        public abstract void JoinRoomByName(string roomName);

        /// <summary>
        /// Join an existing room by its unique ID.
        /// </summary>
        /// <param name="roomID">The unique identifier of the room to join.</param>
        public abstract void JoinRoomByID(string roomID);

        /// <summary>
        /// Leave the current room.
        /// </summary>
        public abstract void LeaveRoom();

        /// <summary>
        /// Get a list of available public rooms.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of RoomData.</returns>
        public abstract Task<IEnumerable<RoomData>> GetRoomList();

        /// <summary>
        /// Send the current match state to other players in the room.
        /// </summary>
        /// <param name="opCode">Operation code indicating the type of the match state.</param>
        /// <param name="actionParams">Parameters representing the match state.</param>
        public abstract void SendMatchState(long opCode, IDictionary<string, string> actionParams);

        /// <summary>
        /// Adds a handler for processing specific network operations identified by an operation code.
        /// </summary>
        /// <param name="handler">The action to perform when the specified OpCode is received. The action takes a dictionary of string key-value pairs representing the parameters of the network operation.</param>
        /// <param name="OpCode">The operation code that identifies the type of network operation.</param>
        public virtual void AddHandler(Action<Dictionary<string, string>> handler, long OpCode)
        {
            Handlers.Add(OpCode, handler);
        }

        /// <summary>
        /// Initializes the random number generator with a specific seed. This is useful for ensuring that random number generation is consistent and replicable across different instances of the game, which is important in multiplayer environments.
        /// </summary>
        /// <param name="seed">The seed value to initialize the random number generator. Typically, this seed should be synchronized across all clients in a multiplayer game to ensure consistent random number generation.</param>
        public virtual void InitializeRng(int seed)
        {
            UnityEngine.Random.InitState(seed);
        }

        protected virtual void Awake()
        {
            Handlers.Add((long)OpCode.TurnEnded, HandleRemoteTurnEnding);
            Handlers.Add((long)OpCode.AbilityUsed, HandleRemoteAbilityUsed);
        }

        protected virtual void Start()
        {
            _cellGrid.UnitAdded += OnUnitAdded;
            _cellGrid.TurnEnded += OnTurnEndedLocal;
        }

        protected void InvokeServerConnected()
        {
            Debug.Log("Server connected");
            ServerConnected?.Invoke(this, EventArgs.Empty);
        }
        protected void InvokeRoomJoined(RoomData roomData)
        {
            var players = roomData.Users.ToList();
            Debug.Log($"Joined room: {roomData.RoomID}; players inside: {players.Count}");
            RoomJoined?.Invoke(this, roomData);
        }

        protected void InvokeRoomExited()
        {
            Debug.Log("Exited room");
            RoomExited?.Invoke(this, EventArgs.Empty);
        }

        protected void InvokePlayerEnteredRoom(NetworkUser networkUser)
        {
            Debug.Log($"Player {networkUser.UserID} entered room");
            PlayerEnteredRoom?.Invoke(this, networkUser);
        }

        protected void InvokePlayerLeftRoom(NetworkUser networkUser)
        {
            Debug.Log($"Player {networkUser.UserID} left room");
            PlayerLeftRoom?.Invoke(this, networkUser);
        }
        protected void InvokeCreateRoomFailed(string message)
        {
            CreateRoomFailed?.Invoke(this, message);
        }
        protected void InvokeJoinRoomFailed(string message)
        {
            JoinRoomFailed?.Invoke(this, message);
        }

        private void OnUnitAdded(object sender, UnitCreatedEventArgs e)
        {
            // Get Abilities of newly added unit and subscribe to their AbilityUsed event
            foreach (var ability in e.unit.GetComponent<Unit>().Abilities)
            {
                ability.AbilityUsed += OnAbilityUsedLocal;
            }
            e.unit.GetComponent<Unit>().AbilityAddded += OnAbilityAdded;
        }
        private void OnAbilityAdded(object sender, AbilityAddedEventArgs e)
        {
            e.ability.AbilityUsed += OnAbilityUsedLocal;
        }

        private void OnAbilityUsedLocal(object sender, (bool isNetworkInvoked, IDictionary<string, string> actionParams) e)
        {
            // If Ability was triggered by a remote instance, do nothing
            if (e.isNetworkInvoked)
            {
                return;
            }

            // If ability was triggered by the local instance, forward it to other players
            SendMatchState((int)OpCode.AbilityUsed, e.actionParams);
        }
        private void OnTurnEndedLocal(object sender, bool isNetworkInvoked)
        {
            // If turn ending was triggered by a remote instance, do nothing
            if (isNetworkInvoked)
            {
                return;
            }

            // If turn ending was triggered by the local instance, forward it to other players
            SendMatchState((int)OpCode.TurnEnded, new Dictionary<string, string>());
        }

        private void HandleRemoteAbilityUsed(Dictionary<string, string> actionParams)
        {
            var unit = _cellGrid.Units.Find(u => u.UnitID == int.Parse(actionParams["unit_id"]));
            var ability = unit.Abilities.Find(a => a.AbilityID == int.Parse(actionParams["ability_id"]));

            EventQueue.Enqueue((() => ability.OnAbilitySelected(_cellGrid), () => ability.Apply(_cellGrid, actionParams)));
            if (!processingEvents)
            {
                StartCoroutine(ProcessEvents());
            }
        }
        private void HandleRemoteTurnEnding(Dictionary<string, string> actionParams)
        {
            EventQueue.Enqueue((new Action(() => { }), () => EndTurn(true)));
            if (!processingEvents)
            {
                StartCoroutine(ProcessEvents());
            }
        }

        protected IEnumerator EndTurn(bool isNetworkInvoked)
        {
            _cellGrid.EndTurn(isNetworkInvoked);
            yield return null;
        }

        protected virtual IEnumerator ProcessEvents()
        {
            processingEvents = true;
            while (EventQueue.Count > 0)
            {
                var (preAction, routine) = EventQueue.Dequeue();

                preAction.Invoke();
                yield return StartCoroutine(routine.Invoke());
            }
            processingEvents = false;
        }
    }

    /// <summary>
    /// Represents the data for a room in a multiplayer game.
    /// Contains information about the room, such as the users in the room, room name, and ID.
    /// </summary>
    public class RoomData
    {
        /// <summary>
        /// The local user's network user data.
        /// </summary>
        public NetworkUser LocalUser { get; private set; }

        /// <summary>
        /// The collection of network users currently in the room.
        /// </summary>
        public IEnumerable<NetworkUser> Users { get; private set; }

        /// <summary>
        /// The current number of users in the room.
        /// </summary>
        public int UserCount { get; private set; }

        /// <summary>
        /// The maximum number of users allowed in the room.
        /// </summary>
        public int MaxUsers { get; private set; }

        /// <summary>
        /// The name of the room.
        /// </summary>
        public string RoomName { get; private set; }

        /// <summary>
        /// The unique identifier for the room.
        /// </summary>
        public string RoomID { get; private set; }

        /// <summary>
        /// Constructor for creating a new RoomData instance.
        /// </summary>
        /// <param name="localUser">Local user's network data.</param>
        /// <param name="users">List of users in the room.</param>
        /// <param name="userCount">Number of users currently in the room.</param>
        /// <param name="maxUsers">Maximum number of users allowed in the room.</param>
        /// <param name="roomName">Name of the room.</param>
        /// <param name="roomID">Unique identifier of the room.</param>
        public RoomData(NetworkUser localUser, IEnumerable<NetworkUser> users, int userCount, int maxUsers, string roomName, string roomID)
        {
            LocalUser = localUser;
            Users = users;
            UserCount = userCount;
            MaxUsers = maxUsers;
            RoomName = roomName;
            RoomID = roomID;
        }
    }

    /// <summary>
    /// Represents a user in a multiplayer network environment.
    /// Contains information such as the user's ID, name, and custom properties.
    /// </summary>
    public class NetworkUser
    {
        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        public string UserID { get; private set; }

        /// <summary>
        /// The name of the user.
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// Indicates whether the user is the host of the room.
        /// </summary>
        public bool IsHost { get; private set; }

        /// <summary>
        /// Custom properties associated with the user, represented as key-value pairs.
        /// </summary>
        public Dictionary<string, string> CustomProperties { get; private set; }

        /// <summary>
        /// Constructor for creating a new NetworkUser instance.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="userID">Unique identifier of the user.</param>
        /// <param name="customProperties">Custom properties of the user.</param>
        /// <param name="isHost">Indicates whether the user is the host of the room.</param>
        public NetworkUser(string userName, string userID, Dictionary<string, string> customProperties, bool isHost = false)
        {
            UserName = userName;
            UserID = userID;
            CustomProperties = customProperties;
            IsHost = isHost;
        }

        public override bool Equals(object obj)
        {
            return obj is NetworkUser && (obj as NetworkUser).UserID.Equals(UserID);
        }

        public override int GetHashCode()
        {
            return UserID.GetHashCode();
        }
    }


    public enum OpCode : long
    {
        TurnEnded,
        AbilityUsed,
        PlayerNumberChanged,
        IsReadyChanged,
    }
}