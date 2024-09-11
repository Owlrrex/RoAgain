using Shared;
using System;
using UnityEngine;

namespace Client
{
    [Serializable]
    public class SkillClientData
    {
        public Sprite Sprite;
        public LocalizedStringId NameId;
        public LocalizedStringId Description;
    }

    [CreateAssetMenu(fileName = "SkillClientDataTable", menuName = "ScriptableObjects/SkillClientData")]
    public class SkillClientDataTable : GenericTable<SkillId, SkillClientData, SkillClientDataTable> { }
}
