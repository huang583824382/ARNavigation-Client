using System.Collections.Generic;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
// using UnityEngine.Experimental.XR;

public class ImageAccesser : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The ARCameraManager which will produce frame events.")]
    ARCameraManager cameraManager;
    List<string> m_ConfigurationNames;
    bool sendImgFlag = true;
    bool cameraInitFinish = false;
    XRCameraConfiguration currentConfig;
    Network network;
    PoseManager poseManager;
    StateController stateController;

    void Start()
    {
        network = gameObject.GetComponent<Network>();
        poseManager = gameObject.GetComponent<PoseManager>();
        stateController = FindObjectOfType<StateController>();
        SetCameraConfig();
        // lastLocTime = DateTime.Now;
    }

    void Awake()
    {
        m_ConfigurationNames = new List<string>();
    }

    void Update()
    {
        if (m_ConfigurationNames.Count == 0)
            SetCameraConfig();
    }

    [Obsolete]
    void OnEnable()
    {
        Debug.Log("ImageAccesser start");
        // cameraManager.frameReceived += OnCameraFrameReceived;
    }

    [Obsolete]
    void OnDisable()
    {
        // cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    public float[] GetCameraIntrinsic()
    {
        while(!Application.isEditor && !cameraInitFinish){
            Debug.Log($"wait for camera init {Application.isEditor}");
            // 休眠1秒
            // System.Threading.Thread.Sleep(1000);
        }
        float[] res = new float[] { 0, 0, 0, 0 };
        if (cameraManager != null && !Application.isEditor){
            XRCameraIntrinsics intrinsics;
            while(!cameraManager.TryGetIntrinsics(out intrinsics));
            float fx = intrinsics.focalLength.x;
            float fy = intrinsics.focalLength.y;
            float cx = intrinsics.principalPoint.x;
            float cy = intrinsics.principalPoint.y;
            Vector2 resolution = intrinsics.resolution;
            // 如果resolution中的分辨率和config中的不一样，重新获取内参
            if (resolution.x != currentConfig.width || resolution.y != currentConfig.height){
                Debug.Log("resolution not match, get again");
                SetCameraConfig();
                return new float[]{0, 0, 0, 0};
            }
            Debug.Log("fx: " + fx + ", fy: " + fy + ", cx: " + cx + ", cy: " + cy + ", resolution: " + resolution);
            res = new float[] { (fx + fy) / 2, cy, cx, 0f };
            return res;
        }
        else{
            res = new float[] { 968.8154f, 360.4154f, 647.3516f, 0};
            return res;
        }
    }

    void SetCameraConfig()
    {
        using (var configurations = cameraManager.GetConfigurations(Allocator.Temp))
        {
            //Debug.Log($"SetCameraConfig has {configurations.Length} configs");
            if (!configurations.IsCreated || (configurations.Length <= 0))
            {
                return;
            }
            // There are two ways to enumerate the camera configurations.

            // 1. Use a foreach to iterate over all the available configurations
            foreach (var config in configurations)
            {
                m_ConfigurationNames.Add($"{config.width}x{config.height}{(config.framerate.HasValue ? $" at {config.framerate.Value} Hz" : "")}{(config.depthSensorSupported == Supported.Supported ? " depth sensor" : "")}");
                Debug.Log($"{config.width}x{config.height}{(config.framerate.HasValue ? $" at {config.framerate.Value} Hz" : "")}{(config.depthSensorSupported == Supported.Supported ? " depth sensor" : "")}");
            }
            //m_Dropdown.AddOptions(m_ConfigurationNames);

            // 2. Use a normal for...loop
            cameraManager.currentConfiguration = configurations[1];
            Debug.Log($"current: {configurations[1].width}x{configurations[1].height}{(configurations[1].framerate.HasValue ? $" at {configurations[1].framerate.Value} Hz" : "")}{(configurations[1].depthSensorSupported == Supported.Supported ? " depth sensor" : "")}");
            currentConfig = configurations[1];
            cameraInitFinish = true;
        }
    }

    void WaitForInitFinish(){
        cameraInitFinish = true;
    }

    public void ChangeSendImgFlag()
    {
        bool state;
        Toggle toggle = GameObject.Find("SendImg").GetComponent<Toggle>();
        state = toggle.isOn;
        Debug.Log($"change send img state: {state}");
        sendImgFlag = state;
    }

    // Get camera frame
    public unsafe Texture2D GetCameraFrame()
    {
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            //Can't retrieve image
            throw new Exception("Can't retrieve image");
        }
        // Once we have a valid XRCpuImage, we can access the individual image "planes"
        // (the separate channels in the image). XRCpuImage.GetPlane provides
        // low-overhead access to this data. This could then be passed to a
        // computer vision algorithm. Here, we will convert the camera image
        // to an RGBA texture and draw it on the screen.

        // Choose an RGBA format.
        // See XRCpuImage.FormatSupported for a complete list of supported formats.
        var format = TextureFormat.RGBA32;
        var frameTexture = new Texture2D(image.width, image.height, format, false);

        // Convert the image to format, flipping the image across the Y axis.
        // We can also get a sub rectangle, but we'll get the full image here.

        XRCpuImage.Transformation m_Transformation = XRCpuImage.Transformation.MirrorY;
        var conversionParams = new XRCpuImage.ConversionParams(image, format, m_Transformation);
        try
        {
            // Get the point to the texture data and change with the data from the frame
            var rawTextureData = frameTexture.GetRawTextureData<byte>();
            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
            frameTexture.Apply();

        }
        catch (Exception e)
        {
            Debug.Log($"GetCameraFrame failed {e}");
            throw e;
        }
        finally
        {
            // We must dispose of the XRCpuImage after we're finished
            // with it to avoid leaking native resources.
            image.Dispose();
        }
        return frameTexture;
        // Apply the updated texture data to our texture

    }

    public int GetJPGByteLength(byte[] b)
    {
        int i = 0;
        while (b[i] != 0xff || b[i + 1] != 0xd9)
        {
            i++;
        }
        i += 2;
        return i;
    }


}
