using OwlLogging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    [Serializable]
    public class  ItemIconData
    {
        public Sprite Sprite;
    }

    [CreateAssetMenu(fileName = "ItemIconTable", menuName = "ScriptableObjects/ItemIconTable")]
    public class ItemIconTable : GenericTable<int, ItemIconData, ItemIconTable> { }
}

