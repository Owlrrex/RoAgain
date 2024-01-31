using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Client
{
    public class ClientMain : MonoBehaviour
    {
        // Editor-time set fields
        public Canvas MainUiCanvas;
        [SerializeField]
        private OneButtonNotification _oneButtonNotification;
        [SerializeField]
        private ZeroButtonNotification _zeroButtonNotification;

        // Data Tables
        [SerializeField]
        private CellEffectPrefabTable _cellEffectPrefabTable;
        [SerializeField]
        private MapPrefabTable _mapPrefabTable;
        [SerializeField]
        private EntityPrefabTable _entityPrefabTable;
        [SerializeField]
        private SkillClientDataTable _skillClientDataTable;

        [SerializeField]
        private GameObject _dummyUnitPrefab;

        // TODO: Will be replaced by Character-Class dependent asset database
        public GameObject CharacterPrefab;

        [SerializeField]
        private Rect _titlePlacement;

        // TODO: Replace with proper Loading-screen flow
        [SerializeField]
        private Vector4 LoadingMessagePlacement;

        // Runtime set fields
        public static ClientMain Instance;

        // TODO: Replace with proper Loading-screen flow
        private string LoadingMessage;

        // TODO: Make statically available easier then ClientMain.Instance.ConnectionToServer ?
        public ServerConnection ConnectionToServer;
        // TODO: replace this with LocalCharacterEntity.Current or similar
        // TODO: Reduce access to this on hot paths
        public LocalCharacterEntity CurrentCharacterData { get; private set; }
        
        // TODO: move to MapModule, since that one manages movers
        private GridEntityMover _characterGridMover;

        public ClientMapModule MapModule { get; private set; }

        public string IpInput;
        private string _port;

        private int _characterLoginId;

        // Can't move this to MapModule since these are also used in case the MapModule isn't ready yet (map still loading)
        private List<GridEntityData> _queuedEntities = new();

        private Action _sessionCreationCallback;
        private List<CharacterSelectionData> _charData;

        void Awake()
        {
            if (Instance == this)
            {
                OwlLogger.Log("ClientMain tried to re-register itself", GameComponent.Other);
                return;
            }

            if (Instance != null)
            {
                OwlLogger.LogError($"Duplicate ClientMain script on GameObject {gameObject.name}", GameComponent.Other);
                Destroy(this);
                return;
            }
            Instance = this;

            ClientConfiguration config = new();
            config.LoadConfig();
            // TODO: Check for errors 

            IpInput = config.GetMiscConfig(ConfigurationKey.ServerIp);
            _port = config.GetMiscConfig(ConfigurationKey.ServerPort);

            KeyboardInput keyboardInput = new();
            keyboardInput.Initialize(config);

            InitializeTables();

            MapModule = new();
            MapModule.Initialize();

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            PreGameUI.Instance.ShowLoginWindow();
        }

        private void InitializeTables()
        {
            if(!OwlLogger.PrefabNullCheckAndLog(_cellEffectPrefabTable, "cellEffectPrefabTable", this, GameComponent.Other))
                _cellEffectPrefabTable.Register();

            if (!OwlLogger.PrefabNullCheckAndLog(_mapPrefabTable, "mapPrefabTable", this, GameComponent.Other))
                _mapPrefabTable.Register();

            if(!OwlLogger.PrefabNullCheckAndLog(_entityPrefabTable, "entityPrefabTable", this, GameComponent.Other))
                _entityPrefabTable.Register();

            if(!OwlLogger.PrefabNullCheckAndLog(_skillClientDataTable, "skillClientDataTable", this, GameComponent.Other))
                _skillClientDataTable.Register();
        }

        private void Update()
        {
            // Ensure OnDisconnected runs on main thread
            if(ConnectionToServer != null && !ConnectionToServer.IsAlive)
            {
                OnDisconnected();
            }

            MapModule?.Update();

            ConnectionToServer?.Update();
        }

        public bool ConnectToServer()
        {
            if(ConnectionToServer != null)
            {
                OwlLogger.LogError($"Can't connect to Server - Connection already present!", GameComponent.Other);
                return false;
            }

            // TODO: load settings for initial server connection
            //string ipPort = "";
            //int initResult = CreateDummyConnection(ipPort);

            string ipPort = IpInput + ":" + _port;
            int initResult = CreateConnection(ipPort);

            // receive & handle initialization results
            if (initResult != 0)
            {
                Debug.LogError($"DummyServer init failed with error code {initResult} - aborting ConnectToServer");
                ConnectionToServer = null;
                return false;
            }

            return true;
        }

        private int CreateDummyConnection(string ipPort)
        {
            if (ConnectionToServer != null)
                DetachFromConnection();

            ConnectionToServer = new DummyServerConnection();
            SetupWithConnection();
            return ConnectionToServer.Initialize(ipPort);
        }

        private int CreateConnection(string ipPort)
        {
            if (ConnectionToServer != null)
                DetachFromConnection();

            ConnectionToServer = new ServerConnectionImpl();
            SetupWithConnection();
            return ConnectionToServer.Initialize(ipPort);
        }

        private void SetupWithConnection()
        {
            if(ConnectionToServer == null)
            {
                OwlLogger.LogError("Can't Setup with null connection!", GameComponent.Other);
                return;
            }

            // TODO: Make submodules subscribe to the Connection instead where appropriate, to remove routing-functions from ClientMain
            ConnectionToServer.SessionReceived += OnSessionReceived;
            ConnectionToServer.LoginResponseReceived += OnLoginResponseReceived;
            ConnectionToServer.UnitMovementReceived += OnUnitMovementReceived;
            ConnectionToServer.GridEntityDataReceived += OnGridEntityDataReceived;
            ConnectionToServer.BattleEntityDataReceived += OnBattleEntityDataReceived;
            ConnectionToServer.RemoteCharacterDataReceived += OnRemoteCharacterDataReceived;
            ConnectionToServer.LocalCharacterDataReceived += OnLocalCharacterDataReceived;
            ConnectionToServer.CharacterSelectionDataReceived += OnCharacterSelectionDataReceived;
            ConnectionToServer.EntityRemovedReceived += OnEntityRemovedReceived;
            // Can't use this if OnDisconnected contains mainThread code
            //ConnectionToServer.DisconnectDetected += OnDisconnected;
            //ConnectionToServer.MapChangeReceived += OnMapChangeReceived;
            ConnectionToServer.CellEffectGroupPlacedReceived += OnCellEffectGroupPlacedReceived;
            ConnectionToServer.CellEffectGroupRemovedReceived += OnCellEffectGroupRemovedReceived;
            ConnectionToServer.DamageTakenReceived += OnDamageTakenReceived;
            ConnectionToServer.CastProgressReceived += OnCastProgressReceived;
            ConnectionToServer.EntitySkillExecutionReceived += OnEntitySkillReceived;
            ConnectionToServer.GroundSkillExecutionReceived += OnGroundSkillReceived;
            ConnectionToServer.ChatMessageReceived += OnChatMessageReceived;
            ConnectionToServer.HpChangeReceived += OnHpChangeReceived;
            ConnectionToServer.SpChangeReceived += OnSpChangeReceived;
            ConnectionToServer.StatUpdateReceived += OnStatUpdateReceived;
            ConnectionToServer.StatFloatUpdateReceived += OnStatFloatUpdateReceived;
            ConnectionToServer.StatCostUpdateReceived += OnStatCostUpdateReceived;
            ConnectionToServer.StatPointUpdateReceived += OnStatPointUpdateReceived;
            ConnectionToServer.ExpUpdateReceived += OnExpUpdateReceived;
            ConnectionToServer.LevelUpdateReceived += OnLevelUpdateReceived;
            ConnectionToServer.AccountCreationResponseReceived += OnAccountCreationResponseReceived;
            ConnectionToServer.CharacterCreationResponseReceived += OnCharacterCreationResponseReceived;
            ConnectionToServer.LocalPlayerEntitySkillQueuedReceived += OnLocalPlayerEntitySkillQueuedReceived;
            ConnectionToServer.LocalPlayerGroundSkillQueuedReceived += OnLocalPlayerGroundSkillQueuedReceived;
            ConnectionToServer.SkillTreeEntryUpdateReceived += OnSkillTreeUpdateReceived;
            ConnectionToServer.SkillTreeEntryRemoveReceived += OnSkillTreeRemoveReceived;
            ConnectionToServer.SkillPointAllocateResponseReceived += OnSkillPointUpdateReceived;
        }

        private void DetachFromConnection()
        {
            if(ConnectionToServer == null)
            {
                OwlLogger.LogError("Can't Detach from null connection!", GameComponent.Other);
                return;
            }
            
            // TODO: Make submodules subscribe to the Connection instead where appropriate, to remove routing-functions from ClientMain
            ConnectionToServer.SessionReceived -= OnSessionReceived;
            ConnectionToServer.LoginResponseReceived -= OnLoginResponseReceived;
            ConnectionToServer.UnitMovementReceived -= OnUnitMovementReceived;
            ConnectionToServer.GridEntityDataReceived -= OnGridEntityDataReceived;
            ConnectionToServer.BattleEntityDataReceived -= OnBattleEntityDataReceived;
            ConnectionToServer.RemoteCharacterDataReceived -= OnRemoteCharacterDataReceived;
            ConnectionToServer.LocalCharacterDataReceived -= OnLocalCharacterDataReceived;
            ConnectionToServer.CharacterSelectionDataReceived -= OnCharacterSelectionDataReceived;
            ConnectionToServer.EntityRemovedReceived -= OnEntityRemovedReceived;
            // Can't use this if OnDisconnected contains mainThread code
            //ConnectionToServer.DisconnectDetected -= OnDisconnected;
            //ConnectionToServer.MapChangeReceived -= OnMapChangeReceived;
            ConnectionToServer.CellEffectGroupPlacedReceived -= OnCellEffectGroupPlacedReceived;
            ConnectionToServer.CellEffectGroupRemovedReceived -= OnCellEffectGroupRemovedReceived;
            ConnectionToServer.DamageTakenReceived -= OnDamageTakenReceived;
            ConnectionToServer.CastProgressReceived -= OnCastProgressReceived;
            ConnectionToServer.EntitySkillExecutionReceived -= OnEntitySkillReceived;
            ConnectionToServer.GroundSkillExecutionReceived -= OnGroundSkillReceived;
            ConnectionToServer.ChatMessageReceived -= OnChatMessageReceived;
            ConnectionToServer.HpChangeReceived -= OnHpChangeReceived;
            ConnectionToServer.SpChangeReceived -= OnSpChangeReceived;
            ConnectionToServer.StatUpdateReceived -= OnStatUpdateReceived;
            ConnectionToServer.StatFloatUpdateReceived -= OnStatFloatUpdateReceived;
            ConnectionToServer.StatCostUpdateReceived -= OnStatCostUpdateReceived;
            ConnectionToServer.StatPointUpdateReceived -= OnStatPointUpdateReceived;
            ConnectionToServer.ExpUpdateReceived -= OnExpUpdateReceived;
            ConnectionToServer.LevelUpdateReceived -= OnLevelUpdateReceived;
            ConnectionToServer.AccountCreationResponseReceived -= OnAccountCreationResponseReceived;
            ConnectionToServer.CharacterCreationResponseReceived -= OnCharacterCreationResponseReceived;
            ConnectionToServer.LocalPlayerEntitySkillQueuedReceived -= OnLocalPlayerEntitySkillQueuedReceived;
            ConnectionToServer.LocalPlayerGroundSkillQueuedReceived -= OnLocalPlayerGroundSkillQueuedReceived;
            ConnectionToServer.SkillTreeEntryUpdateReceived -= OnSkillTreeUpdateReceived;
            ConnectionToServer.SkillTreeEntryRemoveReceived -= OnSkillTreeRemoveReceived;
            ConnectionToServer.SkillPointAllocateResponseReceived -= OnSkillPointUpdateReceived;
        }

        public void Disconnect()
        {
            if (ConnectionToServer == null)
            {
                return;
            }

            ConnectionToServer.Disconnect();
        }

        public void OnDisconnected()
        {
            DetachFromConnection();
            ConnectionToServer = null;

            // This client could've belonged to a "ClientMenu" scene (launch as client) or a "MainMenu" scene (launch as both)
            // We could write detection for that in Start or sth, and return to the correct one,
            // but for now, we can just go back to the start scene
            if (SceneManager.GetActiveScene().buildIndex != 0)
            {
                // TODO: This cleanup doesn't clean up statics, or other classes.
                // cleanup this current ClientMain object because it's dontDestroyOnLoad
                // TODO: Check if this conflicts with the new object created during scene loading
                Destroy(gameObject);
                // We can't cleanup the server here, because it's in a different namespace
                SceneManager.LoadScene(0);
            }

            CurrentCharacterData = null;
            MapModule?.DestroyCurrentMap();
        }

        public void CreateSessionWithServer(Action callback)
        {
            _sessionCreationCallback = callback;
            bool successful = ConnectToServer();
            if (!successful)
            {
                OwlLogger.LogError("Connecting to server failed!", GameComponent.Network);
                return;
            }
        }

        private void OnSessionReceived(int sessionId)
        {
            // TODO: Store sessionId?
            if(_sessionCreationCallback != null)
            {
                // Why am I setting _sessionCreationCallback to null before calling it?
                Action callback = _sessionCreationCallback;
                _sessionCreationCallback = null;
                callback.Invoke();
            }
        }

        public void CreateAccount(string username, string password)
        {
            DisplayZeroButtonNotification("Creating account...");

            if (ConnectionToServer == null)
            {
                CreateSessionWithServer(() => CreateAccount(username, password));
                return;
            }

            AccountCreationRequestPacket packet = new()
            {
                Username = username,
                Password = password
            };
            ConnectionToServer.Send(packet);
        }

        private void OnAccountCreationResponseReceived(int result)
        {
            _zeroButtonNotification.Hide();

            string message = "Unknown error";
            if (result == 0)
            {
                message = "Account creation successful!";
            }
            else if (result == 3)
            {
                message = "Invalid username!";
            }
            else if (result == 2)
            {
                message = "Username already taken!";
            }
            else if (result == 1)
            {
                message = "Invalid password!";
            }

            DisplayOneButtonNotification(message, () => { PreGameUI.Instance.ShowLoginWindow(); });
        }

        public void LoginWithAccountData(string username, string password)
        {
            if(ConnectionToServer == null)
            {
                CreateSessionWithServer(() => LoginWithAccountData(username, password));
                return;
            }

            LoginRequestPacket loginPacket = new() { Username = username, Password = password };
            ConnectionToServer.Send(loginPacket);
        }

        private void OnLoginResponseReceived(LoginResponse loginResponse)
        {
            if (!loginResponse.IsSuccessful)
            {
                DisplayOneButtonNotification("Login Failed!", null);
                return;
            }

            LoadCharacterSelectionData();
        }

        public void LoadCharacterSelectionData()
        {
            PreGameUI.Instance.DeleteCurrentWindow();
            DisplayZeroButtonNotification("Loading characters..."); // Don't need to store - we already have a reference in this class

            ConnectionToServer.ResetCharacterSelectionData();
            CharacterSelectionRequestPacket charSelRequest = new();
            ConnectionToServer.Send(charSelRequest);
        }

        private void OnCharacterSelectionDataReceived(List<CharacterSelectionData> charData)
        {
            _charData = charData;

            _zeroButtonNotification.Hide();

            ShowCharacterSelection();
        }

        public void ShowCharacterSelection()
        {
            PreGameUI.Instance.ShowCharacterSelectionWindow(_charData);
        }

        public void CreateCharacter(string name, int gender)
        {
            DisplayZeroButtonNotification("Creating Character...");

            CharacterCreationRequestPacket packet = new()
            {
                Name = name,
                Gender = gender,
            };
            ConnectionToServer.Send(packet);
        }

        private void OnCharacterCreationResponseReceived(int result)
        {
            _zeroButtonNotification.Hide();

            if (result == 0)
            {
                LoadCharacterSelectionData();
                return;
            }

            string message = "Unknown error";
            if (result == -10 || result == -4)
            {
                message = "Character-name already taken!";
            }
            else if (result == -1)
            {
                message = "Character Creation error code: -1";
            }
            else if (result == -2)
            {
                message = "Character Creation error code: -2";
            }
            else if (result == -3)
            {
                message = "Character Creation error code: -3";
            }

            DisplayOneButtonNotification(message, null);
        }

        public void StartCharacterLogin(int characterId)
        {
            // This function does alot of mixed things - UI, Network, etc. 
            // Needs cleaning up later

            PreGameUI.Instance.DeleteCurrentWindow();
            DisplayZeroButtonNotification("Connecting to world...");

            CurrentCharacterData = null;

            _characterLoginId = characterId;

            // Start loading gameplay-scene
            AsyncOperation asyncOp = SceneManager.LoadSceneAsync("Gameplay"); // Maybe load a loading-screen-scene instead?
            asyncOp.allowSceneActivation = true;
            asyncOp.completed += OnGameplaySceneLoadCompleted;
            // loading-bar?
        }

        private void OnGameplaySceneLoadCompleted(AsyncOperation loadOp)
        {
            if (_characterLoginId <= 0)
            {
                OwlLogger.LogError("Gameplay scene loaded with empty characterLoginId - aborting login!", GameComponent.Other);
                Disconnect();
                return;
            }

            CharacterLoginPacket characterLoginPacket = new() { CharacterId = _characterLoginId };
            ConnectionToServer.Send(characterLoginPacket);
        }

        private void OnLocalCharacterDataReceived(LocalCharacterData data)
        {
            if (data.UnitId == -1)
            {
                OwlLogger.Log($"Received decline of character login - Disconnecting!", GameComponent.Network);
                Disconnect();
                return;
            }

            if(_characterLoginId > 0)
            {
                // We're during character login
                OwlLogger.Log($"LocalCharacterDataPacket received, new CurrentChar has ID {data.UnitId}", GameComponent.Character);
                if(CurrentCharacterData == null)
                {
                    CurrentCharacterData = new(data);
                }
                else
                {
                    CurrentCharacterData.SetData(data);
                }
                
                OnAllLoginDataReceived();
                return;
            }

            // We're during gameplay
            if (CurrentCharacterData != null && CurrentCharacterData.Id != data.UnitId)
            {
                OwlLogger.LogError($"LocalCharacterDataPacket received for Id {data.UnitId} that differs from CurrentCharacterData id {CurrentCharacterData.Id}!", GameComponent.Character);
            }

            string oldMapId = CurrentCharacterData.MapId;
            // This isn't exactly clean, but it's what we have:
            // This sets & handles grid-based changes... (including place & remove calls)
            MapModule.UpdateExistingEntityData(data);

            // ...and this sets all the other data
            CurrentCharacterData.SetData(data);

            if (oldMapId != data.MapId)
            {
                OnMapChangeReceived(data.MapId, data.Coordinates);
            }
        }

        private void OnAllLoginDataReceived()
        {
            if (CurrentCharacterData == null)
            {
                OwlLogger.LogError("CharacterLogin completed called before all data was ready!", GameComponent.Other);
                return;
            }

            _characterLoginId = 0;
            _zeroButtonNotification.Hide();

            OnMapChangeReceived(CurrentCharacterData.MapId, CurrentCharacterData.Coordinates);
        }

        private int EnterMap()
        {
            if(MapModule.Grid.Data.FindOccupant(CurrentCharacterData.Id) != null)
            {
                OwlLogger.LogError("Tried to enter map that character's already on!", GameComponent.Other);
                return -1;
            }

            // place character on map
            MapModule.Grid.Data.PlaceOccupant(CurrentCharacterData, CurrentCharacterData.Coordinates);

            // Create new Display gameobject, otherwise shutdown the components we expect to be on it.
            if(_characterGridMover == null)
            {
                GameObject characterInstance = Instantiate(CharacterPrefab, Vector3.zero, Quaternion.identity);
                _characterGridMover = characterInstance.GetComponentInChildren<GridEntityMover>();
            }
            else
            {
                _characterGridMover.Shutdown();
                PlayerMain.Instance.Shutdown();
            }
            
            if (_characterGridMover == null)
            {
                OwlLogger.LogError($"Can't find GridEntityMover on Character Prefab!", GameComponent.Other);
                return -2;
            }

            _characterGridMover.Initialize(CurrentCharacterData, MapModule.Grid);
            PlayerMain.Instance.Initialize(CurrentCharacterData);
            return 0;
        }

        private void OnUnitMovementReceived(UnitMovementInfo moveInfo)
        {
            if(!MapModule.IsReady())
            {
                OwlLogger.LogWarning($"Received EntityMovement while MapModule wasn't ready - ignoring, this may cause position desync!", GameComponent.Other);
                return;
            }

            MapModule.OnUnitMovement(moveInfo);            
        }

        private void OnGridEntityDataReceived(GridEntityData data)
        {
            if (!MapModule.IsReady()
                || data.MapId != CurrentCharacterData.MapId)
            {
                OwlLogger.Log($"Queueing entity data for id {data.UnitId} for Mapchange.", GameComponent.Other, LogSeverity.Verbose);
                _queuedEntities.Add(data);
            }
            else
            {
                MapModule.OnGridEntityData(data);
            }
        }

        private void OnBattleEntityDataReceived(BattleEntityData data)
        {
            // For now: Move BattleEntities through the same code path as GridEntitites, which will check their type when needed
            // TODO: Split code paths if there's too much type-dependent code
            OnGridEntityDataReceived(data);
        }

        private void OnRemoteCharacterDataReceived(RemoteCharacterData data)
        {
            // For now: Move BattleEntities through the same code path as GridEntitites, which will check their type when needed
            // TODO: Split code paths if there's too much type-dependent code
            OnGridEntityDataReceived(data);
        }

        private void OnEntityRemovedReceived(int entityId)
        {
            if (!MapModule.IsReady())
            {
                OwlLogger.Log($"Received EntityRemoved while MapModule wasn't ready - ignoring, no entities exist anyway!", GameComponent.Other);
                return;
            }
            
            MapModule.OnEntityRemoved(entityId);
        }

        private void OnMapChangeReceived(string newMapId, Vector2Int newMapCoords)
        {
            // TODO: Handling for async destruction & loading of map prefabs - loading screen?
            LoadingMessage = "Entering map...";
            MapModule.SetCurrentMap(newMapId);
            CurrentCharacterData.Coordinates = newMapCoords;
            CurrentCharacterData.ClearPath();
            CurrentCharacterData.MapId = newMapId;
            EnterMap();
            List<GridEntityData> remainingEntities = new();
            foreach (GridEntityData entityData in _queuedEntities)
            {
                if (entityData.MapId == newMapId)
                {
                    MapModule.OnGridEntityData(entityData);
                }
                else
                {
                    remainingEntities.Add(entityData);
                }
            }
            _queuedEntities = remainingEntities;
            LoadingMessage = null;
        }

        private void OnCellEffectGroupPlacedReceived(CellEffectData data)
        {
            if (!MapModule.IsReady())
            {
                OwlLogger.LogWarning($"Received CellEffectPlaced while MapModule wasn't ready - ignoring, this may cause client desync!", GameComponent.Other);
                return;
            }

            MapModule.OnCellEffectGroupPlaced(data);
        }

        private void OnCellEffectGroupRemovedReceived(int groupId)
        {
            if (!MapModule.IsReady())
            {
                OwlLogger.Log($"Received CellEffectRemoved while MapModule wasn't ready - ignoring, effects are already cleared anyway!", GameComponent.Other);
                return;
            }

            MapModule.OnCellEffectGroupRemoved(groupId);
        }

        private void OnDamageTakenReceived(int entityId, int damage, bool isSpDamage, bool isCrit, int chainCount)
        {
            // This being handled in ClientMain directly feels wrong. Maybe move to different class later
            if (MapModule.Grid.Data.FindOccupant(entityId) is not ClientBattleEntity entity)
            {
                OwlLogger.LogError($"Client received DamageTaken for entity {entityId} that's not in client system or not a battleEntity!", GameComponent.Battle);
                return;
            }

            // Clamping here feels misplaced - there should be a central function to modify HP, regardless of whether or not its context
            if (isSpDamage)
            {
                entity.CurrentSp = Math.Clamp(entity.CurrentSp - damage, 0, entity.MaxSp.Total);
            }
            else
            {
                entity.CurrentHp = Math.Clamp(entity.CurrentHp - damage, 0, entity.MaxHp.Total);
            }
            entity.TookDamage?.Invoke(entity, damage, isSpDamage, isCrit, chainCount);
        }

        private void OnCastProgressReceived(int casterId, SkillId skillId, TimerFloat castTime, int targetId, Vector2Int targetCoords)
        {
            if (MapModule.Grid.Data.FindOccupant(casterId) is not ClientBattleEntity entity)
            {
                OwlLogger.LogError($"Client received CastProgress for entity {casterId} that's not in client system or not a battleEntity!", GameComponent.Other);
                return;
            }

            // TODO: We either need skillexecution-ids to know which skillexecution to overwrite (if any, in case of a cast-time update for a cast the client already knows about, like an interruption)
            // Or we simply assume that a single entity can only ever _cast_ a given skill-Id a single time, so if we have a cast-update for skillId X and we find a skill-execution for SkillId X that's currently casting,
            // we overwrite its casttime instead of placing a new skill into the resolve-list.

            ASkillExecution matchingSkillExec = null;
            foreach(ASkillExecution skillExec in entity.CurrentlyResolvingSkills)
            {
                // Don't use IsCasting() here since it causes an additional iteration over CurrentResolvingSkills - unnecessary
                if (skillExec.SkillId == skillId
                    && !skillExec.CastTime.IsFinished())
                {
                    matchingSkillExec = skillExec;
                    break;
                }
            }

            if(matchingSkillExec != null)
            {
                matchingSkillExec.CastTime = castTime;
                // don't change target here because it's impossible & makes the client more complicated
            }
            else
            {
                ClientSkillExecution skill = new();
                SkillTarget target;
                if (targetId > 0)
                {
                    ClientBattleEntity bTarget = MapModule.Grid.Data.FindOccupant(targetId) as ClientBattleEntity;
                    target = new(bTarget);
                }
                else
                {
                    target = new(targetCoords);
                }

                skill.Initialize(skillId, 1, entity, 0, 1, 0, 0.1f, target);

                skill.CastTime = castTime; // overwrite casttime, in case the packet sent partly-completed cast-times, which the initialize-function can't handle

                // this skill-object only needs to have its cast-time related values filled out
                entity.CurrentlyResolvingSkills.Add(skill);
            }
        }

        private void OnEntitySkillReceived(int userId, SkillId skillId, int targetId, float animCd)
        {
            if (MapModule.Grid.Data.FindOccupant(userId) is not ClientBattleEntity entity)
            {
                // This can actually happen if the skill-user Entity is out of sight for the client, but the target is visible!
                // TODO: Handle
                OwlLogger.Log($"Client received EntitySkillExecution for entity {userId} that's not in client system or not a battleEntity!", GameComponent.Other);
                return;
            }

            if (MapModule.Grid.Data.FindOccupant(targetId) is not ClientBattleEntity targetEntity)
            {
                // This can actually happen if the skillUser entity is in sight of the client, but the target is not.
                // This can also happen for damage-packets that kill an entity: The death-packet can arrive & be handled before the damage-packet.
                // TODO: Handle
                OwlLogger.Log($"Client received EntitySkillExecution for target {targetId} that's not on client system or not a battleEntity!", GameComponent.Other);
                return;
            }

            GridEntity target = MapModule.Grid.Data.FindOccupant(targetId);
            ClientBattleEntity bTarget = target as ClientBattleEntity;
            ClientSkillExecution skill = new();
            skill.Initialize(skillId, 1, entity, 0, 1, 0, animCd, new(bTarget));
            skill.HasExecutionStarted = true;

            // TODO: fill out other values as best we can

            // TODO: Somehow hook in visualization on target (targeting-circle)
            // Execution-animation should be handled by EntityDisplay component/sytsem

            // this skill-object only needs to have its anim-Cd related values filled out
            entity.CurrentlyResolvingSkills.Add(skill);
        }

        private void OnGroundSkillReceived(int userId, SkillId skillId, Vector2Int targetCoords, float animCd)
        {
            if (MapModule.Grid.Data.FindOccupant(userId) is not ClientBattleEntity entity)
            {
                // This can actually happen if the skill-user Entity is out of sight for the client, but the target is visible!
                // TODO: Handle
                OwlLogger.LogError($"Client received GroundSkillExecution for entity {userId} that's not in client system or not a battleEntity!", GameComponent.Other);
                return;
            }

            ClientSkillExecution groundSkill = new();
            groundSkill.Initialize(skillId, 1, entity, 0, 1, 0, animCd, new(targetCoords));
            groundSkill.HasExecutionStarted = true;

            // TODO: fill out other values as best we can

            // TODO: Somehow hook in visualization on target area (targeting-circle)
            // Execution-animation should be handled by EntityDisplay component/sytsem

            // this skill-object only needs to have its anim-Cd related values filled out
            entity.CurrentlyResolvingSkills.Add(groundSkill);
        }

        private void OnChatMessageReceived(ChatMessageData data)
        {
            DisplayChatMessageIfVisible(data);
            PlayerUI.Instance.ChatSystem.DisplayInChatWindow(data);
        }

        private void DisplayChatMessageIfVisible(ChatMessageData data)
        {
            // No overhead display wanted for whispers
            if (data.Scope == ChatMessagePacket.Scope.Whisper)
                return;

            // Player model is easily available
            if(data.SenderId == CurrentCharacterData.Id)
            {
                PlayerMain.Instance.SetSkilltext($"{data.SenderName}: {data.Message}", 5);
                return;
            }

            BattleEntityModelMain bModel = MapModule.GetComponentFromEntityDisplay<BattleEntityModelMain>(data.SenderId);
            if(bModel == null)
                return;

            bModel.SetSkilltext($"{data.SenderName}: {data.Message}", 5);
        }

        private void OnHpChangeReceived(int entityId, int newHp)
        {
            // This being handled in ClientMain directly feels wrong. Maybe move to different class later
            if (MapModule.Grid.Data.FindOccupant(entityId) is not ClientBattleEntity entity)
            {
                OwlLogger.LogError($"Client received HpChange for entity {entityId} that's not in client system or not a battleEntity!", GameComponent.Battle);
                return;
            }

            entity.CurrentHp = newHp;
        }

        private void OnSpChangeReceived(int entityId, int newSp)
        {
            // This being handled in ClientMain directly feels wrong. Maybe move to different class later
            if (MapModule.Grid.Data.FindOccupant(entityId) is not ClientBattleEntity entity)
            {
                OwlLogger.LogError($"Client received HpChange for entity {entityId} that's not in client system or not a battleEntity!", GameComponent.Battle);
                return;
            }

            entity.CurrentSp = newSp;
        }

        private void OnStatUpdateReceived(EntityPropertyType type, Stat newValue)
        {
            switch (type)
            {
                case EntityPropertyType.Str:
                    CurrentCharacterData.Str = newValue;
                    break;
                case EntityPropertyType.Agi:
                    CurrentCharacterData.Agi = newValue;
                    break;
                case EntityPropertyType.Vit:
                    CurrentCharacterData.Vit = newValue;
                    break;
                case EntityPropertyType.Int:
                    CurrentCharacterData.Int = newValue;
                    break;
                case EntityPropertyType.Dex:
                    CurrentCharacterData.Dex = newValue;
                    break;
                case EntityPropertyType.Luk:
                    CurrentCharacterData.Luk = newValue;
                    break;
                case EntityPropertyType.MaxHp:
                    CurrentCharacterData.MaxHp = newValue;
                    break;
                case EntityPropertyType.MaxSp:
                    CurrentCharacterData.MaxSp = newValue;
                    break;
                case EntityPropertyType.CurrentAtkMin:
                    CurrentCharacterData.AtkMin = newValue;
                    break;
                case EntityPropertyType.CurrentAtkMax:
                    CurrentCharacterData.AtkMax = newValue;
                    break;
                case EntityPropertyType.MatkMin:
                    CurrentCharacterData.MatkMin = newValue;
                    break;
                case EntityPropertyType.MatkMax:
                    CurrentCharacterData.MatkMax = newValue;
                    break;
                case EntityPropertyType.AnimationSpeed:
                    // Set animation speed, somehow
                    break;
                case EntityPropertyType.SoftDef:
                    CurrentCharacterData.SoftDef = newValue;
                    break;
                case EntityPropertyType.SoftMDef:
                    CurrentCharacterData.SoftMdef = newValue;
                    break;
                case EntityPropertyType.Flee:
                    CurrentCharacterData.Flee = newValue;
                    break;
                case EntityPropertyType.Hit:
                    CurrentCharacterData.Hit = newValue;
                    break;
                case EntityPropertyType.WeightLimit:
                    CurrentCharacterData.Weightlimit = newValue;
                    break;
                default:
                    OwlLogger.LogError($"Client received StatUpdate for stat type {type} that can't be handled!", GameComponent.Character);
                    break;
            }

            // TODO: Broadcast events/set dirty once UI is no longer Update-based
        }

        private void OnStatFloatUpdateReceived(EntityPropertyType type, StatFloat newValue)
        {
            switch(type)
            {
                case EntityPropertyType.HardDef:
                    CurrentCharacterData.HardDef = newValue;
                    break;
                case EntityPropertyType.HardMDef:
                    CurrentCharacterData.HardMdef = newValue;
                    break;
                case EntityPropertyType.Crit:
                    CurrentCharacterData.Crit = newValue;
                    break;
                case EntityPropertyType.PerfectFlee:
                    CurrentCharacterData.PerfectFlee = newValue;
                    break;
                default:
                    OwlLogger.LogError($"Client received StatFloatUpdate for stat type {type} that can't be handled!", GameComponent.Character);
                    break;
            }

            // TODO: Broadcast events/set dirty once UI is no longer Update-based
        }

        private void OnStatCostUpdateReceived(EntityPropertyType type, int newCost)
        {
            switch(type)
            {
                case EntityPropertyType.Str:
                    CurrentCharacterData.StrIncreaseCost = newCost;
                    break;
                case EntityPropertyType.Agi:
                    CurrentCharacterData.AgiIncreaseCost = newCost;
                    break;
                case EntityPropertyType.Vit:
                    CurrentCharacterData.VitIncreaseCost = newCost;
                    break;
                case EntityPropertyType.Int:
                    CurrentCharacterData.IntIncreaseCost = newCost;
                    break;
                case EntityPropertyType.Dex:
                    CurrentCharacterData.DexIncreaseCost = newCost;
                    break;
                case EntityPropertyType.Luk:
                    CurrentCharacterData.LukIncreaseCost = newCost;
                    break;
                default:
                    OwlLogger.LogError($"Client received StatCostUpdate for stat type {type} that can't be handled!", GameComponent.Character);
                    break;
            }

            // TODO: Broadcast events/set dirty once UI is no longer Update-based
        }

        private void OnStatPointUpdateReceived(int newRemaining)
        {
            CurrentCharacterData.RemainingStatPoints = newRemaining;
            // TODO: Broadcast events/set dirty once UI is no longer Update-based
        }

        private void OnExpUpdateReceived(int baseExp, int jobExp)
        {
            if(CurrentCharacterData == null)
            {
                OwlLogger.LogError("ExpUpdate received before CurrentCharacterData was set!", GameComponent.Character);
                return;
            }

            CurrentCharacterData.CurrentBaseExp = baseExp;
            CurrentCharacterData.CurrentJobExp = jobExp;
            // TODO: Broadcast events/set dirty once UI is no longer Update-based
        }

        private void OnLocalPlayerEntitySkillQueuedReceived(SkillId skillId, int targetId)
        {
            if (CurrentCharacterData == null)
            {
                OwlLogger.LogError("Received SkillQueue while CurrentCharacterData was null!", GameComponent.Other);
                return;
            }

            if(skillId == SkillId.Unknown)
            {
                CurrentCharacterData.QueuedSkill = null;
                return;
            }

            OwlLogger.Log($"Updating SkillQueue for local player with skill {skillId}", GameComponent.Character, LogSeverity.Verbose);
            ClientBattleEntity bTarget = MapModule.Grid.Data.FindOccupant(targetId) as ClientBattleEntity;
            ClientSkillExecution skill = new();
            skill.Initialize(skillId, 1, CurrentCharacterData, 0, 1, 0, 0, new(bTarget));
            CurrentCharacterData.QueuedSkill = skill;
        }

        private void OnLocalPlayerGroundSkillQueuedReceived(SkillId skillId, Vector2Int target)
        {
            if (CurrentCharacterData == null)
            {
                OwlLogger.LogError("Received SkillQueue while CurrentCharacterData was null!", GameComponent.Other);
                return;
            }

            if (skillId == SkillId.Unknown)
            {
                CurrentCharacterData.QueuedSkill = null;
                return;
            }

            OwlLogger.Log($"Updating SkillQueue for local player with skill {skillId}", GameComponent.Character, LogSeverity.Verbose);
            ClientSkillExecution skill = new();
            skill.Initialize(skillId, 1, CurrentCharacterData, 0, 1, 0, 0, new(target));
            CurrentCharacterData.QueuedSkill = skill;
        }

        private void OnLevelUpdateReceived(int entityId, int level, bool isJob, int requiredExp)
        {
            if(CurrentCharacterData != null && entityId == CurrentCharacterData.Id)
            {
                if (isJob)
                {
                    CurrentCharacterData.JobLvl = level;
                    CurrentCharacterData.RequiredJobExp = requiredExp;
                    PlayerMain.Instance.DisplayJobLvlUp();
                }
                else
                {
                    CurrentCharacterData.BaseLvl = level;
                    CurrentCharacterData.RequiredBaseExp = requiredExp;
                    PlayerMain.Instance.DisplayBaseLvlUp();
                }

            }
            else
            {
                if (MapModule.Grid.Data.FindOccupant(entityId) is not ClientBattleEntity entity)
                {
                    OwlLogger.LogError($"Received LevelUpdate for entity {entityId} that's not on Grid!", GameComponent.Other);
                    return;
                }

                if (!isJob)
                {
                    entity.BaseLvl = level;
                    MapModule.DisplayBaseLvlUpForRemoteEntity(entityId);
                }
                else
                {
                    // No action needed except display
                    MapModule.DisplayJobLvlUpForRemoteEntity(entityId);
                }
            }

            // TODO: Broadcast events/set dirty once UI is no longer Update-based
        }

        private void OnSkillTreeUpdateReceived(SkillTreeEntry entry)
        {
            // TODO: Verifications

            if(CurrentCharacterData == null)
            {
                CurrentCharacterData = new(new());
            }

            CurrentCharacterData.SkillTree[entry.SkillId] = entry;
            CurrentCharacterData.SkillTreeUpdated?.Invoke();
        }

        private void OnSkillTreeRemoveReceived(SkillId skillId)
        {
            if (CurrentCharacterData == null)
                return;

            CurrentCharacterData.SkillTree.Remove(skillId);
            CurrentCharacterData.SkillTreeUpdated?.Invoke();
        }

        private void OnSkillPointUpdateReceived(int remainingSkillPoints)
        {
            if (CurrentCharacterData == null)
            {
                OwlLogger.LogError("Received SkillPointAllocateResponse while CurrentChracter was null!", GameComponent.Other);
                return;
            }

            CurrentCharacterData.RemainingSkillPoints = remainingSkillPoints;
            CurrentCharacterData.SkillTreeUpdated?.Invoke();
        }

        public void DisplayOneButtonNotification(string message, Action callback)
        {
            if(_oneButtonNotification == null)
            {
                OwlLogger.LogError($"Tried to display generic notification {message} but notification is not available!", GameComponent.UI);
                return;
            }

            _oneButtonNotification.transform.SetAsLastSibling();
            _oneButtonNotification.SetContent(message, callback);
        }

        public ZeroButtonNotification DisplayZeroButtonNotification(string message)
        {
            if(_oneButtonNotification == null)
            {
                OwlLogger.LogError($"Tried to display zero-button notification {message} but notification is not available!", GameComponent.UI);
                return null;
            }

            _zeroButtonNotification.SetContent(message);
            _zeroButtonNotification.transform.SetAsLastSibling();
            return _zeroButtonNotification;
        }

        public void RequestReturnToSave()
        {
            if (CurrentCharacterData == null)
            {
                OwlLogger.LogError("Tried to request return to savepoint when local character data was null!", GameComponent.Other);
                return;
            }

            ReturnAfterDeathRequestPacket packet = new()
            {
                CharacterId = CurrentCharacterData.Id
            };

            ConnectionToServer.Send(packet);
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(LoadingMessage))
            {
                GUI.Label(LoadingMessagePlacement.ToRect(), LoadingMessage);
                return;
            }

            GUI.Label(_titlePlacement, "Ragnarok Again (Client)");

            if(ConnectionToServer == null)
            {
                Rect working = _titlePlacement;

                working.y += 40;
                GUI.Label(working, "Server IP:");
                working.y += 20;
                string newIp = GUI.TextField(working, IpInput);
                if(newIp != IpInput)
                {
                    IpInput = newIp;
                    ClientConfiguration.Instance.SetMiscConfig(ConfigurationKey.ServerIp, newIp);
                    ClientConfiguration.Instance.SaveConfig();
                }
                
            }

            //// Disconnect-Button/Label
            //if (ConnectionToServer == null)
            //{
            //    GUI.Label(DisconnectPlacement.ToRect(), "Disconnected");
            //}
            //else
            //{
            //    if (GUI.Button(DisconnectPlacement.ToRect(), "Disconnect"))
            //    {
            //        Disconnect();
            //    }
            //}
        }
    }
}
