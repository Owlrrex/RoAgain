using OwlLogging;
using Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class BattleEntityModelMain : MonoBehaviour
    {
        [SerializeField]
        protected Slider _hpSlider;
        [SerializeField]
        protected Slider _spSlider;

        // TODO: This will need to be moved to a general GridEntityModel eventually, since it displays chat
        // as well and NPCs can also talk
        [SerializeField]
        protected Text _skillNameText;

        [SerializeField]
        protected Slider _castTimeSlider;

        [SerializeField]
        protected Text _entityNameText;

        [SerializeField]
        protected DamageNumberEmitter _damageNumberEmitter;

        [SerializeField]
        protected Image _skillTargetIndicator;
        private SkillId _currentIndicatorSkillId;

        protected ClientBattleEntity _entity;

        private TimerFloat _skilltextTimer = new();

        // tmp for scaling
        [SerializeField]
        private GameObject _model;

        public int Initialize(ClientBattleEntity entity)
        {
            if (entity == null)
            {
                OwlLogger.LogError("Can't initialize BattleEntityModel with null entity", GameComponent.UI);
                return -1;
            }

            if (_entity != null)
            {
                _entity.TookDamage -= OnTookDamage;
                // TODO: Unset from previous entity, to support pooling of displays
            }

            _entity = entity;
            _entity.TookDamage += OnTookDamage;

            UpdateDisplay();

            if(_entity is not LocalCharacterEntity)
            {
                float scaleFactor = _entity.MaxHp.Total / 90.0f;
                _model.transform.localScale = new(transform.localScale.x * scaleFactor, transform.localScale.y, transform.localScale.z * scaleFactor);
            }

            return 0;
        }

        public void Shutdown()
        {
            if (_entity != null)
            {
                _entity.TookDamage -= OnTookDamage;
                _entity = null;
            }
        }

        private void OnTookDamage(BattleEntity entity, int damage, bool isSpDamage)
        {
            // TODO: Cast to ClientBattleEntity here?
            if (_entity != entity)
            {
                OwlLogger.LogError($"BattleEntityModelMain received TookDamage for wrong entity! registered entity {_entity.Id}, received entity {entity.Id}", GameComponent.UI);
                return;
            }

            if (_damageNumberEmitter != null)
            {
                _damageNumberEmitter.DisplayDamageNumber(damage);
            }
        }

        // TODO: Split this further into how often each field updates, detect writes, or other improvements
        protected void UpdateDisplay()
        {
            if (_hpSlider != null)
            {
                // TODO: These only need to be set when the data is updated
                _hpSlider.maxValue = _entity.MaxHp.Total;

                _hpSlider.value = _entity.CurrentHp;

            }

            if (_spSlider != null)
            {
                // TODO: These only need to be set when the data is updated
                _spSlider.maxValue = _entity.MaxSp.Total;

                _spSlider.value = _entity.CurrentSp;
            }

            // TODO: make this whole block event-driven, we don't need UI-updates every frame for SetActive() & text-calls
            if (_entity.CurrentlyExecutingSkills.Count > 0)
            {
                ASkillExecution skillToShowName = _entity.CurrentlyExecutingSkills[0];
                if (_castTimeSlider != null)
                {
                    bool anyCast = false;
                    foreach (var skill in _entity.CurrentlyExecutingSkills)
                    {
                        if (skill.CastTime.MaxValue > 0)
                        {
                            _castTimeSlider.gameObject.SetActive(true);
                            _castTimeSlider.value = 1 - (skill.CastTime.RemainingValue / skill.CastTime.MaxValue);
                            skillToShowName = skill;
                            anyCast = true;
                            break;
                        }
                    }
                    if (!anyCast)
                    {
                        _castTimeSlider.gameObject.SetActive(false);
                    }
                }

                if (_skillNameText != null)
                {
                    if(skillToShowName.SkillId != SkillId.AutoAttack)
                    {
                        string skillname = SkillClientDataTable.GetDataForId(skillToShowName.SkillId).Name;
                        SetSkilltext(skillname + "!", 0);
                    }
                }
            }
            else
            {
                // Somewhat hacky way to detect whether or not the skill message is still up:
                // The skill message is (for now) the only message with time = 0
                if (_skilltextTimer.MaxValue == 0)
                {
                    ClearSkilltext();
                }
                _castTimeSlider.gameObject.SetActive(false);
            }

            if (_entityNameText != null)
            {
                _entityNameText.text = _entity.Name;
            }
        }

        // Start is called before the first frame update
        protected void Start()
        {
            if (_hpSlider != null)
            {
                _hpSlider.minValue = 0;
            }
            else
            {
                OwlLogger.LogWarning($"BattleEntityModelMain has no HP Display!", GameComponent.UI);
            }

            if (_spSlider != null)
            {
                _spSlider.minValue = 0;
            }
            else
            {
                OwlLogger.LogWarning($"BattleEntityModelMain has no SP Display!", GameComponent.UI);
            }

            if (_castTimeSlider != null)
            {
                _castTimeSlider.minValue = 0;
            }
            else
            {
                OwlLogger.LogWarning($"BattleEntityModelMain has no castTimeSlider!", GameComponent.UI);
            }

            if (_skillNameText == null)
            {
                OwlLogger.LogWarning($"BattleEntityModelMain has no skillNameText!", GameComponent.UI);
            }
            else
            {
                _skillNameText.canvas.worldCamera = PlayerMain.Instance.UiCamera;
            }

            if (_entityNameText == null)
            {
                OwlLogger.LogWarning($"BattleEntityModelMain has no unitNameText!", GameComponent.UI);
            }
            else
            {
                _entityNameText.canvas.worldCamera = PlayerMain.Instance.UiCamera;
            }

            if (_damageNumberEmitter == null)
            {
                OwlLogger.LogWarning($"BattleEntityModelMain has no DamageNumberEmitter!", GameComponent.UI);
            }
        }

        // Update is called once per frame
        protected void Update()
        {
            if (_entity == null)
                return;

            UpdateDisplay();

            if (_skilltextTimer.MaxValue > 0)
            {
                _skilltextTimer.Update(Time.deltaTime);
                if (_skilltextTimer.IsFinished())
                    ClearSkilltext();
            }
        }

        public void SetSkilltext(string text, float time)
        {
            _skillNameText.text = text;
            _skillNameText.gameObject.SetActive(true);

            if(time > 0)
            {
                _skilltextTimer.Initialize(time);
            }
            else
            {
                _skilltextTimer.Initialize(0);
            }
        }

        public void ClearSkilltext()
        {
            _skillNameText.gameObject.SetActive(false);
            _skillNameText.text = string.Empty;
            _skilltextTimer.Initialize(0);
        }

        public void DisplaySkillTargetIndicator(SkillId skillId)
        {
            if (_skillTargetIndicator == null)
                return;

            if (skillId == _currentIndicatorSkillId)
                return;
            
            _currentIndicatorSkillId = skillId;

            if(skillId == SkillId.Unknown)
            {
                _skillTargetIndicator.gameObject.SetActive(false);
                return;
            }

            Sprite skillSprite = SkillClientDataTable.GetDataForId(skillId).Sprite;
            _skillTargetIndicator.sprite = skillSprite;
            _skillTargetIndicator.gameObject.SetActive(true);
        }
    }
}

