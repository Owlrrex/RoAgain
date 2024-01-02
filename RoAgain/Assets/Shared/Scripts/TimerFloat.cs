using OwlLogging;

public class TimerFloat
{
    public float MaxValue;
    public float RemainingValue;

    public TimerFloat() { }

    public TimerFloat(float value)
    {
        MaxValue = value;
        RemainingValue = value;
    }

    public void Initialize(float value)
    {
        MaxValue = value;
        RemainingValue = MaxValue;
    }

    public void Reset()
    {
        RemainingValue = MaxValue;
    }
    
    public void Update(float deltaTime)
    {
        if(RemainingValue > 0)
        {
            RemainingValue -= deltaTime;
            if (RemainingValue < 0)
                RemainingValue = 0;
        }
    }

    public bool IsFinished()
    {
        return RemainingValue <= 0;
    }
}
