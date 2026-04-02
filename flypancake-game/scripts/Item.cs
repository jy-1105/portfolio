using System.Collections;
using UnityEngine;

/// <summary>
/// パンケーキに衝突した際にアイテム効果を適用するクラス。
/// 現在はサイズ変更アイテムに対応している。
/// </summary>
public class Item : MonoBehaviour
{
    public enum ItemType { SizeChange }
    public ItemType itemType;

    public float sizeMultiplierMax = 0.75f;
    public float hideDuration = 3f;

    private Renderer itemRenderer;

    private void Start()
    {
        itemRenderer = GetComponent<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("pancake"))
        {
            Debug.Log("衝突発生: パンケーキとアイテム");
            ApplyItemEffect(other.gameObject);
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

    /// <summary>
    /// パンケーキ本体とColliderの両方を変更し、見た目と判定を一致させる。
    /// </summary>
    private void ChangeSize(GameObject pancake)
    {
        Debug.Log("サイズ変更開始");

        Vector3 originalScale = pancake.transform.localScale;

        BoxCollider pancakeCollider = pancake.GetComponent<BoxCollider>();
        if (pancakeCollider == null)
        {
            Debug.LogWarning("パンケーキオブジェクトにBoxColliderがありません！");
            return;
        }

        Vector3 originalColliderSize = pancakeCollider.size;
        float sizeMultiplier = sizeMultiplierMax;

        pancake.transform.localScale = originalScale * sizeMultiplier;
        pancakeCollider.size = originalColliderSize * sizeMultiplier;

        Debug.Log("サイズ変更: " + pancake.transform.localScale);
    }

    private IEnumerator HideItem()
    {
        gameObject.SetActive(false);
        yield return new WaitForSeconds(hideDuration);
        gameObject.SetActive(true);

        Debug.Log("アイテムを再度有効化");
    }
}