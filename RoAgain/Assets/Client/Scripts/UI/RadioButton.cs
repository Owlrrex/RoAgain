using OwlLogging;
using System;
using UnityEngine;
using UnityEngine.UI;

public class RadioButton<T> : MonoBehaviour
{
    public T Value;
    public Action<RadioButton<T>> OnClick;
    public Button Button { get; private set; }

    private void Awake()
    {
        Button = GetComponentInChildren<Button>();
        if (Button == null)
        {
            OwlLogger.LogError("Can't find button on GameObject!", GameComponent.UI);
            return;
        }

        Button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        OnClick?.Invoke(this);
    }
}
