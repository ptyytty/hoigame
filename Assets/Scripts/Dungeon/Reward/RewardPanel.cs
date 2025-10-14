using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보상 UI
/// </summary>
public class RewardPanel : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Image rewardSoul;
    [SerializeField] private Image rewardCoin;
    [SerializeField] private TMP_Text soulLineText;    // 예: "+3 빨간 소울"
    [SerializeField] private TMP_Text coinLineText;    // 예: "+25 코인"

    [Header("Icons")]
    [SerializeField] private Sprite[] soulSprites; // index = (int)SoulType
    [SerializeField] private Sprite coinSprite;

    /// <param name="soulType">드랍된 소울 타입</param>
    /// <param name="soulAmount">소울 개수(요구대로 항상 1이어도 OK)</param>
    /// <param name="coins">코인</param>
    public void Bind(SoulType soulType, int soulAmount, int coins)
    {
        if (soulLineText) soulLineText.text = $"+{soulAmount}";
        if (coinLineText)  coinLineText.text  = $"+{coins}";

        // 아이콘 연결
        if (rewardSoul && soulSprites != null)
        {
            int idx = (int)soulType;
            if (idx >= 0 && idx < soulSprites.Length && soulSprites[idx] != null)
                rewardSoul.sprite = soulSprites[idx];
        }

        if (rewardCoin && coinSprite != null)
            rewardCoin.sprite = coinSprite;
    }

}
