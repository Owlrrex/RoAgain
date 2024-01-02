using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class DamageNumberDisplay : MonoBehaviour
    {
        [SerializeField]
        private Text _text;

        [SerializeField]
        private Rigidbody _rb;

        [SerializeField]
        private Canvas _canvas;

        public void Initialize(string text)
        {
            _text.text = text;
            _rb.velocity = new(2, 5, 0);
            _canvas.worldCamera = PlayerMain.Instance.UiCamera;
        }
    }
}