using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyInternetComponent : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        if (ADummyInternet.Instance != null)
        {
            OwlLogger.LogError("Tried to Multiple DummyInternet instances - aborting!", GameComponent.Network);
            return;
        }

        DummyInternet dummyInternet = new();
        dummyInternet.Initialize();
    }
}
