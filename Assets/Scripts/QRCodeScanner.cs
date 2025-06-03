using UnityEngine;
using UnityEngine.UI;
using ZXing;
using System;

/// <summary>
/// 負責掃描畫面中的 QR Code 並解析其內容
/// </summary>
public class QRCodeScanner : MonoBehaviour
{
    [Header("UI 元件")]
    public Text qrCodeText; // 用於顯示掃描結果的文字元件
    public RawImage sourceImage; // 來源影像（從 DroneVideoReceiver 獲取）

    [Header("掃描設定")]
    public float scanInterval = 0.5f; // 掃描間隔（秒）

    private BarcodeReader barcodeReader;
    private float nextScanTime;
    private DroneController droneController;

    void Start()
    {
        // 初始化 ZXing 掃描器
        barcodeReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true
            }
        };

        droneController = GetComponent<DroneController>();
        if (droneController == null)
        {
            Debug.LogWarning("找不到 DroneController 元件");
        }

        // 設定初始文字
        UpdateQRCodeText("等待掃描 QR Code...");
    }

    void Update()
    {
        // 檢查是否到達下一次掃描時間
        if (Time.time >= nextScanTime)
        {
            ScanQRCode();
            nextScanTime = Time.time + scanInterval;
        }
    }

    /// <summary>
    /// 執行 QR Code 掃描
    /// </summary>
    private void ScanQRCode()
    {
        if (sourceImage.texture == null) return;

        try
        {
            // 獲取當前顯示的材質
            Texture2D texture = sourceImage.texture as Texture2D;
            if (texture == null)
            {
                Debug.LogWarning("無法獲取影像材質");
                return;
            }

            // 獲取像素資料
            Color32[] pixels = texture.GetPixels32();

            // 使用 ZXing 解碼
            var result = barcodeReader.Decode(pixels, texture.width, texture.height);

            if (result != null)
            {
                string decodedText = result.Text;
                UpdateQRCodeText($"掃描到 QR Code: {decodedText}");

                // 如果有連接 DroneController，則發送指令
                if (droneController != null)
                {
                    droneController.SendCommand(decodedText);
                }
            }
            else
            {
                UpdateQRCodeText("未偵測到 QR Code");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"QR Code 掃描錯誤: {e.Message}");
            UpdateQRCodeText("掃描發生錯誤");
        }
    }

    /// <summary>
    /// 更新 UI 上的 QR Code 文字
    /// </summary>
    private void UpdateQRCodeText(string message)
    {
        if (qrCodeText != null)
        {
            qrCodeText.text = message;
        }
    }
}