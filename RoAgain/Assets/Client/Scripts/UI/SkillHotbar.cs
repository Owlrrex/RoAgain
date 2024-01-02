using OwlLogging;
using Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class SkillHotbar : MonoBehaviour
    {
        // This will probably go into some userconfig-class eventually, and be passed in here as data model
        [Serializable]
        public class SkillHotbarEntry
        {
            public SkillId SkillId;
            public int SkillParam;
            public ConfigurableHotkey Hotkey;
        }

        [SerializeField]
        private List<SkillSlot> _skillSlots;

        [SerializeField]
        private List<SkillHotbarEntry> _data;

        // TMP: For checking hotkeys using Input.GetKeyDown()
        // Read from config later, and/or use different input system
        public List<SkillHotbarEntry> Data => _data;

        // Start is called before the first frame update
        void Start()
        {
            OwlLogger.PrefabNullCheckAndLog(_skillSlots, "skillSlots", this, GameComponent.UI);

            while (_data.Count < _skillSlots.Count)
            {
                _data.Add(default);
            }

            if (_data.Count != _skillSlots.Count)
            {
                OwlLogger.LogError($"SkillSlots & Hotkeys array size mismatch: {_skillSlots.Count} slots, {_data.Count} hotkeys!", GameComponent.UI);
            }

            // TODO: For now, use data straight from editor
            // Later, some other part of the game loads this from config & passes it in during initialization of the UI
            UpdateDisplay();

            foreach (SkillSlot slot in _skillSlots)
            {
                slot.SkillDataChanged += OnSkillIdChanged;
            }
        }

        public void SetData(List<SkillHotbarEntry> newData)
        {
            if (newData.Count > _skillSlots.Count)
            {
                OwlLogger.LogWarning($"SkillHotbarData length exceeds hotbar display capacity!", GameComponent.UI);
            }

            for (int i = 0; i < _skillSlots.Count; i++)
            {
                if (i < newData.Count)
                    _data[i] = newData[i];
                else
                    _data[i] = default;
            }

            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            for (int i = 0; i < _skillSlots.Count; i++)
            {
                _skillSlots[i].SetSkillId(_data[i].SkillId);
                _skillSlots[i].SetSkillParam(_data[i].SkillParam);
                _skillSlots[i].SetHotkey(_data[i].Hotkey);
            }
        }

        // TODO: Make it return the whole SkillHotbarEntry data in case the Data-property gets hidden
        //public SkillId GetSkillIdForKey(ConfigurableHotkey hotkey)
        //{
        //    for (int i = 0; i < _data.Count; i++)
        //    {
        //        if (_data[i].Hotkey == hotkey)
        //            return _data[i].SkillId;
        //    }

        //    return SkillId.Unknown;
        //}

        private void OnSkillIdChanged(SkillSlot slot)
        {
            int index = _skillSlots.IndexOf(slot);
            if (index < 0)
            {
                OwlLogger.LogError($"SkillHotbar received onSkillDrop for slot that wasn't found in list!", GameComponent.UI);
                return;
            }

            SkillHotbarEntry data = _data[index];
            data.SkillId = slot.SkillId;
            data.SkillParam = slot.SkillParam;
        }
    }
}
