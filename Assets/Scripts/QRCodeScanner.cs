using System;
using System.Collections;
using UnityEngine;
using TMPro;
using ZXing;
using System.Collections.Generic;

public class QRCodeScanner : MonoBehaviour
{
    [Header("掃描設定")]
    public RenderTexture sourceRenderTexture;
    public float scanInterval = 0.5f;

    private BarcodeReader barcodeReader;
    private Coroutine scanCoroutine;
    private bool isScanning = false;

    private TextMeshProUGUI qrCodeText;
    private DroneController droneController;

    private Texture2D readTexture;
    private int failedScanCount = 0; // 🆕 失敗次數統計
    private const int maxFailedScans = 5;

    private HashSet<string> validCommands = new HashSet<string> { "forward", "left", "right" }; // 🆕 合法指令表

    void Start()
    {
        InitializeQRCodeReader();
        readTexture = new Texture2D(sourceRenderTexture.width, sourceRenderTexture.height, TextureFormat.RGB24, false);
    }

    private void InitializeQRCodeReader()
    {
        barcodeReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true
            }
        };
    }

    public void StartScanning()
    {
        if (!isScanning)
        {
            isScanning = true;
            scanCoroutine = StartCoroutine(ScanLoop());
        }
    }

    public void StopScanning()
    {
        if (isScanning)
        {
            isScanning = false;
            if (scanCoroutine != null)
            {
                StopCoroutine(scanCoroutine);
            }
        }
    }

    private IEnumerator ScanLoop()
    {
        while (isScanning)
        {
            ScanQRCode();
            yield return new WaitForSecondsRealtime(scanInterval);
        }
    }

    private void ScanQRCode()
    {
        if (sourceRenderTexture == null) return;

        try
        {
            RenderTexture.active = sourceRenderTexture;
            readTexture.ReadPixels(new Rect(0, 0, sourceRenderTexture.width, sourceRenderTexture.height), 0, 0);
            readTexture.Apply();
            RenderTexture.active = null;

            Color32[] pixels = readTexture.GetPixels32();
            var result = barcodeReader.Decode(pixels, readTexture.width, readTexture.height);

            if (result != null)
            {
                string decodedText = result.Text.Trim().ToLower();
                Debug.Log($"掃描結果: {decodedText}");

                if (validCommands.Contains(decodedText))
                {
                    UpdateQRCodeText($"掃描到有效指令: {decodedText}", Color.green);

                    if (droneController != null)
                    {
                        droneController.SendCommand(decodedText);
                    }

                    failedScanCount = 0; // 成功掃描歸零
                }
                else
                {
                    UpdateQRCodeText($"無效指令: {decodedText}", Color.red);
                }
            }
            else
            {
                failedScanCount++;
                UpdateQRCodeText("未偵測到 QR Code", Color.red);

                if (failedScanCount >= maxFailedScans)
                {
                    Debug.LogWarning("連續未偵測到 QR Code，請調整鏡頭！");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"QR Code 掃描錯誤: {e.Message}");
            UpdateQRCodeText("掃描發生錯誤", Color.red);
        }
    }

    private void UpdateQRCodeText(string message, Color color)
    {
        if (qrCodeText != null)
        {
            qrCodeText.text = message;
            qrCodeText.color = color;
        }
    }

    public void SetQRCodeText(TextMeshProUGUI text)
    {
        qrCodeText = text;
    }

    public void SetDroneController(DroneController controller)
    {
        droneController = controller;
        droneController.OnDroneResponse += OnDroneResponseReceived; // 🆕 訂閱無人機回應
    }

    private void OnDroneResponseReceived(string response)
    {
        UpdateQRCodeText($"無人機回應: {response}", Color.cyan);
    }
}
