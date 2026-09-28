using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using WorkFolder.PAP.Scripts.MapSystem;

namespace _00.Scripts.Lobby
{
    public sealed class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        public NetworkRunner runner;

        private List<SessionInfo> _realSessionList;
        public static LobbyManager instance;
        
        [SerializeField] private Transform sessionContent;
        [SerializeField] private SessionItem sessionItemPrefab;
        [SerializeField] private PrivateRoomJoin privateRoomJoin;
        [SerializeField] private GameObject loading;
        [SerializeField] private GameObject loadingFailed;
        [SerializeField]private TextMeshProUGUI logText;
        
        private Coroutine _textCorutine;

        // 방 만들기 UI의 맵 드롭다운(MapSelectDropdown)이 고른 맵 번호. MapCatalog 순서와 같다.
        public int SelectedMapIndex { get; set; }

        #region LifeCycle
        
        private void Awake()
        {
            instance = this;
            _realSessionList = new List<SessionInfo>();
            runner.AddCallbacks(this);
        }

        private void Start()
        {
            JoinLobby();
        }
        
        #endregion
        
        #region MakeRoom
        
        public async void CreateRoom(string roomName, bool isPrivate, string passWord)
        {
            loading.SetActive(true);
            var properties = new Dictionary<string, SessionProperty>()
            {
                { "private", isPrivate ? 1 : 0 },
                // 게임 씬의 MapLoader가 이 값을 읽어 같은 맵을 생성한다.
                { MapCatalogSO.SessionPropertyKey, SelectedMapIndex }
            };

            if (isPrivate)
            {
                properties.Add("password", passWord);
            }

            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = roomName,
                Scene = SceneRef.FromIndex(1),
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>(),
                SessionProperties = properties
            });

            if (result.Ok)
            {
                Debug.Log($"방 생성 성공: {roomName}");
            }
            else
            {
                TextChange($"방 생성 실패: {result.ShutdownReason}");
                Debug.LogError($"방 생성 실패: {result.ShutdownReason}");
                loading.SetActive(false);
            }
        }
        
        public void MakePublic(string roomName)
        {
            CreateRoom(roomName, false, "");
        }

        public void MakePrivate(string roomName, string passWord)
        {
            CreateRoom(roomName, true, passWord);
        }
        
        #endregion
        
        #region Join
        
        public void TryJoinPrivateRoom(string roomName, string password)
        {
            foreach (SessionInfo session in _realSessionList)
            {
                if (session.Name != roomName)
                    continue;

                if (!session.Properties.TryGetValue("password", out SessionProperty passwordProperty))
                {
                    Debug.LogError("비밀번호 정보가 없습니다.");
                    return;
                }

                string savedPassword = (string)passwordProperty.PropertyValue;

                if (savedPassword == password)
                {
                    privateRoomJoin.Close();

                    JoinRoom(roomName);
                }
                else
                {
                   TextChange("비밀번호가 일치하지 않습니다.");
                }

                return;
            }
        }
        
        public async void JoinLobby()
        {
            loadingFailed.SetActive(false);
            loading.SetActive(true);
            
            var result = await runner.JoinSessionLobby(SessionLobby.Shared);
            
            if (result.Ok)
            {
                Debug.Log("Shared Lobby 참가 성공");
                loading.SetActive(false);
            }

            else
            {
                Debug.LogError($"Lobby 참가 실패: {result.ShutdownReason}");
                loadingFailed.SetActive(true);
            }
        }
        
        public async void JoinRoom(string roomName)
        {
            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = roomName,
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
            });

            
            
            if (result.Ok)
            {
                Debug.Log("Joined room");
            }
            else
            {
                Debug.Log($"Failed : {result.ShutdownReason}");
                TextChange($"예상치 못한 오류가 발생했습니다.{result.ShutdownReason}");
            }
        }
        
        #endregion
        #region Etc
        public void TextChange(string text)
        {
            _textCorutine =  StartCoroutine(TextCorutine(text));
        }
        
        private IEnumerator TextCorutine(string text)
        {
            if (_textCorutine != null)
            {
                StopCoroutine(_textCorutine);
            }
            logText.text = text;
            yield return new WaitForSeconds(1.5f);
            logText.text = "";
        }
        
        public void OpenPasswordPopup(string roomName)
        {
            privateRoomJoin.Open(roomName);
        }
        
        #endregion
        

        public void OnSessionListUpdated( NetworkRunner runner, List<SessionInfo> sessionList)
        {
            _realSessionList.Clear();

            foreach (Transform child in sessionContent)
            {
                Destroy(child.gameObject);
            }

            foreach (SessionInfo session in sessionList)
            {
                _realSessionList.Add(session);

                Debug.Log($"방 발견: {session.Name}");
                Debug.Log($"방 발견: {session.PlayerCount}");

                bool isPrivate = false;

                if (session.Properties.TryGetValue("private", out SessionProperty privateProperty))
                {
                    isPrivate = privateProperty == 1;
                }

                SessionItem item = Instantiate(
                    sessionItemPrefab,
                    sessionContent
                );

                item.SetSession(
                    session.Name,
                    session.PlayerCount,
                    session.MaxPlayers,
                    isPrivate
                );
            }
        }
    
        #region Useless
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            
        }
        #endregion
    }
}
