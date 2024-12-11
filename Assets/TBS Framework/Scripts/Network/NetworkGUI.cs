using System;
using System.Collections;
using System.Collections.Generic;
using TbsFramework.Grid;
using TbsFramework.Players;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace TbsFramework.Network
{
    public class NetworkGUI : MonoBehaviour
    {
        /// <summary>
        /// The NetworkConnection object assosiated with this scene
        /// </summary>
        [SerializeField] private NetworkConnection _networkConnection;
        [SerializeField] private CellGrid _cellGrid;

        [SerializeField] private Text _statusText;
        [SerializeField] private Text _roomNameText;

        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private GameObject _roomPanel;

        [SerializeField] private GameObject _playerEntryPrefab;
        [SerializeField] private GameObject _roomEntryPrefab;

        [SerializeField] private InputField _usernameInput;
        [SerializeField] private InputField _createRoomNameInput;
        [SerializeField] private InputField _joinRoomNameInput;

        [SerializeField] private Button _connectButton;
        [SerializeField] private Button _quickMatchButton;
        [SerializeField] private Button _createRoomButton;
        [SerializeField] private Button _joinRoomButton;
        [SerializeField] private Button _leaveRoomButton;

        [SerializeField] private Toggle _isPrivateToggle;

        [SerializeField] private Transform _playersParent;

        private NetworkUser _localUser;
        private int _localPlayerNumber;
        private Dictionary<string, GameObject> _playerPanels = new Dictionary<string, GameObject>();
        private List<GameObject> _roomPanels = new List<GameObject>();
        private int _readyCount = 0;

        /// <summary>
        /// The maximum number of human players that can join and participate in a multiplayer game session.
        /// </summary>
        [SerializeField] private int _maxPlayers = 2;

        private void Start()
        {
            _networkConnection.ServerConnected += OnServerConnected;
            _networkConnection.RoomJoined += OnRoomJoined;
            _networkConnection.RoomExited += OnRoomExited;
            _networkConnection.PlayerEnteredRoom += OnPlayerEnteredRoom;
            _networkConnection.PlayerLeftRoom += OnPlayerLeftRoom;
            _networkConnection.CreateRoomFailed += OnFailed;
            _networkConnection.JoinRoomFailed += OnFailed;


            _networkConnection.AddHandler((actionParams) => OnPlayerNumberChanged(actionParams), (long)OpCode.PlayerNumberChanged);
            _networkConnection.AddHandler((actionParams) => OnPlayerReadyChanged(actionParams), (long)OpCode.IsReadyChanged);

            _createRoomButton.onClick.AddListener(() => _networkConnection.CreateRoom(_createRoomNameInput.text, _maxPlayers, _isPrivateToggle.isOn, new Dictionary<string, string>()));
            _joinRoomButton.onClick.AddListener(() => _networkConnection.JoinRoomByName(_joinRoomNameInput.text));
            _leaveRoomButton.onClick.AddListener(() => { _networkConnection.LeaveRoom(); });
        }

        private void OnFailed(object sender, string message)
        {
            SetStatus(message);
        }

        private void OnPlayerLeftRoom(object sender, NetworkUser networkUser)
        {
            Destroy(_playerPanels[networkUser.UserID]);
            _playerPanels.Remove(networkUser.UserID);
            _readyCount -= networkUser.CustomProperties.ContainsKey("isReady") ? bool.Parse(networkUser.CustomProperties["isReady"]) ? 1 : 0 : 0;

            _cellGrid.CheckGameFinished();
        }

        private void OnPlayerEnteredRoom(object sender, NetworkUser networkUser)
        {
            var playerSelectionPanelInstance = CreatePlayerPanel(networkUser, _playerPanels.Count + 1, _maxPlayers, string.Empty, false);
            playerSelectionPanelInstance.SetActive(true);

            _playerPanels.Add(networkUser.UserID, playerSelectionPanelInstance);
        }

        private void OnRoomJoined(object sender, RoomData roomData)
        {
            _createRoomNameInput.text = string.Empty;
            _quickMatchButton.interactable = false;

            _playerPanels = new Dictionary<string, GameObject>();
            _localUser = roomData.LocalUser;

            _lobbyPanel.SetActive(false);
            _roomPanel.SetActive(true);
            _roomNameText.text = roomData.RoomName;
            int userIndex = 1;
            foreach (var networkUser in roomData.Users)
            {
                var playerNumber = networkUser.CustomProperties.ContainsKey("playerNumber") ? networkUser.CustomProperties["playerNumber"] : string.Empty;
                var isReady = networkUser.CustomProperties.ContainsKey("isReady") && bool.Parse(networkUser.CustomProperties["isReady"]);
                var playerSelectionPanelInstance = CreatePlayerPanel(networkUser, userIndex, roomData.MaxUsers, playerNumber, isReady);
                playerSelectionPanelInstance.SetActive(true);

                _playerPanels.Add(networkUser.UserID, playerSelectionPanelInstance);
                userIndex += 1;
            }
            SetStatus("Room joined");
        }

        private void OnRoomExited(object sender, EventArgs e)
        {
            _lobbyPanel.SetActive(true);
            _roomPanel.SetActive(false);
            _quickMatchButton.interactable = true;
            _readyCount = 0;

            foreach (var key in _playerPanels.Keys)
            {
                Destroy(_playerPanels[key]);
            }
            _playerPanels = new Dictionary<string, GameObject>();

            SetStatus("Room exited");
        }

        private GameObject CreatePlayerPanel(NetworkUser user, int userIndex, int maxUserCount, string playerNumber, bool isReady)
        {
            Assert.IsNotNull(_localUser, $"{nameof(_localUser)} field is not set up");

            var playerSelectionPanelInstance = Instantiate(_playerEntryPrefab, _playerEntryPrefab.transform.parent);
            playerSelectionPanelInstance.transform.Find("Player#").GetComponent<Text>().text = string.Format("#{0}", userIndex.ToString());
            playerSelectionPanelInstance.transform.Find("PlayerName").GetComponent<Text>().text = user.UserName;

            playerSelectionPanelInstance.transform.Find("PlayerNumber").GetComponentInChildren<InputField>().text = playerNumber;
            playerSelectionPanelInstance.transform.Find("PlayerNumber").GetComponent<InputField>().interactable = user.UserID.Equals(_localUser.UserID);
            if (user.UserID.Equals(_localUser.UserID))
            {
                playerSelectionPanelInstance.transform.Find("PlayerNumber").GetComponent<InputField>().onValueChanged.AddListener((value) =>
                {
                    playerSelectionPanelInstance.transform.Find("IsReady").GetComponent<Toggle>().interactable = value != string.Empty && user.UserID.Equals(_localUser.UserID);
                    _localPlayerNumber = int.Parse(value);
                    var actionParams = new Dictionary<string, string>
                    {
                        { "user_id", _localUser.UserID },
                        { "player_number", value.ToString() }
                    };
                    _networkConnection.SendMatchState((long)OpCode.PlayerNumberChanged, actionParams);
                });
            }

            playerSelectionPanelInstance.transform.Find("IsReady").GetComponent<Toggle>().isOn = isReady;
            playerSelectionPanelInstance.transform.Find("IsReady").GetComponent<Toggle>().interactable = playerNumber != string.Empty && user.UserID.Equals(_localUser.UserID);
            if (user.UserID.Equals(_localUser.UserID))
            {
                playerSelectionPanelInstance.transform.Find("IsReady").GetComponent<Toggle>().onValueChanged.AddListener((value) =>
                {
                    playerSelectionPanelInstance.transform.Find("PlayerNumber").GetComponent<InputField>().interactable = !value;

                    var actionParams = new Dictionary<string, string>
                    {
                    { "user_id", _localUser.UserID },
                    { "is_ready", value.ToString() }
                    };
                    _networkConnection.SendMatchState((long)OpCode.IsReadyChanged, actionParams);
                    _readyCount += value ? 1 : -1;
                    if (_readyCount == maxUserCount)
                    {
                        StartCoroutine(SetupMatch());
                    }
                });
            }

            return playerSelectionPanelInstance;
        }

        private void OnPlayerReadyChanged(Dictionary<string, string> actionParams)
        {
            var userID = actionParams["user_id"];
            if (userID.Equals(_localUser.UserID))
            {
                return;
            }

            var isReady = bool.Parse(actionParams["is_ready"]);
            _readyCount += isReady ? 1 : -1;

            if (_readyCount == _maxPlayers)
            {
                StartCoroutine(SetupMatch());
            }

            _playerPanels[userID].transform.Find("IsReady").GetComponent<Toggle>().isOn = bool.Parse(actionParams["is_ready"]);
        }

        private void OnPlayerNumberChanged(Dictionary<string, string> actionParams)
        {
            var userID = actionParams["user_id"];
            if (userID.Equals(_localUser.UserID))
            {
                return;
            }

            _playerPanels[userID].transform.Find("PlayerNumber").GetComponent<InputField>().interactable = true;
            _playerPanels[userID].transform.Find("PlayerNumber").GetComponent<InputField>().text = actionParams["player_number"];
            _playerPanels[userID].transform.Find("PlayerNumber").GetComponent<InputField>().interactable = false;
        }

        private void OnServerConnected(object sender, EventArgs e)
        {
            SetStatus("Connected");
            _quickMatchButton.interactable = true;
            _connectButton.interactable = false;
            _lobbyPanel.SetActive(true);
        }

        private IEnumerator SetupMatch()
        {
            for (int i = 0; i < _playersParent.childCount; i++)
            {
                var playerGO = _playersParent.GetChild(i).gameObject;
                if (!playerGO.activeInHierarchy)
                {
                    continue;
                }

                var player = playerGO.GetComponent<Player>();
                var playerNumber = player.PlayerNumber;

                if (!playerNumber.Equals(_localPlayerNumber) && player is HumanPlayer || player is AIPlayer && !_networkConnection.IsHost)
                {
                    Destroy(player);
                    var remotePlayer = playerGO.AddComponent<RemotePlayer>();
                    remotePlayer.NetworkConnection = _networkConnection;
                    remotePlayer.PlayerNumber = playerNumber;
                }
            }

            yield return new WaitForEndOfFrame();
            gameObject.SetActive(false);
            _cellGrid.InitializeAndStart();
        }

        public void ConnectToServer()
        {
            SetStatus("Connecting...");
            var userName = _usernameInput.text;
            _networkConnection.ConnectToServer(userName, new Dictionary<string, string>());
        }

        public void JoinQuickMatch()
        {
            SetStatus("Looking for match...");
            _networkConnection.JoinQuickMatch(_maxPlayers, new Dictionary<string, string>());
        }

        public async void RefreshLobby()
        {
            for (int i = 0; i < _roomPanels.Count; i++)
            {
                Destroy(_roomPanels[i]);
            }
            _roomPanels.Clear();

            var roomIndex = 1;
            foreach (var room in await _networkConnection.GetRoomList())
            {
                var roomEntry = Instantiate(_roomEntryPrefab, _roomEntryPrefab.transform.parent);
                roomEntry.transform.Find("Room#").GetComponent<Text>().text = string.Format("#{0}", roomIndex.ToString());
                roomEntry.transform.Find("RoomNameText").GetComponent<Text>().text = room.RoomName;
                roomEntry.transform.Find("RoomCapacityText").GetComponent<Text>().text = string.Format("{0}/{1}", room.UserCount, room.MaxUsers);
                roomEntry.transform.Find("JoinButton").GetComponent<Button>().onClick.AddListener(() => _networkConnection.JoinRoomByID(room.RoomID));
                roomEntry.SetActive(true);

                _roomPanels.Add(roomEntry);
            }
        }

        private void SetStatus(string status)
        {
            _statusText.text = status;
        }
    }
}