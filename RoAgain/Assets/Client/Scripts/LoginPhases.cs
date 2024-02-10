using OwlLogging;
using System.Collections.Generic;

namespace Client
{
    // TODO: Implement timeout-mechanism for these

    public class AccountLogin
    {
        
        public enum State
        {
            Ready,
            WaitingForAccountLogin,
            Error_LoginFail,
            Complete
        }
        public int SessionId = -1;

        public State CurrentState { get; private set; } = State.Ready;
        private ServerConnection _connection;

        public int Login(ServerConnection serverConnection, string username, string password)
        {
            if(serverConnection == null)
            {
                OwlLogger.LogError("Can't initialize with a null serverConnection", GameComponent.Other);
                return -1;
            }

            _connection = serverConnection;

            _connection.LoginResponseReceived += OnLoginResponseReceived;

            CurrentState = State.WaitingForAccountLogin;

            LoginRequestPacket loginPacket = new() { Username = username, Password = password };
            _connection.Send(loginPacket);
            
            return 0;
        }

        public int Logout()
        {
            if (_connection != null)
            {
                _connection.Disconnect(); // TODO: Instead of aborting the whole connection, maybe tell the server we're going to logout first?
                _connection.LoginResponseReceived -= OnLoginResponseReceived;
            }

            _connection = null;
            SessionId = -1;
            CurrentState = State.Ready;

            return 0;
        }

        private void OnLoginResponseReceived(LoginResponse loginResponse)
        {
            if (!loginResponse.IsSuccessful)
            {
                CurrentState = State.Error_LoginFail;
                return;
            }

            SessionId = loginResponse.SessionId;
            CurrentState = State.Complete;
        }
    }

    public class CharacterSelectionDataList
    {
        public List<CharacterSelectionData> Data { get; private set; }

        private ServerConnection _connection;

        public void Fetch(ServerConnection serverConnection)
        {
            if (serverConnection == null)
            {
                OwlLogger.LogError("Can't initialize with a null serverConnection", GameComponent.Other);
                return;
            }

            Clear(); // Clear previous data so it's not used while being overwritten when the fetch completes

            _connection = serverConnection;

            _connection.CharacterSelectionDataReceived += OnCharacterSelectionDataReceived;

            LoadCharacterSelectionData();
        }

        private void LoadCharacterSelectionData()
        {
            _connection.ResetCharacterSelectionData();
            

            CharacterSelectionRequestPacket charSelRequest = new();
            _connection.Send(charSelRequest);
        }

        private void OnCharacterSelectionDataReceived(List<CharacterSelectionData> charData)
        {
            Data = charData;
        }

        public void Clear()
        {
            if(_connection != null)
            {
                _connection.CharacterSelectionDataReceived -= OnCharacterSelectionDataReceived;
            }

            _connection = null;
            Data = null;
        }
    }

    public class CharacterLogin
    {
        public LocalCharacterEntity CurrentCharacater;

        public int Start()
        {
            return 0;
        }

        public bool IsStarted()
        {
            return false;
        }

        public bool IsFinished()
        {
            return false;
        }

        public int Clear()
        {
            return 0;
        }
    }
}
