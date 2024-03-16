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

    public GraphicRaycaster uiRaycaster;
    private List<RaycastResult> _raycastResults = new();
    private PointerEventData _isHoveringUiEventData;

    // Skill Dragging
    public GameObject SkillIconPrefab;

    [SerializeField]
    private SkillHotbar _hotbar;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

        OwlLogger.PrefabNullCheckAndLog(_hotbar, "hotbar", this, GameComponent.UI);
        if(!OwlLogger.PrefabNullCheckAndLog(ChatSystem, "ChatSystem", this, GameComponent.UI))
            ChatSystem.Initialize();

        if(!OwlLogger.PrefabNullCheckAndLog(CharacterWindow, "CharacterWindow", this, GameComponent.UI))
            CharacterWindow.Initialize(ClientMain.Instance.CurrentCharacterData);

        if (!OwlLogger.PrefabNullCheckAndLog(StatWindow, "StatWindow", this, GameComponent.UI))
            StatWindow.gameObject.SetActive(false);

        if (!OwlLogger.PrefabNullCheckAndLog(SkillTreeWindow, "SkillTreeWindow", this, GameComponent.UI))
            SkillTreeWindow.gameObject.SetActive(false);

        if(!OwlLogger.PrefabNullCheckAndLog(DeathWindow, "DeathWindow", this, GameComponent.UI))
            DeathWindow.gameObject.SetActive(false);

        if(!OwlLogger.PrefabNullCheckAndLog(GameMenuWindow, "GameMenuWindow", this, GameComponent.UI))
            GameMenuWindow.gameObject.SetActive(false);
    }

    public bool IsHoveringUI(Vector2 position)
    {
        _isHoveringUiEventData ??= new(EventSystem.current);
        _isHoveringUiEventData.position = position;
        _raycastResults.Clear();
        uiRaycaster.Raycast(_isHoveringUiEventData, _raycastResults);
        return _raycastResults.Count > 0;
    }    

    private void Update()
    {
        if (ChatSystem.IsChatFocused)
            return; // don't process hotkeys while chat is open

        foreach (SkillHotbar.SkillHotbarEntry entry in _hotbar.Data)
        {
            if (entry.Hotkey == ConfigurableHotkey.Unknown)
                continue;

            if (KeyboardInput.Instance?.IsConfigurableHotkeyDown(entry.Hotkey) == true)
            {
                SendSkillInput(entry);
            }
        }

        if(KeyboardInput.Instance?.IsConfigurableHotkeyDown(ConfigurableHotkey.ToggleStatWindow) == true)
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

        if (KeyboardInput.Instance?.IsConfigurableHotkeyDown(ConfigurableHotkey.ToggleSkillWindow) == true)
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

        if (KeyboardInput.Instance?.IsConfigurableHotkeyDown(ConfigurableHotkey.ToggleHotbar) == true)
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

        if(KeyboardInput.Instance?.IsConfigurableHotkeyDown(ConfigurableHotkey.ToggleGameMenuWindow) == true)
        {
            if(GameMenuWindow.gameObject.activeSelf)
            {
                GameMenuWindow.gameObject.SetActive(false);
            }
            else
            {
                GameMenuWindow.gameObject.SetActive(true);
            }
        }

        if(ClientMain.Instance.CurrentCharacterData != null)
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
}
