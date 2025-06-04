using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;

public class DroneController : MonoBehaviour
{
    [Header("無人機網路設定")]
    public string droneIP = "192.168.10.1";
    public int dronePort = 8889;
    public int listenPort = 8890; // 🆕 新增監聽回應 Port

    [Header("指令設定")]
    public int forwardDistance = 20;
    public int turnAngle = 90;

    private UdpClient udpClient;
    private UdpClient listenerClient; // 🆕 監聽用 UDP
    private IPEndPoint endPoint;
    private Thread listenerThread;

    public event Action<string> OnDroneResponse; // 🆕 事件：回傳訊息通知 UI

    private readonly Dictionary<string, string> commandMap = new Dictionary<string, string>
    {
        { "forward", "forward {0}" },
        { "left", "left {0}" },
        { "right", "right {0}" }
    };

    void Start()
    {
        InitializeUDP();
    }

    void OnDestroy()
    {
        CloseUDP();
        StopListening();
    }

    public bool InitializeUDP()
    {
        try
        {
            udpClient = new UdpClient();
            endPoint = new IPEndPoint(IPAddress.Parse(droneIP), dronePort);
            Debug.Log("UDP 連接已初始化");

            StartListening(); // 🆕 開始監聽無人機回應

            string testCommand = "command";
            byte[] sendBytes = Encoding.ASCII.GetBytes(testCommand);
            udpClient.Send(sendBytes, sendBytes.Length, endPoint);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP 初始化失敗: {e.Message}");
            return false;
        }
    }

    private void StartListening()
    {
        try
        {
            listenerClient = new UdpClient(listenPort);
            listenerThread = new Thread(ListenForResponse);
            listenerThread.IsBackground = true;
            listenerThread.Start();
            Debug.Log("開始監聽無人機回應");
        }
        catch (Exception e)
        {
            Debug.LogError($"啟動監聽失敗: {e.Message}");
        }
    }

    private void ListenForResponse()
    {
        try
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, listenPort);
            while (true)
            {
                byte[] data = listenerClient.Receive(ref remoteEP);
                string response = Encoding.ASCII.GetString(data);
                Debug.Log($"收到無人機回應: {response}");

                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    OnDroneResponse?.Invoke(response);
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"監聽回應失敗: {e.Message}");
        }
    }

    public void StopListening()
    {
        if (listenerThread != null)
        {
            listenerThread.Abort();
            listenerThread = null;
        }
        if (listenerClient != null)
        {
            listenerClient.Close();
            listenerClient = null;
        }
    }

    private void CloseUDP()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }

    public void SendCommand(string qrCodeText)
    {
        if (udpClient == null)
        {
            Debug.LogError("UDP 客戶端未初始化");
            return;
        }

        try
        {
            string command = qrCodeText.Trim().ToLower();

            if (commandMap.TryGetValue(command, out string commandFormat))
            {
                string fullCommand = string.Format(
                    commandFormat,
                    command == "forward" ? forwardDistance : turnAngle
                );

                byte[] sendBytes = Encoding.ASCII.GetBytes(fullCommand);
                udpClient.Send(sendBytes, sendBytes.Length, endPoint);

                Debug.Log($"已發送指令: {fullCommand}");
            }
            else
            {
                Debug.LogWarning($"未知的指令: {command}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"發送指令時發生錯誤: {e.Message}");
        }
    }
}
