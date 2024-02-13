using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMenuWindow : MonoBehaviour
{
    [SerializeField]
    private Button _returnToGameButton;
    [SerializeField]
    private Button _optionsButton;
    [SerializeField]
    private Button _gameGuideButton;
    [SerializeField]
    private Button _charSelectButton;
    [SerializeField]
    private Button _quitButton;

    // Start is called before the first frame update
    void Start()
    {
        if (!OwlLogger.PrefabNullCheckAndLog(_returnToGameButton, "returnToGameButton", this, GameComponent.UI))
        {
            _returnToGameButton.onClick.AddListener(OnReturnToGameButtonClicked);
        }

        if (!OwlLogger.PrefabNullCheckAndLog(_optionsButton, "optionsButton", this, GameComponent.UI))
        {
            _optionsButton.onClick.AddListener(OnOptionsButtonClicked);
        }

        if (!OwlLogger.PrefabNullCheckAndLog(_gameGuideButton, "gameGuideButton", this, GameComponent.UI))
        {
            _gameGuideButton.onClick.AddListener(OnGameGuideButtonClicked);
        }

        if (!OwlLogger.PrefabNullCheckAndLog(_charSelectButton, "charSelectButton", this, GameComponent.UI))
        {
            _charSelectButton.onClick.AddListener(OnCharSelectButtonClicked);
        }

        if (!OwlLogger.PrefabNullCheckAndLog(_quitButton, "quitButton", this, GameComponent.UI))
        {
            _quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
    }

    private void OnReturnToGameButtonClicked()
    {

    }

    private void OnOptionsButtonClicked()
    {

    }

    private void OnGameGuideButtonClicked()
    {

    }

    private void OnCharSelectButtonClicked()
    {

    }

    private void OnQuitButtonClicked()
    {

    }
}
