using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// 負責將 QR Code 指令轉換為無人機控制命令並透過 UDP 發送
/// </summary>
public class DroneController : MonoBehaviour
{
    [Header("無人機網路設定")]
    public string droneIP = "192.168.10.1";
    public int dronePort = 8889;

    [Header("指令設定")]
    public int forwardDistance = 20; // 前進距離（公分）
    public int turnAngle = 90; // 轉向角度（度）

    private UdpClient udpClient;
    private IPEndPoint endPoint;

    // 指令對應表
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
    }

    /// <summary>
    /// 初始化 UDP 連接
    /// </summary>
    private void InitializeUDP()
    {
        try
        {
            udpClient = new UdpClient();
            endPoint = new IPEndPoint(IPAddress.Parse(droneIP), dronePort);
            Debug.Log("UDP 連接已初始化");
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP 初始化失敗: {e.Message}");
        }
    }

    /// <summary>
    /// 關閉 UDP 連接
    /// </summary>
    private void CloseUDP()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }

    /// <summary>
    /// 發送控制指令到無人機
    /// </summary>
    /// <param name="qrCodeText">QR Code 掃描到的文字</param>
    public void SendCommand(string qrCodeText)
    {
        if (udpClient == null)
        {
            Debug.LogError("UDP 客戶端未初始化");
            return;
        }

        try
        {
            // 將 QR Code 文字轉換為小寫並去除空白
            string command = qrCodeText.Trim().ToLower();

            // 檢查是否為有效指令
            if (commandMap.TryGetValue(command, out string commandFormat))
            {
                // 根據指令類型設定參數
                string fullCommand = string.Format(
                    commandFormat,
                    command == "forward" ? forwardDistance : turnAngle
                );

                // 轉換為 byte 陣列並發送
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