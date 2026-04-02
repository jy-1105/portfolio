using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 客ごとの注文内容、制限時間、UI表示、正誤判定、スコア反映を管理するクラス。
/// 注文の生成から結果処理まで一連の流れを担当する。
/// </summary>
public class CustomerOrderWithTimer : MonoBehaviour
{
    [Header("注文設定")]
    public string[] possibleSushiTypes = { "Maguro", "Tamago", "Salmon" };
    public float timeLimit = 45f;
    public float nextOrderDelay = 1.0f;

    private string currentRequestedSushi;
    private float remainingTime;
    private bool isOrderActive = false;

    [Header("UI（頭上のキャンバス）")]
    public GameObject orderCanvas;
    public TMP_Text orderText;
    public Image orderImage;
    public TMP_Text timerText;

    [Header("⏱ タイムゲージ（Slider）")]
    public Slider timeSlider;
    public Image timeSliderFill;
    public Color greenColor = Color.green;
    public Color yellowColor = Color.yellow;
    public Color redColor = Color.red;
    [Range(0f, 1f)] public float yellowThreshold = 0.6f;
    [Range(0f, 1f)] public float redThreshold = 0.3f;

    [Header("寿司アイコン画像")]
    public Sprite maguroSprite;
    public Sprite tamagoSprite;
    public Sprite salmonSprite;

    [Header("注文ボイス")]
    public AudioClip maguroSound;
    public AudioClip tamagoSound;
    public AudioClip salmonSound;

    [Header("リアクション（任意）")]
    public GameObject correctEffect;
    public GameObject wrongEffect;
    public AudioClip[] correctSounds;
    public AudioClip[] wrongSounds;
    public AudioClip[] timeoutSounds;
    public Animator animator;
    public string correctTrigger = "Happy";
    public string wrongTrigger = "Sad";

    [Header("スコア設定")]
    [Tooltip("正解時の獲得スコア")]
    public int correctScore = 100;
    [Tooltip("不正解時の減点スコア（正の値で入力）")]
    public int wrongScore = 10;
    [Tooltip("タイムアウト時の減点スコア（正の値で入力）")]
    public int timeoutScore = 10;

    void Start()
    {
        if (orderCanvas != null) orderCanvas.SetActive(false);

        if (timeSlider != null)
        {
            timeSlider.minValue = 0f;
            timeSlider.maxValue = 1f;
            timeSlider.value = 1f;
        }
    }

    public void ActivateOrder()
    {
        if (!isOrderActive)
            StartNewOrder();
    }

    void Update()
    {
        if (!isOrderActive) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            OnTimeout();
        }

        UpdateTimerUI();
        UpdateTimeSlider();
    }

    public bool WantsSushi(string sushiType)
    {
        if (!isOrderActive) return false;
        return currentRequestedSushi == sushiType;
    }

    public void ReceiveSushi(string sushiType, bool isCorrect)
    {
        if (!isOrderActive) return;

        if (isCorrect) OnReceiveCorrectSushi(null);
        else OnReceiveWrongSushi(null);
    }

    /// <summary>
    /// 新しい注文を生成し、UIとタイマーを初期化する。
    /// </summary>
    void StartNewOrder()
    {
        int rand = Random.Range(0, possibleSushiTypes.Length);
        currentRequestedSushi = possibleSushiTypes[rand];

        remainingTime = timeLimit;
        isOrderActive = true;

        if (orderCanvas != null) orderCanvas.SetActive(true);
        if (orderText != null) orderText.text = currentRequestedSushi;

        UpdateOrderImage();
        UpdateTimerUI();

        if (timeSlider != null)
        {
            timeSlider.value = 1f;
            UpdateTimeSliderColor(1f);
        }

        PlayOrderSound(currentRequestedSushi);
    }

    void PlayOrderSound(string sushiType)
    {
        AudioClip clip = null;
        switch (sushiType)
        {
            case "Maguro": clip = maguroSound; break;
            case "Tamago": clip = tamagoSound; break;
            case "Salmon": clip = salmonSound; break;
        }

        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    void UpdateOrderImage()
    {
        if (orderImage == null) return;

        Sprite sprite = null;
        switch (currentRequestedSushi)
        {
            case "Maguro": sprite = maguroSprite; break;
            case "Tamago": sprite = tamagoSprite; break;
            case "Salmon": sprite = salmonSprite; break;
        }

        orderImage.sprite = sprite;
        orderImage.enabled = (sprite != null);

        if (orderText != null)
            orderText.enabled = (sprite == null);
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        int sec = Mathf.CeilToInt(remainingTime);
        timerText.text = $"{sec}s";
    }

    void UpdateTimeSlider()
    {
        if (timeSlider == null) return;

        float ratio = Mathf.Clamp01(remainingTime / timeLimit);
        timeSlider.value = ratio;
        UpdateTimeSliderColor(ratio);
    }

    /// <summary>
    /// 残り時間に応じてゲージ色を変更し、時間切れの危険度を視覚的に伝える。
    /// </summary>
    void UpdateTimeSliderColor(float ratio)
    {
        if (timeSliderFill == null) return;

        if (ratio <= redThreshold)
            timeSliderFill.color = redColor;
        else if (ratio <= yellowThreshold)
            timeSliderFill.color = yellowColor;
        else
            timeSliderFill.color = greenColor;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isOrderActive) return;

        SushiType sushi = other.GetComponent<SushiType>();
        if (sushi == null) return;

        if (sushi.GetSushiType() == currentRequestedSushi)
            OnReceiveCorrectSushi(other.gameObject);
        else
            OnReceiveWrongSushi(other.gameObject);
    }

    /// <summary>
    /// 正しい寿司が提供された際の演出とスコア加算を行う。
    /// </summary>
    void OnReceiveCorrectSushi(GameObject sushiObj)
    {
        isOrderActive = false;
        if (orderCanvas != null) orderCanvas.SetActive(false);

        if (correctEffect != null)
        {
            var fx = Instantiate(correctEffect, transform.position + Vector3.up * 2f, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (correctSounds != null && correctSounds.Length > 0)
            AudioSource.PlayClipAtPoint(correctSounds[Random.Range(0, correctSounds.Length)], transform.position);

        if (animator != null && !string.IsNullOrEmpty(correctTrigger))
            animator.SetTrigger(correctTrigger);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(correctScore);
            ScoreManager.Instance.servedCount++;

            float duration = timeLimit - remainingTime;
            ScoreManager.Instance.totalServiceTime += duration;
        }

        StartCoroutine(StartNextOrderAfterDelay());
    }

    /// <summary>
    /// 間違った寿司が提供された際の演出と減点処理を行う。
    /// </summary>
    void OnReceiveWrongSushi(GameObject sushiObj)
    {
        if (wrongEffect != null)
        {
            var fx = Instantiate(wrongEffect, transform.position + Vector3.up * 2f, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (wrongSounds != null && wrongSounds.Length > 0)
            AudioSource.PlayClipAtPoint(wrongSounds[Random.Range(0, wrongSounds.Length)], transform.position);

        if (animator != null && !string.IsNullOrEmpty(wrongTrigger))
            animator.SetTrigger(wrongTrigger);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(-wrongScore);
            ScoreManager.Instance.wrongCount++;
        }
    }

    /// <summary>
    /// 制限時間切れ時の失敗演出と減点処理を行う。
    /// </summary>
    void OnTimeout()
    {
        if (!isOrderActive) return;

        isOrderActive = false;
        if (orderCanvas != null) orderCanvas.SetActive(false);

        if (wrongEffect != null)
        {
            var fx = Instantiate(wrongEffect, transform.position + Vector3.up * 2f, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (timeoutSounds != null && timeoutSounds.Length > 0)
            AudioSource.PlayClipAtPoint(timeoutSounds[Random.Range(0, timeoutSounds.Length)], transform.position);

        if (animator != null && !string.IsNullOrEmpty(wrongTrigger))
            animator.SetTrigger(wrongTrigger);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(-timeoutScore);
            ScoreManager.Instance.missedCount++;
        }

        StartCoroutine(StartNextOrderAfterDelay());
    }

    IEnumerator StartNextOrderAfterDelay()
    {
        yield return new WaitForSeconds(nextOrderDelay);
        StartNewOrder();
    }
}