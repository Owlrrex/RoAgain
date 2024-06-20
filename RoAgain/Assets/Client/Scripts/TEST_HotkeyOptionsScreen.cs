using OwlLogging;
using UnityEngine;

namespace Client
{
    public class TEST_HotkeyOptionsScreen : MonoBehaviour
    {
        [SerializeField]
        private HotkeyLineWidget _testHotkeyLine;

        // Start is called before the first frame update
        void Start()
        {
            if (OwlLogger.PrefabNullCheckAndLog(_testHotkeyLine, nameof(_testHotkeyLine), this, GameComponent.UI))
                return;

            _testHotkeyLine.Init(ConfigKey.Hotkey_ToggleStatWindow);
        }
    }
}