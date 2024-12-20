using OwlLogging;
using Shared;
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

        [SerializeField]
        private LocalizedStringId _loggingIntoAccountLocId;
        [SerializeField]
        private LocalizedStringId _loadingAccountConfigLocId;
        [SerializeField]
        private LocalizedStringId _loginFailedLocId;
        [SerializeField]
        private LocalizedStringId _loadingCharactersLocId;
        [SerializeField]
        private LocalizedStringId _creatingAccountLocId;
        [SerializeField]
        private LocalizedStringId _creatingCharacterLocId;
        [SerializeField]
        private LocalizedStringId _connectingToWorldLocId;
        [SerializeField]
        private LocalizedStringId _loginFailAlreadyLoggedInLocId;
        [SerializeField]
        private LocalizedStringId _loginFailUnknownLocId;

        private GameObject _currentWindow;

        private ServerConnection _currentConnection;

        private AccountLogin _accountLogin = new();
        private AccountLogin.State _lastAccountLoginState = AccountLogin.State.Ready;
        private CharacterSelectionDataList _characterSelectionData = new();
        private bool _hasShownCharSelection;
        private CharacterLogin _characterLogin = new();
        private bool _waitingForCharLogin = false;

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
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
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
            _accountLogin.Update();
            if (_accountLogin.CurrentState != _lastAccountLoginState)
            {
                _lastAccountLoginState = _accountLogin.CurrentState;
                switch (_accountLogin.CurrentState)
                {
                    case AccountLogin.State.Ready:
                        ShowAccountLoginWindow();
                        break;
                    case AccountLogin.State.WaitingForAccountLogin:
                        DeleteCurrentWindow();
                        ClientMain.Instance.DisplayZeroButtonNotification(_loggingIntoAccountLocId);
                        break;
                    case AccountLogin.State.WaitingForAccountConfig:
                        ClientMain.Instance.DisplayZeroButtonNotification(_loadingAccountConfigLocId);
                        break;
                    case AccountLogin.State.Error_LoginFail:
                        ClientMain.Instance.DisplayZeroButtonNotification(null);
                        // TODO: Look up string for error codes & display failure reason
                        ClientMain.Instance.DisplayOneButtonNotification(_loginFailedLocId, () =>
                        {
                            _accountLogin.Logout(); // to reset internal state
                            ClientMain.Instance.DisplayOneButtonNotification(null, null);
                            ShowAccountLoginWindow();
                        });
                        break;
                    case AccountLogin.State.Complete:
                        StartCharacterSelectionFetch();
                        break;
                }
            }

            if (!_hasShownCharSelection && _characterSelectionData.Data != null)
            {
                ShowCharacterSelection();
                _hasShownCharSelection = true;
            }

            _characterLogin.Update();
            if(_waitingForCharLogin && _characterLogin.IsFinished())
            {
                _waitingForCharLogin = false;
                ClientMain.Instance.DisplayZeroButtonNotification(null);
                switch (_characterLogin.ResultCode)
                {
                    case 0:
                        break;
                    case -1:
                        ClientMain.Instance.DisplayOneButtonNotification(_loginFailAlreadyLoggedInLocId, ShowCharacterSelection);
                        return;
                    default:
                        ClientMain.Instance.DisplayOneButtonNotification(string.Format(_loginFailUnknownLocId.Resolve(), _characterLogin.ResultCode), ShowCharacterSelection);
                        return;
                }

                ClientMain.Instance.OnCharacterLoginCompleted(_characterLogin.ResultCode, _characterLogin.CharacterData, _characterLogin.SkillTreeEntries);
            }
        }

        private void StartCharacterSelectionFetch()
        {
            ClientMain.Instance.DisplayZeroButtonNotification(_loadingCharactersLocId);
            _hasShownCharSelection = false;
            _characterSelectionData.Fetch(ClientMain.Instance.ConnectionToServer);
        }

        public void ShowAccountLoginWindow()
        {
            DeleteCurrentWindow();
            _currentWindow = Instantiate(_loginWindowPrefab, ClientMain.Instance.MainUiCanvas.transform);
        }

        public void SkipAccountCreation(int sessionId)
        {
            _accountLogin.Skip(sessionId);
        }

        public void ShowAccountCreationWindow()
        {
            DeleteCurrentWindow();
            _currentWindow = Instantiate(_accountCreationWindowPrefab, ClientMain.Instance.MainUiCanvas.transform);
        }

        public void CreateAccount(string username, string password)
        {
            ClientMain.Instance.DisplayZeroButtonNotification(_creatingAccountLocId);

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
            System.Action callback = null;
            string message = result switch
            {
                0 => "Account creation successful!",
                1 => "Invalid password!",// TODO: Tell user about Password requirements
                2 => "Username already taken!",
                3 => "Invalid username!",
                _ => "Unknown error",
            };

            if(result == 0)
            {
                callback = ShowAccountLoginWindow;
            }
            ClientMain.Instance.DisplayOneButtonNotification(message, callback);
        }

        public void ShowCharacterSelection()
        {
            ClientMain.Instance.DisplayZeroButtonNotification(null);
            ShowCharacterSelectionWindow(_characterSelectionData.Data);
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

            if(characterSelectionData != null)
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

            ClientMain.Instance.DisplayZeroButtonNotification(_creatingCharacterLocId);

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
            switch(result) // TODO: Better user-facing error messages, localization
            {
                case > 0: // contains characterId, currently not used here
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

        public void StartCharacterLogin(int characterId)
        {
            if(_characterLogin.IsStarted() && !_characterLogin.IsFinished())
            {
                OwlLogger.LogError("Can't start CharacterLogin while previous login attempt is still running!", GameComponent.Other);
                return;
            }

            _characterLogin.Clear();

            DeleteCurrentWindow();
            ClientMain.Instance.DisplayZeroButtonNotification(_connectingToWorldLocId);

            // we can't start the gameplay-scene load here yet, because the login may still fail for this character

            _waitingForCharLogin = true;
            _characterLogin.Start(ClientMain.Instance.ConnectionToServer, characterId);
        }

        public void OnDisconnect()
        {
            if(_currentConnection != null)
            {
                _currentConnection.AccountCreationResponseReceived -= OnAccountCreationResponseReceived;
                _currentConnection.CharacterCreationResponseReceived -= OnCharacterCreationResponseReceived;
                _currentConnection = null;
            }
            
            _characterLogin.Clear();
            _characterSelectionData.Clear();
            _hasShownCharSelection = false;
            _accountLogin.Logout();
        }
    }
}