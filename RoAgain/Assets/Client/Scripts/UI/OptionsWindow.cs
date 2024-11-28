using OwlLogging;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class OptionsMenuData
    {
        public enum Tab
        {
            Unknown,
            Misc,
            Gameplay,
            Video,
            Audio,
            Hotkeys
        }

        public class Entry
        {
            public Tab Tab;
            public int PrefabIndex;
        }

        public Dictionary<ConfigKey, Entry> Data = new(); // This represents the whole input for key-tab pairs - including values offered by the Server or read from some file

        // Temporary, until Server-serving for config values is supported
        private static OptionsMenuData _defaultOptionsData;
        public static OptionsMenuData GetDefault()
        {
            if (_defaultOptionsData == null)
            {
                _defaultOptionsData = new();
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_Hotbar11, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_Hotbar12, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_Hotbar13, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_Hotbar14, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_Hotbar15, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_Hotbar16, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_Hotbar17, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_Hotbar18, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_Hotbar19, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_Hotbar10, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_ToggleCharMainWindow, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_ToggleChatInput, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_ToggleGameMenuWindow, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_ToggleHotbar, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_ToggleSkillWindow, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_ToggleStatWindow, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
                _defaultOptionsData.Data.Add(ConfigKey.Hotkey_ToggleEquipmentWindow, new() { PrefabIndex = 0, Tab = Tab.Hotkeys });
            }

            return _defaultOptionsData;
        }
    }

    public class OptionsWindow : MonoBehaviour
    {
        [SerializeField]
        private Transform _optionWidgetContainer;

        [SerializeField]
        private OptionsTabRadioGroup _tabRadioGroup;

        [SerializeField]
        private Button _closeButton;

        [SerializeField]
        private Button _applyButton;

        [SerializeField]
        private Button _revertButton;

        private OptionsMenuData.Tab _currentTab = OptionsMenuData.Tab.Misc;

        private OptionsMenuData _data;

        private HashSet<ConfigKey> _unsavedChanges = new();

        private Dictionary<ConfigKey, AConfigLineWidget> _createdWidgets = new();
    
        // Start is called before the first frame update
        void Start()
        {
            OwlLogger.PrefabNullCheckAndLog(_optionWidgetContainer, nameof(_optionWidgetContainer), this, GameComponent.UI);

            if(!OwlLogger.PrefabNullCheckAndLog(_tabRadioGroup, nameof(_tabRadioGroup), this, GameComponent.UI))
            {
                _tabRadioGroup.SelectionChanged += OnTabSelectionChanged;
                if(_tabRadioGroup.CurrentRadioButton != null)
                    _currentTab = _tabRadioGroup.CurrentRadioButton.Value;
            }

            if (!OwlLogger.PrefabNullCheckAndLog(_closeButton, nameof(_closeButton), this, GameComponent.UI))
                _closeButton.onClick.AddListener(OnCloseClicked);

            if(!OwlLogger.PrefabNullCheckAndLog(_applyButton, nameof(_applyButton), this, GameComponent.UI))
                _applyButton.onClick.AddListener(OnApplyClicked);

            if(!OwlLogger.PrefabNullCheckAndLog(_revertButton, nameof(_revertButton), this, GameComponent.UI))
                _revertButton.onClick.AddListener(OnRevertClicked);
        }

        public void Init(OptionsMenuData data)
        {
            if(data == null)
            {
                OwlLogger.LogError("Can't initialize with null data!", GameComponent.UI);
                return;
            }

            if(_data != null)
            {
                // No de-init steps necessary at the moment
            }

            _data = data;

            // TODO: Generate Radio Buttons for Tabs

            PopulateOptionWidgets();
        }

        private void PopulateOptionWidgets()
        {
            foreach(var kvp in _createdWidgets)
            {
                Destroy(kvp.Value.gameObject);
            }
            _createdWidgets.Clear();

            foreach (var kvp in _data.Data)
            {
                if (kvp.Value.Tab != _currentTab)
                    continue;

                GameObject prefab = ConfigWidgetRegistry.GetPrefabForIndex(kvp.Value.PrefabIndex);
                if(prefab == null)
                {
                    OwlLogger.LogError($"Can't find prefab for index {kvp.Value.PrefabIndex}, config key {kvp.Key}", GameComponent.UI);
                    continue;
                }
                GameObject go = Instantiate(prefab, _optionWidgetContainer);
                AConfigLineWidget clw = go.GetComponent<AConfigLineWidget>();
                clw.Init(kvp.Key);
                _createdWidgets.Add(kvp.Key, clw);
                clw.ValueChanged += OnValueChanged;
            }
        }

        private void OnTabSelectionChanged(RadioButton<OptionsMenuData.Tab> button)
        {
            if (button is not OptionsTabRadioButton tabButton)
            {
                OwlLogger.LogError($"OptionsWindow received TabSelection callback from non-TabRadioButton {button.gameObject.name}", GameComponent.UI);
                return;
            }

            if (_currentTab == button.Value)
                return;

            if (_unsavedChanges.Count > 0)
            {
                // TODO: Warn user with "Proceed or cancel" dialog
            }

            // This would only be called if the user clicks "proceed" in the dialog
            InitializeCurrentTab(button.Value);
        }

        private void InitializeCurrentTab(OptionsMenuData.Tab tab)
        {
            OnRevertClicked();

            _currentTab = tab;

            PopulateOptionWidgets();
        }

        private void OnValueChanged(ConfigKey key)
        {
            _unsavedChanges.Add(key);

            _applyButton.interactable = true;
            _revertButton.interactable = true;
        }

        private void OnApplyClicked()
        {
            foreach(ConfigKey changedKey in _unsavedChanges)
            {
                _createdWidgets[changedKey].Save();
            }
            _unsavedChanges.Clear();
            _applyButton.interactable = false;
            _revertButton.interactable = false;
        }

        private void OnRevertClicked()
        {
            foreach(ConfigKey changedKey in _unsavedChanges)
            {
                _createdWidgets[changedKey].Init(changedKey);
            }
            _unsavedChanges.Clear();
            _applyButton.interactable = false;
            _revertButton.interactable = false;
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }
    }
}