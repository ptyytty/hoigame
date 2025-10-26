using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShowMoney : MonoBehaviour
{
    [Header("Show Current Money")]
    [SerializeField] private TMP_Text coin;
    [SerializeField] private TMP_Text redSoul;
    [SerializeField] private TMP_Text blueSoul;
    [SerializeField] private TMP_Text greenSoul;

    private InventoryRuntime wallet;

    void OnEnable()
    {
        wallet = InventoryRuntime.Instance;
        if (wallet != null)
        {
            wallet.OnCurrencyChanged += RefreshTexts; // 변경 이벤트 구독
            RefreshTexts();                            // 즉시 1회 갱신
        }
    }

    void OnDisable()
    {
        if (wallet != null) wallet.OnCurrencyChanged -= RefreshTexts;
    }

    /// <summary> [역할] 텍스트 최신화(이벤트/씬 복귀 시 호출) </summary>
    private void RefreshTexts()
    {
        if (!wallet) return;
        if (coin)      coin.text      = wallet.Gold.ToString();
        if (redSoul)   redSoul.text   = wallet.redSoul.ToString();
        if (blueSoul)  blueSoul.text  = wallet.blueSoul.ToString();
        if (greenSoul) greenSoul.text = wallet.greenSoul.ToString();
    }
}
