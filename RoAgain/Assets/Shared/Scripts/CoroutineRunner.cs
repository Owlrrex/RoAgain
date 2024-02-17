using OwlLogging;
using System;
using System.Collections;
using UnityEngine;

public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;

    void OnEnable()
    {
        if (_instance != null)
        {
            OwlLogger.LogError("CoroutineRunner OnEnable found _instance non-empty - multiple Coroutine-Runners are not supported!", GameComponent.Other);
            Destroy(this);
            return;
        }

        if(_instance == this)
        {
            OwlLogger.LogError("CoroutineRunner double-registering!", GameComponent.Other);
            return;
        }

        _instance = this;
    }

    private void OnDisable()
    {
        if (_instance == null)
            throw new InvalidOperationException("CoroutineRunner OnDisable found _instance empty - this should not be possible!");

        if (_instance != this)
            return;

        StopAllCoroutines();
        _instance = null;
    }

    public static bool IsReady()
    {
        return _instance != null;
    }

    public static Coroutine StartNewCoroutine(IEnumerator function)
    {
        if (_instance == null)
            throw new InvalidOperationException("CoroutineRunner can't be used without _instance");

        return _instance.StartCoroutine(function);
    }
}
