using System;
using OwlLogging;


namespace Shared
{
    public class TimingScheduler
    {
        public static TimingScheduler Instance { get; private set; }

        public Action<float> UpdateMax;
        public Action Update1Hz;
        private float _timer1Hz;

        public void Init()
        {
            if (Instance != null)
            {
                if (Instance == this)
                {
                    OwlLogger.LogWarning("TimingScheduler is re-registered!", GameComponent.Other);
                    return;
                }
                else
                {
                    OwlLogger.LogError("Tried to register a TimingScheduler when another is already initialized!", GameComponent.Other);
                    return;
                }
            }

            Instance = this;
        }

        public void Update(float deltaTime)
        {
            UpdateMax?.Invoke(deltaTime);

            _timer1Hz += deltaTime;
            if (_timer1Hz > 1.0f)
            {
                Update1Hz?.Invoke();
                _timer1Hz -= 1.0f;
                // if this was a while-loop, it could fire multiple times per frame. 
                // Not sure if that's useful, so for now, we limit it to once per frame, that should be enough
                // to catch up on any "hiccups" that happened in calling Update()
            }
        }
    }
}