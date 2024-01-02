using OwlLogging;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class PreGameUI : MonoBehaviour
    {
        public static PreGameUI Instance;

        [SerializeField]
        private GameObject _loginWindowPrefab;

        [SerializeField]
        private GameObject _accountCreationWindowPrefab;

        [SerializeField]
        private GameObject _characterSelectionWindowPrefab;

        [SerializeField]
        private GameObject _characterCreationWindowPrefab;

        private GameObject _currentWindow;

        // Start is called before the first frame update
        void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_accountCreationWindowPrefab, "accountCreationWindowPrefab", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_loginWindowPrefab, "loginWindowPrefab", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_characterCreationWindowPrefab, "characterCreationWindowPrefab", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_characterSelectionWindowPrefab, "characterSelectionWindowPrefab", this, GameComponent.UI);

            if (Instance != null)
            {
                OwlLogger.LogError("PreGameUI instance alread set - duplicate gameobject!", GameComponent.UI);
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public void ShowLoginWindow()
        {
            DeleteCurrentWindow();
            _currentWindow = Instantiate(_loginWindowPrefab, ClientMain.Instance.MainUiCanvas.transform);
        }

        public void ShowAccountCreationWindow()
        {
            DeleteCurrentWindow();
            _currentWindow = Instantiate(_accountCreationWindowPrefab, ClientMain.Instance.MainUiCanvas.transform);
        }

        public void ShowCharacterSelectionWindow(List<CharacterSelectionData> characterSelectionData)
        {
            DeleteCurrentWindow();
            _currentWindow = Instantiate(_characterSelectionWindowPrefab, ClientMain.Instance.MainUiCanvas.transform);
            CharacterSelectionWindow charSelComp = _currentWindow.GetComponent<CharacterSelectionWindow>();
            if (charSelComp == null)
            {
                OwlLogger.LogError("Can't find CharacterSelectionWindow component on CharacterSelectionWindowPrefab!", GameComponent.UI);
                return;
            }
            charSelComp.Initialize(characterSelectionData);
        }

        public void ShowCharacterCreationWindow()
        {
            DeleteCurrentWindow();
            _currentWindow = Instantiate(_characterCreationWindowPrefab, ClientMain.Instance.MainUiCanvas.transform);
        }

        public void DeleteCurrentWindow()
        {
            if (_currentWindow == null)
                return;

            Destroy(_currentWindow);
            _currentWindow = null;
        }
    }
}