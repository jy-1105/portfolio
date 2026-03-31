using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustomerSitting : MonoBehaviour
{
    [HideInInspector]
    public SeatPoint currentSeat;   // 現在座っている席

    private SeatPoint targetSeat;   // 目標となる席（移動先）
    private NavMeshAgent agent;
    private Animator anim;

    private bool isGoingToSeat = false; // 席に向かって移動中かどうか
    private bool isSitting = false;     // 既に座ったかどうか

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();   // 🔥 이거여야 함 (루트에 없고 자식에 있을 때)

        if (anim == null)
        {
            Debug.LogError("CustomerSitting: Animator が見つかりません。");
        }
        else
        {
            anim.applyRootMotion = false;
            Debug.Log("CustomerSitting: Animator OK, controller = " + anim.runtimeAnimatorController?.name);
        }

        if (agent != null)
        {
            agent.updateRotation = true;
        }
    }

    /// <summary>
    /// 指定された Seat に向かって歩き始める処理
    /// </summary>
    public void GoToSeat(SeatPoint seat)
    {
        if (seat == null) return;

        targetSeat = seat;
        targetSeat.isOccupied = true;  // 席を「使用中」状態にする

        isGoingToSeat = true;
        isSitting = false;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.SetDestination(targetSeat.transform.position); // Seat の位置へ移動開始
        }

        // 歩きアニメーション開始
        if (anim != null)
        {
            anim.SetBool("IsWalking", true);  // ← Animator パラメーター名と完全一致が必要
        }
    }

    void Update()
    {
        // 席に向かって移動中 & まだ座っていない状態
        if (isGoingToSeat && !isSitting && agent != null && agent.enabled)
        {
            // NavMeshAgent が目標地点にほぼ到達したかどうか
            if (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                ArriveAndSit();
            }
        }
    }

    /// <summary>
    /// 席に到着した時に実行される処理（座る動作）
    /// </summary>
    private void ArriveAndSit()
    {
        isGoingToSeat = false;
        isSitting = true;

        currentSeat = targetSeat;   // 最終的に座っている席として記録

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;  // もう移動しないため Agent を無効化
        }

        // 座る位置・向きを Seat と完全一致させる
        transform.position = currentSeat.transform.position;
        transform.rotation = currentSeat.transform.rotation;  // 必要に応じて 180 度回転版に変更可

        if (anim != null)
        {
            anim.SetBool("IsWalking", false);  // 歩きアニメーション停止
            anim.SetTrigger("Sit");            // 座るアニメーション再生
            anim.SetBool("IsWalking", false);  // 歩きアニメーション停止
            anim.SetTrigger("Sit");            // 座るアニメーション再生
        }

        // 席についたので注文を開始！
        CustomerOrderWithTimer orderScript = GetComponent<CustomerOrderWithTimer>();
        if (orderScript != null)
        {
            orderScript.ActivateOrder();
        }
    }

    /// <summary>
    /// 客が退店する時の処理（席を空けて、オブジェクトを削除）
    /// </summary>
    public void Leave()
    {
        if (currentSeat != null)
        {
            currentSeat.isOccupied = false;  // 席を空席状態に戻す
            currentSeat = null;
        }

        Destroy(gameObject);  // 客オブジェクトの削除
    }
}
