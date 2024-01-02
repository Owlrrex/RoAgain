using OwlLogging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class CharacterWindow : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _characterNameText;

        [SerializeField]
        private TMP_Text _characterJobText;

        [SerializeField]
        private TMP_Text _hpText;

        [SerializeField]
        private TMP_Text _hpPercentText;

        [SerializeField]
        private Slider _hpSlider;

        [SerializeField]
        private TMP_Text _spText;

        [SerializeField]
        private TMP_Text _spPercentText;

        [SerializeField]
        private Slider _spSlider;

        [SerializeField]
        private TMP_Text _baseLevelText;

        [SerializeField]
        private TMP_Text _jobLevelText;

        [SerializeField]
        private Slider _baseExpSlider;

        [SerializeField]
        private Slider _jobExpSlider;

        [SerializeField]
        private TMP_Text _weightText;

        [SerializeField]
        private TMP_Text _zenyText;

        private LocalCharacterEntity _character;

        void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_characterNameText, "characterNameText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_characterJobText, "characterJobText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_hpText, "hpText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_hpPercentText, "hpPercentText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_hpSlider, "hpSlider", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_spText, "spText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_spPercentText, "spPercentText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_spSlider, "spSlider", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_baseLevelText, "baseLevelText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_jobLevelText, "jobLevelText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_baseExpSlider, "baseExpSlider", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_jobExpSlider, "jobExpSlider", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_weightText, "weightText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_zenyText, "zenyText", this, GameComponent.UI);
        }

        // Update is called once per frame
        void Update()
        {
            if (_character == null)
                return;

            // TODO: make this listen to events or use a dirty-state, once they've been added
            UpdateCommonDisplays();
        }

        public void Initialize(LocalCharacterEntity character)
        {
            if (character == null)
            {
                OwlLogger.LogError("Can't initialize CharacterWindow with null character!", GameComponent.UI);
                return;
            }

            _character = character;

            UpdateCharacterDisplay();
            UpdateCommonDisplays();
        }

        public void UpdateCommonDisplays()
        {
            UpdateHpDisplay();
            UpdateSpDisplay();
            UpdateExpDisplay();
            UpdateWeightDisplay();
            UpdateZenyDisplay();
            UpdateBaseLvlDisplay();
            UpdateJobLvlDisplay();
        }

        public void UpdateHpDisplay()
        {
            _hpText.text = $"{_character.CurrentHp} / {_character.MaxHp.Total}";
            float fraction = 0;
            if (_character.MaxHp.Total != 0)
                fraction = _character.CurrentHp / (float)_character.MaxHp.Total;
            _hpSlider.value = fraction;
            _spPercentText.text = $"{(int)(fraction * 100)}%";
        }

        public void UpdateSpDisplay()
        {
            _spText.text = $"{_character.CurrentSp} / {_character.MaxSp.Total}";
            float fraction = 0;
            if (_character.MaxSp.Total != 0)
                fraction = _character.CurrentSp / (float)_character.MaxSp.Total;
            _spSlider.value = fraction;
            _spPercentText.text = $"{(int)(fraction * 100)}%";
        }

        public void UpdateExpDisplay()
        {
            // If CurrentExp resets to 0 upon levelup:
            _baseExpSlider.value = _character.RequiredBaseExp != 0 ? _character.CurrentBaseExp / (float)_character.RequiredBaseExp : 0;
            _jobExpSlider.value = _character.RequiredJobExp != 0 ? _character.CurrentJobExp / (float)_character.RequiredJobExp : 0;
            // with CurrentExp staying continuous: Would require the client knowing at what exp values a levelup happened
            // TODO: Tooltip
        }

        public void UpdateBaseLvlDisplay()
        {
            _baseLevelText.text = _character.BaseLvl.ToString();
        }

        public void UpdateJobLvlDisplay()
        {
            _jobLevelText.text = _character.JobLvl.ToString();
        }

        public void UpdateWeightDisplay()
        {
            // TODO: Read weight properly
            _weightText.text = $"451 / {_character.Weightlimit.Total}";
        }

        public void UpdateZenyDisplay()
        {
            //_zenyText.text = _character.Inventory.Zeny.ToString();
        }

        public void UpdateCharacterDisplay()
        {
            // TODO: Fetch localized names of class
            _characterNameText.text = _character.Name;
            _characterJobText.text = _character.JobId.ToString();
        }
    }
}