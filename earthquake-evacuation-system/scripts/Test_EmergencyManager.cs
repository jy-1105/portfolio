using UnityEngine;

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
        // 開始時は緊急マークを非表示
        if (emergencyMark != null)
            emergencyMark.SetActive(false);

        // 最初の緊急イベントを予約
        ScheduleNextEmergency();
    }

    void Update()
    {
        if (!isEmergency) return;

        // P キー入力
        bool keyPressed = Input.GetKeyDown(KeyCode.P);

        // 距離センサで机の下に入ったか確認
        bool sensorTriggered = distanceSensorReader != null && distanceSensorReader.UnderDesk;

        // キー入力またはセンサ反応があれば緊急解除
        if (keyPressed || sensorTriggered)
        {
            EndEmergency();
        }
    }

    void TriggerEmergency()
    {
        isEmergency = true;

        if (emergencyMark != null) emergencyMark.SetActive(true);
        if (player != null) player.canMove = false;
        if (jishinparticle != null) jishinparticle.StartEarthquakeEffect();
        if (noiseController != null) noiseController.EnableNoise();

        Debug.Log("緊急イベント発生！");
    }

    void EndEmergency()
    {
        isEmergency = false;

        if (emergencyMark != null) emergencyMark.SetActive(false);
        if (player != null) player.canMove = true;
        if (jishinparticle != null) jishinparticle.StopEarthquakeEffect();
        if (noiseController != null) noiseController.DisableNoise();

        Debug.Log("緊急解除完了！");

        // 次の緊急イベントを再予約
        ScheduleNextEmergency();
    }

    void ScheduleNextEmergency()
    {
        // 指定範囲のランダム時間後に緊急イベント発生
        float delay = Random.Range(minTime, maxTime);
        Invoke(nameof(TriggerEmergency), delay);
    }
}