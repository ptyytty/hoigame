using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [역할] 다이아몬드 슬롯 1칸. Combatant 바인딩 → HP/이름 갱신.
///        (표시 전용 모드 지원: 버튼/레이캐스트 차단 없이 HUD만 보여줌)
/// </summary>
public class DiamondSlot : MonoBehaviour
{
    [Header("UI")]
    public Button clickButton;          // [선택] 타깃 확정용 버튼(월드 클릭을 쓰면 비워둬도 됨)
    public TMP_Text nameText;           // [역할] 이름 라벨
    public HealthBarUI hpUI;            // [역할] 간단 HP 바(프리팹 재사용)

    private Combatant _bound;
    private System.Action<Combatant> _onClicked;
    private bool _clickable;            // [역할] 슬롯을 클릭 가능한지(표시 전용이면 false)

    void Awake()
    {
        // [역할] 인스펙터에서 hpUI 누락 시, 자식에서 자동 탐색(런타임 안전망)
        if (!hpUI) hpUI = GetComponentInChildren<HealthBarUI>(includeInactive: true);
        if (!nameText) nameText = GetComponentInChildren<TMP_Text>(includeInactive: true);
    }

    /// <summary>
    /// [역할] 슬롯을 표시 전용 or 클릭 가능 모드로 전환한다.
    ///        표시 전용이면 UI가 레이캐스트를 막지 않아 월드 클릭이 그대로 통과된다.
    /// </summary>
    public void SetClickable(bool clickable)
    {
        _clickable = clickable;

        // 버튼 존재 시 on/off
        if (clickButton) clickButton.gameObject.SetActive(clickable);

        // 슬롯 내 모든 Graphic의 raycastTarget를 통일
        var graphics = GetComponentsInChildren<Graphic>(includeInactive: true);
        foreach (var g in graphics)
        {
            g.raycastTarget = clickable; // 표시 전용이면 false → 월드 클릭 통과
        }
    }

    /// <summary>
    /// [역할] Combatant 바인딩 및 클릭 콜백 등록(필요한 경우에만).
    /// </summary>
    public void Bind(Combatant c, System.Action<Combatant> onClicked = null)
    {
        _bound = c;
        _onClicked = onClicked;

        if (nameText) nameText.text = _bound ? _bound.DisplayName : "";

        // [핵심] HP바 바인딩 (누락 시에도 예외 없이 동작)
        if (hpUI) hpUI.TryBind(_bound);
        else Debug.LogWarning("[DiamondSlot] hpUI 미연결 - HealthBarUI를 슬롯 자식에 붙이고 필드에 할당하세요.", this);

        // 클릭 가능 모드일 때만 리스너 연결
        if (clickButton)
        {
            clickButton.onClick.RemoveListener(HandleClick);
            if (_clickable) clickButton.onClick.AddListener(HandleClick);
        }
    }

    /// <summary>
    /// [역할] 바인딩 해제(이벤트/리스너 정리).
    /// </summary>
    public void Unbind()
    {
        if (hpUI) hpUI.Unbind();
        if (clickButton) clickButton.onClick.RemoveListener(HandleClick);
        _bound = null;
        _onClicked = null;
    }

    /// <summary>
    /// [역할] (클릭 가능 모드에서만) 슬롯 클릭 시 외부 콜백 호출.
    /// </summary>
    void HandleClick()
    {
        if (_bound != null) _onClicked?.Invoke(_bound);
    }

    void OnDisable()
    {
        Unbind();
    }
}
