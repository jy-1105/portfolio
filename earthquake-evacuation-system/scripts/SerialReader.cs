using System;
using System.IO.Ports;
using UnityEngine;

/// <summary>
/// Arduinoから送信された文字列データをシリアル通信で受信し、
/// センサー反応の有無を判定するクラス。
/// </summary>
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
        SensorTriggered = false;

        if (serialPort == null || !serialPort.IsOpen) return;

        try
        {
            string data = serialPort.ReadLine();

            if (data.Trim() == "TRIGGER")
            {
                Debug.Log("センサからTRIGGER受信！");
                SensorTriggered = true;
            }
        }
        catch (TimeoutException)
        {
        }
        catch
        {
        }
    }

    void OnDestroy()
    {
        if (serialPort != null)
        {
            try
            {
                if (serialPort.IsOpen) serialPort.Close();
                serialPort.Dispose();
            }
            catch
            {
            }
        }
    }
}