using OwlLogging;

namespace Client
{
    public class TestRadioGroupInstance : RadioGroup<InventoryFilter>
    {
        private new void Start()
        {
            base.Start();

            SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(RadioButton<InventoryFilter> button)
        {
            OwlLogger.Log($"Selection changed to Value {button.Value}", GameComponent.UI);
        }
    }
}
