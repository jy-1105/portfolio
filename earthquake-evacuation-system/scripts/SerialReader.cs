using System;
using System.IO.Ports;
using UnityEngine;

public class SerialReader : MonoBehaviour
{
    [SerializeField] private string portName = "COM5";
    [SerializeField] private int baudRate = 9600;
    [SerializeField] private int readTimeout = 50;

    private SerialPort serialPort;
    public bool SensorTriggered { get; private set; }

    void Start()
    {
        try
        {
            // シリアルポートの初期化
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = readTimeout;
            serialPort.Open();
        }
        catch (Exception e)
        {
            Debug.LogError("シリアル通信エラー: " + e.Message);
            enabled = false;
        }
    }

    void Update()
    {
        // このフレームでは未検知として初期化
        SensorTriggered = false;

        if (serialPort == null || !serialPort.IsOpen) return;

        try
        {
            // Arduino から文字列を受信
            string data = serialPort.ReadLine();

            // TRIGGER を受信したら反応ありと判定
            if (data.Trim() == "TRIGGER")
            {
                Debug.Log("センサからTRIGGER受信！");
                SensorTriggered = true;
            }
        }
        catch (TimeoutException)
        {
            // タイムアウトは無視
        }
        catch
        {
            // 必要ならここでログを出す
        }
    }

    void OnDestroy()
    {
        // 終了時にポートを閉じる
        if (serialPort != null)
        {
            try
            {
                if (serialPort.IsOpen) serialPort.Close();
                serialPort.Dispose();
            }
            catch
            {
                // 終了時の例外は無視
            }
        }
    }
}