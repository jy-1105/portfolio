using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Mousecontroll3 : MonoBehaviour
{
    public float holdTime = 0f; // 発射までのチャージ時間
    public ChargeGauge_Slider chargeGaugeSlider;
    private float maxPower = 1500f; // 最大の発射力
    public float moveSpeed = 5f; // 移動速度
    private bool isHolding = false; // マウスボタンを押しているかどうか
    private Rigidbody rb; // Rigidbodyコンポーネント
    private Vector3 aimDirection; // 照準方向
    private pausemenucontroller menuController; // メニューコントローラー
    public AudioClip whooshSound;
    private AudioSource audioSource;

    private bool canShoot = true; // 発射可能かどうかを管理する変数
    public float minSpeedToShoot = 1.5f; // 発射可能とみなす最小速度

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // 初期状態では物理演算の影響を受けないように設定

        menuController = FindFirstObjectByType<pausemenucontroller>(); // メニューコントローラーを取得
        SetCursorState(false); // カーソル状態を設定

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>(); // AudioSourceを追加
    }

    void Update()
    {
        // メニューが開いている間は発射しない
        if (menuController != null && menuController.menuPanel.activeSelf)
        {
            return; // メニューが開いている場合は処理を中断
        }

        if (Camera.main != null)
        {
            // 照準方向を更新
            aimDirection = Camera.main.transform.forward.normalized;
        }

        // マウスボタンを押し始めたとき
        if (Input.GetMouseButtonDown(0) && canShoot && rb.velocity.magnitude < minSpeedToShoot)
        {
            isHolding = true;
            holdTime = 0f; // チャージ時間を初期化
        }

        // マウスボタンを押している間
        if (Input.GetMouseButton(0) && isHolding && rb.velocity.magnitude < minSpeedToShoot)
        {
            holdTime += Time.deltaTime * 2f; // チャージ時間を増加
        }

        // マウスボタンを離したとき
        if (Input.GetMouseButtonUp(0) && canShoot && rb.velocity.magnitude < minSpeedToShoot)
        {
            isHolding = false;
            float chargeFactor = chargeGaugeSlider != null ? chargeGaugeSlider.CurrentCharge : 1f; // ゲージの割合を取得
            if (chargeFactor > 0) // チャージが0のときは発射しない
            {
                Shoot(chargeFactor); // ゲージ値を反映して発射
                PlayWhooshSound();
            }

            // 発射後、少し待ってから再び発射可能にする
            canShoot = false;
            Invoke(nameof(ResetShoot), 0.2f); // 0.2秒後に発射可能へ戻す

            // 発射後にゲージを初期化
            if (chargeGaugeSlider != null)
            {
                chargeGaugeSlider.ResetCharge(); // ChargeGauge_Slider側の初期化処理を呼ぶ
            }
        }
    }

    public void Shoot(float chargeFactor)
    {
        // 発射力を計算
        float power = Mathf.Clamp(chargeFactor * maxPower, 0f, maxPower);

        // 発射速度を計算
        Vector3 launchVelocity = aimDirection * power * 0.5f;
        launchVelocity.y += power * 0.25f;

        rb.isKinematic = false;
        rb.AddForce(launchVelocity);
    }

    private void ResetShoot()
    {
        canShoot = true; // 再び発射可能な状態に戻す
    }

    // カーソル状態を設定
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