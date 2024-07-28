using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OwlLogging;
using System.Linq;
using Shared;

namespace Client
{
    public class CharacterCreationWindow : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField _charNameInput;

        [SerializeField]
        private ToggleGroup _genderSelectGroup;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private Button _createButton;

        [SerializeField]
        private LocalizedStringId _enterCharNameLocId;
        [SerializeField]
        private LocalizedStringId _enterGenderLocId;

        private void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_charNameInput, "charNameInput", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_genderSelectGroup, "genderSelectGroup", this, GameComponent.UI);
            if (!OwlLogger.PrefabNullCheckAndLog(_backButton, "backButton", this, GameComponent.UI))
                _backButton.onClick.AddListener(OnBackButtonClicked);
            if (!OwlLogger.PrefabNullCheckAndLog(_createButton, "createButton", this, GameComponent.UI))
                _createButton.onClick.AddListener(OnCreateButtonClicked);
        }

        private void OnBackButtonClicked()
        {
            PreGameUI.Instance.ShowCharacterSelection();
        }

        private void OnCreateButtonClicked()
        {
            string charname = _charNameInput.text.Trim();

            if (string.IsNullOrWhiteSpace(charname))
            {
                ClientMain.Instance.DisplayOneButtonNotification(_enterCharNameLocId, null);
                return;
            }

            var selectedToggles = _genderSelectGroup.ActiveToggles();
            if (selectedToggles.Count() != 1)
            {
                ClientMain.Instance.DisplayOneButtonNotification(_enterGenderLocId, null);
                return;
            }

            Toggle toggle = _genderSelectGroup.GetFirstActiveToggle();
            int gender = toggle.transform.GetSiblingIndex();

            PreGameUI.Instance.CreateCharacter(charname, gender);
        }
    }
}