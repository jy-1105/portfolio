using System.Collections;
using UnityEngine;

public class Item : MonoBehaviour
{
    public enum ItemType { SizeChange }
    public ItemType itemType;

    public float sizeMultiplierMax = 0.75f;   // サイズ倍率の最大値（大きさを変更するときに使用）
    public float hideDuration = 3f;  // アイテムを非表示にする時間

    private Renderer itemRenderer;

    private void Start()
    {
        itemRenderer = GetComponent<Renderer>(); // Rendererコンポーネントを取得
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("pancake")) // パンケーキと衝突したとき
        {
            Debug.Log("衝突発生: パンケーキとアイテム"); // 衝突確認用のログ

            // アイテム効果を適用
            ApplyItemEffect(other.gameObject);

            // アイテムを非表示にする
            StartCoroutine(HideItem());
        }
    }

    private void ApplyItemEffect(GameObject pancake)
    {
        if (itemType == ItemType.SizeChange)
        {
            ChangeSize(pancake);
        }
    }

    private void ChangeSize(GameObject pancake)
    {
        Debug.Log("サイズ変更開始");

        // パンケーキの元の大きさを保存
        Vector3 originalScale = pancake.transform.localScale;

        // パンケーキのColliderの大きさを保存
        BoxCollider pancakeCollider = pancake.GetComponent<BoxCollider>();
        if (pancakeCollider == null)
        {
            Debug.LogWarning("パンケーキオブジェクトにBoxColliderがありません！");
            return;
        }
        Vector3 originalColliderSize = pancakeCollider.size;

        // サイズ倍率を最大値に設定
        float sizeMultiplier = sizeMultiplierMax;

        // 大きさを変更
        pancake.transform.localScale = originalScale * sizeMultiplier;
        pancakeCollider.size = originalColliderSize * sizeMultiplier;

        Debug.Log("サイズ変更: " + pancake.transform.localScale); // サイズ変更確認用のログ
    }

    private IEnumerator HideItem()
    {
        // アイテムを非表示にする
        gameObject.SetActive(false);

        // hideDuration後にアイテムを再表示する
        yield return new WaitForSeconds(hideDuration);

        // アイテムを表示する
        gameObject.SetActive(true);

        Debug.Log("アイテムを再度有効化");
    }
}