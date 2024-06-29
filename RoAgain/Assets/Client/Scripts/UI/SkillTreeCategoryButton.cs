using OwlLogging;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class SkillTreeCategoryButton : MonoBehaviour
    {
        public SkillCategory Category = SkillCategory.FirstClass;
        public Action<SkillTreeCategoryButton> OnClick;
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
}