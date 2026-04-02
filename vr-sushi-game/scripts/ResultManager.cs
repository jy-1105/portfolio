using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class ResultManager : MonoBehaviour
{
    [Header("UI References")]
    public Image rankImage; // Dynamic Rank Text Image (e.g. "Taisho")
    public Image rankStaticImage; // Static Background Image (Initial)
    public RawImage replayImage; // Replay
    public TMPro.TMP_Text scoreText; // Score Text

    [Header("Rank Sprites (Text)")]
    public Sprite rankS_Text;
    public Sprite rankA_Text;
    public Sprite rankB_Text;
    public Sprite rankC_Text;
    public Sprite rankD_Text;
    public Sprite rankE_Text;

    [Header("Rank Sprites (Static Background)")]
    public Sprite rankS_Static;
    public Sprite rankA_Static;
    public Sprite rankB_Static;
    public Sprite rankC_Static;
    public Sprite rankD_Static;
    public Sprite rankE_Static;

    [Header("Rank Videos")]
    public VideoClip rankSVideo;
    public VideoClip rankAVideo;
    public VideoClip rankBVideo;
    public VideoClip rankCVideo;
    public VideoClip rankDVideo;
    public VideoClip rankEVideo;

    [Header("Video Player")]
    public VideoPlayer rankVideoPlayer;

    [Header("Replay Settings")]
    public float frameRate = 0.1f; // 10FPSで再生

    [Header("Scene Settings")]
    public string titleSceneName = "TitleScene";

    [Header("Result Images")]
    [Tooltip("リザルト画像のリスト（0: 通常リザルト, 1: タイトルに戻りますか？）")]
    public List<Sprite> resultImages; // 0=通常, 1=確認画面

    [Tooltip("リザルト画像を表示するUI Image")]
    public Image resultDisplayImage;

    [Header("Spawn Settings")]
    public Transform spawnPoint; // プレイヤーの強制移動先

    [Header("Stats UI")]
    public TMPro.TMP_Text servedCountText;    // 提供回数
    public TMPro.TMP_Text wrongCountText;     // ミス回数
    public TMPro.TMP_Text missedCountText;    // 提供失敗回数
    public TMPro.TMP_Text totalAngryTimeText; // クレーマー滞在時間
    public TMPro.TMP_Text avgServiceTimeText; // 平均提供時間

    [Header("Rank Sounds")]
    public AudioClip rankSSound;
    public AudioClip rankASound;
    public AudioClip rankBSound;
    public AudioClip rankCSound;
    public AudioClip rankDSound;
    public AudioClip rankESound;

    [Header("Audio")]
    public AudioClip drumRollSound;       // ドラムロール（スコア集計中）
    public AudioClip scoreFinishSound;    // 「ジャン！」（スコア決定、統計開始前）
    public AudioClip popSound;            // 「ポン」（統計の各項目表示）
    public AudioClip rankAppearSound;     // 「バン！」（予備/共通のランク表示音）
    public AudioClip resultCompleteSound; // 「じゃじゃーん！」（全演出終了）

    [Header("Special Effect")]
    public Image oyakataImage; // 互換性のため残すが、現在はRankImageを使用（使用しないならNoneでOK）

    [Header("Continuous Effects")]
    [Tooltip("リザルトシーンでずっと再生するエフェクト1のPrefab")]
    public GameObject continuousEffect1Prefab;

    [Tooltip("エフェクト1の配置位置")]
    public Transform effect1SpawnPoint;

    [Tooltip("リザルトシーンでずっと再生するエフェクト2のPrefab")]
    public GameObject continuousEffect2Prefab;

    [Tooltip("エフェクト2の配置位置")]
    public Transform effect2SpawnPoint;

    private AudioSource audioSource;
    private Dictionary<TMPro.TMP_Text, Coroutine> spinCoroutines = new Dictionary<TMPro.TMP_Text, Coroutine>();
    private int currentResultIndex = 0; // 0=通常リザルト, 1=確認画面
    private GameObject effect1Instance; // 生成されたエフェクト1のインスタンス
    private GameObject effect2Instance; // 生成されたエフェクト2のインスタンス

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        // カーソルを表示
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 初期状態は通常リザルト画面
        currentResultIndex = 0;
        UpdateResultImage();

        // 全てのテキストを「ぐるぐる」開始（表示ONにする）
        // 0000が変わるように、という要望に応えるためランダム表示ループを開始
        StartSpinning(servedCountText);
        StartSpinning(wrongCountText);
        StartSpinning(missedCountText);
        StartSpinning(totalAngryTimeText);
        StartSpinning(avgServiceTimeText);
        StartSpinning(scoreText);

        // 継続エフェクトを再生開始
        StartContinuousEffects();

        MovePlayerAndReset();
        StartCoroutine(ShowResultSequence());
        StartCoroutine(PlayDigest());
    }

    void MovePlayerAndReset()
    {
        if (spawnPoint == null) return;

        // 重複したプレイヤーを削除（DontDestroyOnLoadで残っている可能性）
        RemoveDuplicatePlayers();

        // プレイヤー（CameraRig等）を探して移動
        GameObject player = FindPlayer();

        if (player != null)
        {
            // 位置を強制移動
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
            Debug.Log($"Player moved to Result Spawn Point: {spawnPoint.position}");
        }
        else
        {
            Debug.LogWarning("Player not found for Result Spawn!");
        }
    }

    /// <summary>
    /// プレイヤーオブジェクトを探す
    /// </summary>
    GameObject FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null) player = GameObject.Find("[CameraRig]");
        if (player == null) player = GameObject.Find("XR Origin");
        return player;
    }

    /// <summary>
    /// 重複したプレイヤーを削除（1つだけ残す）
    /// </summary>
    void RemoveDuplicatePlayers()
    {
        // すべてのプレイヤーを探す
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        // Tagで見つからない場合は名前で探す
        if (players.Length == 0)
        {
            GameObject player1 = GameObject.Find("Player");
            GameObject player2 = GameObject.Find("[CameraRig]");
            GameObject player3 = GameObject.Find("XR Origin");

            // 見つかったものをリストアップ
            List<GameObject> foundPlayers = new List<GameObject>();
            if (player1 != null) foundPlayers.Add(player1);
            if (player2 != null) foundPlayers.Add(player2);
            if (player3 != null) foundPlayers.Add(player3);

            players = foundPlayers.ToArray();
        }

        // 2つ以上ある場合、最初の1つ以外を削除
        if (players.Length > 1)
        {
            Debug.LogWarning($"Found {players.Length} players in Result Scene! Removing duplicates...");

            for (int i = 1; i < players.Length; i++)
            {
                Debug.Log($"Destroying duplicate player: {players[i].name}");
                Destroy(players[i]);
            }
        }
    }

    IEnumerator ShowResultSequence()
    {
        if (ScoreManager.Instance == null) yield break;

        // 0. 初期設定：ランク判定と静止画表示
        int score = ScoreManager.Instance.GetCurrentScore();
        Sprite currentRankText = rankE_Text;
        Sprite currentRankStatic = rankE_Static;
        VideoClip currentRankVideo = rankEVideo;
        AudioClip currentRankSound = rankESound; // デフォルト音

        if (score >= ScoreManager.Instance.rankS) 
        {
            currentRankText = rankS_Text;
            currentRankStatic = rankS_Static;
            currentRankVideo = rankSVideo;
            currentRankSound = rankSSound;
        }
        else if (score >= ScoreManager.Instance.rankA) 
        {
            currentRankText = rankA_Text;
            currentRankStatic = rankA_Static;
            currentRankVideo = rankAVideo;
            currentRankSound = rankASound;
        }
        else if (score >= ScoreManager.Instance.rankB) 
        {
            currentRankText = rankB_Text;
            currentRankStatic = rankB_Static;
            currentRankVideo = rankBVideo;
            currentRankSound = rankBSound;
        }
        else if (score >= ScoreManager.Instance.rankC) 
        {
            currentRankText = rankC_Text;
            currentRankStatic = rankC_Static;
            currentRankVideo = rankCVideo;
            currentRankSound = rankCSound;
        }
        else if (score >= ScoreManager.Instance.rankD) 
        {
            currentRankText = rankD_Text;
            currentRankStatic = rankD_Static;
            currentRankVideo = rankDVideo;
            currentRankSound = rankDSound;
        }
        
        // 静止画（背景）を表示
        if (rankStaticImage != null)
        {
            rankStaticImage.sprite = currentRankStatic;
            rankStaticImage.gameObject.SetActive(true);
        }

        // テキスト画像は隠しておく
        if (rankImage != null)
        {
            rankImage.sprite = currentRankText;
            rankImage.gameObject.SetActive(false);
        }

        // ドラムロール音再生（全体が回っている間鳴らす）
        if (drumRollSound != null && audioSource != null)
        {
            audioSource.Stop(); // 重なり防止
            audioSource.clip = drumRollSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        yield return new WaitForSeconds(2.0f); // 最初の「全部ぐるぐる」を見せる時間

        // 1. 各統計を順番に止めて確定表示（上から順に）
        
        // 提供回数
        StopSpinning(servedCountText, $"{ScoreManager.Instance.servedCount}");
        PlayPopSound();
        yield return new WaitForSeconds(0.4f); // テンポよく
        
        // ミス回数
        StopSpinning(wrongCountText, $"{ScoreManager.Instance.wrongCount}");
        PlayPopSound();
        yield return new WaitForSeconds(0.4f);

        // クレーマー滞在時間
        StopSpinning(totalAngryTimeText, $"{ScoreManager.Instance.totalAngryTime:F1}");
        PlayPopSound();
        yield return new WaitForSeconds(0.4f);

        // 提供失敗回数
        StopSpinning(missedCountText, $"{ScoreManager.Instance.missedCount}");
        PlayPopSound();
        yield return new WaitForSeconds(0.4f);
        
        // 平均提供時間
        float avgTime = ScoreManager.Instance.servedCount > 0 
            ? ScoreManager.Instance.totalServiceTime / ScoreManager.Instance.servedCount 
            : 0f;
        StopSpinning(avgServiceTimeText, $"{avgTime:F1}");
        PlayPopSound();
        yield return new WaitForSeconds(1.0f); // スコア前のタメ

        // 2. スコア確定 & ランク演出（テキストドーン！＆動画再生＆背景Off）
        StopSpinning(scoreText, $"{score}");

        // ドラムロール停止
        if (audioSource != null && audioSource.clip == drumRollSound)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        // 静止画を隠す
        if (rankStaticImage != null)
            rankStaticImage.gameObject.SetActive(false);

        // 動画再生開始
        if (rankVideoPlayer != null && currentRankVideo != null)
        {
            rankVideoPlayer.clip = currentRankVideo;
            rankVideoPlayer.isLooping = true;
            rankVideoPlayer.Play();
        }

        // 「ジャン！」音（スコア決定）
        if (scoreFinishSound != null && audioSource != null)
            audioSource.PlayOneShot(scoreFinishSound);

        // ランクテキスト表示＆アニメーション
        if (rankImage != null)
        {
            rankImage.gameObject.SetActive(true);
            StartCoroutine(AnimateRankText(rankImage.transform));
        }

        // 「そのランクの音」を再生（設定されてなければ共有の音）
        if (currentRankSound != null && audioSource != null)
        {
             audioSource.PlayOneShot(currentRankSound);
        }
        else if (rankAppearSound != null && audioSource != null)
        {
             audioSource.PlayOneShot(rankAppearSound);
        }

        yield return new WaitForSeconds(0.5f);

        // 「じゃじゃーん！」（全演出終了）
        if (resultCompleteSound != null && audioSource != null)
            audioSource.PlayOneShot(resultCompleteSound);
    }

    // ランクテキストをドーンと出すアニメーション
    IEnumerator AnimateRankText(Transform target)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = new Vector3(3f, 3f, 1f); // 3倍から
        Vector3 endScale = Vector3.one;

        target.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Elastic Out Easeっぽい動き
            float scale = Mathf.Lerp(3f, 1f, t); 
            // 簡易的なバウンス
            if (t > 0.7f) scale = 1.0f + Mathf.Sin((t - 0.7f) * 20f) * 0.1f * (1f - t);
            
            target.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        target.localScale = endScale;
    }

    void StartSpinning(TMPro.TMP_Text target)
    {
        if (target == null) return;
        target.gameObject.SetActive(true);
        
        // 既に回っていれば止める（念のため）
        if (spinCoroutines.ContainsKey(target) && spinCoroutines[target] != null)
        {
            StopCoroutine(spinCoroutines[target]);
        }

        spinCoroutines[target] = StartCoroutine(SpinRoutine(target));
    }

    void StopSpinning(TMPro.TMP_Text target, string finalValue)
    {
        if (target == null) return;

        if (spinCoroutines.ContainsKey(target) && spinCoroutines[target] != null)
        {
            StopCoroutine(spinCoroutines[target]);
            spinCoroutines.Remove(target);
        }
        target.text = finalValue;
    }

    IEnumerator SpinRoutine(TMPro.TMP_Text target)
    {
        while (true)
        {
            // 4桁全部動いて見えるように 1000 ~ 9999 の範囲にする
            target.text = Random.Range(1000, 10000).ToString(); 
            yield return new WaitForSeconds(0.05f); // 更新頻度
        }
    }

    void PlayPopSound()
    {
        if (popSound != null && audioSource != null)
            audioSource.PlayOneShot(popSound);
    }

    IEnumerator PlayDigest()
    {
        if (ScoreManager.Instance == null || replayImage == null) yield break;

        List<Texture2D> frames = ScoreManager.Instance.replayFrames;
        Debug.Log($"Replay Frames Count: {frames.Count}"); // デバッグログ

        if (frames.Count == 0)
        {
            Debug.LogWarning("再生するフレームが0枚です！録画がされていないか、データが渡っていません。");
            yield break;
        }

        int index = 0;
        while (true)
        {
            if (frames[index] != null)
            {
                replayImage.texture = frames[index];
            }

            yield return new WaitForSeconds(frameRate);

            index++;
            if (index >= frames.Count) index = 0;
        }
    }

    /// <summary>
    /// 継続エフェクトを再生開始
    /// </summary>
    void StartContinuousEffects()
    {
        // エフェクト1をPrefabから生成
        if (continuousEffect1Prefab != null)
        {
            // 配置位置を決定
            Vector3 spawnPos = effect1SpawnPoint != null ? effect1SpawnPoint.position : Vector3.zero;
            Quaternion spawnRot = effect1SpawnPoint != null ? effect1SpawnPoint.rotation : Quaternion.identity;

            // Prefabからインスタンスを生成
            effect1Instance = Instantiate(continuousEffect1Prefab, spawnPos, spawnRot);

            // ParticleSystemを全て取得（子オブジェクトも含む）
            ParticleSystem[] ps1Array = effect1Instance.GetComponentsInChildren<ParticleSystem>(true);
            if (ps1Array.Length > 0)
            {
                foreach (ParticleSystem ps in ps1Array)
                {
                    var main = ps.main;
                    main.loop = true;
                    main.playOnAwake = true;
                    ps.gameObject.SetActive(true);
                    ps.Play();
                }
                Debug.Log($"Continuous Effect 1 spawned at {spawnPos} ({ps1Array.Length} ParticleSystems)");
            }
            else
            {
                Debug.Log($"Continuous Effect 1 spawned at {spawnPos} (no ParticleSystem found)");
            }
        }
        else
        {
            Debug.LogWarning("Continuous Effect 1 Prefab is not assigned!");
        }

        // エフェクト2をPrefabから生成
        if (continuousEffect2Prefab != null)
        {
            // 配置位置を決定
            Vector3 spawnPos = effect2SpawnPoint != null ? effect2SpawnPoint.position : Vector3.zero;
            Quaternion spawnRot = effect2SpawnPoint != null ? effect2SpawnPoint.rotation : Quaternion.identity;

            // Prefabからインスタンスを生成
            effect2Instance = Instantiate(continuousEffect2Prefab, spawnPos, spawnRot);

            // ParticleSystemを全て取得（子オブジェクトも含む）
            ParticleSystem[] ps2Array = effect2Instance.GetComponentsInChildren<ParticleSystem>(true);
            if (ps2Array.Length > 0)
            {
                foreach (ParticleSystem ps in ps2Array)
                {
                    var main = ps.main;
                    main.loop = true;
                    main.playOnAwake = true;
                    ps.gameObject.SetActive(true);
                    ps.Play();
                }
                Debug.Log($"Continuous Effect 2 spawned at {spawnPos} ({ps2Array.Length} ParticleSystems)");
            }
            else
            {
                Debug.Log($"Continuous Effect 2 spawned at {spawnPos} (no ParticleSystem found)");
            }
        }
        else
        {
            Debug.LogWarning("Continuous Effect 2 Prefab is not assigned!");
        }
    }

    /// <summary>
    /// 進むボタンがクリックされたとき（TutorialSushiから呼ばれる）
    /// </summary>
    public void OnTitleButtonClicked()
    {
        currentResultIndex++;

        if (currentResultIndex >= resultImages.Count)
        {
            // 全ての画像を表示し終わったらタイトルに戻る
            GoToTitle();
        }
        else
        {
            // 次の画像を表示（確認画面）
            UpdateResultImage();
        }
    }

    /// <summary>
    /// 戻るボタンがクリックされたとき（TutorialSushiから呼ばれる）
    /// </summary>
    public void OnBackButtonClicked()
    {
        currentResultIndex--;

        // 最初の画像より前には戻らない
        if (currentResultIndex < 0)
        {
            currentResultIndex = 0;
        }

        UpdateResultImage();
    }

    /// <summary>
    /// 現在のインデックスに基づいて表示画像を更新
    /// </summary>
    private void UpdateResultImage()
    {
        if (resultDisplayImage != null && resultImages != null &&
            currentResultIndex >= 0 && currentResultIndex < resultImages.Count)
        {
            resultDisplayImage.sprite = resultImages[currentResultIndex];
            resultDisplayImage.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// タイトルシーンに戻る
    /// </summary>
    private void GoToTitle()
    {
        // エフェクトを破棄
        if (effect1Instance != null) Destroy(effect1Instance);
        if (effect2Instance != null) Destroy(effect2Instance);

        // スコアリセット
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
            ScoreManager.Instance.replayFrames.Clear(); // メモリ開放
        }
        SceneManager.LoadScene(titleSceneName);
    }

    void OnDestroy()
    {
        // シーン終了時にエフェクトを破棄
        if (effect1Instance != null) Destroy(effect1Instance);
        if (effect2Instance != null) Destroy(effect2Instance);
    }
}
