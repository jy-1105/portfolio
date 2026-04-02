using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 客を指定された席まで移動させ、到着後に着席と注文開始を行うクラス。
/// 移動状態から着席状態への遷移を管理する。
/// </summary>
public class CustomerSitting : MonoBehaviour
{
    [HideInInspector]
    public SeatPoint currentSeat;

    private SeatPoint targetSeat;
    private NavMeshAgent agent;
    private Animator anim;

    private bool isGoingToSeat = false;
    private bool isSitting = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

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
    /// 指定された席へ向かって移動を開始する。
    /// </summary>
    public void GoToSeat(SeatPoint seat)
    {
        if (seat == null) return;

        targetSeat = seat;
        targetSeat.isOccupied = true;

        isGoingToSeat = true;
        isSitting = false;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.SetDestination(targetSeat.transform.position);
        }

        if (anim != null)
        {
            anim.SetBool("IsWalking", true);
        }
    }

    void Update()
    {
        if (isGoingToSeat && !isSitting && agent != null && agent.enabled)
        {
            if (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                ArriveAndSit();
            }
        }
    }

    /// <summary>
    /// 席に到着した際に、位置補正・着席アニメーション・注文開始を行う。
    /// </summary>
    private void ArriveAndSit()
    {
        isGoingToSeat = false;
        isSitting = true;

        currentSeat = targetSeat;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        transform.position = currentSeat.transform.position;
        transform.rotation = currentSeat.transform.rotation;

        if (anim != null)
        {
            anim.SetBool("IsWalking", false);
            anim.SetTrigger("Sit");
        }

        CustomerOrderWithTimer orderScript = GetComponent<CustomerOrderWithTimer>();
        if (orderScript != null)
        {
            orderScript.ActivateOrder();
        }
    }

    /// <summary>
    /// 客が退店するときに席を空け、自身を削除する。
    /// </summary>
    public void Leave()
    {
        if (currentSeat != null)
        {
            currentSeat.isOccupied = false;
            currentSeat = null;
        }

        Destroy(gameObject);
    }
}