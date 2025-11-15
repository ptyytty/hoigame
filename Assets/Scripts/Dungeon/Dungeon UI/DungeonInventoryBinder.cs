using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 던전 인벤토리: "소비 아이템만" 최대 6칸에 표시하는 정적 바인더
/// - 동적 생성 없음: 이미 배치된 슬롯(버튼/이미지/TMP_Text)에 값만 꽂음
/// - 장비는 완전히 무시
/// - InventoryRuntime에 변경 이벤트가 없어도 동작(수동 Refresh 호출)
/// </summary>
public class DungeonInventoryBinder : MonoBehaviour
{
    [Serializable]
    public class SlotRefs
    {
        public Button button;
        public Image icon;
        public TMP_Text count;
    }

    [Header("Dungeon Inventory")]
    [SerializeField] private DungeonInventory dungeonInventory; // 비워두면 자동 탐색

    [Header("6 Slots")]
    [SerializeField] private SlotRefs[] slots = new SlotRefs[6];

    public Action<int> OnSlotClicked;       // 슬롯 선택

    private int _selectedIndex = -1;        // 선택한 인벤토리 슬롯 인덱스
    public int SelectedIndex => _selectedIndex;

    void OnEnable()
    {
        if (!dungeonInventory && DungeonManager.instance != null)
            dungeonInventory = DungeonManager.instance.DungeonInventory;

        // 2) 그래도 없으면(예: 테스트용 씬) 최후의 수단으로 씬에서 직접 탐색
        if (!dungeonInventory)
            dungeonInventory = FindObjectOfType<DungeonInventory>(true);

        if (dungeonInventory != null)
            dungeonInventory.Changed += Refresh;

        Refresh();
    }

    void OnDisable()
    {
        if (dungeonInventory != null)
            dungeonInventory.Changed -= Refresh;
    }

    public void Bind(DungeonInventory inv)
    {
        if (dungeonInventory != null)
            dungeonInventory.Changed -= Refresh;

        dungeonInventory = inv;

        if (dungeonInventory != null)
            dungeonInventory.Changed += Refresh;

        Refresh();
    }

    public void Refresh()
    {
        if (dungeonInventory == null || slots == null) return;

        var list = dungeonInventory.GetSlots();
        int n = Mathf.Min(slots.Length, list.Count);

        for (int i = 0; i < n; i++)
        {
            var ui = slots[i];
            var slot = list[i];

            WireButton(ui, i); // 클릭 시 소비 안 함(아래 참고)

            if (slot.IsEmpty)
                FillEmpty(ui);
            else
                FillWithItem(ui, slot.item?.icon, slot.count);
        }

        // 남는 칸 비우기
        for (int i = n; i < slots.Length; i++)
        {
            WireButton(slots[i], i);
            FillEmpty(slots[i]);
        }
    }

    void WireButton(SlotRefs s, int index)
    {
        if (s == null || s.button == null) return;
        s.button.onClick.RemoveAllListeners();

        // [역할] 슬롯 클릭 시 '선택만' 하고 소비하지 않음(추후 사용 버튼에서 소비)
        s.button.onClick.AddListener(() =>
        {
            Debug.Log($"[DungeonInventoryBinder] Slot {index} clicked (no consume yet).");
            _selectedIndex = index;              // ✅ 선택 캐시
            OnSlotClicked?.Invoke(index);
            // TODO: 선택 비주얼이 있다면 여기서 갱신
        });
    }

    void FillWithItem(SlotRefs s, Sprite sprite, int count)
    {
        if (s == null) return;

        // 아이콘 즉시 표시
        if (s.icon)
        {
            s.icon.sprite = sprite;
            s.icon.enabled = (sprite != null);
            s.icon.gameObject.SetActive(sprite != null);
        }

        // 수량 즉시 표시 (1개여도 "1" 표시)
        if (s.count)
        {
            s.count.text = count.ToString();
            s.count.gameObject.SetActive(true);
        }

        // 버튼 인터랙션 활성
        if (s.button) s.button.interactable = true;
    }

    void FillEmpty(SlotRefs s)
    {
        if (s == null) return;

        if (s.icon)
        {
            s.icon.sprite = null;
            s.icon.enabled = false;
            s.icon.gameObject.SetActive(false);
        }

        if (s.count)
        {
            s.count.text = "";
            s.count.gameObject.SetActive(false);
        }

        if (s.button) s.button.interactable = false;
    }

    public void ClearSelection()
    {
        _selectedIndex = -1;
        // 필요 시 슬롯 하이라이트/테두리 등 시각 효과도 여기서 해제
    }

}