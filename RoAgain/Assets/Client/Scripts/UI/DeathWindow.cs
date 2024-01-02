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
            if (ClientMain.Instance?.CurrentCharacterData?.IsDead() != true)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        private void OnBackToSaveButtonClicked()
        {
            ClientMain.Instance?.RequestReturnToSave();
        }
    }
}