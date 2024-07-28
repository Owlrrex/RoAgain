using OwlLogging;
using Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class AccountCreationWindow : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField _usernameInput;

        [SerializeField]
        private TMP_InputField _passwordInput;

        [SerializeField]
        private TMP_InputField _passwordRepeatInput;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private Button _createButton;

        [SerializeField]
        private LocalizedStringId _enterUsernameLocId;
        [SerializeField]
        private LocalizedStringId _enterPasswordLocId;
        [SerializeField]
        private LocalizedStringId _repeatPasswordLocId;

        // Start is called before the first frame update
        void Start()
        {
            OwlLogger.PrefabNullCheckAndLog(_usernameInput, "usernameInput", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_passwordInput, "passwordInput", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_passwordRepeatInput, "passwordRepeatInput", this, GameComponent.UI);
            if (!OwlLogger.PrefabNullCheckAndLog(_backButton, "backButton", this, GameComponent.UI))
                _backButton.onClick.AddListener(OnBackButtonClicked);
            if (!OwlLogger.PrefabNullCheckAndLog(_createButton, "createButton", this, GameComponent.UI))
                _createButton.onClick.AddListener(OnCreateButtonClicked);
        }

        private void OnCreateButtonClicked()
        {
            string username = _usernameInput.text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                ClientMain.Instance.DisplayOneButtonNotification(_enterUsernameLocId, null);
                return;
            }

            if (string.IsNullOrWhiteSpace(_passwordInput.text))
            {
                ClientMain.Instance.DisplayOneButtonNotification(_enterPasswordLocId, null);
                return;
            }

            if (_passwordInput.text != _passwordRepeatInput.text)
            {
                ClientMain.Instance.DisplayOneButtonNotification(_repeatPasswordLocId, null);
                return;
            }

            PreGameUI.Instance.CreateAccount(username, _passwordInput.text);
        }

        private void OnBackButtonClicked()
        {
            PreGameUI.Instance.ShowAccountLoginWindow();
        }
    }
}