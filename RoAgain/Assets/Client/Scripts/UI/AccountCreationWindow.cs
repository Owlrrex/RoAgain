using OwlLogging;
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
                ClientMain.Instance.DisplayOneButtonNotification("Please enter a username!", null);
                return;
            }

            if (string.IsNullOrWhiteSpace(_passwordInput.text))
            {
                ClientMain.Instance.DisplayOneButtonNotification("Please enter a password!", null);
                return;
            }

            if (_passwordInput.text != _passwordRepeatInput.text)
            {
                ClientMain.Instance.DisplayOneButtonNotification("You have to repeat you password correctly!", null);
                return;
            }

            ClientMain.Instance.CreateAccount(username, _passwordInput.text);
        }

        private void OnBackButtonClicked()
        {
            PreGameUI.Instance.ShowLoginWindow();
        }
    }
}