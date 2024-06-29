using OwlLogging;

public class TestRadioGroupInstance : RadioGroup<int>
{
    private new void Start()
    {
        base.Start();

        SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(RadioButton<int> button)
    {
        OwlLogger.Log($"Selection changed to Value {button.Value}", GameComponent.UI);
    }
}
