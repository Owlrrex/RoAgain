using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class CursorModifierComponent : MonoBehaviour
    {
        public CursorChanger.HoverTargetType TargetType = CursorChanger.HoverTargetType.Unknown;

        // Start is called before the first frame update
        void Start()
        {
            if (TargetType == CursorChanger.HoverTargetType.Unknown
                || TargetType == CursorChanger.HoverTargetType.Normal)
            {
                OwlLogger.LogError($"Gameobject {gameObject} has improper TargetType!", GameComponent.Other);
            }
        }
    }
}