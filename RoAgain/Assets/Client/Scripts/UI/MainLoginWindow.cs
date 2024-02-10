using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class MainLoginWindow : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField _usernameInput;

        [SerializeField]
        private TMP_InputField _passwordInput;

        [SerializeField]
        private Button _loginButton;

        [SerializeField]
        private Button _quitButton;

        [SerializeField]
        private Button _createAcctButton;

        // Start is called before the first frame update
        void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_usernameInput, "usernameInput", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_passwordInput, "passwordInput", this, GameComponent.UI);
            if(!OwlLogger.PrefabNullCheckAndLog(_loginButton, "loginButton", this, GameComponent.UI))
                _loginButton.onClick.AddListener(OnLoginButtonClicked);
            if(!OwlLogger.PrefabNullCheckAndLog(_quitButton, "quitButton", this, GameComponent.UI))
                _quitButton.onClick.AddListener(OnQuitButtonClicked);
            if(!OwlLogger.PrefabNullCheckAndLog(_createAcctButton, "createAcctButton", this, GameComponent.UI))
                _createAcctButton.onClick.AddListener(OnCreateAcctButtonClicked);
        }

        private void OnLoginButtonClicked()
        {
            string username = _usernameInput.text;
            string password = _passwordInput.text;
            if(string.IsNullOrWhiteSpace(username)
                || string.IsNullOrWhiteSpace(password))
            {
                ClientMain.Instance.DisplayOneButtonNotification("Please enter Username & Password!", null);
                return;
            }

            PreGameUI.Instance.StartAccountLogin(username, password);
        }

        private void OnQuitButtonClicked()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }
#endif
            Application.Quit(0);
        }

        private void OnCreateAcctButtonClicked()
        {
            PreGameUI.Instance.ShowAccountCreationWindow();
        }
    }
}

