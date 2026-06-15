using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;
public static class CameraManager
{
    private static List<CinemachineCamera> cinemachineCameras = new();
    private static CinemachineCamera currentCamera;
    private static CinemachineCamera mainCamera;
    private static CinemachineBrain cinemachineBrain;

    public static CinemachineBrain CinemachineBrain => cinemachineBrain;
    public static CinemachineCamera CurrentCamera => currentCamera;

    public static void Register(CinemachineCamera camera)
    {
        cinemachineCameras.Add(camera);
    }

    public static void UnRegister(CinemachineCamera camera)
    {
        cinemachineCameras.Remove(camera);
    }

    public static void RegisterBrain(CinemachineBrain brain)
    {
        cinemachineBrain = brain;
    }

    public static void RegisterMainCamera(CinemachineCamera camera)
    {
        mainCamera = camera;
        Register(camera);
    }

    public static void SwitchCamera(CinemachineCamera targetCamera)
    {
        SwitchCamera(targetCamera, CinemachineBlendDefinition.Styles.EaseInOut);
    }

    public static void SwitchCamera(CinemachineCamera targetCamera, CinemachineBlendDefinition.Styles style)
    {
        if(targetCamera == null)
        {
            Logger.Log("The given camera is null!", null, LogLevel.Warning);
            return;
        }
        cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(style, 2);
        currentCamera = targetCamera;
        currentCamera.Priority = 9;

        foreach(CinemachineCamera camera in cinemachineCameras)
        {
            if(camera != currentCamera)
            {
                camera.Priority = 0;
            }
        }
    }

    public static void SwitchCameraToMain()
    {
        SwitchCamera(mainCamera);
    }
}
