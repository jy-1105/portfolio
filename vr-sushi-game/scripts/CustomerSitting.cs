using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustomerSitting : MonoBehaviour
{
    [HideInInspector]
    public SeatPoint currentSeat;   // 現在座っている席

    private SeatPoint targetSeat;   // 移動先となる目標の席
    private NavMeshAgent agent;
    private Animator anim;

    private bool isGoingToSeat = false; // 席へ移動中かどうか
    private bool isSitting = false;     // すでに座ったかどうか

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();   // Animator がルートではなく子オブジェクトにある場合に取得

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
            agent.updateRotation = true; // 移動方向に合わせて自動回転させる
        }
    }

    /// <summary>
    /// 指定された席へ向かって移動を開始する
    /// </summary>
    public void GoToSeat(SeatPoint seat)
    {
        if (seat == null) return;

        targetSeat = seat;
        targetSeat.isOccupied = true;  // この席を使用中にする

        isGoingToSeat = true;
        isSitting = false;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.SetDestination(targetSeat.transform.position); // 目標の席へ移動開始
        }

        // 歩行アニメーションを再生
        if (anim != null)
        {
            anim.SetBool("IsWalking", true);  // Animator のパラメーター名と一致している必要がある
        }
    }

    void Update()
    {
        // 席へ移動中で、まだ座っていない場合のみ到着判定を行う
        if (isGoingToSeat && !isSitting && agent != null && agent.enabled)
        {
            // NavMeshAgent が目的地付近まで到達したか確認
            if (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                ArriveAndSit();
            }
        }
    }

    /// <summary>
    /// 席に到着したときの処理
    /// </summary>
    private void ArriveAndSit()
    {
        isGoingToSeat = false;
        isSitting = true;

        currentSeat = targetSeat;   // 現在座っている席として保存

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;  // 到着後は移動しないため無効化
        }

        // キャラクターの位置と向きを席にぴったり合わせる
        transform.position = currentSeat.transform.position;
        transform.rotation = currentSeat.transform.rotation;  // 必要なら 180 度回転させる形に変更可能

        if (anim != null)
        {
            anim.SetBool("IsWalking", false);  // 歩行アニメーションを停止
            anim.SetTrigger("Sit");            // 座るアニメーションを再生
            anim.SetBool("IsWalking", false);  // 歩行アニメーションを停止
            anim.SetTrigger("Sit");            // 座るアニメーションを再生
        }

        // 着席後に注文処理を開始
        CustomerOrderWithTimer orderScript = GetComponent<CustomerOrderWithTimer>();
        if (orderScript != null)
        {
            orderScript.ActivateOrder();
        }
    }

    /// <summary>
    /// 客が退店するときの処理
    /// 席を空席に戻し、自身を削除する
    /// </summary>
    public void Leave()
    {
        if (currentSeat != null)
        {
            currentSeat.isOccupied = false;  // 席を空席状態に戻す
            currentSeat = null;
        }

        Destroy(gameObject);  // 客オブジェクトを削除
    }
}