using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    // TODO: Pool damagenumbers
    // Note: Disable gameobject first, then reparent into pool. Reparent to new hirarchy, then enable. This avoids some unnecessary UI updates
    public class DamageNumberDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _text;

        [SerializeField]
        private Rigidbody _rb;

        [SerializeField]
        private Canvas _canvas;

        [SerializeField]
        private float _critSizeMultiplier = 1.2f;

        // TODO: Remove temporary chain-display
        public void Initialize(int damage, bool isSpDamage, bool isCrit, bool isLocalChar, int chainCount)
        {
            if(isLocalChar)
            {
                if (isSpDamage)
                    _text.color = Color.magenta;
                else
                    _text.color = Color.red;
            }
            else
            {
                if (isSpDamage)
                    _text.color = Color.blue;
                else
                    _text.color = Color.white;
            }
            

            if (damage >= 0)
                _text.text = damage.ToString();
            else if (damage == -1)
                _text.text = "Miss";
            else if (damage == -2)
                _text.text = "PDodge";
            else
                _text.text = "Unknown";

            if (isCrit)
            {
                _text.text += "!";
                _text.fontSize *= _critSizeMultiplier;
            }

            if(chainCount > 0)
            {
                _text.text += "x" + chainCount.ToString();
            }

            _rb.velocity = new(2, 5, 0);
            _canvas.worldCamera = PlayerMain.Instance.UiCamera;
        }
    }
}