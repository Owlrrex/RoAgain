using OwlLogging;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class SkillTreeCategoryButton : MonoBehaviour
    {
        public SkillCategory Category = SkillCategory.FirstClass;
        public Action<SkillCategory> OnClick;

        private void Start()
        {
            Button button = GetComponentInChildren<Button>();
            if (button == null)
            {
                OwlLogger.LogError("Can't find button on GameObject!", GameComponent.UI);
                return;
            }

            button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            OnClick?.Invoke(Category);
        }
    }
}