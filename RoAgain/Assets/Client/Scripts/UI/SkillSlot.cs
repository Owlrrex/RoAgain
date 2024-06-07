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

        private SkillIcon _skillIcon = null;
        private ConfigKey _hotkey;

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
            _skillIcon.CopyOnDrag = CopyOnDrag;
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
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"Slot ondrop {gameObject.name}");
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
    }
}