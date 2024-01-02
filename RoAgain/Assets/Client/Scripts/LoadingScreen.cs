using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class LoadingScreen : MonoBehaviour
    {
        public Vector4 LoadingMessagePlacement;
        [HideInInspector]
        public string LoadingMessage;

        void OnGUI()
        {
            GUI.Label(LoadingMessagePlacement.ToRect(), LoadingMessage);
        }
    }
}
