using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QRCodeDroneManager : MonoBehaviour
{
    [Header("UI 元件")]
    public TextMeshProUGUI qrCodeText;
    public TextMeshProUGUI droneStatusText;
    public Button startScanButton;
    public Button stopScanButton;
    public QRCodeScanner qrCodeScanner; // 引用 QRCodeScanner
    public DroneController droneController; // 引用 DroneController

    void Start()
    {
        InitializeButtons();

        UpdateDroneStatus("連線中", Color.yellow); // 啟動時預設黃燈
        if (droneController.InitializeUDP())
        {
            UpdateDroneStatus("已連線", Color.green);
        }
        else
        {
            UpdateDroneStatus("連線失敗", Color.red);
        }

        UpdateQRCodeText("等待掃描 QR Code...", Color.black);

        // 傳遞 UI 元件給 QRCodeScanner
        qrCodeScanner.SetQRCodeText(qrCodeText);
        qrCodeScanner.SetDroneController(droneController);
    }

    private void InitializeButtons()
    {
        startScanButton.onClick.AddListener(StartScanning);
        stopScanButton.onClick.AddListener(StopScanning);
        stopScanButton.interactable = false; // 初始時停用 Stop
    }

    private void StartScanning()
    {
        qrCodeScanner.StartScanning();
        startScanButton.interactable = false;
        stopScanButton.interactable = true;
        UpdateQRCodeText("掃描啟動中...", Color.black);
    }

    private void StopScanning()
    {
        qrCodeScanner.StopScanning();
        startScanButton.interactable = true;
        stopScanButton.interactable = false;
        UpdateQRCodeText("掃描已停止", Color.black);
    }

    private void UpdateQRCodeText(string message, Color color)
    {
        if (qrCodeText != null)
        {
            qrCodeText.text = message;
            qrCodeText.color = color;
        }
    }

    private void UpdateDroneStatus(string message, Color color)
    {
        if (droneStatusText != null)
        {
            droneStatusText.text = message;
            droneStatusText.color = color;
        }
    }
}
