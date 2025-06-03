using UnityEngine;
using UnityEngine.UI;
using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using Debug = UnityEngine.Debug;

public interface IVideoStreamDecoder
{
    void StartStream();
    void StopStream();
    Texture2D GetCurrentFrame();
}

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
public class WindowsFfmpegDecoder : IVideoStreamDecoder
{
    public string ffmpegPath;
    public string inputUrl = "udp://0.0.0.0:11111";
    public int frameWidth = 960;
    public int frameHeight = 720;

    private Process ffmpegProcess;
    private Texture2D videoTexture;
    private byte[] frameBuffer;
    private bool isRunning = false;
    private Thread readThread;

    public WindowsFfmpegDecoder()
    {
        videoTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);
        frameBuffer = new byte[frameWidth * frameHeight * 3];
    }

    public Texture2D GetCurrentFrame()
    {
        return videoTexture;
    }

    public void StartStream()
    {
        if (isRunning)
            return;

        try
        {
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                Debug.LogError("FFmpeg path is not set.");
                return;
            }

            if (!File.Exists(ffmpegPath))
            {
                Debug.LogError($"FFmpeg not found at path: {ffmpegPath}");
                return;
            }

            ffmpegProcess = new Process();
            ffmpegProcess.StartInfo.FileName = ffmpegPath;
            ffmpegProcess.StartInfo.Arguments = $"-i {inputUrl} -f rawvideo -pix_fmt rgb24 -";
            ffmpegProcess.StartInfo.UseShellExecute = false;
            ffmpegProcess.StartInfo.RedirectStandardOutput = true;
            ffmpegProcess.StartInfo.CreateNoWindow = true;

            ffmpegProcess.Start();
            isRunning = true;

            readThread = new Thread(ReadFrames);
            readThread.IsBackground = true;
            readThread.Start();

            Debug.Log("FFmpeg process started.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start FFmpeg: {e.Message}");
        }
    }

    public void StopStream()
    {
        isRunning = false;

        if (readThread != null && readThread.IsAlive)
            readThread.Join();

        if (ffmpegProcess != null && !ffmpegProcess.HasExited)
        {
            ffmpegProcess.Kill();
            ffmpegProcess.Dispose();
            ffmpegProcess = null;
        }
    }

    private void ReadFrames()
    {
        try
        {
            while (isRunning)
            {
                if (ffmpegProcess == null || ffmpegProcess.HasExited)
                {
                    Debug.LogWarning("FFmpeg process exited, restarting...");
                    StartStream();
                    return;
                }

                int bytesRead = 0;
                int frameSize = frameWidth * frameHeight * 3;

                while (bytesRead < frameSize)
                {
                    int remaining = frameSize - bytesRead;
                    int read = ffmpegProcess.StandardOutput.BaseStream.Read(frameBuffer, bytesRead, remaining);
                    if (read == 0) break;
                    bytesRead += read;
                }

                if (bytesRead == frameSize)
                {
                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        videoTexture.LoadRawTextureData(frameBuffer);
                        videoTexture.Apply();
                    });
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading video frames: {e.Message}");
        }
    }
}
#endif

#if UNITY_ANDROID || UNITY_IOS
public class MobileFfmpegDecoder : IVideoStreamDecoder
{
    public void StartStream()
    {
        Debug.Log("Mobile FFmpeg decoder not yet implemented.");
    }
    public void StopStream() { }
    public Texture2D GetCurrentFrame() { return null; }
}
#endif

public class DroneVideoReceiver : MonoBehaviour
{
    [Header("UI Components")]
    public RawImage displayImage;

    [Header("FFmpeg Settings")]
    [Tooltip("相對於 Assets 資料夾的 ffmpeg.exe 路徑，例如 'Plugins/ffmpeg-n7.1-latest-win64-gpl-shared-7.1/bin/ffmpeg.exe'")]
    public string ffmpegRelativePath = "Plugins/ffmpeg-n7.1-latest-win64-gpl-shared-7.1/bin/ffmpeg.exe";

    public string inputUrl = "udp://0.0.0.0:11111";
    public int frameWidth = 960;
    public int frameHeight = 720;

    private IVideoStreamDecoder videoDecoder;

    void Start()
    {
        // 利用 Application.dataPath 自動組合完整路徑
        string fullFfmpegPath = Path.Combine(Application.dataPath, ffmpegRelativePath);

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        var decoder = new WindowsFfmpegDecoder();
        decoder.ffmpegPath = fullFfmpegPath;
        decoder.inputUrl = inputUrl;
        decoder.frameWidth = frameWidth;
        decoder.frameHeight = frameHeight;
        videoDecoder = decoder;
#elif UNITY_ANDROID || UNITY_IOS
        videoDecoder = new MobileFfmpegDecoder();
#else
        Debug.LogError("Unsupported platform for video decoding.");
#endif
        videoDecoder.StartStream();

        if (displayImage != null)
            displayImage.texture = videoDecoder.GetCurrentFrame();
    }

    void OnDestroy()
    {
        if (videoDecoder != null)
            videoDecoder.StopStream();
    }

    void Update()
    {
        if (displayImage != null && videoDecoder != null)
        {
            displayImage.texture = videoDecoder.GetCurrentFrame();
        }
    }
}

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher instance;
    private readonly Queue<Action> actionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("UnityMainThreadDispatcher");
                instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public void Enqueue(Action action)
    {
        lock (actionQueue)
        {
            actionQueue.Enqueue(action);
        }
    }

    void Update()
    {
        while (true)
        {
            Action action = null;
            lock (actionQueue)
            {
                if (actionQueue.Count == 0) return;
                action = actionQueue.Dequeue();
            }
            action?.Invoke();
        }
    }
}

