using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// マウス入力とチャージゲージを用いて、パンケーキの発射処理を行うクラス。
/// ゲージ量に応じて発射力を変化させ、物理挙動による飛距離と軌道を制御する。
/// </summary>
public class MouseControlledShooter3 : MonoBehaviour
{
    public float holdTime = 0f;
    public ChargeGauge_Slider chargeGaugeSlider;
    private float maxPower = 1500f;
    public float moveSpeed = 5f;
    private bool isHolding = false;
    private Rigidbody rb;
    private Vector3 aimDirection;
    private pausemenucontroller menuController;
    public AudioClip whooshSound;
    private AudioSource audioSource;

    private bool canShoot = true;
    public float minSpeedToShoot = 1.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        menuController = FindFirstObjectByType<pausemenucontroller>();
        SetCursorState(false);

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (menuController != null && menuController.menuPanel.activeSelf)
        {
            return;
        }

        if (Camera.main != null)
        {
            aimDirection = Camera.main.transform.forward.normalized;
        }

        if (Input.GetMouseButtonDown(0) && canShoot && rb.velocity.magnitude < minSpeedToShoot)
        {
            isHolding = true;
            holdTime = 0f;
        }

        if (Input.GetMouseButton(0) && isHolding && rb.velocity.magnitude < minSpeedToShoot)
        {
            holdTime += Time.deltaTime * 2f;
        }

        if (Input.GetMouseButtonUp(0) && canShoot && rb.velocity.magnitude < minSpeedToShoot)
        {
            isHolding = false;
            float chargeFactor = chargeGaugeSlider != null ? chargeGaugeSlider.CurrentCharge : 1f;

            if (chargeFactor > 0)
            {
                Shoot(chargeFactor);
                PlayWhooshSound();
            }

            // 連続発射を防ぐため、短時間だけ再発射を無効化する
            canShoot = false;
            Invoke(nameof(ResetShoot), 0.2f);

            if (chargeGaugeSlider != null)
            {
                chargeGaugeSlider.ResetCharge();
            }
        }
    }

    /// <summary>
    /// 現在のチャージ量に応じて発射力を計算し、パンケーキを飛ばす。
    /// </summary>
    public void Shoot(float chargeFactor)
    {
        float power = Mathf.Clamp(chargeFactor * maxPower, 0f, maxPower);

        Vector3 launchVelocity = aimDirection * power * 0.5f;
        launchVelocity.y += power * 0.25f;

        rb.isKinematic = false;
        rb.AddForce(launchVelocity);
    }

    /// <summary>
    /// 一定時間後に再び発射可能な状態へ戻す。
    /// </summary>
    private void ResetShoot()
    {
        canShoot = true;
    }

    private void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
        {
            SceneManager.LoadScene("Eita Boxroom3");
        }
    }

    private void PlayWhooshSound()
    {
        if (whooshSound != null)
        {
            audioSource.PlayOneShot(whooshSound);
        }
        else
        {
            Debug.LogWarning("Whoosh sound not assigned!");
        }
    }
}