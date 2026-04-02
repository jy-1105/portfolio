using UnityEngine;
using System.IO.Ports;
using System.Threading;
using UniRx;

/// <summary>
/// Arduinoから距離センサーの値をシリアル通信で受信し、
/// ユーザーが机の下に入ったかどうかを判定するクラス。
/// 判定結果はキャラクターの状態変化にも反映される。
/// </summary>
public class DistanceSensorReader : MonoBehaviour
{
    [Header("Serial Settings")]
    [SerializeField] private string portName = "COM6";
    [SerializeField] private int baudRate = 9600;
    [SerializeField] private int readTimeout = 100;

    [Header("Distance Settings")]
    [SerializeField] private float threshold = 45f;

    [Header("Target")]
    [SerializeField] private GameObject character;

    private SerialPort serial;
    private volatile bool isRunning = false;
    private volatile float distance = 0f;

    public bool UnderDesk { get; private set; } = false;

    private Vector3 normalScale = new Vector3(1, 1, 1);
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);

    void Start()
    {
        if (character == null)
        {
            Debug.LogError("Character is not assigned.");
            enabled = false;
            return;
        }

        serial = new SerialPort(portName, baudRate);
        serial.ReadTimeout = readTimeout;

        try
        {
            serial.Open();
            isRunning = true;

            // シリアル受信による待機でメインスレッドを止めないよう、別スレッドで読み取る
            Scheduler.ThreadPool.Schedule(ReadData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"シリアルポートが開けませんでした: {e.Message}");
            enabled = false;
        }
    }

    /// <summary>
    /// 別スレッドでシリアルポートから距離データを継続的に受信する。
    /// </summary>
    void ReadData()
    {
        while (isRunning)
        {
            try
            {
                string line = serial.ReadLine();

                if (float.TryParse(line, out float parsed))
                {
                    distance = parsed;
                }
            }
            catch (TimeoutException)
            {
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// 受信した距離データを基に、机の下に入った状態かどうかを判定する。
    /// </summary>
    void Update()
    {
        if (distance <= 0f) return;

        // しきい値より近い場合のみ「机の下に入った」と判定する
        bool newUnderDesk = distance < threshold;

        if (newUnderDesk == UnderDesk) return;

        UnderDesk = newUnderDesk;

        if (UnderDesk)
        {
            Debug.Log("机の下に入った！");
            character.transform.localScale = crouchScale;
        }
        else
        {
            character.transform.localScale = normalScale;
        }
    }

    void OnDestroy()
    {
        isRunning = false;

        if (serial != null)
        {
            try
            {
                if (serial.IsOpen) serial.Close();
                serial.Dispose();
            }
            catch
            {
            }
        }
    }
}