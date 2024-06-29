using OwlLogging;
using TMPro;
using UnityEngine;

namespace Client
{
    public class HotkeyLineData
    {
        public ConfigKey Key;
        public HotkeyConfigEntry LocalValue;
        public HotkeyConfigEntry CharValue;
        public HotkeyConfigEntry AccValue;
    }  

    public class HotkeyLineWidget : AConfigLineWidget
    {
        [SerializeField]
        private TMP_Text _keyText;

        [SerializeField]
        private HotkeyWidget _localHotkeyWidget;
        [SerializeField]
        private HotkeyWidget _accHotkeyWidget;
        [SerializeField]
        private HotkeyWidget _charHotkeyWidget;

        private HotkeyLineData _data;

        private void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_keyText, nameof(_keyText), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_localHotkeyWidget, nameof(_localHotkeyWidget), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_accHotkeyWidget, nameof(_accHotkeyWidget), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_charHotkeyWidget, nameof(_charHotkeyWidget), this, GameComponent.UI);

            _localHotkeyWidget.ValueChanged += OnLocalValueChanged;
            _accHotkeyWidget.ValueChanged += OnAccValueChanged;
            _charHotkeyWidget.ValueChanged += OnCharValueChanged;
        }

        public override void Init(ConfigKey configKey)
        {
            if(!configKey.IsHotkey())
            {
                OwlLogger.LogError($"Can't initialize HotkeyLineWidget with non-hotkey config key {configKey}!", GameComponent.UI);
                return;
            }

            HotkeyLineData data = new HotkeyLineData();
            data.Key = configKey;
            data.LocalValue = MixedConfiguration.Instance.GetHotkey(configKey, MixedConfigSource.Local) ?? new();
            data.AccValue = MixedConfiguration.Instance.GetHotkey(configKey, MixedConfigSource.Account) ?? new();
            data.CharValue = MixedConfiguration.Instance.GetHotkey(configKey, MixedConfigSource.Character) ?? new();

            Init(data);
        }

        public void Init(HotkeyLineData data)
        {
            _data = data;
            UpdateKeyString();
            UpdateLocalHotkeyWidget();
            UpdateAccHotkeyWidget();
            UpdateCharHotkeyWidget();
        }

        private void UpdateKeyString()
        {
            _keyText.SetText(_data.Key.ToString());
        }

        private void UpdateLocalHotkeyWidget()
        {
            _localHotkeyWidget.SetValue(_data.LocalValue);
        }

        private void UpdateAccHotkeyWidget()
        {
            _accHotkeyWidget.SetValue(_data.AccValue);
        }

        private void UpdateCharHotkeyWidget()
        {
            _charHotkeyWidget.SetValue(_data.CharValue);
        }

        private void OnLocalValueChanged(HotkeyWidget _)
        {
            _data.LocalValue = _localHotkeyWidget.Value;

            UpdateLocalHotkeyWidget();

            ValueChanged?.Invoke(_data.Key);
        }

        private void OnAccValueChanged(HotkeyWidget _)
        {
            _data.AccValue = _accHotkeyWidget.Value;

            UpdateAccHotkeyWidget();

            ValueChanged?.Invoke(_data.Key);
        }

        private void OnCharValueChanged(HotkeyWidget _)
        {
            _data.CharValue = _charHotkeyWidget.Value;

            UpdateCharHotkeyWidget();

            ValueChanged?.Invoke(_data.Key);
        }

        public override void Save()
        {
            if (_data.LocalValue.IsValid())
                MixedConfiguration.Instance.SetHotkey(_data.Key, _data.LocalValue, MixedConfigSource.Local);
            else
                MixedConfiguration.Instance.ClearConfigValue(_data.Key, MixedConfigSource.Local);

            if (_data.AccValue.IsValid())
                MixedConfiguration.Instance.SetHotkey(_data.Key, _data.AccValue, MixedConfigSource.Account);
            else
                MixedConfiguration.Instance.ClearConfigValue(_data.Key, MixedConfigSource.Account);

            if (_data.CharValue.IsValid())
                MixedConfiguration.Instance.SetHotkey(_data.Key, _data.CharValue, MixedConfigSource.Character);
            else
                MixedConfiguration.Instance.ClearConfigValue(_data.Key, MixedConfigSource.Character);
        }
    }
}