using OwlLogging;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class PreGameUI : MonoBehaviour
    {
        public static PreGameUI Instance;

        [SerializeField]
        private GameObject _loginWindowPrefab;

        [SerializeField]
        private GameObject _accountCreationWindowPrefab;

        [SerializeField]
        private GameObject _characterSelectionWindowPrefab;

        [SerializeField]
        private GameObject _characterCreationWindowPrefab;

        private GameObject _currentWindow;

        private ServerConnection _currentConnection;

        private AccountLogin _accountLogin;
        private AccountLogin.State _lastAccountLoginState = AccountLogin.State.Ready;
        private CharacterSelectionDataList _characterSelectionData;
        private bool _hasShownCharSelection;
        private CharacterLogin _characterLogin;

        void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_accountCreationWindowPrefab, "accountCreationWindowPrefab", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_loginWindowPrefab, "loginWindowPrefab", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_characterCreationWindowPrefab, "characterCreationWindowPrefab", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_characterSelectionWindowPrefab, "characterSelectionWindowPrefab", this, GameComponent.UI);

            if (Instance != null)
            {
                OwlLogger.LogError("PreGameUI instance alread set - duplicate gameobject!", GameComponent.UI);
                Destroy(this);
                return;
            }

            Instance = this;

            _accountLogin = new();
            _characterSelectionData = new();
            _characterLogin = new();
        }

        public void SetConnectionToServer(ServerConnection connection)
        {
            if(_currentConnection != null)
            {
                _currentConnection.AccountCreationResponseReceived -= OnAccountCreationResponseReceived;
                _currentConnection.CharacterCreationResponseReceived -= OnCharacterCreationResponseReceived;
            }

            _currentConnection = connection;

            _currentConnection.AccountCreationResponseReceived += OnAccountCreationResponseReceived;
            _currentConnection.CharacterCreationResponseReceived += OnCharacterCreationResponseReceived;
        }

        private void Update()
        {
            UpdateLoginPhaseUI();
        }

        private void UpdateLoginPhaseUI()
        {
            // TODO: Move this function to PreGameUI maybe?
            if (_accountLogin.CurrentState != _lastAccountLoginState)
            {
                _lastAccountLoginState = _accountLogin.CurrentState;
                switch (_accountLogin.CurrentState)
                {
                    case AccountLogin.State.Ready:
                        ShowLoginWindow();
                        break;
                    case AccountLogin.State.WaitingForAccountLogin:
                        DeleteCurrentWindow();
                        ClientMain.Instance.DisplayZeroButtonNotification("Logging into Account...");
                        break;
                    case AccountLogin.State.Error_LoginFail:
                        ClientMain.Instance.DisplayOneButtonNotification("Login failed!", () =>
                        {
                            ClientMain.Instance.DisplayOneButtonNotification(null, null);
                            ShowLoginWindow();
                        });
                        break;
                    case AccountLogin.State.Complete:
                        StartCharacterSelectionFetch();
                        break;
                }
            }

            if (!_hasShownCharSelection && _characterSelectionData.Data != null)
            {
                ShowCharacterSelectionWindow(_characterSelectionData.Data);
                _hasShownCharSelection = true;
            }
        }

        private void StartCharacterSelectionFetch()
        {
            ClientMain.Instance.DisplayZeroButtonNotification("Loading Characters...");
            _hasShownCharSelection = false;
            _characterSelectionData.Fetch(ClientMain.Instance.ConnectionToServer);
        }

        public void ShowLoginWindow()
        {
            DeleteCurrentWindow();
            _currentWindow = Instantiate(_loginWindowPrefab, ClientMain.Instance.MainUiCanvas.transform);
        }

        public void ShowAccountCreationWindow()
        {
            DeleteCurrentWindow();
            _currentWindow = Instantiate(_accountCreationWindowPrefab, ClientMain.Instance.MainUiCanvas.transform);
        }

        public void CreateAccount(string username, string password)
        {
            ClientMain.Instance.DisplayZeroButtonNotification("Creating account...");

            if (_currentConnection == null)
            {
                ClientMain.Instance.CreateSessionWithServer(() => CreateAccount(username, password));
                return;
            }

            AccountCreationRequestPacket packet = new()
            {
                Username = username,
                Password = password
            };
            _currentConnection.Send(packet);
        }

        private void OnAccountCreationResponseReceived(int result)
        {
            ClientMain.Instance.DisplayZeroButtonNotification(null);
            string message = result switch
            {
                0 => "Account creation successful!",
                1 => "Invalid password!",// TODO: Tell user about Password requirements
                2 => "Username already taken!",
                3 => "Invalid username!",
                _ => "Unknown error",
            };
            ClientMain.Instance.DisplayOneButtonNotification(message, () => { ShowLoginWindow(); });
        }

        public void ShowCharacterSelectionWindow(List<CharacterSelectionData> characterSelectionData)
        {
            DeleteCurrentWindow();
            _currentWindow = Instantiate(_characterSelectionWindowPrefab, ClientMain.Instance.MainUiCanvas.transform);
            if (!_currentWindow.TryGetComponent(out CharacterSelectionWindow charSelComp))
            {
                OwlLogger.LogError("Can't find CharacterSelectionWindow component on CharacterSelectionWindowPrefab!", GameComponent.UI);
                return;
            }
            charSelComp.Initialize(characterSelectionData);
        }

        public void ShowCharacterCreationWindow()
        {
            DeleteCurrentWindow();
            _currentWindow = Instantiate(_characterCreationWindowPrefab, ClientMain.Instance.MainUiCanvas.transform);
        }

        public void CreateCharacter(string name, int gender)
        {
            if (_currentConnection == null)
            {
                ClientMain.Instance.CreateSessionWithServer(() => CreateCharacter(name, gender));
                return;
            }

            ClientMain.Instance.DisplayZeroButtonNotification("Creating Character...");

            CharacterCreationRequestPacket packet = new()
            {
                Name = name,
                Gender = gender,
            };
            ClientMain.Instance.ConnectionToServer.Send(packet);
        }

        private void OnCharacterCreationResponseReceived(int result)
        {
            ClientMain.Instance.DisplayZeroButtonNotification(null);

            System.Action callback = null;
            string message;
            switch(result) // TODO: Better user-facing error messages
            {
                case 0:
                    message = "Character creation successful!";
                    callback = StartCharacterSelectionFetch;
                    break;
                case -1:
                    message = "Character Creation error code: -1";
                    break;
                case -2:
                    message = "Character Creation error code: -2";
                    break;
                case -3:
                    message = "Character Creation error code: -3";
                    break;
                case -10:
                case -4:
                    message = "Character-name already taken!";
                    break;
                default:
                    message = "Unknown Error code: " + result;
                    break;
            }

            ClientMain.Instance.DisplayOneButtonNotification(message, callback);
        }

        public void DeleteCurrentWindow()
        {
            if (_currentWindow == null)
                return;

            Destroy(_currentWindow);
            _currentWindow = null;
        }

        public void StartAccountLogin(string username, string password)
        {
            if (ClientMain.Instance.ConnectionToServer == null)
            {
                ClientMain.Instance.CreateSessionWithServer(() => { StartAccountLogin(username, password); });
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                OwlLogger.LogError($"Tried to start Account Login with empty username!", GameComponent.Other);
                return;
            }

            if (_accountLogin.CurrentState != AccountLogin.State.Ready)
            {
                OwlLogger.LogError($"Tried to start Account Login, but AccountLoginPhase was in state {_accountLogin.CurrentState}!", GameComponent.Other);
                return;
            }

            _accountLogin.Login(ClientMain.Instance.ConnectionToServer, username, password);
        }


        public void OnDisconnect()
        {
            _characterLogin.Clear();
            _characterSelectionData.Clear();
            _hasShownCharSelection = false;
            _accountLogin.Logout();
        }
    }
}