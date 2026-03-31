using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChargeGauge_Slider : MonoBehaviour
{
    public Slider chargeSlider;
    private bool isCharging = false;
    private bool isIncreasing = true;

    public float maxChargeTime = 3f;
    private float currentChargeTime = 0f;

    private bool canCharge = true;
    private float chargeCooldown = 1f;

    public AudioSource audioSource; // 効果音を再生するためのAudioSource
    public AudioClip chargeSound; // チャージ中に再生する効果音

    public float CurrentCharge => currentChargeTime / maxChargeTime; // 現在のチャージ量を0～1に正規化して返す

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && canCharge)
        {
            isCharging = true;

            // チャージ開始時に効果音を再生
            if (audioSource != null && chargeSound != null)
            {
                audioSource.clip = chargeSound;
                audioSource.loop = true; // チャージ中は効果音をループ再生する
                audioSource.Play();
            }
        }

        if (Input.GetMouseButton(0) && isCharging)
        {
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

            // 発射後にゲージを初期化
            if (chargeSlider != null)
                chargeSlider.value = 0f;

            // 効果音を停止
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

    // ゲージを初期化するメソッド
    public void ResetCharge()
    {
        currentChargeTime = 0f;
        if (chargeSlider != null)
        {
            chargeSlider.value = 0f;
        }
    }
}