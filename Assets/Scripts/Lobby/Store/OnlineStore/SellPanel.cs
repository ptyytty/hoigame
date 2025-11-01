using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SellPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text txtItemName;
    [SerializeField] private TMP_Text txtOwnedCount;
    [SerializeField] private TMP_Text txtSellCount;
    [SerializeField] private TMP_InputField inputPrice;
    [SerializeField] private Button btnPlus;
    [SerializeField] private Button btnMinus;
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    private int ownedCount = 0;    // 보유 수량
    private int sellCount = 1;     // 판매 예정 수량
    private string itemName = "";
    private Product selectedProduct;

    void Start()
    {
        btnPlus.onClick.AddListener(OnPlus);
        btnMinus.onClick.AddListener(OnMinus);
        btnConfirm.onClick.AddListener(OnConfirm);
        btnCancel.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// [역할] 판매 패널 열기 및 선택한 아이템 정보 표시
    /// </summary>
    public void Show(Product product, int count)
    {
        selectedProduct = product;
        ownedCount = count;
        sellCount = 1;

        itemName = product.IsConsume ? product.BoundConsume.name_item :
                   product.IsEquip ? product.BoundEquip.name_item : "알 수 없음";

        txtItemName.text = itemName;
        txtOwnedCount.text = $"보유 수량: {ownedCount}";
        txtSellCount.text = $"{sellCount}";
        inputPrice.text = "10";

        gameObject.SetActive(true);
    }

    private void OnPlus()
    {
        if (sellCount < ownedCount)
        {
            sellCount++;
            txtSellCount.text = $"{sellCount}";
        }
    }

    private void OnMinus()
    {
        if (sellCount > 1)
        {
            sellCount--;
            txtSellCount.text = $"{sellCount}";
        }
    }

    /// <summary>
    /// [역할] 판매 확정 버튼 클릭 시 실행
    /// </summary>
    private void OnConfirm()
    {
        if (selectedProduct == null) return;

        // 패널 닫기
        Hide();
    }

    /// <summary>
    /// [역할] 패널 숨김
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
