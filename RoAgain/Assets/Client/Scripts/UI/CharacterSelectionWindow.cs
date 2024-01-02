using OwlLogging;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class CharacterSelectionWindow : MonoBehaviour
    {
        [SerializeField]
        private GameObject _characterSelectWidgetPrefab;

        [SerializeField]
        private GameObject _characterGrid;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private Button _createCharacterButton;

        [SerializeField]
        private Button _loginButton;

        [SerializeField]
        private Button _deleteCharacterButton;

        [SerializeField]
        private TMP_Text _mapText;

        [SerializeField]
        private TMP_Text _jobText;

        [SerializeField]
        private TMP_Text _baseLvlText;

        [SerializeField]
        private TMP_Text _baseExpText;

        [SerializeField]
        private TMP_Text _hpText;

        [SerializeField]
        private TMP_Text _spText;

        [SerializeField]
        private TMP_Text _strText;

        [SerializeField]
        private TMP_Text _agiText;

        [SerializeField]
        private TMP_Text _vitText;

        [SerializeField]
        private TMP_Text _intText;

        [SerializeField]
        private TMP_Text _dexText;

        [SerializeField]
        private TMP_Text _lukText;

        private List<CharacterSelectionData> _charData;

        private List<CharacterSelectWidget> _createdCharWidgets = new();

        private int _selectedCharIndex = -1;

        private float _lastCharClick = 0.0f;
        private const float DOUBLE_CLICK_THRESHOLD = 0.5f;

        // Start is called before the first frame update
        void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_characterSelectWidgetPrefab, "characterSelectWidgetPrefab", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_characterGrid, "characterGrid", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_mapText, "mapText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_jobText, "jobText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_baseLvlText, "baseLvlText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_baseExpText, "baseExpText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_hpText, "hpText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_spText, "spText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_strText, "strText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_agiText, "agiText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_vitText, "vitText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_intText, "intText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_dexText, "dexText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_lukText, "lukText", this, GameComponent.UI);
            if (!OwlLogger.PrefabNullCheckAndLog(_backButton, "backButton", this, GameComponent.UI))
                _backButton.onClick.AddListener(OnBackButtonClicked);
            if (!OwlLogger.PrefabNullCheckAndLog(_deleteCharacterButton, "deleteCharacterButton", this, GameComponent.UI))
                _deleteCharacterButton.onClick.AddListener(OnDeleteCharacterButtonClicked);
            if (!OwlLogger.PrefabNullCheckAndLog(_createCharacterButton, "createCharacterButton", this, GameComponent.UI))
                _createCharacterButton.onClick.AddListener(OnCreateCharacterButtonClicked);
            if (!OwlLogger.PrefabNullCheckAndLog(_loginButton, "loginButton", this, GameComponent.UI))
                _loginButton.onClick.AddListener(OnLoginButtonClicked);
        }

        public void Initialize(List<CharacterSelectionData> charData)
        {
            if (charData == null)
            {
                OwlLogger.LogError("Can't initialize CharacterSelectionWindow with null charData!", GameComponent.UI);
                return;
            }

            _charData = charData;

            ClearOldCharWidgets();
            CreateCharacterWidgets();
        }

        private void CreateCharacterWidgets()
        {
            foreach (CharacterSelectionData charData in _charData)
            {
                GameObject newWidget = Instantiate(_characterSelectWidgetPrefab, _characterGrid.transform);
                CharacterSelectWidget widgetComp = newWidget.GetComponent<CharacterSelectWidget>();
                if (widgetComp == null)
                {
                    OwlLogger.LogError("Can't find CharacterSelectWidget on characterSelectWidgetPrefab!", GameComponent.UI);
                    return;
                }

                widgetComp.OnWidgetClicked += OnWidgetClicked;
                widgetComp.SetCharacterData(charData);
                _createdCharWidgets.Add(widgetComp);
            }
        }

        public void ClearOldCharWidgets()
        {
            foreach (CharacterSelectWidget oldWidget in _createdCharWidgets)
            {
                oldWidget.OnWidgetClicked -= OnWidgetClicked;
                Destroy(oldWidget.gameObject);
            }
            _createdCharWidgets.Clear();
        }

        private void OnWidgetClicked(CharacterSelectWidget widget)
        {
            if (_selectedCharIndex != -1)
            {
                if (_createdCharWidgets[_selectedCharIndex] == widget)
                {
                    if (_lastCharClick + DOUBLE_CLICK_THRESHOLD >= Time.time)
                    {
                        OnLoginButtonClicked();
                        return;
                    }
                }

                Deselect();
            }

            int index = _createdCharWidgets.IndexOf(widget);
            SelectCharacter(index);
            _lastCharClick = Time.time;
        }

        private void SelectCharacter(int index)
        {
            if (index < 0 || index >= _createdCharWidgets.Count)
            {
                OwlLogger.LogWarning("CharacterSelectionWindow is deselecting - invalid character index!", GameComponent.UI);
                Deselect();
                return;
            }

            _selectedCharIndex = index;
            _createdCharWidgets[index].SetSelected(true);

            CharacterSelectionData data = _charData[index];
            _mapText.text = data.MapId; // TODO: Translate this to proper mapname
            _jobText.text = data.JobId.ToString(); // TODO: translate to proper jobname
            _baseLvlText.text = data.BaseLevel.ToString();
            _baseExpText.text = data.BaseExp.ToString();
            _hpText.text = data.Hp.ToString();
            _spText.text = data.Sp.ToString();
            _strText.text = data.Str.ToString();
            _agiText.text = data.Agi.ToString();
            _vitText.text = data.Vit.ToString();
            _intText.text = data.Int.ToString();
            _dexText.text = data.Dex.ToString();
            _lukText.text = data.Luk.ToString();
        }

        private void Deselect()
        {
            if (_selectedCharIndex == -1)
                return;

            _createdCharWidgets[_selectedCharIndex].SetSelected(false);
            _selectedCharIndex = -1;

            _mapText.text = "";
            _jobText.text = "";
            _baseLvlText.text = "";
            _baseExpText.text = "";
            _hpText.text = "";
            _spText.text = "";
            _strText.text = "";
            _agiText.text = "";
            _vitText.text = "";
            _intText.text = "";
            _dexText.text = "";
            _lukText.text = "";
        }

        private void OnBackButtonClicked()
        {
            PreGameUI.Instance.DeleteCurrentWindow();
            ClientMain.Instance.Disconnect();
        }

        private void OnCreateCharacterButtonClicked()
        {
            PreGameUI.Instance.ShowCharacterCreationWindow();
        }

        private void OnDeleteCharacterButtonClicked()
        {
            // TODO: Show Character-Deletion-Dialog
        }

        private void OnLoginButtonClicked()
        {
            if (_selectedCharIndex == -1)
            {
                ClientMain.Instance.DisplayOneButtonNotification("Please select a character first!", null);
                return;
            }

            CharacterSelectionData data = _charData[_selectedCharIndex];
            ClientMain.Instance.StartCharacterLogin(data.CharacterId);
        }
    }
}