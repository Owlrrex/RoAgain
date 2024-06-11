using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
            LocalizedStringTable.SetClientLanguage((ClientLanguage)(newValue+1)); // +1 to skip the "Unknown" entry
        }
    }
}