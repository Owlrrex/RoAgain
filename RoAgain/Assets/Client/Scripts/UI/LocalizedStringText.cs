using OwlLogging;
using TMPro;
using UnityEngine;

namespace Client
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedStringText : MonoBehaviour
    {
        [SerializeField]
        private LocalizedStringId _localizedStringId = LocalizedStringId.INVALID;
        public LocalizedStringId LocalizedStringId => _localizedStringId;

        private TMP_Text _text;

        // Start is called before the first frame update
        void Start()
        {
            if(!TryGetComponent(out _text))
            {
                OwlLogger.LogError($"No TMP_Text found for LocalizedStringText!", GameComponent.UI);
                return;
            }

            UpdateStringDisplay();
        }

        private void OnEnable()
        {
            LocalizedStringTable.LanguageChanged += OnLanguageChanged;
            UpdateStringDisplay();
        }

        private void OnDisable()
        {
            LocalizedStringTable.LanguageChanged -= OnLanguageChanged;
        }

        public void SetLocalizedString(LocalizedStringId newStringId)
        {
            if (newStringId == _localizedStringId)
                return;

            _localizedStringId = newStringId;
            UpdateStringDisplay();
        }

        private void OnLanguageChanged()
        {
            UpdateStringDisplay();
        }

        private void UpdateStringDisplay()
        {
            if (_text == null)
                return;

            if (_localizedStringId == LocalizedStringId.INVALID)
                return;

            _text.text = LocalizedStringTable.GetStringById(_localizedStringId);
        }
    }
}