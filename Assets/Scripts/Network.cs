using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

public class Network : MonoBehaviour
{
    [SerializeField] NavigationManager navigationManager;
    [SerializeField] TMP_Text textInfo;
    string serverAddr = "ws://47.120.34.84:6006";
    Thread t_WebSocket;
    Thread t_WSListener;
    bool t_SocketConnect = false;
    Uri m_uri = null;
    ClientWebSocket m_webSocket = null;
    int checkInterval = 1000;
    bool disposed = false;
    CancellationToken m_cToken;
    CancellationTokenSource source;
    PoseManager poseManager;
    PathManager pathManager;
    StateController stateController;
    ShareManager shareManager;
    ImageAccesser imageAccesser;
    AdManager adManager;
    bool checkBug = false;
    public bool initFinish = false;
    public string userName = "user58";

    public enum PackageType : short
    {
        locRequest = 1,
        locRet,
        errorInfo,
        initUser,
        navRequest,
        pathInfo,
        sharePos,
        updatePose,
        broadcast,
        adInfo,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PackageHead
    {
        public PackageType packageType;

        public PackageHead(byte[] b)
        {
            packageType = (PackageType)BitConverter.ToInt16(b, 0);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        userName = PlayerPrefs.GetString("userName");
        if (Application.isEditor)
        {
            userName = "editor";
        }
        Debug.Log("userName: " + userName);
        poseManager = FindObjectOfType<PoseManager>();
        pathManager = FindObjectOfType<PathManager>();
        stateController = FindObjectOfType<StateController>();
        shareManager = FindObjectOfType<ShareManager>();
        imageAccesser = FindObjectOfType<ImageAccesser>();
        adManager = FindObjectOfType<AdManager>();

        // connect to server    
        source = new CancellationTokenSource();
        m_cToken = source.Token;
        StartCoroutine("CheckingWebSocketState");
    }

    void RestartCoroutine()
    {
        Debug.Log("RestartCoroutine");
        StopCoroutine("CheckingWebSocketState");
        StartCoroutine("CheckingWebSocketState");
        // source.Cancel();
    }

    private async Task ConnectAsync()
    {
        try
        {
            checkBug = true;
            m_webSocket = new ClientWebSocket();
            await m_webSocket.ConnectAsync(new Uri(serverAddr), m_cToken);
            if (m_webSocket.State == WebSocketState.Open)
            {
                if (stateController != null)
                    stateController.NetworkStatus_Connected();
                if (t_WSListener != null)
                    t_WSListener.Abort();
                t_WSListener = new Thread(ListenWS);
                t_WSListener.Start();
                Invoke("SendInit", 0.3f);
                Debug.Log("Connect success");
            }
        }
        catch (WebSocketException e)
        {
            Debug.LogError($"WebSocket connection error");
            Debug.LogError(e.Message);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to WebSocket server");
            Debug.LogError(e.Message);
        }
        finally
        {
            checkBug = false;
        }
    }

    async void ListenWS()
    {
        int bufferSize = 1024 * 1024 * 5;
        Debug.Log("Start listen thread");
        try
        {
            while (m_webSocket.State == WebSocketState.Open)
            {
                ArraySegment<byte> buffer = new(new byte[bufferSize]);
                WebSocketReceiveResult result = new(0, WebSocketMessageType.Binary, false);
                List<byte[]> messagePart = new();
                int length = 0;
                while (!result.EndOfMessage)
                {
                    result = await m_webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    byte[] tmp = new byte[result.Count];
                    Buffer.BlockCopy(buffer.Array, 0, tmp, 0, result.Count);
                    messagePart.Add(tmp);
                    length += result.Count;
                }
                byte[] message = new byte[length];
                int index = 0;
                foreach (byte[] part in messagePart)
                {
                    Buffer.BlockCopy(part, 0, message, index, part.Length);
                    index += part.Length;
                }
                ParsePackage(message);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"WebSocket listener error: {e}");
        }
        Debug.Log("Websocket listen thread quit");
    }

    private async void CheckingWebSocketState()
    {
        await Task.Delay(1000);
        await ConnectAsync();
        while (!disposed)
        {
            try
            {
                await Task.Delay(checkInterval);
                if (m_webSocket.State != WebSocketState.Open)
                {
                    await Task.Delay(1000); //等待1s后开始执行重连
                    poseManager.locRequesting = false;
                    if (stateController != null)
                        stateController.NetworkStatus_Disconnected();
                    Debug.LogWarning("websocket connection is broken, attempting to reconnect...");
                    await ConnectAsync();
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
    }

    void ParsePackage(byte[] data)
    {
        PackageHead dataHead = new(data);
        int dataLength = data.Length - Marshal.SizeOf(dataHead);
        byte[] payload = new byte[dataLength];
        string json;
        try
        {
            JObject obj;
            Buffer.BlockCopy(data, Marshal.SizeOf(dataHead), payload, 0, dataLength);
            switch (dataHead.packageType)
            {
                case PackageType.errorInfo:
                    poseManager.locRequesting = false;
                    Debug.Log($"recv errorInfo: {Encoding.ASCII.GetString(payload)}");
                    textInfo.text = Encoding.ASCII.GetString(payload) + " " + DateTime.Now.ToString("HH:mm:ss");
                    // if (stateController != null)
                    //     stateController.LocSystemStatus_LocationFailed();
                    break;
                case PackageType.locRet:
                    poseManager.locRequesting = false;
                    Debug.Log("Recv pose result");
                    json = Encoding.ASCII.GetString(payload);
                    obj = JObject.Parse(json);
                    Debug.Log(obj["PnP_ret"]["success"]);
                    textInfo.text = "locate success: " + obj["PnP_ret"]["num_inliers"].ToString() + " " + DateTime.Now.ToString("HH:mm:ss");
                    if ((bool)obj["PnP_ret"]["success"] == true)
                    {
                        float[] tvec = obj["PnP_ret"]["tvec"].ToObject<List<float>>().ToArray();
                        float[] qvec = obj["PnP_ret"]["qvec"].ToObject<List<float>>().ToArray();
                        Pose p = new(new Vector3(tvec[0], tvec[1], tvec[2]), new Quaternion(qvec[1], qvec[2], qvec[3], qvec[0]));
                        Debug.Log($"Loc success, pose update {p.ToString()}");
                        poseManager.AddCamPose(p, int.Parse(obj["PnP_ret"]["num_inliers"].ToString()));
                        poseManager.UpdateUserPose();
                        poseManager.SetFloor((int)obj["floor"]);
                    }
                    else
                    {
                        // falied, remove last ar pose
                        poseManager.RemoveLastARPose();
                    }
                    break;
                case PackageType.initUser:
                    Debug.Log("Recv init");
                    json = Encoding.UTF8.GetString(payload);
                    // Debug.Log($"{json}");
                    obj = JObject.Parse(json);
                    Dictionary<int, List<string>> floors = obj["places"].ToObject<Dictionary<int, List<string>>>();
                    navigationManager.InitPlaces(floors);
                    break;
                case PackageType.pathInfo:
                    Debug.Log("Recv pathInfo");
                    json = Encoding.UTF8.GetString(payload);
                    List<Vector3> path = new();
                    obj = JObject.Parse(json);
                    foreach (var coor in obj["PathCoor"])
                    {
                        var coorList = coor.ToObject<List<float>>();
                        path.Add(new Vector3(coorList[0], coorList[1], coorList[2]));
                    }
                    List<int> floor = obj["Floor"].ToObject<List<int>>();
                    List<int> type = obj["Type"].ToObject<List<int>>();
                    navigationManager.CallNavigation(path, floor, type, obj["Des"].ToString());
                    break;
                case PackageType.broadcast:
                    Debug.Log("Recv broadcast");
                    string json_str = Encoding.UTF8.GetString(payload);
                    // Debug.Log(json_str);
                    shareManager.CallParseBroadcast(json_str);
                    break;
                case PackageType.adInfo:
                    Debug.Log("Recv advertisement");
                    json = Encoding.UTF8.GetString(payload);
                    // Debug.Log(json);
                    // parse the base64 image data
                    JObject ads = JObject.Parse(json);
                    IEnumerable<JProperty> properties = ads.Properties();
                    foreach (JProperty ad in properties)
                    {
                        int ad_id = int.Parse(ad.Name);
                        JObject ad_info = JObject.Parse(ad.Value.ToString());
                        string ad_name = ad_info["name"].ToString();
                        string ad_url = ad_info["url"].ToString();
                        // parse base64 string image in ad_info["image"]
                        string base64_image = ad_info["image"].ToString();
                        byte[] image_bytes = Convert.FromBase64String(base64_image);
                        Vector3 ad_position = new Vector3(float.Parse(ad_info["position"][0].ToString()), float.Parse(ad_info["position"][1].ToString()), float.Parse(ad_info["position"][2].ToString()));
                        int ad_floor = int.Parse(ad_info["floor"].ToString());
                        adManager.AddAdsToShow(ad_id, ad_name, ad_url, ad_position, image_bytes, ad_floor);
                    }
                    adManager.CallAdsShow();
                    // TODO
                    break;
                default:
                    Debug.Log($"default {dataHead.packageType}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    void SendInit()
    {
        Debug.Log("Send Init");
        ImageAccesser imageAccesser = gameObject.GetComponent<ImageAccesser>();

        float[] intrinsic = new float[4];
        intrinsic = imageAccesser.GetCameraIntrinsic();
        if(intrinsic[0] == 0){
            Invoke("SendInit", 0.2f);
        }

        JObject initMsg = new();
        initMsg.Add("Intrinsic", JToken.FromObject(intrinsic));
        initMsg.Add("Name", JToken.FromObject(userName));
        // initMsg.Add("Navigating", JToken.FromObject(pathManager.navigating));
        initMsg.Add("Navigating", JToken.FromObject(navigationManager.GetNavigationState()));
        initMsg.Add("Sharing", JToken.FromObject(shareManager.sharing));

        string json = JsonConvert.SerializeObject(initMsg);
        SendText(json, PackageType.initUser);
        initFinish = true;
        stateController.NetworkStatus_Connected();

    }

    public void SendText(string s, PackageType type)
    {
        byte[] b = Encoding.UTF8.GetBytes(s);
        SendByte(b, type, b.Length);
    }

    public void SendByte(byte[] data, PackageType type, int length)
    {
        PackageHead head = new();
        byte[] package = new byte[Marshal.SizeOf(head) + length];
        head.packageType = type;
        // Debug.Log($"SendByte {type}");
        int size = Marshal.SizeOf(head);
        IntPtr intPtr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(head, intPtr, true);
        Marshal.Copy(intPtr, package, 0, size);
        for (int i = 0; i < length; i++)
        {
            package[i + size] = data[i];
        }
        Debug.Log($"package length: {package.Length}");
        m_webSocket.SendAsync(new ArraySegment<byte>(package), WebSocketMessageType.Binary, true, CancellationToken.None);
    }

    public void Dispose()
    {
        Debug.Log("Dispose");
        disposed = true;
    }

    private void OnDestroy()
    {
        Debug.Log("Destroy");
        disposed = true;
    }
}


