using OwlLogging;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class HotkeyWidget : MonoBehaviour
    {
        private static HotkeyWidget _currentlyEditingWidget;

        public bool AllowEditing;

        public HotkeyConfigEntry Value { get; private set; }
        public Action<HotkeyWidget> ValueChanged;

        [SerializeField]
        private Button _editButton;

        [SerializeField]
        private TMP_Text _hotkeyText;

        private Event _event = new();
        private KeyCode _firstKey = KeyCode.None;

        private void Awake()
        {
            if (!OwlLogger.PrefabNullCheckAndLog(_editButton, nameof(_editButton), this, GameComponent.UI))
                _editButton.onClick.AddListener(OnEditClicked);

            OwlLogger.PrefabNullCheckAndLog(_hotkeyText, nameof(_hotkeyText), this, GameComponent.UI);
        }

        public void SetValue(HotkeyConfigEntry value)
        {
            if (value == null)
            {
                OwlLogger.LogError("Can't initialize HotkeyWidget with null HotkeyConfigEntry!", GameComponent.UI);
                return;
            }

            Value = value;

            UpdateHotkeyText();
        }

        private void UpdateHotkeyText()
        {
            _hotkeyText.SetText(Value.ToString());
        }

        private void OnEditClicked()
        {
            if (IsEditing())
            {
                DisableEditing(Value.Modifier, Value.Key);
            }
            else
            {
                if (!CanStartEdit())
                    return;

                EnableEditing();
            }
        }

        private bool CanStartEdit()
        {
            return AllowEditing && _currentlyEditingWidget == null;
        }

        // TODO: Localized Strings
        private void SetEditingUIMessage(string message)
        {
            if (message != null)
            {
                // Show & update Editing-UI
                ClientMain.Instance.DisplayZeroButtonNotification(message);
            }
            else
            {
                // Hide editing-UI
                ClientMain.Instance.DisplayZeroButtonNotification(null);
            }

        }

        public void EnableEditing()
        {
            // Set "editing"-marker on this UI element

            _currentlyEditingWidget = this;

            // TODO: Localized String 
            ClientMain.Instance.DisplayZeroButtonNotification("Please press the key/s you want to assign...");
        }

        private void Update()
        {
            if (!IsEditing())
                return;

            Event.PopEvent(_event); // Unsure if this has any side-effects - keep an eye on this

            if (_event.type == EventType.KeyDown)
            {
                if (_event.keyCode == KeyCode.Escape)
                {
                    DisableEditing(Value.Modifier, Value.Key);
                    return;
                }

                if (_firstKey == KeyCode.None)
                {
                    _firstKey = _event.keyCode;
                    SetEditingUIMessage($"Please press the key/s you want to assign...\n{_firstKey} + ...");
                }
                else
                {
                    if (_firstKey != _event.keyCode)
                        DisableEditing(_firstKey, _event.keyCode);
                }
            }
            else if (_event.type == EventType.KeyUp)
            {
                if (_firstKey != KeyCode.None && _firstKey == _event.keyCode)
                {
                    _firstKey = KeyCode.None;
                    SetEditingUIMessage("Please press the key/s you want to assign...");
                    DisableEditing(KeyCode.None, _event.keyCode);
                }
            }
        }

        public void DisableEditing(KeyCode mod, KeyCode key)
        {
            if (_currentlyEditingWidget != this)
            {
                OwlLogger.LogError("Desynced Editing mode!", GameComponent.UI);
                return;
            }

            Value.Modifier = mod;
            Value.Key = key;

            _currentlyEditingWidget = null;
            _firstKey = KeyCode.None;
            UpdateHotkeyText();
            SetEditingUIMessage(null);
            ValueChanged?.Invoke(this);
        }

        public bool IsEditing()
        {
            return _currentlyEditingWidget == this;
        }
    }
}