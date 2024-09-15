using UnityEngine;
using OwlLogging;
using TMPro;
using UnityEngine.UI;
using System;

namespace Client
{
    public delegate void NumberConfirmed(int value);
    public delegate void NumberCancelled();

    public class GeneralNumberInput : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField _input;
        [SerializeField]
        private Button _confirmButton;
        [SerializeField]
        private Button _cancelButton;
        [SerializeField]
        private Button _minButton;
        [SerializeField]
        private Button _minusButton;
        [SerializeField]
        private Button _plusButton;
        [SerializeField]
        private Button _maxButton;

        private int _lastValidValue;
        private int _minValue;
        private int _maxValue;
        private NumberConfirmed _confirmCallback;
        private NumberCancelled _cancelCallback;

        // Start is called before the first frame update
        private void Awake()
        {
            if (!OwlLogger.PrefabNullCheckAndLog(_input, nameof(_input), this, GameComponent.UI))
            {
                _input.onValueChanged.AddListener(OnValueChanged);
            }

            if (!OwlLogger.PrefabNullCheckAndLog(_confirmButton, nameof(_confirmButton), this, GameComponent.UI))
                _confirmButton.onClick.AddListener(OnConfirmClicked);
            if (!OwlLogger.PrefabNullCheckAndLog(_cancelButton, nameof(_cancelButton), this, GameComponent.UI))
                _cancelButton.onClick.AddListener(OnCancelClicked);
            if (!OwlLogger.PrefabNullCheckAndLog(_minButton, nameof(_minButton), this, GameComponent.UI))
                _minButton.onClick.AddListener(OnMinClicked);
            if (!OwlLogger.PrefabNullCheckAndLog(_minusButton, nameof(_minusButton), this, GameComponent.UI))
                _minusButton.onClick.AddListener(OnMinusClicked);
            if (!OwlLogger.PrefabNullCheckAndLog(_plusButton, nameof(_plusButton), this, GameComponent.UI))
                _plusButton.onClick.AddListener(OnPlusClicked);
            if (!OwlLogger.PrefabNullCheckAndLog(_maxButton, nameof(_maxButton), this, GameComponent.UI))
                _maxButton.onClick.AddListener(OnMaxClicked);
        }

        private void OnValueChanged(string newValue)
        {
            if (string.IsNullOrEmpty(newValue))
                newValue = "0";

            if (!int.TryParse(newValue, out int newInt))
            {
                _input.text = _lastValidValue.ToString();
                return;
            }

            if (newInt == _lastValidValue)
                return;

            newInt = Math.Clamp(newInt, _minValue, _maxValue);
            _lastValidValue = newInt;
            _input.text = newInt.ToString(); // To get rid of unnecessary zeroes & such
        }

        private void OnConfirmClicked()
        {
            NumberConfirmed tmpCbk = _confirmCallback;
            int amount = _lastValidValue;
            Hide(); // Hide first so callbacks can show the Input again if they want to
            tmpCbk?.Invoke(amount);
        }

        private void OnCancelClicked()
        {
            NumberCancelled tmpCbk = _cancelCallback;
            Hide(); // Hide first so callbacks can show the Input again if they want to
            tmpCbk?.Invoke();
        }

        private void OnMaxClicked()
        {
            _input.text = _maxValue.ToString();
            _lastValidValue = _maxValue;
        }

        private void OnMinClicked()
        {
            _input.text = _minValue.ToString();
            _lastValidValue = _minValue;
        }

        private void OnPlusClicked()
        {
            if (_lastValidValue >= _maxValue)
                return;

            _input.text = (++_lastValidValue).ToString();
        }

        private void OnMinusClicked()
        {
            if (_lastValidValue <= _minValue)
                return;

            _input.text = (--_lastValidValue).ToString();
        }

        public void Show(int min, int max, int start, NumberConfirmed confirmCallback, NumberCancelled cancelCallback)
        {
            if (gameObject.activeSelf)
            {
                OwlLogger.LogWarning("GeneralNumberInput is Shown while already visible - this likely means a previous interaction was interrupted!", GameComponent.UI);
            }

            if(start < min || start > max)
            {
                OwlLogger.LogError($"Tried to show GeneralNumberInput with invalid start value {start}", GameComponent.UI);
                start = min;
            }

            _minValue = min;
            _maxValue = max;
            _lastValidValue = start;
            _input.text = start.ToString();
            _confirmCallback = confirmCallback;
            _cancelCallback = cancelCallback;
            gameObject.SetActive(true);
            _input.Select();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _minValue = 0;
            _maxValue = 0;
            _input.text = "Hidden";
            _confirmCallback = null;
            _cancelCallback = null;
        }
    }
}
