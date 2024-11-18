using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Shared;
using OwlLogging;
using UnityEngine.EventSystems;

namespace Client
{
    public class SkillTreeEntryWidget : MonoBehaviour
    {
        [SerializeField]
        private SkillIcon _skillIcon;

        [SerializeField]
        private Button _decreaseCurrentLevelButton;
        [SerializeField]
        private Button _increaseCurrentLevelButton;

        [SerializeField]
        private Image _highlightOverlay;

        [SerializeField]
        private TMP_Text _currentLevelText;
        [SerializeField]
        private TMP_Text _maxLevelText;

        [SerializeField]
        private TMP_Text _plannedSkillPointsText;

        [SerializeField]
        private TMP_Text _requirementSkillLevelText;

        [SerializeField]
        private TMP_Text _skillNameText;

        [SerializeField]
        private GameObject _contentContainer;

        [SerializeField]
        private Image _greyOutOverlay;

        [SerializeField]
        private GameObject _levelSelectAnchor;

        public Action<SkillTreeEntryWidget> Clicked;

        public SkillId SkillId { get; private set; } = SkillId.Unknown;

        public int CurrentLevel { get; private set; } = -1;
        public int MaxLevel { get; private set; } = -1;

        private bool _highlightOverride = false;

        private Draggable _iconDraggable;

        void Awake()
        {
            if (!OwlLogger.PrefabNullCheckAndLog(_skillIcon, "skillIcon", this, GameComponent.UI))
            {
                _iconDraggable = _skillIcon.GetComponent<Draggable>();
                OwlLogger.PrefabNullCheckAndLog(_iconDraggable, nameof(_iconDraggable), this, GameComponent.UI);
            }

            if(!OwlLogger.PrefabNullCheckAndLog(_decreaseCurrentLevelButton, "decreaseCurrentLevelButton", this, GameComponent.UI))
                _decreaseCurrentLevelButton.onClick.AddListener(OnDecreaseCurrentLevelClicked);
            if (!OwlLogger.PrefabNullCheckAndLog(_increaseCurrentLevelButton, "increaseCurrentLevelButton", this, GameComponent.UI))
                _increaseCurrentLevelButton.onClick.AddListener(OnIncreaseCurrentLevelClicked);

            OwlLogger.PrefabNullCheckAndLog(_highlightOverlay, "highlightOverlay", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_plannedSkillPointsText, "plannedSkillPointsText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_requirementSkillLevelText, "requirementSkillLevelText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_skillNameText, "skillNameText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_contentContainer, "contentContainer", this, GameComponent.UI);

            _skillIcon.Clicked += OnIconClicked;

            LocalizedStringTable.LanguageChanged += OnLanguageChanged;
        }

        public void SetCurrentLevel(int newCurrentLevel)
        {
            if (newCurrentLevel == CurrentLevel)
                return;

            if (newCurrentLevel > MaxLevel)
                return;

            if (newCurrentLevel < 0)
            {
                OwlLogger.LogError($"Can't set SkillLevel to {newCurrentLevel} in SkillTreeEntryWidget - negative level not allowed.", GameComponent.UI);
                return;
            }

            CurrentLevel = newCurrentLevel;
            _skillIcon.SetSkillData(_skillIcon.SkillId, CurrentLevel);
            _currentLevelText.text = CurrentLevel.ToString();
            UpdateButtonVisibility();
        }

        private void UpdateButtonVisibility()
        {
            _decreaseCurrentLevelButton.enabled = CurrentLevel > 1;
            _increaseCurrentLevelButton.enabled = CurrentLevel < MaxLevel;
        }

        public void SetMaxLevel(int newMaxLevel)
        {
            if (newMaxLevel == MaxLevel)
                return;

            if (newMaxLevel < 0)
            {
                OwlLogger.LogError($"SkillTreeEntryWidget: Can't set MaxLevel from {MaxLevel} to {newMaxLevel}.", GameComponent.UI);
                return;
            }

            if (CurrentLevel > newMaxLevel)
            {
                OwlLogger.LogWarning($"SkillTreeEntryWidget: Setting MaxLevel from {MaxLevel} to {newMaxLevel} reduces CurrentLevel from {CurrentLevel}.", GameComponent.UI);
                SetCurrentLevel(newMaxLevel);
            }

            MaxLevel = newMaxLevel;
            _maxLevelText.text = MaxLevel.ToString();

            SetDisplayGreyedOut(MaxLevel <= 0);
        }

        public void SetHighlight(bool highlighted)
        {
            SetHighlight(highlighted, Color.clear);
        }

        public void SetHighlight(bool highlighted, Color color)
        {
            _highlightOverride = highlighted;
            if (_highlightOverlay != null && color != Color.clear)
            {
                _highlightOverlay.color = color;
            }
            UpdateHighlight();
        }

        private void OnIncreaseCurrentLevelClicked()
        {
            SetCurrentLevel(Math.Min(CurrentLevel + 1, MaxLevel));
        }

        private void OnDecreaseCurrentLevelClicked()
        {
            SetCurrentLevel(Math.Max(CurrentLevel - 1, 1));
        }

        public void SetSkillId(SkillId id)
        {
            if (_skillIcon == null)
                return;

            SkillId = id;
            _skillIcon.SetSkillData(id, _skillIcon.SkillParam);
            SetSkillNameText();
        }

        private void SetSkillNameText()
        {
            SkillClientData data = SkillClientDataTable.GetDataForId(SkillId);
            if (data == null)
            {
                _skillNameText.text = "Unknown";
            }
            else
            {
                _skillNameText.text = LocalizedStringTable.GetStringById(data.NameId);
            }
        }

        public int SetPlannedSkillPoints(int skillPointNumber)
        {
            int returnValue = 0;
            if (skillPointNumber < 0)
            {
                OwlLogger.LogError($"Can't plan less than 0 skill points: {skillPointNumber}", GameComponent.UI);
                return -1;
            }

            if (skillPointNumber == 0)
            {
                _plannedSkillPointsText.gameObject.SetActive(false);
            }
            else
            {
                _plannedSkillPointsText.gameObject.SetActive(true);
                _plannedSkillPointsText.text = "+" + skillPointNumber.ToString();
            }

            return returnValue;
        }

        public int SetRequiredSkillLevel(int skillLevel)
        {
            int returnValue = 0;
            if (skillLevel < 0)
            {
                OwlLogger.LogError($"Can't set required SkillLevel to less than 0: {skillLevel}", GameComponent.UI);
                return -1;
            }

            if (skillLevel == 0)
            {
                _requirementSkillLevelText.gameObject.SetActive(false);
            }
            else
            {
                _requirementSkillLevelText.gameObject.SetActive(true);
                _requirementSkillLevelText.text = skillLevel.ToString();
            }

            return returnValue;
        }

        public void SetDisplayEmpty()
        {
            _contentContainer.SetActive(false);
        }

        public void SetDisplayVisible()
        {
            // This function has to be the inverse of SetDisplayEmpty
            _contentContainer.SetActive(true);
        }

        public void SetDisplayGreyedOut(bool newGreyedOut)
        {
            _greyOutOverlay.enabled = newGreyedOut;
            _levelSelectAnchor.SetActive(!newGreyedOut);
            _iconDraggable.AllowDrag = !newGreyedOut;
        }

        private void OnIconClicked(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Clicked?.Invoke(this);
            }
        }

        private void UpdateHighlight()
        {
            SetHighlightInternal(_highlightOverride);
        }

        private void SetHighlightInternal(bool highlighted)
        {
            if (_highlightOverlay != null)
            {
                _highlightOverlay.enabled = highlighted;
            }
        }

        private void OnLanguageChanged()
        {
            SetSkillNameText();
        }

        private void OnDestroy()
        {
            LocalizedStringTable.LanguageChanged -= OnLanguageChanged;
        }
    }
}