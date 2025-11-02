using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [역할] 스킬 타게팅 모드에서만 2×2 다이아몬드 HUD를 표시.
///        방향(좌/우)에 따라 슬롯 4개의 위치를 시계/반시계로 자동 교환.
///        기본은 표시 전용(월드 오브젝트 클릭으로 확정).
/// </summary>
public class DiamondTargetHUD : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas hudCanvas;

    [Header("Root & Slots")]
    public RectTransform root;
    public DiamondSlot slotTop;      // 전열 0
    public DiamondSlot slotRight;    // 전열 1
    public DiamondSlot slotBottom;   // 후열 0
    public DiamondSlot slotLeft;     // 후열 1

    [Header("Layout")]
    public float radius = 160f;
    public float worldCenterYOffset = 0.5f;

    public bool invertRotation = true;

    [Header("Interaction")]
    [Tooltip("false: 표시 전용(월드 오브젝트 클릭으로 확정) / true: 슬롯 버튼으로 확정")]
    public bool interactive = false;
    public GraphicRaycaster raycaster;   // Optional
    public CanvasGroup canvasGroup;      // Optional

    [Header("Smoothing")]
    public bool smoothFollow = true;
    public float smoothTime = 0.08f;
    private Vector2 _vel;

    // 내부 상태
    bool _active;
    Side _currentSide = Side.Enemy;

    // [역할] 기본(0도) 다이아몬드 좌표 캐시
    Vector2 _pTop, _pRight, _pBottom, _pLeft;

    void Awake()
    {
        if (!hudCanvas) hudCanvas = GetComponentInParent<Canvas>();
        Show(false);

        // [역할] 원본(기준) 좌표 캐시
        _pTop = new Vector2(0f, radius);
        _pRight = new Vector2(radius, 0f);
        _pBottom = new Vector2(0f, -radius);
        _pLeft = new Vector2(-radius, 0f);

        // 최초 한 번 기준 위치 세팅
        ApplyAnchors(_pTop, _pRight, _pBottom, _pLeft);
    }

    void OnEnable()
    {
        ApplyInteractionMode();
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnTargetingStateChanged += HandleTargetingToggle;
    }

    void OnDisable()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnTargetingStateChanged -= HandleTargetingToggle;
        Show(false);
    }

    // ── 타게팅 on/off ─────────────────────────
    void HandleTargetingToggle(bool on)
    {
        if (!on) { Show(false); return; }

        var bm = BattleManager.Instance;
        var s = bm?.PendingSkill;
        var caster = bm?.PendingCaster;
        if (s == null || caster == null) { Show(false); return; }

        _currentSide = (s.target == Target.Ally || s.target == Target.Self)
            ? caster.side
            : (caster.side == Side.Hero ? Side.Enemy : Side.Hero);

        RebuildForSide(_currentSide);  // 슬롯 데이터 바인딩 + 위치 즉시 보정
        Show(true);
    }

    /// <summary>
    /// [역할] 인터랙션 모드(표시 전용/클릭형)에 맞춰 Raycast 및 슬롯 모드 조정.
    /// </summary>
    void ApplyInteractionMode()
    {
        if (raycaster) raycaster.enabled = interactive;
        if (canvasGroup) canvasGroup.blocksRaycasts = interactive;

        foreach (var s in new[] { slotTop, slotRight, slotBottom, slotLeft })
            if (s) s.SetClickable(interactive);
    }

    // ── 슬롯 바인딩/배치 ─────────────────────────
    void RebuildForSide(Side side)
    {
        // [역할] 대상 측(아군/적) 생존 멤버 수집
        var members = FindObjectsOfType<Combatant>(true)
            .Where(c => c && c.IsAlive && c.side == side)
            .ToList();

        // 아무도 없으면 전부 숨김
        if (members.Count == 0)
        {
            BindSlot(slotTop, null);
            BindSlot(slotRight, null);
            BindSlot(slotBottom, null);
            BindSlot(slotLeft, null);
            return;
        }

        // [역할] 화면/캔버스 기준 좌표 준비
        var cam = hudCanvas && hudCanvas.worldCamera ? hudCanvas.worldCamera : Camera.main;
        var canvasRect = (RectTransform)hudCanvas.transform;
        Vector3 worldCenter = ComputeWorldCenter(side) + Vector3.up * worldCenterYOffset;

        if (!TryWorldToCanvasLocal(cam, canvasRect, worldCenter, out var centerLocal))
        {
            // 변환 실패 시 안전하게 전부 끄기
            BindSlot(slotTop, null);
            BindSlot(slotRight, null);
            BindSlot(slotBottom, null);
            BindSlot(slotLeft, null);
            return;
        }

        // [역할] 각 멤버의 "센터 대비 오프셋"과 각도 계산
        var scored = new List<(Combatant c, Vector2 off, float angDeg)>(members.Count);
        foreach (var m in members)
        {
            if (!TryWorldToCanvasLocal(cam, canvasRect, m.transform.position, out var lp)) continue;
            var off = lp - centerLocal;
            if (off.sqrMagnitude <= 0.0001f) continue;

            float ang = Mathf.Atan2(off.y, off.x) * Mathf.Rad2Deg; // -180~180
            scored.Add((m, off, ang));
        }

        // [역할] 슬롯을 돌리지 않고 고정된 네 슬롯에 각도 기반으로 1:1 배정
        var slots = new[] { slotTop, slotRight, slotBottom, slotLeft };
        var slotAngles = new Dictionary<DiamondSlot, float>
    {
        { slotTop,    90f  },
        { slotRight,   0f  },
        { slotBottom,-90f  },
        { slotLeft,  180f  } // (-180과 동일)
    };

        // [역할] 배정 결과: 슬롯 -> 유닛
        var assigned = new Dictionary<DiamondSlot, Combatant>();

        // 겹침 최소화를 위해 “센터에서 더 멀리 있는 유닛 우선”으로 배정(겹칠 때 시각적 충돌 감소)
        foreach (var it in scored.OrderByDescending(s => s.off.sqrMagnitude))
        {
            // 이 유닛이 선호하는 슬롯 우선순위(각도 차이가 작은 순)
            var pref = GetSlotPreferenceByAngle(it.angDeg, slotAngles);

            foreach (var slot in pref)
            {
                if (!assigned.ContainsKey(slot))
                {
                    assigned[slot] = it.c;
                    break;
                }
            }
            // 모든 슬롯이 이미 찼으면(적이 4명 초과일 때) 무시됨 → 최대 4명만 표시
        }

        // [역할] 바인딩: 유닛이 배정된 슬롯만 활성, 나머지는 숨김
        BindSlot(slotTop, assigned.TryGetValue(slotTop, out var cTop) ? cTop : null);
        BindSlot(slotRight, assigned.TryGetValue(slotRight, out var cRight) ? cRight : null);
        BindSlot(slotBottom, assigned.TryGetValue(slotBottom, out var cBottom) ? cBottom : null);
        BindSlot(slotLeft, assigned.TryGetValue(slotLeft, out var cLeft) ? cLeft : null);

        // [역할] 리빌드 직후 HUD 중심 위치 보정(스무딩 시작점 세팅)
        UpdateRootPositionImmediate(side);

    }

    /// <summary>
    /// [역할] 슬롯 하나를 Combatant에 바인딩(표시 전용이면 클릭 콜백 없음).
    /// </summary>
    void BindSlot(DiamondSlot slot, Combatant c)
    {
        if (!slot) return;
        if (!c) { slot.gameObject.SetActive(false); return; }

        slot.gameObject.SetActive(true);
        slot.Bind(c, interactive ? OnSlotClicked : null);
    }

    /// <summary>
    /// [역할] 화면-로컬 좌표 변환 시도 (캔버스 기준 localPoint 산출)
    /// </summary>
    bool TryWorldToCanvasLocal(Camera cam, RectTransform canvasRect, Vector3 worldPos, out Vector2 local)
    {
        local = default;
        if (!cam || !canvasRect) return false;
        var sp = cam.WorldToScreenPoint(worldPos);
        if (sp.z <= 0f) return false;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, cam, out local);
    }

    /// <summary>
    /// [역할] 특정 각도(유닛의 화면상 방향)에 대해
    ///        Top(90) / Right(0) / Bottom(-90) / Left(±180)과의 각도 차가
    ///        작은 슬롯부터 우선순위를 리턴한다.
    /// </summary>
    IEnumerable<DiamondSlot> GetSlotPreferenceByAngle(float angDeg, Dictionary<DiamondSlot, float> slotAngles)
    {
        float Norm(float a)
        {
            // [역할] -180~180 범위로 정규화
            while (a > 180f) a -= 360f;
            while (a < -180f) a += 360f;
            return a;
        }

        // 각 슬롯과의 각도 차(절대값) 계산 후 오름차순
        return slotAngles
            .Select(kv => (slot: kv.Key, diff: Mathf.Abs(Norm(angDeg - kv.Value))))
            .OrderBy(t => t.diff)
            .Select(t => t.slot);
    }


    /// <summary>
    /// [역할] 오프셋 리스트에서 X/Y 기준 '최댓값' 또는 '최솟값'을 고르되,
    ///        동률이면 Front 우선, 그다음 Back.
    /// </summary>
    Combatant PickBy(List<(Combatant c, Vector2 local)> list, System.Func<Vector2, float> keySel, Loc tieBreakFrontFirst, bool max)
    {
        if (list == null || list.Count == 0) return null;

        float best = max ? float.NegativeInfinity : float.PositiveInfinity;
        List<(Combatant c, float k)> candidates = new();

        foreach (var it in list)
        {
            float k = keySel(it.local);
            if ((max && k > best) || (!max && k < best))
            {
                best = k;
                candidates.Clear();
                candidates.Add((it.c, k));
            }
            else if (Mathf.Approximately(k, best))
            {
                candidates.Add((it.c, k));
            }
        }

        // 동률 -> Front 우선, 없으면 Back
        var front = candidates.FirstOrDefault(x => x.c.currentLoc == Loc.Front).c;
        if (front) return front;
        var back = candidates.FirstOrDefault(x => x.c.currentLoc == Loc.Back).c;
        return back ? back : candidates[0].c;
    }

    /// <summary>
    /// [역할] 슬롯 클릭으로 확정(인터랙티브 모드에서만 사용).
    /// </summary>
    void OnSlotClicked(Combatant c)
    {
        if (!c) return;
        BattleManager.Instance?.NotifyCombatantClicked(c);
    }

    // ── 방향 판단 & 슬롯 회전 ─────────────────────
    /// <summary>
    /// [역할] 해당 측 유닛들의 "평균 전방"을 화면에 투영해,
    ///        카메라 Right와의 내적으로 우향(+) / 좌향(-)을 판정.
    /// </summary>
    bool IsSideFacingRightOnScreen(Side side)
    {
        var cam = hudCanvas.worldCamera ? hudCanvas.worldCamera : Camera.main;
        if (!cam) return true;

        var members = FindObjectsOfType<Combatant>(true)
            .Where(c => c && c.IsAlive && c.side == side).ToList();
        if (members.Count == 0) return true;

        // 평균 forward를 카메라 평면에 투영
        Vector3 avgFwd = Vector3.zero;
        foreach (var m in members) avgFwd += m.transform.forward;
        avgFwd /= members.Count;

        // 카메라 업 기준으로 평면 투영
        Vector3 camUp = cam.transform.up;
        Vector3 proj = Vector3.ProjectOnPlane(avgFwd, camUp).normalized;

        // 화면상의 우측 여부(카메라의 Right와 내적)
        float dot = Vector3.Dot(proj, cam.transform.right);
        return dot >= 0f; // 0 이상이면 화면 기준 오른쪽을 향함
    }

    /// <summary>
    /// [역할] 파티가 오른쪽을 향하면 "시계 방향"으로, 왼쪽이면 "반시계 방향"으로
    ///        기본 다이아몬드 좌표를 회전시켜 슬롯에 적용.
    /// </summary>
    void ApplyDiamondRotation(bool facingRight)
    {
        // 기본(0도) 순서: Top, Right, Bottom, Left
        Vector2 top = _pTop, right = _pRight, bottom = _pBottom, left = _pLeft;

        if (facingRight)
        {
            // 시계 방향 회전: Top→Right, Right→Bottom, Bottom→Left, Left→Top
            var T = top;
            top = left;        // Left가 위로
            left = bottom;     // Bottom이 좌로
            bottom = right;    // Right가 아래로
            right = T;         // Top이 오른쪽으로
        }
        else
        {
            // 반시계 방향 회전: Top→Left, Left→Bottom, Bottom→Right, Right→Top
            var T = top;
            top = right;       // Right가 위로
            right = bottom;    // Bottom이 오른쪽으로
            bottom = left;     // Left가 아래로
            left = T;          // Top이 좌로
        }

        ApplyAnchors(top, right, bottom, left);
    }

    /// <summary>
    /// [역할] 슬롯들의 anchoredPosition을 한 번에 적용.
    /// </summary>
    void ApplyAnchors(Vector2 top, Vector2 right, Vector2 bottom, Vector2 left)
    {
        if (slotTop) slotTop.GetComponent<RectTransform>().anchoredPosition = top;
        if (slotRight) slotRight.GetComponent<RectTransform>().anchoredPosition = right;
        if (slotBottom) slotBottom.GetComponent<RectTransform>().anchoredPosition = bottom;
        if (slotLeft) slotLeft.GetComponent<RectTransform>().anchoredPosition = left;
    }

    // ── 표시 on/off ────────────────────────────────
    void Show(bool show)
    {
        _active = show;
        if (root) root.gameObject.SetActive(show);
    }

    // ── 월드 중심 추적 ─────────────────────────────
    void LateUpdate()
    {
        if (!_active || !root || !hudCanvas) return;
        UpdateRootPositionSmooth(_currentSide);
    }

    /// <summary>[역할] 부드럽게 추적</summary>
    void UpdateRootPositionSmooth(Side side)
    {
        var cam = hudCanvas.worldCamera ? hudCanvas.worldCamera : Camera.main;
        if (!cam) return;

        Vector3 centerWorld = ComputeWorldCenter(side) + Vector3.up * worldCenterYOffset;
        var canvasRect = (RectTransform)hudCanvas.transform;
        var sp = cam.WorldToScreenPoint(centerWorld);
        if (sp.z <= 0f) { root.gameObject.SetActive(false); return; }
        if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, cam, out var local))
        {
            var cur = root.anchoredPosition;
            root.anchoredPosition = smoothFollow ? Vector2.SmoothDamp(cur, local, ref _vel, smoothTime) : local;
        }
    }

    /// <summary>[역할] 즉시 위치 세팅(리빌드 직후 1회)</summary>
    void UpdateRootPositionImmediate(Side side)
    {
        var cam = hudCanvas.worldCamera ? hudCanvas.worldCamera : Camera.main;
        if (!cam) return;

        Vector3 centerWorld = ComputeWorldCenter(side) + Vector3.up * worldCenterYOffset;
        var canvasRect = (RectTransform)hudCanvas.transform;
        var sp = cam.WorldToScreenPoint(centerWorld);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, cam, out var local))
        {
            _vel = Vector2.zero;
            root.anchoredPosition = local;
        }
    }

    /// <summary>
    /// [역할] 해당 측 Combatant들의 렌더러 바운즈를 합쳐 “현재 실제 중심” 계산
    /// </summary>
    Vector3 ComputeWorldCenter(Side side)
    {
        var members = FindObjectsOfType<Combatant>(true)
            .Where(c => c && c.IsAlive && c.side == side).ToList();

        Renderer first = null;
        foreach (var c in members)
        {
            var r = c.GetComponentInChildren<Renderer>(true);
            if (r) { first = r; break; }
        }
        if (!first)
        {
            if (members.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (var c in members) sum += c.transform.position;
            return sum / members.Count;
        }

        Bounds b = first.bounds;
        foreach (var c in members)
            foreach (var r in c.GetComponentsInChildren<Renderer>(true))
                b.Encapsulate(r.bounds);

        return b.center;
    }
}
