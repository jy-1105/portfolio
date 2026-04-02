using UnityEngine;

/// <summary>
/// 地震発生から避難、解除までの緊急イベント全体の進行を管理するクラス。
/// センサー入力やキー入力をもとに避難成功を判定する。
/// </summary>
public class Test_EmergencyManager : MonoBehaviour
{
    [SerializeField] private GameObject emergencyMark;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private DistanceSensorReader distanceSensorReader;
    [SerializeField] private JishinParticle jishinparticle;
    [SerializeField] private NoiseController noiseController;

    [SerializeField] private float minTime = 5f;
    [SerializeField] private float maxTime = 10f;

    private bool isEmergency = false;

    void Start()
    {
        if (emergencyMark != null)
            emergencyMark.SetActive(false);

        ScheduleNextEmergency();
    }

    void Update()
    {
        if (!isEmergency) return;

        bool keyPressed = Input.GetKeyDown(KeyCode.P);
        bool sensorTriggered = distanceSensorReader != null && distanceSensorReader.UnderDesk;

        if (keyPressed || sensorTriggered)
        {
            EndEmergency();
        }
    }

    /// <summary>
    /// 緊急イベントを開始し、警告表示・移動制限・演出を有効化する。
    /// </summary>
    void TriggerEmergency()
    {
        isEmergency = true;

        if (emergencyMark != null) emergencyMark.SetActive(true);
        if (player != null) player.canMove = false;
        if (jishinparticle != null) jishinparticle.StartEarthquakeEffect();
        if (noiseController != null) noiseController.EnableNoise();

        Debug.Log("緊急イベント発生！");
    }

    /// <summary>
    /// 緊急イベントを終了し、演出停止と次回イベント予約を行う。
    /// </summary>
    void EndEmergency()
    {
        isEmergency = false;

        if (emergencyMark != null) emergencyMark.SetActive(false);
        if (player != null) player.canMove = true;
        if (jishinparticle != null) jishinparticle.StopEarthquakeEffect();
        if (noiseController != null) noiseController.DisableNoise();

        Debug.Log("緊急解除完了！");

        ScheduleNextEmergency();
    }

    /// <summary>
    /// 指定範囲内のランダムな時間で次の緊急イベントを予約する。
    /// </summary>
    void ScheduleNextEmergency()
    {
        float delay = Random.Range(minTime, maxTime);
        Invoke(nameof(TriggerEmergency), delay);
    }
}