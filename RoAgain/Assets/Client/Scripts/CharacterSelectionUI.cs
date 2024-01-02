using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Client
{
    public class CharacterSelectionUI : MonoBehaviour
    {
        public Vector4 CharButtonPlacement;
        public Vector4 CharInfoPlacement;
        private List<CharacterSelectionData> _charData;
        private int _selectedIndex = -1;
        private readonly StringBuilder _builder = new();

        public void Start()
        {
            if (ClientMain.Instance != null)
            {
                if (ClientMain.Instance.CharacterSelection != null)
                {
                    OwlLogger.LogError("Duplicate CharacterSelection instance!", GameComponent.UI);
                    Destroy(this);
                }
                else
                {
                    ClientMain.Instance.CharacterSelection = this;
                }
            }
        }

        public void OnDestroy()
        {
            if (ClientMain.Instance != null
                && ClientMain.Instance.MainMenu == this)
            {
                ClientMain.Instance.MainMenu = null;
            }
        }

        public void SetCharacterList(List<CharacterSelectionData> charData)
        {
            _charData = charData;
            DisplayCharacterList();
        }

        private void DisplayCharacterList()
        {
            _selectedIndex = -1;
        }

        private void OnGUI()
        {
            if (_charData == null || _charData.Count == 0)
                return;

            Vector4 charButtonAnchor = CharButtonPlacement;
            for (int i = 0; i < _charData.Count; ++i)
            {
                CharacterSelectionData charData = _charData[i];
                if (i == _selectedIndex)
                {
                    if (GUI.Button(charButtonAnchor.ToRect(), "Login with " + charData.Name))
                    {
                        ClientMain.Instance.StartCharacterLogin(charData.CharacterId);
                    }
                }
                else
                {
                    if (GUI.Button(charButtonAnchor.ToRect(), charData.Name))
                    {
                        _selectedIndex = i;
                    }
                }

                charButtonAnchor.y += CharButtonPlacement.w + 5;
            }
            if (_selectedIndex >= 0)
            {
                CharacterSelectionData charData = _charData[_selectedIndex];
                _builder.Clear();

                _builder.Append("Name: ");
                _builder.Append(charData.Name);
                _builder.Append("\n");

                _builder.Append("Map: ");
                _builder.Append(charData.MapId);
                _builder.Append("\n");

                _builder.Append("Job: ");
                _builder.Append(charData.JobId);
                _builder.Append("\n");

                //_builder.Append("Base Level: ");
                //_builder.Append(charData.Stats.BaseLevel);
                //_builder.Append("\n");

                _builder.Append("Job Level: ");
                _builder.Append(charData.JobLevel);
                _builder.Append("\n");

                //_builder.Append("HP: ");
                //_builder.Append(charData.MaxHp.Total);
                //_builder.Append("\n");

                //_builder.Append("SP: ");
                //_builder.Append(charData.MaxSp.Total);
                //_builder.Append("\n");

                //_builder.Append("Strength: ");
                //_builder.Append(charData.Stats.Str.Base);
                //_builder.Append("\n");

                //_builder.Append("Agility: ");
                //_builder.Append(charData.Stats.Agi.Base);
                //_builder.Append("\n");

                //_builder.Append("Vitality: ");
                //_builder.Append(charData.Stats.Vit.Base);
                //_builder.Append("\n");

                //_builder.Append("Dexterity: ");
                //_builder.Append(charData.Stats.Dex.Base);
                //_builder.Append("\n");

                //_builder.Append("Intelligence: ");
                //_builder.Append(charData.Stats.Int.Base);
                //_builder.Append("\n");

                //_builder.Append("Luck: ");
                //_builder.Append(charData.Stats.Luk.Base);

                GUI.Label(CharInfoPlacement.ToRect(), _builder.ToString());
            }
        }
    }
}
