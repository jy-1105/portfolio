﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 客を一定間隔で生成し、空席を判定して適切な席へ誘導するクラス。
/// スポーン位置の補正や座席管理も担当する。
/// </summary>
public class CustomerSpawner : MonoBehaviour
{
    [Header("客のプレハブ（複数）")]
    [Tooltip("複数の客Prefabを設定すると、上から順番にスポーンする")]
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

    private int nextPrefabIndex = 0;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private void Update()
    {
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
    /// 一定間隔で客の生成を試みるループ処理。
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
    /// 空席がある場合に客を生成し、対象の席へ移動させる。
    /// </summary>
    private void TrySpawnCustomer()
    {
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

        SeatPoint freeSeat = GetFreeSeat();
        if (freeSeat == null)
        {
            Debug.Log("空いている椅子がないため、これ以上客を生成しません。");
            return;
        }

        GameObject prefab = GetNextCustomerPrefabInOrder();
        if (prefab == null)
        {
            Debug.LogError("[CustomerSpawner] customerPrefabs に有効なPrefabがありません。");
            return;
        }

        Vector3 finalSpawnPos = spawnPoint.position;
        NavMeshHit hit;

        // スポーン直後に移動できるよう、NavMesh上の有効な位置へ補正する
        if (NavMesh.SamplePosition(spawnPoint.position, out hit, 20.0f, NavMesh.AllAreas))
        {
            finalSpawnPos = hit.position;
        }
        else
        {
            Debug.LogError($"[CustomerSpawner] スポーン地点 ({spawnPoint.position}) の近くにNavMeshが見つかりません。");
            return;
        }

        GameObject obj = Instantiate(prefab, finalSpawnPos, spawnPoint.rotation);

        if (spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(spawnSound, finalSpawnPos);
        }

        CustomerSitting customer = obj.GetComponent<CustomerSitting>();
        if (customer != null)
        {
            NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(finalSpawnPos);

                if (!agent.isOnNavMesh)
                {
                    Debug.LogError("生成した客がNavMeshに乗っていません。NavMesh設定を確認してください。");
                    Destroy(obj);
                    return;
                }
            }

            customer.GoToSeat(freeSeat);
        }
        else
        {
            Debug.LogWarning("CustomerSitting スクリプトが客プレハブにアタッチされていません。");
        }
    }

    /// <summary>
    /// 配列の先頭から順番に有効な客Prefabを取得する。
    /// </summary>
    private GameObject GetNextCustomerPrefabInOrder()
    {
        if (customerPrefabs == null || customerPrefabs.Length == 0) return null;

        int checkedCount = 0;

        while (checkedCount < customerPrefabs.Length)
        {
            GameObject prefab = customerPrefabs[nextPrefabIndex];
            nextPrefabIndex = (nextPrefabIndex + 1) % customerPrefabs.Length;

            if (prefab != null)
                return prefab;

            checkedCount++;
        }

        return null;
    }

    /// <summary>
    /// 未使用の席を1つ取得する。空席がなければ null を返す。
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