using OwlLogging;
using Shared;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client
{
    public class SkillSlot : MonoBehaviour, IDropHandler
    {
        public SkillId SkillId => _skillIcon != null ? _skillIcon.SkillId : SkillId.Unknown;
        public int SkillParam => _skillIcon != null ? _skillIcon.SkillParam : 0;

        public bool CopyOnDrag;

        public Action<SkillSlot> SkillDataChanged;

        [SerializeField]
        private Transform _iconSlot;

        [SerializeField]
        private TMP_Text _hotkeyText;

        [SerializeField]
        private TMP_Text _skillParamText;

        [SerializeField]
        private MouseTooltipTriggerLocalized _tooltipTriggerLoc;

        private SkillIcon _skillIcon = null;
        private ConfigKey _hotkey = ConfigKey.Unknown;

        private void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_iconSlot, nameof(_iconSlot), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_hotkeyText, nameof(_hotkeyText), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_skillParamText, nameof(_skillParamText), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_tooltipTriggerLoc, nameof(_tooltipTriggerLoc), this, GameComponent.UI);
            UpdateTooltip();
        }

        public void SetSkillId(SkillId skillId)
        {
            // Only create an icon if we really need it. 
            // This means empty slots can never fire drag-events
            if (skillId == SkillId.Unknown)
            {
                ClearSkill();
                return;
            }

            if (_skillIcon == null)
            {
                CreateIcon();
            }
            _skillIcon.SetSkillData(skillId, _skillIcon.SkillParam);

            UpdateTooltip();

            SkillDataChanged?.Invoke(this);
        }

        public void ClearSkill()
        {
            SetSkillParam(0);

            if (_skillIcon != null)
            {
                Destroy(_skillIcon.gameObject);
                _skillIcon = null;
            }

            UpdateTooltip();

            SkillDataChanged?.Invoke(this);
        }

        private void CreateIcon()
        {
            GameObject iconObj = Instantiate(PlayerUI.Instance.SkillIconPrefab, _iconSlot);
            _skillIcon = iconObj.GetComponent<SkillIcon>();
            if (_skillIcon == null)
            {
                OwlLogger.LogError($"Can't find SkillIcon component on SkillIconPrefab!", GameComponent.UI);
                return;
            }
            _skillIcon.CurrentSlot = this;
            Draggable draggable = iconObj.GetComponent<Draggable>();
            draggable.SetCreateNewOnDrag(CopyOnDrag);
        }

        public void SetSkillParam(int newParam)
        {
            if (newParam == 0)
            {
                _skillParamText.text = string.Empty;
            }
            else
            {
                _skillParamText.text = newParam.ToString();
            }

            if (_skillIcon != null)
            {
                _skillIcon.SetSkillData(_skillIcon.SkillId, newParam);
            }

            UpdateTooltip();

            SkillDataChanged?.Invoke(this);
        }

        public void SetHotkey(ConfigKey hotkey)
        {
            _hotkey = hotkey;

            if (_hotkeyText != null)
            {
                HotkeyConfigEntry entry = MixedConfiguration.Instance.GetHotkey(hotkey);
                _hotkeyText.text = entry?.ToString();
            }

            UpdateTooltip();
        }

        public void OnDrop(PointerEventData eventData)
        {
            SkillIcon droppedSkillIcon = null;
            if(eventData.pointerDrag != null)
                droppedSkillIcon = eventData.pointerDrag.GetComponent<SkillIcon>();

            if (droppedSkillIcon == null)
            {
                OwlLogger.Log($"Received non-SkillIcon drop in SkillSlot", GameComponent.UI, LogSeverity.Verbose);
                return;
            }

            SkillId skillId = droppedSkillIcon.SkillId;
            if (skillId == SkillId.Unknown)
            {
                OwlLogger.LogError($"SkillSlot received SkillIcon with skillId {skillId}!", GameComponent.UI);
                return;
            }

            SetSkillId(skillId);
            SetSkillParam(droppedSkillIcon.SkillParam);
        }

        public void OnIconRemoved()
        {
            _skillIcon.CurrentSlot = null;
            _skillIcon = null; // Set this to null first so that Clearskill() doesn't destroy the icon that's being dragged
            SetSkillId(SkillId.Unknown);
        }

        private void UpdateTooltip()
        {
            if (SkillId == SkillId.Unknown)
            {
                _tooltipTriggerLoc.LocalizedString = new LocalizedStringId(247);
            }
            else
            {
                string hotkeyStr = null;
                if(_hotkey != ConfigKey.Unknown)
                {
                    HotkeyConfigEntry entry = MixedConfiguration.Instance.GetHotkey(_hotkey);
                    hotkeyStr = entry?.ToString();
                }

                SkillClientData skillData = SkillClientDataTable.GetDataForId(SkillId);
                ILocalizedString skillName = skillData.NameId;
                string skillParam = _skillParamText != null ? _skillParamText.text : null;
                _tooltipTriggerLoc.LocalizedString = new CompositeLocalizedString()
                {
                    FormatString = new LocalizedStringId(248),
                    Arguments = { hotkeyStr, skillName, skillParam }
                };
            }
        }
    }
}