using UnityEngine;
using Valve.VR.InteractionSystem;

/// <summary>
/// プレイヤーが掴む操作を行ったときに、対応する魚ネタを生成して手に持たせるクラス。
/// VR空間における直感的な食材取得処理を担当する。
/// </summary>
[RequireComponent(typeof(Interactable))]
public class FishSource : MonoBehaviour
{
    [Header("Fish Settings")]
    [Tooltip("生成する魚ネタのPrefab")]
    public GameObject fishPrefab;

    [Tooltip("魚ネタの生成位置オフセット（手からの相対位置）")]
    public Vector3 fishSpawnOffset = new Vector3(0f, 0.1f, 0f);

    [Header("Audio")]
    [Tooltip("魚ネタを掴んだときの効果音")]
    public AudioClip fishGrabSound;

    [Header("Settings")]
    [Tooltip("連続生成のクールダウン時間（秒）")]
    public float cooldownTime = 0.5f;

    [Header("Visual Feedback")]
    [Tooltip("この元が生成する魚の種類（表示用）")]
    public string fishTypeName = "Maguro";

    private Interactable interactable;
    private float lastSpawnTime = 0f;

    void Awake()
    {
        interactable = GetComponent<Interactable>();
        interactable.onAttachedToHand += OnAttachedToHand;
        name = $"FishSource ({fishTypeName})";
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.onAttachedToHand -= OnAttachedToHand;
        }
    }

    void Update()
    {
        if (interactable != null && interactable.hoveringHand != null)
        {
            Hand hand = interactable.hoveringHand;
            GrabTypes startingGrabType = hand.GetGrabStarting();

            if (startingGrabType != GrabTypes.None)
            {
                Debug.Log($"[{name}] Grab detected in Update! Type: {startingGrabType}");
                SpawnFish(hand, startingGrabType);
            }
        }
    }

    /// <summary>
    /// 指定した手の位置に魚ネタを生成し、手にアタッチする。
    /// </summary>
    private void SpawnFish(Hand hand, GrabTypes grabType)
    {
        // 連続生成を防ぐため、短時間のクールダウンを設ける
        if (Time.time - lastSpawnTime < cooldownTime)
        {
            Debug.Log("魚ネタ生成のクールダウン中です");
            return;
        }

        if (fishPrefab == null)
        {
            Debug.LogError("魚ネタのPrefabが設定されていません。");
            return;
        }

        Vector3 spawnPosition = hand.transform.position + hand.transform.TransformDirection(fishSpawnOffset);
        Quaternion spawnRotation = hand.transform.rotation;

        GameObject newFish = Instantiate(fishPrefab, spawnPosition, spawnRotation);

        // 生成元との衝突で不自然な挙動が出ないよう、直後の衝突を無効化する
        Collider sourceCollider = GetComponent<Collider>();
        Collider fishCollider = newFish.GetComponent<Collider>();
        if (sourceCollider != null && fishCollider != null)
        {
            Physics.IgnoreCollision(sourceCollider, fishCollider);
        }

        Debug.Log($"{fishTypeName}ネタを{hand.name}に生成しました");

        if (fishGrabSound != null)
        {
            AudioSource.PlayClipAtPoint(fishGrabSound, spawnPosition);
        }

        hand.AttachObject(newFish, grabType);
        lastSpawnTime = Time.time;
    }

    private void OnAttachedToHand(Hand hand)
    {
    }

    void OnDrawGizmos()
    {
        switch (fishTypeName)
        {
            case "Maguro":
                Gizmos.color = Color.red;
                break;
            case "Tamago":
                Gizmos.color = Color.yellow;
                break;
            case "Salmon":
                Gizmos.color = new Color(1f, 0.5f, 0.3f);
                break;
            default:
                Gizmos.color = Color.white;
                break;
        }

        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);
    }
}