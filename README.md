# Unity RoboMaster QR Code 控制系統

這個專案實現了透過 QR Code 來控制 DJI RoboMaster TT 無人機的功能。系統使用 FFmpeg 接收無人機的視訊串流，使用 ZXing.Net 進行 QR Code 掃描，並透過 UDP 協議發送控制指令。

## 功能特點

- 即時接收和顯示無人機視訊串流
- 每 0.5 秒自動掃描 QR Code
- 支援基本飛行指令（前進、左轉、右轉）
- 即時顯示掃描結果
- 自動重連機制

## 系統需求

- Unity 2021 LTS 或更新版本
- FFmpeg（需要預先安裝）
- Windows 10 或更新版本
- .NET Framework 4.x

## 必要套件

- ZXing.Net（需要透過 NuGet 安裝）
- System.Net.Sockets（Unity 內建）
- System.Diagnostics（Unity 內建）

## 安裝步驟

1. 克隆或下載此專案
2. 在 Unity Hub 中開啟專案
3. 將 FFmpeg 執行檔放置在專案的執行檔目錄中
4. 安裝 ZXing.Net 套件
5. 確保無人機已連接到同一個網路

## 場景設置

1. 建立新場景
2. 添加 Canvas 物件
3. 在 Canvas 下添加：
   - RawImage（用於顯示視訊串流）
   - Text（用於顯示 QR Code 掃描結果）
4. 建立一個空物件，添加以下腳本：
   - DroneVideoReceiver
   - QRCodeScanner
   - DroneController
5. 設置必要的參考和參數

## 使用方法

1. 啟動專案
2. 確保無人機已開啟並連接到正確的網路
3. 使用支援的 QR Code 指令：
   - "forward"：向前飛行 20 公分
   - "left"：向左轉 90 度
   - "right"：向右轉 90 度

## 注意事項

- 確保 FFmpeg.exe 位於正確的目錄
- 檢查無人機的 IP 位址是否正確（預設：192.168.10.1）
- 確保 UDP 端口設置正確（視訊串流：11111，控制指令：8889）
- 建議在使用前先測試網路連接

## 故障排除

- 如果視訊串流無法顯示，檢查 FFmpeg 是否正確安裝
- 如果 QR Code 無法掃描，確保光線充足且 QR Code 清晰可見
- 如果無法發送指令，檢查網路連接和 UDP 設置

## 開發者資訊

此專案包含三個主要腳本：

- `DroneVideoReceiver.cs`：處理視訊串流接收和顯示
- `QRCodeScanner.cs`：處理 QR Code 掃描
- `DroneController.cs`：處理無人機控制指令

## 授權

MIT License 