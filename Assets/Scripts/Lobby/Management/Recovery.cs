using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// 의무실 메인 탭: 고정 3 슬롯 + 체력 프리뷰/확정까지 관리
/// - 리스트 클릭: SetPendingHero(hero)로 '대기 영웅'만 저장
/// - 슬롯 클릭: 비었으면 대기 영웅을 배치, 차있으면 비움
/// - PreviewHealAll/ConfirmHealAll/ClearAllPreview 로 회복량(예: +20) 미리보기/확정 처리
public class Recovery : MonoBehaviour
{
    // ========= 외부에 노출되는 '잠금된 영웅' 집합 =========
    // - HeroListUp/PartySelector에서 참조해 던전 투입을 막는다
    public static readonly HashSet<string> LockedInstanceIds = new HashSet<string>();

    /// 잠금 변경 알림(리스트가 즉시 갱신되도록 연결처에서 구독 가능)
    public static event Action OnLocksChanged;

    [Serializable]
    public class SlotView
    {
        [Header("Button / Background")]
        public Button slotButton;
        public Image bgTarget;
        public Sprite emptyBg;
        public Sprite filledBg;

        [Header("Confirm Button (per slot)")]
        public Button confirmButton;
        public TMP_Text confirmLabel;

        [Header("Group Root (visuals)")]
        public GameObject contentRoot;
        public GameObject emptyHint;

        [Header("Hero UI")]
        public Image portrait;
        public TMP_Text displayName;
        public TMP_Text jobName;
        public TMP_Text hpText;
        public HealthBarUI hpBar;
    }

    [Header("Fixed 3 Slots (assign in Inspector)")]
    [SerializeField] private SlotView[] slots = new SlotView[3];

    [Header("Fallbacks")]
    [SerializeField] private Sprite defaultPortrait;

    private readonly Job[] _heroes = new Job[3];
    private readonly Dictionary<Job, int> _indexByHero = new();
    private readonly List<Job> _buf = new(3);
    private Job selectedHero;
    private int healAmount = 0;

    // 회복 확정 후 “던전 전까지” 슬롯 잠금 플래그
    private readonly bool[] _confirmLocked = new bool[3];

    /// 외부에서 현재 선택(배치)된 영웅들 읽기
    public IReadOnlyList<Job> SelectedHeroes
    {
        get
        {
            _buf.Clear();
            for (int i = 0; i < _heroes.Length; i++)
                if (_heroes[i] != null) _buf.Add(_heroes[i]);
            return _buf;
        }
    }

    void Awake()
    {
        // 슬롯 버튼/확인 버튼 리스너 연결
        for (int i = 0; i < slots.Length; i++)
        {
            int idx = i; // 람다 캡처 최소화

            var v = slots[idx];
            if (v?.slotButton)
            {
                v.slotButton.onClick.RemoveAllListeners();
                v.slotButton.onClick.AddListener(() => OnClickSlot(idx)); // 배치/비움
            }
            if (v?.confirmButton)
            {
                v.confirmButton.onClick.RemoveAllListeners();
                v.confirmButton.onClick.AddListener(() => OnClickConfirm(idx)); // 슬롯별 회복 확정
            }
        }
        RefreshAll(); // 시작 시 빈 상태로
    }

    // ─────────────────────────────────────────────
    // 외부 호출 API
    // ─────────────────────────────────────────────

    /// 리스트 클릭 시 선택 영웅 저장
    public void SetPendingHero(Job hero) => selectedHero = hero;

    /// 회복 프리뷰량 설정(예: +20). 0이면 프리뷰 제거.
    public void SetPendingHealAmount(int amount)
    {
        healAmount = amount;
        // 이미 채워진 슬롯에는 즉시 프리뷰 반영/해제
        for (int i = 0; i < slots.Length; i++)
            ApplyPreviewToSlot(i);
    }

    /// 던전 1회 주기 후(귀환 시점 등) 버튼 잠금/상태 초기화
    public void ResetHealLocks()
    {
        for (int i = 0; i < _confirmLocked.Length; i++)
            _confirmLocked[i] = false;

        // UI 복구: 슬롯 다시 클릭 가능, 확인 버튼 돌아옴
        for (int i = 0; i < slots.Length; i++)
        {
            var v = slots[i];
            if (v == null) continue;

            bool hasHero = _heroes[i] != null;

            if (v.confirmButton)
            {
                v.confirmButton.gameObject.SetActive(hasHero); // 다시 표시
                v.confirmButton.interactable = hasHero;        // 클릭 가능
            }
            if (v.slotButton)
            {
                v.slotButton.interactable = hasHero;           // 슬롯도 다시 클릭 가능
            }
        }

        // 의무실 잠금 해제 → 던전에 다시 투입 가능
        LockedInstanceIds.Clear();
        OnLocksChanged?.Invoke();
    }

    /// 전체 초기화(취소/탭전환 시)
    public void ClearAll()
    {
        selectedHero = null;
        healAmount = 0;
        Array.Clear(_confirmLocked, 0, _confirmLocked.Length);
        _indexByHero.Clear();

        for (int i = 0; i < _heroes.Length; i++)
        {
            _heroes[i] = null;
            RefreshSlot(i);
        }

        // 잠금도 초기화
        LockedInstanceIds.Clear();
        OnLocksChanged?.Invoke();
    }

    // ─────────────────────────────────────────────
    // 슬롯 클릭 / 확인 클릭
    // ─────────────────────────────────────────────

    /// 슬롯 버튼 클릭(비면 배치, 차면 비움)
    public void OnClickSlot(int index)
    {
        if ((uint)index >= (uint)_heroes.Length) return;
        var curHero = _heroes[index];

        // 확정으로 잠겨 있으면(던전 전까지) 클릭 불가
        if (_confirmLocked[index]) return;

        if (curHero == null)
        {
            if (selectedHero == null) return;
            if (_indexByHero.ContainsKey(selectedHero)) { PingSlot(_indexByHero[selectedHero]); return; }

            _heroes[index] = selectedHero;
            _indexByHero[selectedHero] = index;
            selectedHero = null;

            // ✅ 리스트 선택/이미지 초기화
            var lm = FindObjectOfType<ListUpManager>();
            if (lm) lm.ClearExternalSelection();

            RefreshSlot(index);
            ApplyPreviewToSlot(index);

            // ✅ 확인 버튼 표시 + 라벨 "확인"
            EnableConfirmButton(index, true, true);
            SetConfirmVisual(index, scheduled: false);

            // ✅ 슬롯은 계속 클릭 가능(비활성화로 떨어지지 않게)
            if (slots[index]?.slotButton) slots[index].slotButton.interactable = true;
        }
        else
        {
            // 비우기 — 프리뷰 제거 + 확인 버튼 숨김
            if (slots[index]?.hpBar) slots[index].hpBar.ClearPreview();
            _indexByHero.Remove(curHero);
            _heroes[index] = null;
            _confirmLocked[index] = false;

            RefreshSlot(index);

            // ✅ 확인 버튼을 완전히 숨김 (요구: 빈 슬롯은 버튼이 안 보여야 함)
            EnableConfirmButton(index, false, false);

            // ✅ 슬롯은 계속 상호작용 가능해야 함
            if (slots[index]?.slotButton) slots[index].slotButton.interactable = true;
        }
    }

    /// 슬롯별 '확인' 버튼 클릭 → 해당 슬롯만 회복 확정
    public void OnClickConfirm(int index)
    {
        if ((uint)index >= (uint)_heroes.Length) return;

        var hero = _heroes[index];
        var v = slots[index];
        if (v == null) return;

        // ── [토글 1] 이미 스케줄된 상태라면 → 취소 ──
        if (_confirmLocked[index])
        {
            if (v.hpBar) v.hpBar.ClearPreview();

            _confirmLocked[index] = false;
            if (hero != null) _indexByHero.Remove(hero);
            _heroes[index] = null;

            RefreshSlot(index);

            // ✅ 라벨만 "확인"으로 바꾸는 것 + 버튼 숨김(빈 슬롯이므로)
            SetConfirmVisual(index, scheduled: false);
            EnableConfirmButton(index, false, false); // ← 숨김

            // ✅ 슬롯은 계속 상호작용 가능
            if (v.slotButton) v.slotButton.interactable = true;

            // 잠금 해제 알림 유지
            if (hero != null && !string.IsNullOrEmpty(hero.instanceId))
            {
                LockedInstanceIds.Remove(hero.instanceId);
                OnLocksChanged?.Invoke();
            }
            return;
        }

        // ── [토글 2] 스케줄 안 된 상태 → 확정(스케줄만, 실제 회복 X) ──
        if (hero == null) return;            // 비어 있으면 아무 것도 안 함
        if (healAmount == 0) return;         // 회복 예정량이 0이면 확정 의미가 없음

        // 프리뷰(미래 체력)만 표시 유지 — 실제 회복은 나중에 CommitScheduledHeals에서
        if (v.hpBar) v.hpBar.ShowPreviewDelta(+healAmount, HealthBarUI.PreviewType.Heal);

        // 슬롯 잠금(던전 갈 때까지 유지) + 버튼 라벨 “취소”
        _confirmLocked[index] = true;
        SetConfirmVisual(index, scheduled: true);
        if (v.slotButton) v.slotButton.interactable = false;

        // 리스트에서 선택 불가 처리
        if (!string.IsNullOrEmpty(hero.instanceId))
        {
            LockedInstanceIds.Add(hero.instanceId);
            OnLocksChanged?.Invoke();
        }

    }

    /// '확인'↔'취소' 라벨 및 버튼 상호작용 제어
    private void SetConfirmVisual(int i, bool scheduled)
    {
        var v = (i >= 0 && i < slots.Length) ? slots[i] : null;
        if (v?.confirmButton == null) return;

        // 요구: 버튼은 항상 보이게(비활성/활성만 변경), 라벨만 교체
        v.confirmButton.gameObject.SetActive(true);
        v.confirmButton.interactable = true;

        if (v.confirmLabel)
            v.confirmLabel.text = scheduled ? "취소" : "확인";
    }

    /// 스케줄된 슬롯들의 프리뷰를 실제 HP로 커밋(일괄 적용)
    public void CommitScheduledHeals()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!_confirmLocked[i]) continue;       // 스케줄 안 된 슬롯은 스킵

            var hero = _heroes[i];
            var v = slots[i];
            if (hero == null || v == null) continue;

            // 현재/최대 읽기
            GetHpPair(hero, out int hp, out int max);

            // 예정량 커밋
            int after = Mathf.Min(hp + healAmount, max);
            SetHp(hero, after, max);

            // UI 반영: 실제값으로 Set, 프리뷰 제거
            if (v.hpBar)
            {
                // 커밋 애니메이션을 쓰고 싶다면 v.hpBar.CommitPreview(0.2f) 후 Set 동기화
                v.hpBar.ClearPreview();
                v.hpBar.Set(after, max);
            }
            if (v.hpText) v.hpText.text = $"{after}/{max}";
        }

        // 커밋 후에도 슬롯은 계속 잠금(던전 1회 다녀오기 전까지 유지)
        // 버튼 라벨은 여전히 '취소' 상태로 두거나, 커밋 이후엔 비활성하고 싶다면 여기서 조절 가능
    }

    // ─────────────────────────────────────────────
    // 내부 유틸
    // ─────────────────────────────────────────────

    private void RefreshAll()
    {
        for (int i = 0; i < slots.Length; i++) RefreshSlot(i);
    }

    /// 슬롯에 현재 상태(채움/비움)에 맞춰 UI 반영
    private void RefreshSlot(int i)
    {
        var v = (i >= 0 && i < slots.Length) ? slots[i] : null;
        if (v == null) return;

        bool filled = _heroes[i] != null;

        // 배경 교체
        if (v.bgTarget)
            v.bgTarget.sprite = filled ? (v.filledBg ? v.filledBg : v.bgTarget.sprite)
                                       : (v.emptyBg ? v.emptyBg : v.bgTarget.sprite);

        // 내부 비주얼 on/off (버튼은 항상 유지)
        SetContentActive(v, filled);
        if (v.emptyHint) v.emptyHint.SetActive(!filled);

        if (!filled)
        {
            // 안전 초기화
            if (v.portrait) { v.portrait.sprite = defaultPortrait; v.portrait.enabled = defaultPortrait != null; }
            if (v.displayName) v.displayName.text = "";
            if (v.jobName) v.jobName.text = "";
            if (v.hpBar) v.hpBar.Set(0, 1);
            if (v.hpText) v.hpText.text = "";
            return;
        }

        // 채워진 경우 데이터 바인딩
        var hero = _heroes[i];
        if (v.portrait)
        {
            var spr = ResolvePortrait(hero);
            v.portrait.enabled = true;
            v.portrait.sprite = spr != null ? spr : defaultPortrait;
        }
        if (v.displayName) v.displayName.text = hero?.displayName ?? "";
        if (v.jobName) v.jobName.text = hero?.name_job ?? "";

        GetHpPair(hero, out int hp, out int max);
        if (v.hpBar) v.hpBar.Set(hp, max);
        if (v.hpText) v.hpText.text = $"{hp}/{max}";

        EnableConfirmButton(i, filled, filled && !_confirmLocked[i]);
    }

    /// 이미 채워진 슬롯에 회복 프리뷰(또는 제거) 적용
    private void ApplyPreviewToSlot(int i)
    {
        var v = (i >= 0 && i < slots.Length) ? slots[i] : null;
        if (v == null || v.hpBar == null) return;

        if (_heroes[i] == null || healAmount == 0)
        {
            v.hpBar.ClearPreview();
            return;
        }
        v.hpBar.ShowPreviewDelta(+healAmount, HealthBarUI.PreviewType.Heal);
    }

    /// 확인 버튼의 표시/상호작용 상태를 제어
    private void EnableConfirmButton(int i, bool enable, bool interactable)
    {
        var v = (i >= 0 && i < slots.Length) ? slots[i] : null;
        if (v?.confirmButton == null) return;

        v.confirmButton.gameObject.SetActive(enable);
        v.confirmButton.interactable = interactable && !_confirmLocked[i];
    }

    /// 이미 채워진 슬롯을 살짝 강조(중복 배치 시 피드백)
    private void PingSlot(int index)
    {
        var v = (index >= 0 && index < slots.Length) ? slots[index] : null;
        var nameT = v?.displayName;
        if (!nameT) return;
        nameT.CrossFadeAlpha(0.5f, 0.05f, true);
        nameT.CrossFadeAlpha(1f, 0.05f, true);
    }

    /// contentRoot가 버튼 자신이면 자식만 토글(버튼 클릭은 항상 가능)
    private void SetContentActive(SlotView v, bool active)
    {
        if (!v.contentRoot)
        {
            if (v.portrait) v.portrait.gameObject.SetActive(active);
            if (v.displayName) v.displayName.gameObject.SetActive(active);
            if (v.jobName) v.jobName.gameObject.SetActive(active);
            if (v.hpText) v.hpText.gameObject.SetActive(active);
            if (v.hpBar) v.hpBar.gameObject.SetActive(active);
            return;
        }

        bool isButtonRoot = v.slotButton && v.contentRoot == v.slotButton.gameObject;
        if (isButtonRoot)
        {
            var t = v.contentRoot.transform;
            for (int i = 0; i < t.childCount; i++)
                t.GetChild(i).gameObject.SetActive(active);
        }
        else
        {
            v.contentRoot.SetActive(active);
        }
    }

    // ── 데이터 접근 헬퍼(필드명이 프로젝트마다 다를 수 있어 보수적으로 처리) ──
    private Sprite ResolvePortrait(Job hero)
    {
        if (hero == null) return null;
        return TryGetSprite(hero, "portrait", "face", "icon", "sprite", "profile", "avatar", "uiSprite")
               ?? defaultPortrait;
    }

    private void GetHpPair(Job hero, out int hp, out int max)
    {
        hp = 0; max = 0;
        if (hero == null) return;

        if (!TryGetInt(hero, out hp, "currentHp", "curHp", "hp", "nowHp")) hp = 0;
        if (!TryGetInt(hero, out max, "maxHp", "hpMax", "max_health", "maxHP", "MaxHp")) max = Math.Max(max, hp);

        if (max <= 0) max = Math.Max(1, hp);
        hp = Mathf.Clamp(hp, 0, max);
    }

    /// Job 객체의 현재/최대 HP 쓰기(존재하는 첫 필드/프로퍼티에 기록)
    private void SetHp(Job hero, int newHp, int maxHpKnown)
    {
        if (hero == null) return;
        TrySetInt(hero, newHp, "currentHp", "curHp", "hp", "nowHp");
        TrySetInt(hero, maxHpKnown, "maxHp", "hpMax", "max_health", "maxHP", "MaxHp");
    }

    private static bool TryGetInt(object obj, out int value, params string[] names)
    {
        value = 0;
        var t = obj.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n);
            if (f != null && f.FieldType == typeof(int)) { value = (int)f.GetValue(obj); return true; }
            var p = t.GetProperty(n);
            if (p != null && p.PropertyType == typeof(int) && p.CanRead) { value = (int)p.GetValue(obj); return true; }
        }
        return false;
    }

    private static bool TrySetInt(object obj, int value, params string[] names)
    {
        var t = obj.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n);
            if (f != null && f.FieldType == typeof(int)) { f.SetValue(obj, value); return true; }
            var p = t.GetProperty(n);
            if (p != null && p.PropertyType == typeof(int) && p.CanWrite) { p.SetValue(obj, value); return true; }
        }
        return false;
    }

    private static Sprite TryGetSprite(object obj, params string[] names)
    {
        var t = obj.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n);
            if (f != null && typeof(Sprite).IsAssignableFrom(f.FieldType)) return f.GetValue(obj) as Sprite;
            var p = t.GetProperty(n);
            if (p != null && typeof(Sprite).IsAssignableFrom(p.PropertyType) && p.CanRead) return p.GetValue(obj) as Sprite;
        }
        return null;
    }
}
