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
    public ChatSystem ChatSystem { get; private set; }

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
    [SerializeField]
    private ConfigWidgetRegistry _configWidgetRegistry;

    public GraphicRaycaster uiRaycaster;
    private List<RaycastResult> _raycastResults = new();
    private PointerEventData _isHoveringUiEventData;
    private bool? _isHoveringUi;
    private int _uiLayer;

    // Skill Dragging
    public GameObject SkillIconPrefab;

    [SerializeField]
    private SkillHotbar _hotbar;

    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;

        _uiLayer = LayerMask.NameToLayer("UI");

        OwlLogger.PrefabNullCheckAndLog(_hotbar, "hotbar", this, GameComponent.UI);
        if (!OwlLogger.PrefabNullCheckAndLog(ChatSystem, "ChatSystem", this, GameComponent.UI))
            ChatSystem.Initialize();

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

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (ClientMain.Instance.CurrentCharacterData != null)
        {
            CharacterWindow.Initialize(ClientMain.Instance.CurrentCharacterData);
        }
    }

    public bool IsHoveringUI(Vector2 position)
    {
        if (_isHoveringUi != null)
            return _isHoveringUi == true;

        _isHoveringUiEventData ??= new(EventSystem.current);
        _isHoveringUiEventData.position = position;
        _raycastResults.Clear();
        EventSystem.current.RaycastAll(_isHoveringUiEventData, _raycastResults);
        _isHoveringUi = false;
        foreach(RaycastResult result in _raycastResults)
        {
            if(result.gameObject.layer == LayerMask.NameToLayer("UI"))
            {
                _isHoveringUi = true;
                break;
            }
        }
        return _isHoveringUi == true;
    }

    private void LateUpdate()
    {
        _isHoveringUi = null;
    }

    private void Update()
    {
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

        if (ChatSystem.IsChatFocused)
            return; // don't process hotkeys while chat is open

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
    }

    private void SendSkillInput(SkillHotbar.SkillHotbarEntry entry)
    {
        // TODO: 
        // if (entry.SkillId == SkillId.UseItem)
        // {
        //      Handle Item usage code, interpret SkillParam differently, etc
        // }

        Vector2Int mouseCoords = NavMeshClick.Instance.GetMouseGridCoords();
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
