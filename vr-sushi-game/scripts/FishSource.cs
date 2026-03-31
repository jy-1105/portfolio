using UnityEngine;
using Valve.VR.InteractionSystem;

/// <summary>
/// 魚ネタの元 - 掴んだら魚ネタを右手に生成する
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

        // 掴んだときのイベントに登録
        interactable.onAttachedToHand += OnAttachedToHand;

        // オブジェクト名に魚の種類を追加
        name = $"FishSource ({fishTypeName})";
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.onAttachedToHand -= OnAttachedToHand;
        }
    }

    // ---------------------------------------------------------
    // 変更: Updateで毎フレーム監視する（RiceContainerと同じ方式）
    // ---------------------------------------------------------
    void Update()
    {
        // ホバー中の手を取得
        if (interactable != null && interactable.hoveringHand != null)
        {
            Hand hand = interactable.hoveringHand;

            // 掴む操作（Grip or Trigger）が開始されたかチェック
            GrabTypes startingGrabType = hand.GetGrabStarting();

            if (startingGrabType != GrabTypes.None)
            {
                Debug.Log($"[{name}] Grab detected in Update! Type: {startingGrabType}. Spawning Fish...");
                
                // 元オブジェクト自体をアタッチするのではなく、直接魚ネタを生成して持たせる
                SpawnFish(hand, startingGrabType);
            }
        }
    }

    private void SpawnFish(Hand hand, GrabTypes grabType)
    {
        // クールダウンチェック
        if (Time.time - lastSpawnTime < cooldownTime)
        {
            Debug.Log("魚ネタ生成のクールダウン中です");
            return;
        }

        // 魚ネタのPrefabが設定されているか確認
        if (fishPrefab == null)
        {
            Debug.LogError("魚ネタのPrefabが設定されていません！");
            return;
        }

        // 魚ネタを生成する位置（手の位置 + オフセット）
        Vector3 spawnPosition = hand.transform.position + hand.transform.TransformDirection(fishSpawnOffset);
        Quaternion spawnRotation = hand.transform.rotation;

        // 魚ネタを生成
        GameObject newFish = Instantiate(fishPrefab, spawnPosition, spawnRotation);

        // 元オブジェクトと魚ネタの衝突を無視する
        Collider sourceCollider = GetComponent<Collider>();
        Collider fishCollider = newFish.GetComponent<Collider>();
        if (sourceCollider != null && fishCollider != null)
        {
            Physics.IgnoreCollision(sourceCollider, fishCollider);
        }

        Debug.Log($"{fishTypeName}ネタを{hand.name}に生成しました");

        // 効果音を再生
        if (fishGrabSound != null)
        {
            AudioSource.PlayClipAtPoint(fishGrabSound, spawnPosition);
        }

        // 生成した魚ネタを手にアタッチ
        hand.AttachObject(newFish, grabType);

        // クールダウン時間を記録
        lastSpawnTime = Time.time;
    }

    // OnAttachedToHandはもう使わないが、念のため空にして残しておく
    private void OnAttachedToHand(Hand hand)
    {
        // 処理なし
    }

    // エディタでの視覚化
    void OnDrawGizmos()
    {
        // 魚の種類に応じて色を変える
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

        // 頭上にアイコン表示
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);
    }
}
