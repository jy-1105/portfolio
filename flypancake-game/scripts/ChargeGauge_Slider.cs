using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// パワーゲージの増減処理とUI表示を管理するクラス。
/// チャージ量は一定時間ごとに増減を繰り返し、発射処理に利用される。
/// </summary>
public class ChargeGauge_Slider : MonoBehaviour
{
    public Slider chargeSlider;
    private bool isCharging = false;
    private bool isIncreasing = true;

    public float maxChargeTime = 3f;
    private float currentChargeTime = 0f;

    private bool canCharge = true;
    private float chargeCooldown = 1f;

    public AudioSource audioSource;
    public AudioClip chargeSound;

    public float CurrentCharge => currentChargeTime / maxChargeTime;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && canCharge)
        {
            isCharging = true;

            if (audioSource != null && chargeSound != null)
            {
                audioSource.clip = chargeSound;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        if (Input.GetMouseButton(0) && isCharging)
        {
            // チャージ量は最大値到達後に減少へ転じ、タイミング調整を必要とする設計にしている
            if (isIncreasing)
            {
                currentChargeTime += Time.deltaTime;
                if (currentChargeTime >= maxChargeTime)
                {
                    currentChargeTime = maxChargeTime;
                    isIncreasing = false;
                }
            }
            else
            {
                currentChargeTime -= Time.deltaTime;
                if (currentChargeTime <= 0f)
                {
                    currentChargeTime = 0f;
                    isIncreasing = true;
                }
            }

            if (chargeSlider != null)
                chargeSlider.value = currentChargeTime / maxChargeTime;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isCharging = false;

            if (chargeSlider != null)
                chargeSlider.value = 0f;

            if (audioSource != null)
            {
                audioSource.Stop();
            }

            StartCoroutine(ChargeCooldown());
        }
    }

    private IEnumerator ChargeCooldown()
    {
        canCharge = false;
        yield return new WaitForSeconds(chargeCooldown);
        canCharge = true;
    }

    /// <summary>
    /// 発射後にチャージ量とUI表示を初期状態へ戻す。
    /// </summary>
    public void ResetCharge()
    {
        currentChargeTime = 0f;
        if (chargeSlider != null)
        {
            chargeSlider.value = 0f;
        }
    }
}