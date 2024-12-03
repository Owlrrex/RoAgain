using Shared;
using TMPro;

namespace Client
{
    public class LanguagePickerDropdown : TMP_Dropdown
    {
        // Start is called before the first frame update
        protected override void Awake()
        {
            base.Awake();

            onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(int newValue)
        {
            ILocalizedStringTable.Instance.SetClientLanguage((LocalizationLanguage)(newValue+1)); // +1 to skip the "Unknown" entry
        }
    }
}