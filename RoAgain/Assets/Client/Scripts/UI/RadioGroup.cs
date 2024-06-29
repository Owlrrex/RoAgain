using OwlLogging;
using System;
using UnityEngine;

public class RadioGroup<T> : MonoBehaviour
{
    public Action<RadioButton<T>> SelectionChanged;

    [SerializeField]
    private Transform _container;

    public RadioButton<T> CurrentRadioButton { get; private set; }

    protected void Start()
    {
        SetupRadioButtons();
    }

    private void SetupRadioButtons()
    {
        int childCount = _container.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var radioButton = _container.GetChild(i).GetComponentInChildren<RadioButton<T>>();
            if (radioButton == null)
                continue;

            OwlLogger.Log($"RadioGroup discovered RadioButton for value {radioButton.Value}: {radioButton.gameObject.name}", GameComponent.UI, LogSeverity.VeryVerbose);

            if (CurrentRadioButton == null)
            {
                OnRadioButtonClicked(radioButton);
            }

            radioButton.OnClick += OnRadioButtonClicked;
        }
    }

    private void OnRadioButtonClicked(RadioButton<T> button)
    {
        if (button == CurrentRadioButton)
            return;

        if(CurrentRadioButton != null)
            CurrentRadioButton.Button.interactable = true;
        CurrentRadioButton = button;

        if(button != null)
            button.Button.interactable = false;

        SelectionChanged?.Invoke(button);
    }
}
