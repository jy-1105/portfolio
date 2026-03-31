using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustomerSpawner : MonoBehaviour
{
    [Header("客のプレハブ（複数）")]
    [Tooltip("ここに複数の客Prefabを入れると、上から順番にスポーンします")]
    public GameObject[] customerPrefabs;

    [Header("生成時の効果音")]
    public AudioClip spawnSound;

    [Header("客のスポーン位置（入口など）")]
    public Transform spawnPoint;

    [Header("椅子 SeatPoint 一覧（1〜6）")]
    public SeatPoint[] seats;

    [Header("客の生成間隔（秒）")]
    public float spawnInterval = 30f;

    [Header("Difficulty Settings")]
    public float minSpawnInterval = 5f;
    public float difficultyIncreaseInterval = 20f;
    public float difficultyDecreaseAmount = 1f;

    // ✅ 次にスポーンするPrefabのインデックス（順番管理）
    private int nextPrefabIndex = 0;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private void Update()
    {
        // 難易度調整（今回は停止）
        /*
        timer += Time.deltaTime;
        if (timer >= difficultyIncreaseInterval)
        {
            timer = 0f;
            if (spawnInterval > minSpawnInterval)
            {
                spawnInterval -= difficultyDecreaseAmount;
                if (spawnInterval < minSpawnInterval) spawnInterval = minSpawnInterval;
                Debug.Log($"難易度アップ！生成間隔が {spawnInterval}秒 になりました");
            }
        }
        */
    }

    /// <summary>
    /// 一定間隔で客を生成するループ処理
    /// </summary>
    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            TrySpawnCustomer();
            Debug.Log($"[CustomerSpawner] 次の生成まで {spawnInterval} 秒待機します...");
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>
    /// 空いている席があれば客を1人生成して、その席に向かわせる
    /// </summary>
    private void TrySpawnCustomer()
    {
        // 0) 参照チェック
        if (spawnPoint == null)
        {
            Debug.LogError("[CustomerSpawner] spawnPoint が設定されていません。");
            return;
        }

        if (customerPrefabs == null || customerPrefabs.Length == 0)
        {
            Debug.LogError("[CustomerSpawner] customerPrefabs が空です。Inspectorに客Prefabを追加してください。");
            return;
        }

        // 1) 空いている席を探す
        SeatPoint freeSeat = GetFreeSeat();
        if (freeSeat == null)
        {
            Debug.Log("空いている椅子がないため、これ以上客を生成しません。");
            return;
        }

        // 2) ✅ 順番に客プレハブを選ぶ（nullがあってもスキップ）
        GameObject prefab = GetNextCustomerPrefabInOrder();
        if (prefab == null)
        {
            Debug.LogError("[CustomerSpawner] customerPrefabs に有効なPrefabがありません（全部nullの可能性）。");
            return;
        }

        // 3) スポーン位置のNavMesh上の正しい位置を探す
        Vector3 finalSpawnPos = spawnPoint.position;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(spawnPoint.position, out hit, 20.0f, NavMesh.AllAreas))
        {
            finalSpawnPos = hit.position;
        }
        else
        {
            Debug.LogError($"[CustomerSpawner] スポーン地点 ( {spawnPoint.position} ) の近く(20m以内)にNavMeshが見つかりません！床の上か、青いメッシュの近くに配置してください。");
            return;
        }

        // 4) 生成
        GameObject obj = Instantiate(prefab, finalSpawnPos, spawnPoint.rotation);

        // 効果音再生
        if (spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(spawnSound, finalSpawnPos);
        }

        // 5) 生成した客を目標の席に向かわせる
        CustomerSitting customer = obj.GetComponent<CustomerSitting>();
        if (customer != null)
        {
            NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(finalSpawnPos);

                if (!agent.isOnNavMesh)
                {
                    Debug.LogError("致命的エラー: 生成した客がNavMeshに乗っていません。NavMeshをBakeしてください！");
                    Destroy(obj);
                    return;
                }
            }

            customer.GoToSeat(freeSeat);
        }
        else
        {
            Debug.LogWarning("CustomerSitting スクリプトが客プレハブにアタッチされていません！");
        }
    }

    /// <summary>
    /// ✅ 配列の上から順番にPrefabを返す（nullは飛ばす）
    /// 末尾まで行ったら先頭に戻る（ループ）
    /// </summary>
    private GameObject GetNextCustomerPrefabInOrder()
    {
        if (customerPrefabs == null || customerPrefabs.Length == 0) return null;

        int checkedCount = 0;

        while (checkedCount < customerPrefabs.Length)
        {
            // 현재 인덱스의 프리팹
            GameObject prefab = customerPrefabs[nextPrefabIndex];

            // 다음번을 위해 인덱스 증가(루프)
            nextPrefabIndex = (nextPrefabIndex + 1) % customerPrefabs.Length;

            // null이 아니면 반환
            if (prefab != null)
                return prefab;

            checkedCount++;
        }

        // 전부 null이면 null
        return null;
    }

    /// <summary>
    /// 配列 seats の中から「未使用の席」を1つ返す。なければ null。
    /// </summary>
    private SeatPoint GetFreeSeat()
    {
        if (seats == null) return null;

        foreach (var seat in seats)
        {
            if (seat != null && !seat.isOccupied)
            {
                return seat;
            }
        }
        return null;
    }
}
