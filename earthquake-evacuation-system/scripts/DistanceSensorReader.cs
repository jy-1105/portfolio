using UnityEngine;
using System.IO.Ports;
using System.Threading;
using UniRx;

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
        // キャラクターが未設定なら処理を止める
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
            // シリアルポートを開く
            serial.Open();
            isRunning = true;

            // 別スレッドでシリアル受信を開始
            Scheduler.ThreadPool.Schedule(ReadData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"シリアルポートが開けませんでした: {e.Message}");
            enabled = false;
        }
    }

    void ReadData()
    {
        while (isRunning)
        {
            try
            {
                // Arduino から 1 行ずつ距離データを受信
                string line = serial.ReadLine();

                // 数値に変換できた場合のみ更新
                if (float.TryParse(line, out float parsed))
                {
                    distance = parsed;
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
    }

    void Update()
    {
        if (distance <= 0f) return;

        // しきい値未満なら机の下に入ったと判定
        bool newUnderDesk = distance < threshold;

        // 状態が変わらないなら何もしない
        if (newUnderDesk == UnderDesk) return;

        UnderDesk = newUnderDesk;

        if (UnderDesk)
        {
            Debug.Log("机の下に入った！");
            character.transform.localScale = crouchScale;
        }
        else
        {
            // 通常状態に戻す
            character.transform.localScale = normalScale;
        }
    }

    void OnDestroy()
    {
        // 読み取りループを停止
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
                // 終了時の例外は無視
            }
        }
    }
}