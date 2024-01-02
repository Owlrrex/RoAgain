using OwlLogging;
using UnityEngine;

namespace Client
{
    public class MainMenu : MonoBehaviour
    {
        public Vector4 StartDummyServerPlacement;
        public Vector4 LoginPlacement;
        public Vector4 FailedLoginPlacement;
        public Vector4 DisconnectPlacement;
        public Vector4 TitlePlacement;
        [SerializeField]
        private Rect _ipPlacement;

        // this is actually login-logic, this needs to go somewhere better, I guess?
        private bool _isLoginRunning;
        private ServerConnection _connectionCache;

        private string _usernameCache;
        private string _passwordCache;

        public void OnEnable()
        {
            if (ClientMain.Instance != null && ClientMain.Instance.ConnectionToServer != null)
            {
                SetupWithConnection(ClientMain.Instance.ConnectionToServer);
            }
        }

        public void OnDisable()
        {
            DetachFromConnection();
        }

        private void Start()
        {
            if (ClientMain.Instance != null)
            {
                if (ClientMain.Instance.MainMenu != null)
                {
                    OwlLogger.LogError("Duplicate MainMenu instance!", GameComponent.UI);
                    Destroy(this);
                }
                else
                {
                    ClientMain.Instance.MainMenu = this;
                }
            }
        }

        private void OnDestroy()
        {
            if (ClientMain.Instance != null && ClientMain.Instance.MainMenu == this)
            {
                ClientMain.Instance.MainMenu = null;
            }
        }

        private void ConnectToServer()
        {
            if (ClientMain.Instance.ConnectionToServer == null)
            {
                bool successful = ClientMain.Instance.ConnectToServer();
                if (!successful)
                {
                    OwlLogger.LogError("Connecting to server failed!", GameComponent.Network);
                    return;
                }
            }

            SetupWithConnection(ClientMain.Instance.ConnectionToServer);
        }

        private void TryLogin(string username, string password)
        {
            if (_connectionCache != null)
            {
                OwlLogger.Log("Already connected to server.", GameComponent.Other);
                return;
            }

            _usernameCache = username;
            _passwordCache = password;

            ConnectToServer();
        }

        private void SetupWithConnection(ServerConnection connection)
        {
            if (_connectionCache != null)
                DetachFromConnection();

            OwlLogger.Log($"Setting up with connection {connection}", GameComponent.Other);
            _connectionCache = connection;
            _connectionCache.LoginResponseReceived += OnLoginResponseReceived;
            _connectionCache.SessionReceived += OnSessionReceived;
        }

        private void DetachFromConnection()
        {
            if (_connectionCache == null)
                return;

            OwlLogger.Log($"Detaching from connection {_connectionCache}", GameComponent.Other);
            _connectionCache.LoginResponseReceived -= OnLoginResponseReceived;
            _connectionCache = null;
        }

        private void OnSessionReceived(int sessionId)
        {
            if (_connectionCache == null)
            {
                Debug.LogError($"Can't attempt login without connection to server!");
                return;
            }

            // send login packet
            LoginRequestPacket loginPacket = new() { Username = _usernameCache, Password = _passwordCache };
            _usernameCache = string.Empty;
            _passwordCache = string.Empty;

            _isLoginRunning = true;
            _connectionCache.Send(loginPacket);
        }

        private void OnLoginResponseReceived(LoginResponse loginResponse)
        {
            OwlLogger.Log($"Client received login response: {loginResponse}", GameComponent.Other);
            _isLoginRunning = false;

            if (loginResponse.IsSuccessful == false)
            {
                OwlLogger.Log($"Failed login received - disconnecting from Server", GameComponent.Other);
                OnDisconnect();
                return;
            }

            //ClientMain.Instance.LoginComplete(loginResponse.SessionId);
        }

        private void OnDisconnect()
        {
            ClientMain.Instance.Disconnect();
            DetachFromConnection();
        }

        private void OnGUI()
        {
            // Title
            GUI.Label(TitlePlacement.ToRect(), "Ragnarok Again (Client)");

            // Target IP
            Rect working = _ipPlacement;
            working.width /= 2;
            GUI.Label(working, "Server IP:");
            working.x += working.width + 5;
            ClientMain.Instance.IpInput = GUI.TextField(working, ClientMain.Instance.IpInput);

            // Login-Button/Label
            if (_isLoginRunning)
            {
                GUI.Label(LoginPlacement.ToRect(), "Login in progress...");
            }
            else
            {
                if (GUI.Button(LoginPlacement.ToRect(), "Login with hardcoded credentials"))
                {
                    TryLogin("hardcodedUsername", "hardcodedPassword");
                }
            }

            // Failed-Login-Button/Label
            if (!_isLoginRunning)
            {
                if (GUI.Button(FailedLoginPlacement.ToRect(), "Login with failing credentials"))
                {
                    TryLogin("Fail", "Invalid");
                }
            }
        }
    }
}