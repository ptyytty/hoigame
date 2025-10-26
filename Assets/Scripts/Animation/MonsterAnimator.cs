using System.Collections;
using UnityEngine;

/// <summary>
/// (모바일 최적화) 리깅 없이도 가능하도록 Transform/Material만으로
/// - 공격: 뒤로 움찔(윈드업) → 앞으로 박치기 → 제자리 복귀
/// - 피격: 짧은 뒤로 튕김 + 색상 플래시
/// BattleManager가 PlayAttack/PlayHit/GuessAttackTotalSec을 호출함.
/// </summary>
public class MonsterAnimator : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("이동/회전의 기준 트랜스폼(보통 루트). 비우면 this.transform")]
    public Transform root;
    [Tooltip("시각 오브젝트(스케일/가벼운 흔들기). 비우면 root 사용")]
    public Transform visual;
    [Tooltip("히트 플래시용 Renderer(선택)")]
    public Renderer mainRenderer;

    [Header("Timings (sec)")]
    [Tooltip("뒤로 움찔(윈드업) 시간")]
    public float windupTime = 0.18f;
    [Tooltip("앞으로 박치기(임팩트) 시간")]
    public float lungeTime = 0.14f;
    [Tooltip("제자리 복귀 시간")]
    public float recoverTime = 0.18f;
    [Tooltip("피격 플래시 시간")]
    public float hitFlashTime = 0.08f;

    [Header("Distances (meter)")]
    [Tooltip("윈드업 때 뒤로 물러나는 거리(루트.forward의 -방향)")]
    public float backDistance = 0.25f;
    [Tooltip("임팩트 때 앞으로 박는 거리(루트.forward의 +방향)")]
    public float forwardDistance = 0.35f;

    [Header("Scale (Squash/Stretch)")]
    public Vector3 windupScale = new Vector3(0.92f, 1.08f, 0.92f);
    public Vector3 impactScale = new Vector3(1.10f, 0.90f, 1.10f);

    [Header("Material Props (선택)")]
    public string colorProperty = "_BaseColor";
    public Color hitFlashColor = Color.white;

    [Header("Rotation Options")]
    [Tooltip("수평(Yaw)만 회전하도록 강제")]
    public bool yawOnly = true;

    [Tooltip("모델 정면이 +Z가 아니면 보정 (예: -Z면 180)")]
    public float modelForwardYawOffset = 0f;

    [Header("Hit Flash (per-submesh)")]
    [Tooltip("플래시를 적용하지 않을 서브메시 인덱스들 (예: pupil)")]
    public int[] excludeFlashSubmeshIndices;

    Material[] _mats;               // 이 렌더러가 가진 서브머티리얼들
    Color[] _originalColors;

    // 내부 캐시
    Quaternion _visualInitLocalRot;   // 시각 오브젝트의 초기 로컬 회전
    Quaternion _rootInitRot;          // 루트 초기 회전(필요 시 참조)


    // 내부 상태
    Coroutine _playing;
    Vector3 _basePos;
    Vector3 _baseScale;
    Quaternion _baseRot;
    MaterialPropertyBlock _mpb;
    Color _originalColor;

    // 프레임 캐시
    static readonly WaitForEndOfFrame _wf = new WaitForEndOfFrame();

    void Awake()
    {
        if (!root) root = transform;
        if (!visual) visual = root;

        _baseScale = visual.localScale;
        _baseRot = root.rotation;
        _basePos = root.position;

        // ★ 추가: 초기 로테이션 보관
        _visualInitLocalRot = visual.localRotation;
        _rootInitRot = root.rotation;

        if (mainRenderer)
        {
            _mpb = new MaterialPropertyBlock();
            _mats = mainRenderer.sharedMaterials;
            if (_mats != null && _mats.Length > 0)
            {
                _originalColors = new Color[_mats.Length];
                for (int i = 0; i < _mats.Length; i++)
                {
                    var m = _mats[i];
                    _originalColors[i] = (m && m.HasProperty(colorProperty))
                        ? m.GetColor(colorProperty)
                        : Color.white;
                }
            }
        }
    }

    /// <summary>
    /// [역할] 공격 전 목표를 바라보도록 Yaw 회전(선택 호출).
    /// </summary>
    public void AimAt(Vector3 worldPos)
    {
        Vector3 dir = worldPos - root.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        // dir → Yaw 각도(도)
        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        yaw += modelForwardYawOffset;   // 정면 보정

        // ★ 수평(Y)만 회전
        root.rotation = Quaternion.Euler(0f, yaw, 0f);

        // ★ 시각 오브젝트의 로컬 회전은 항상 초기값 유지(기울어 눕는 현상 방지)
        visual.localRotation = _visualInitLocalRot;
    }

    /// <summary>
    /// [역할] 공격 연출 시작(뒤로 → 앞으로 → 복귀). BattleManager가 호출.
    /// </summary>
    public void PlayAttack()
    {
        if (_playing != null) StopCoroutine(_playing);
        _playing = StartCoroutine(CoAttack());
    }

    /// <summary>
    /// [역할] 피격 반응(짧은 뒤로 밀림 + 플래시). BattleManager가 명중 대상에 호출.
    /// </summary>
    public void PlayHit()
    {
        if (_playing != null) StopCoroutine(_playing);
        _playing = StartCoroutine(CoHit());
    }

    /// <summary>
    /// [역할] 공격 전체 예상 시간(컷 길이 산정용). BattleManager가 참조.
    /// </summary>
    public float GuessAttackTotalSec(float fallback) => Mathf.Max(fallback, windupTime + lungeTime + recoverTime);

    // ===== 내부 구현 =====

    IEnumerator CoAttack()
    {
        // 기준 상태 기록
        var startPos = root.position;
        var startScale = visual.localScale;

        // ★ 시작 시 한 번 자세 보정
        var e = root.eulerAngles;
        root.rotation = Quaternion.Euler(0f, e.y, 0f);
        visual.localRotation = _visualInitLocalRot;

        // 1) 뒤로 움찔 …
        Vector3 backPos = startPos - root.forward * Mathf.Abs(backDistance);
        yield return MoveAndScaleOverTime(startPos, backPos, startScale, windupScale, windupTime);

        // 2) 앞으로 박치기 …
        Vector3 hitPos = backPos + root.forward * (Mathf.Abs(backDistance) + Mathf.Abs(forwardDistance));
        yield return MoveAndScaleOverTime(backPos, hitPos, windupScale, impactScale, lungeTime);

        // 3) 제자리 복귀 …
        yield return MoveAndScaleOverTime(hitPos, startPos, impactScale, _baseScale, recoverTime);

        // ★ 종료 시에도 반드시 자세 복원
        e = root.eulerAngles;
        root.rotation = Quaternion.Euler(0f, e.y, 0f);
        visual.localRotation = _visualInitLocalRot;

        _playing = null;
    }

    IEnumerator CoHit()
    {
        var startPos = root.position;

        SetFlashOnRenderer(hitFlashColor);

        // 머티리얼 플래시
        if (mainRenderer && _mpb != null)
        {
            mainRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(colorProperty, hitFlashColor);
            mainRenderer.SetPropertyBlock(_mpb);
        }

        ClearFlashOnRenderer();

        // 짧은 뒤로 밀림
        float t = 0f; float dur = Mathf.Max(0.04f, hitFlashTime);
        Vector3 back = -root.forward * 0.15f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            root.position = Vector3.Lerp(startPos, startPos + back, Mathf.Sin(k * Mathf.PI));
            // 아주 미세한 흔들림
            float shake = (Random.value - 0.5f) * 0.02f;
            visual.localPosition = new Vector3(shake, 0f, 0f);
            yield return _wf;
        }
        visual.localPosition = Vector3.zero;

        // 원위치 복귀
        yield return MoveOverTime(root, root.position, startPos, 0.08f);

        // 플래시 원복
        if (mainRenderer && _mpb != null)
        {
            mainRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(colorProperty, _originalColor);
            mainRenderer.SetPropertyBlock(_mpb);
        }

        _playing = null;
    }

    // ---- 유틸 ----

    /// <summary>트랜스폼 이동 보간(선형)</summary>
    IEnumerator MoveOverTime(Transform tr, Vector3 from, Vector3 to, float dur)
    {
        if (dur <= 0f) { tr.position = to; yield break; }
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            tr.position = Vector3.Lerp(from, to, Mathf.Clamp01(t / dur));
            yield return _wf;
        }
        tr.position = to;
    }

    /// <summary>이동+스케일 동시 보간</summary>
    IEnumerator MoveAndScaleOverTime(Vector3 fromPos, Vector3 toPos, Vector3 fromScale, Vector3 toScale, float dur)
    {
        if (dur <= 0f)
        {
            root.position = toPos; visual.localScale = toScale; yield break;
        }
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            root.position = Vector3.Lerp(fromPos, toPos, k);
            visual.localScale = Vector3.Lerp(fromScale, toScale, k);
            yield return _wf;
        }
        root.position = toPos;
        visual.localScale = toScale;
    }

    /// <summary>
    /// [역할] 피격 순간, 지정한 서브메시(눈 등) 제외하고 플래시를 건다
    /// </summary>
    void SetFlashOnRenderer(Color c)
    {
        if (!mainRenderer || _mats == null) return;

        // 서브메시별로 MPB를 따로 세팅해야 개별 제어가 가능
        for (int i = 0; i < _mats.Length; i++)
        {
            // 제외 목록이면 스킵
            if (excludeFlashSubmeshIndices != null &&
                System.Array.IndexOf(excludeFlashSubmeshIndices, i) >= 0)
            {
                // 제외 서브메시는 원래 색 유지
                _mpb.Clear();
                if (_mats[i] && _mats[i].HasProperty(colorProperty))
                    _mpb.SetColor(colorProperty, _originalColors[i]);
                mainRenderer.SetPropertyBlock(_mpb, i);
                continue;
            }

            _mpb.Clear();
            if (_mats[i] && _mats[i].HasProperty(colorProperty))
                _mpb.SetColor(colorProperty, c);
            mainRenderer.SetPropertyBlock(_mpb, i);
        }
    }

    /// <summary>
    /// [역할] 피격 플래시 해제(모든 서브메시를 원래 색으로 복구)
    /// </summary>
    void ClearFlashOnRenderer()
    {
        if (!mainRenderer || _mats == null) return;

        for (int i = 0; i < _mats.Length; i++)
        {
            _mpb.Clear();
            if (_mats[i] && _mats[i].HasProperty(colorProperty))
                _mpb.SetColor(colorProperty, _originalColors[i]);
            mainRenderer.SetPropertyBlock(_mpb, i);
        }
    }

}
