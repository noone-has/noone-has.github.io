using UnityEngine;
using UnityEngine.InputSystem;

public class GyroControl
{
    private float turnThreshold;
    private bool initialised = false;

    public bool Initialised => initialised;

    public Accelerometer accelerometer;
    public Accelerometer Accelerometer => accelerometer;

    public float SidewaysLean => GetSidewaysLean();
    public float ForwardsLean => GetForwardsLean();

    public GyroControl(float turnThreshold)
    {
        this.turnThreshold = turnThreshold;
    }
    public GyroControl()
    {
        this.turnThreshold = 0;
    }


    public bool EnableGyro()
    {
        Logger.LogInfo($"Support accelerometer: {SystemInfo.supportsAccelerometer}", null);
        if (SystemInfo.supportsGyroscope && SystemInfo.supportsAccelerometer)
        {
            accelerometer = Accelerometer.current;
            InputSystem.EnableDevice(accelerometer);
            initialised = true;
            return true;
        }
        Logger.LogError("Gyroscope not enabled!!!", null);
        return false;
    }

    float GetSidewaysLean()
    {
        if(!initialised) return 0;

        float turnAngle = accelerometer.acceleration.ReadValue().x;
        if(turnAngle != 0) Logger.LogInfo("We are getting a value from the Accelerometer! :)", null);
        
        if(turnAngle < 0 - turnThreshold)
        {  //TURN RIGHT
            return turnAngle;
        }
        else if(turnAngle > 0 + turnThreshold)
        {
            return turnAngle;
        }
        return 0f;
    }

    float GetForwardsLean()
    {
        if(!initialised) return 0;

        float turnAngle = accelerometer.acceleration.ReadValue().y;
        if(turnAngle != 0) Logger.LogInfo("We are getting a value from the Accelerometer! :)", null);

        if(turnAngle < 0 - turnThreshold)
        {
            return turnAngle;
        }
        else if(turnAngle > 0 + turnThreshold)
        {
            return turnAngle;
        }
        return 0f;
    }
}