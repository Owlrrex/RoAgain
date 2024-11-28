using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using OwlLogging;
using Client;
using Shared;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance { get; private set; }

    [field: SerializeField]
    public UIChatSystem ChatSystem { get; private set; }

    [field: SerializeField]
    public CharacterWindow CharacterWindow { get; private set; }

    [field: SerializeField]
    public StatWindow StatWindow { get; private set; }

    [field: SerializeField]
    public SkillTreeWindow SkillTreeWindow { get; private set; }

    [field: SerializeField]
    public DeathWindow DeathWindow { get; private set; }

    [field: SerializeField]
    public GameMenuWindow GameMenuWindow { get; private set; }

    [field: SerializeField]
    public OptionsWindow OptionsWindow { get; private set; }

    [field: SerializeField]
    public InventoryWindow InventoryWindow { get; private set; }

    [field: SerializeField]
    public EquipmentWindow EquipmentWindow { get; private set; }

    [SerializeField]
    private ConfigWidgetRegistry _configWidgetRegistry;

    public bool IsTextInputActive => TextInputCounter > 0;
    public int TextInputCounter = 0;

    private List<RaycastResult> _raycastResults = new();
    private PointerEventData _hoveredLayerQueryEventData;
    private int _uiLayer;

    // Skill Dragging
    public GameObject SkillIconPrefab;

    [SerializeField]
    private SkillHotbar _hotbar;


    private bool _chatSystemNeedsInit = false;

    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;

        _uiLayer = LayerMask.NameToLayer("UI");

        OwlLogger.PrefabNullCheckAndLog(_hotbar, "hotbar", this, GameComponent.UI);
        if (!OwlLogger.PrefabNullCheckAndLog(ChatSystem, "ChatSystem", this, GameComponent.UI))
        {
            if(ClientMain.Instance != null)
            {
                ChatSystem.Initialize(ClientMain.Instance.ChatModule);
            }
            else
            {
                _chatSystemNeedsInit = true;
            }
        }

        if (!OwlLogger.PrefabNullCheckAndLog(CharacterWindow, "CharacterWindow", this, GameComponent.UI))
        {
            if(ClientMain.Instance != null && ClientMain.Instance.CurrentCharacterData != null)
            {
                CharacterWindow.Initialize(ClientMain.Instance.CurrentCharacterData);
            }
        }

        if (!OwlLogger.PrefabNullCheckAndLog(StatWindow, "StatWindow", this, GameComponent.UI))
            StatWindow.gameObject.SetActive(false);

        if (!OwlLogger.PrefabNullCheckAndLog(SkillTreeWindow, "SkillTreeWindow", this, GameComponent.UI))
            SkillTreeWindow.gameObject.SetActive(false);

        if (!OwlLogger.PrefabNullCheckAndLog(DeathWindow, "DeathWindow", this, GameComponent.UI))
            DeathWindow.gameObject.SetActive(false);

        if (!OwlLogger.PrefabNullCheckAndLog(GameMenuWindow, "GameMenuWindow", this, GameComponent.UI))
            GameMenuWindow.gameObject.SetActive(false);

        if (!OwlLogger.PrefabNullCheckAndLog(OptionsWindow, "OptionsWindow", this, GameComponent.UI))
            OptionsWindow.gameObject.SetActive(false);

        if (!OwlLogger.PrefabNullCheckAndLog(_configWidgetRegistry, nameof(_configWidgetRegistry), this, GameComponent.UI))
            _configWidgetRegistry.Init();

        if(!OwlLogger.PrefabNullCheckAndLog(InventoryWindow, nameof(InventoryWindow), this, GameComponent.UI))
            InventoryWindow.gameObject.SetActive(false);

        if (!OwlLogger.PrefabNullCheckAndLog(EquipmentWindow, nameof(EquipmentWindow), this, GameComponent.UI))
            EquipmentWindow.gameObject.SetActive(false);


        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (ClientMain.Instance.CurrentCharacterData != null)
        {
            CharacterWindow.Initialize(ClientMain.Instance.CurrentCharacterData);
        }
    }

    public LayerMask HoveredLayers;
    private bool _hasUpdatedHoveredLayers;

    public void TryUpdateHoveredLayers()
    {
        if (_hasUpdatedHoveredLayers)
            return;

        _hoveredLayerQueryEventData ??= new(EventSystem.current);
        _hoveredLayerQueryEventData.position = Input.mousePosition;
        _raycastResults.Clear();
        EventSystem.current.RaycastAll(_hoveredLayerQueryEventData, _raycastResults);
        HoveredLayers = 0;
        foreach(RaycastResult result in _raycastResults)
        {
            HoveredLayers |= 1 << result.gameObject.layer;
        }

        _hasUpdatedHoveredLayers = true;
    }

    public bool IsHoveringUI(Vector2 position)
    {
        TryUpdateHoveredLayers();

        return HoveredLayers.HasLayer("UI");
    }

    private void LateUpdate()
    {
        _hasUpdatedHoveredLayers = false;
    }

    private void Update()
    {
        if(_chatSystemNeedsInit)
        {
            if(ClientMain.Instance  != null)
            {
                ChatSystem.Initialize(ClientMain.Instance.ChatModule);
                _chatSystemNeedsInit = false;
            }
        }

        if (ClientMain.Instance.CurrentCharacterData != null)
        {
            if (ClientMain.Instance.CurrentCharacterData.IsDead())
            {
                if (!DeathWindow.gameObject.activeSelf)
                {
                    DeathWindow.gameObject.SetActive(true);
                    PlayerMain.Instance.DisplayDeathAnimation(true);
                }
            }
            else
            {
                if (DeathWindow.gameObject.activeSelf)
                {
                    DeathWindow.gameObject.SetActive(false);
                    PlayerMain.Instance.DisplayDeathAnimation(false);
                }
            }
        }

        if (ChatSystem.IsChatFocused || IsTextInputActive)
            return; // don't process hotkeys while chat or other text-inputs are open

        foreach (SkillHotbar.SkillHotbarEntry entry in _hotbar.Data)
        {
            if (entry.Hotkey == ConfigKey.Unknown)
                continue;

            if (KeyboardInput.Instance?.IsConfigurableHotkeyDown(entry.Hotkey) == true)
            {
                SendSkillInput(entry);
            }
        }

        if(KeyboardInput.Instance?.IsConfigurableHotkeyDown(ConfigKey.Hotkey_ToggleStatWindow) == true)
        {
            if(StatWindow.gameObject.activeSelf)
            {
                StatWindow.gameObject.SetActive(false);
            }
            else
            {
                StatWindow.Initialize(ClientMain.Instance.CurrentCharacterData);
                StatWindow.gameObject.SetActive(true);
            }
        }

        if (KeyboardInput.Instance?.IsConfigurableHotkeyDown(ConfigKey.Hotkey_ToggleSkillWindow) == true)
        {
            if(SkillTreeWindow.gameObject.activeSelf)
            {
                SkillTreeWindow.gameObject.SetActive(false);
            }
            else
            {
                SkillTreeWindow.gameObject.SetActive(true);
                SkillTreeWindow.DisplayCurrentCharacterData();
            }
        }

        if (KeyboardInput.Instance?.IsConfigurableHotkeyDown(ConfigKey.Hotkey_ToggleHotbar) == true)
        {
            if(_hotbar.gameObject.activeSelf)
            {
                _hotbar.gameObject.SetActive(false);
            }
            else
            {
                // TODO: Read hotbar data from config?
                _hotbar.gameObject.SetActive(true);
            }
        }

        if (KeyboardInput.Instance?.IsConfigurableHotkeyDown(ConfigKey.Hotkey_ToggleGameMenuWindow) == true)
        {
            if (GameMenuWindow.gameObject.activeSelf)
            {
                GameMenuWindow.gameObject.SetActive(false);
            }
            else
            {
                GameMenuWindow.gameObject.SetActive(true);
            }
        }

        if(KeyboardInput.Instance?.IsConfigurableHotkeyDown(ConfigKey.Hotkey_ToggleInventoryWindow) == true)
        {
            if(InventoryWindow.gameObject.activeSelf)
            {
                InventoryWindow.gameObject.SetActive(false);
            }
            else
            {
                InventoryWindow.SetData(ClientMain.Instance.InventoryModule.PlayerMainInventory);
                InventoryWindow.gameObject.SetActive(true);
            }
        }

        if(KeyboardInput.Instance?.IsConfigurableHotkeyDown(ConfigKey.Hotkey_ToggleEquipmentWindow) == true)
        {
            EquipmentWindow.gameObject.SetActive(!EquipmentWindow.gameObject.activeSelf);
        }
    }

    private void SendSkillInput(SkillHotbar.SkillHotbarEntry entry)
    {
        // TODO: 
        // if (entry.SkillId == SkillId.UseItem)
        // {
        //      Handle Item usage code, interpret SkillParam differently, etc
        // }

        Coordinate mouseCoords = NavMeshClick.Instance.GetMouseGridCoords();
        if (mouseCoords == GridData.INVALID_COORDS)
            return;

        if (entry.SkillId.IsGroundSkill())
        {
            SkillUseGroundRequestPacket packet = new()
            {
                SkillId = entry.SkillId,
                SkillLvl = entry.SkillParam,
                TargetCoords = mouseCoords
            };
            ClientMain.Instance.ConnectionToServer.Send(packet);
        }
        else
        {
            List<GridEntity> occupants = ClientMain.Instance.MapModule.Grid.Data.GetOccupantsOfCell(mouseCoords);
            foreach(GridEntity occupant in occupants)
            {
                if (occupant is not ClientBattleEntity bOccupant)
                    continue;

                SkillUseEntityRequestPacket packet = new()
                {
                    SkillId = entry.SkillId,
                    SkillLvl = entry.SkillParam,
                    TargetId = bOccupant.Id
                };
                ClientMain.Instance.ConnectionToServer.Send(packet);
                break;
            }
        }
    }

    public void ShowOptionsWindow(OptionsMenuData data)
    {
        OptionsWindow.Init(data);
        OptionsWindow.gameObject.SetActive(true);
    }
}
