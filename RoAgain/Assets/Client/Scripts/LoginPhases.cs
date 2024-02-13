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

            _connection.AccountLoginResponseReceived += OnLoginResponseReceived;

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
                _connection.AccountLoginResponseReceived -= OnLoginResponseReceived;
            }

            _connection = null;
            SessionId = -1;
            CurrentState = State.Ready;

            return 0;
        }

        private void OnLoginResponseReceived(AccountLoginResponse loginResponse)
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
        private int _charId;
        private ServerConnection _connection;

        public int ResultCode;
        public LocalCharacterData CharacterData;
        public List<SkillTreeEntry> SkillTreeEntries = new();

        private bool _isStarted = false;
        private bool _isFinished = false;

        public int Start(ServerConnection connection, int characterId)
        {
            if(connection == null)
            {
                OwlLogger.LogError("Can't start CharacterLogin with null connection!", GameComponent.Other);
                return -1;
            }

            if(characterId <= 0)
            {
                OwlLogger.LogError($"Can't start CharacterLogin with invalid characterId {characterId}", GameComponent.Other);
                return -2;
            }

            if(ResultCode != 0)
            {
                OwlLogger.LogError("Tried to start CharacterLogin while it's already running or finished!", GameComponent.Other);
                return -3;
            }

            _charId = characterId;
            _connection = connection;

            _connection.LocalCharacterDataReceived += LocalCharacterDataReceived;
            _connection.SkillTreeEntryUpdateReceived += SkillTreeEntryReceived;
            _connection.CharacterLoginResponseReceived += CharacterLoginResponseReceived;
            // TODO: Subscribe to packets for inventory, buffs & debuffs

            _isStarted = true;

            CharacterLoginPacket characterLoginPacket = new() { CharacterId = _charId };
            _connection.Send(characterLoginPacket);

            return 0;
        }

        private void LocalCharacterDataReceived(LocalCharacterData charData)
        {
            if(charData != null)
            {
                OwlLogger.LogWarning("Received multiple LocalCharData during character login!", GameComponent.Other);
            }

            CharacterData = charData;
        }

        private void SkillTreeEntryReceived(SkillTreeEntry entry)
        {
            SkillTreeEntries.Add(entry);
        }

        private void CharacterLoginResponseReceived(int result)
        {
            ResultCode = result;
            _isFinished = true;

            _connection.LocalCharacterDataReceived -= LocalCharacterDataReceived;
            _connection.SkillTreeEntryUpdateReceived -= SkillTreeEntryReceived;
            _connection.CharacterLoginResponseReceived -= CharacterLoginResponseReceived;
        }

        public bool IsStarted()
        {
            return _isStarted;
        }

        public bool IsFinished()
        {
            return _isFinished;
        }

        public int Clear()
        {
            if(_connection != null)
            {
                _connection.LocalCharacterDataReceived -= LocalCharacterDataReceived;
                _connection.SkillTreeEntryUpdateReceived -= SkillTreeEntryReceived;
                _connection.CharacterLoginResponseReceived -= CharacterLoginResponseReceived;
            }

            _charId = 0;
            _connection = null;

            ResultCode = 0;
            CharacterData = null;
            SkillTreeEntries.Clear();

            return 0;
        }
    }
}
