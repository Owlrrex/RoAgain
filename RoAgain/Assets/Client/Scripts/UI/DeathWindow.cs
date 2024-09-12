using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class DeathWindow : MonoBehaviour
    {
        [SerializeField]
        private Button _backToSaveButton;

        // Start is called before the first frame update
        void Start()
        {
            if (!OwlLogger.PrefabNullCheckAndLog(_backToSaveButton, "backToSaveButton", this, GameComponent.UI))
            {
                _backToSaveButton.onClick.AddListener(OnBackToSaveButtonClicked);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (ClientMain.Instance != null
                && ClientMain.Instance.CurrentCharacterData != null
                && !ClientMain.Instance.CurrentCharacterData.IsDead())
            {
                gameObject.SetActive(false);
            }
        }

        private void OnBackToSaveButtonClicked()
        {
            if(ClientMain.Instance != null)
            {
                ClientMain.Instance.RequestReturnToSave();
            }
        }
    }
}