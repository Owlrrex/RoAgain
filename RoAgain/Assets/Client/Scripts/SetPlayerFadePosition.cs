using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetPlayerFadePosition : MonoBehaviour
{
    [SerializeField]
    private Transform _fadepoint;

    // Start is called before the first frame update
    void Start()
    {
        if(_fadepoint == null)
        {
            OwlLogger.LogWarning($"Autofilling fadepoint for SetPlayerFadePoint component on {gameObject.name}", GameComponent.Other);
            _fadepoint = transform;
        }
    }

    public void SetPosition()
    {
        Shader.SetGlobalVector("_WorldFadePos", _fadepoint.position);
    }

    // Update is called once per frame
    void Update()
    {
        SetPosition();
    }
}
