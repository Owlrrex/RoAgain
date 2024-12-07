using OwlLogging;
using Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Client
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedStringText : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("_localizedStringId")]
        private LocalizedStringId _defaultLocStringId = LocalizedStringId.INVALID;

        public ILocalizedString CurrentLocString { get; private set; }

        private TMP_Text _text;

        // Start is called before the first frame update
        void Start()
        {
            if(!TryGetComponent(out _text))
            {
                OwlLogger.LogError($"No TMP_Text found for LocalizedStringText!", GameComponent.UI);
                return;
            }

            if (ILocalizedString.IsValid(_defaultLocStringId))
                CurrentLocString = _defaultLocStringId;

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

        public void SetLocalizedString(ILocalizedString newLocString)
        {
            CurrentLocString = newLocString;
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

            if (!ILocalizedString.IsValid(CurrentLocString))
                return;

            _text.text = CurrentLocString.Resolve();
        }
    }
}