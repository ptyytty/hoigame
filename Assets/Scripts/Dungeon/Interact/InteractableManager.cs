#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractableManager : MonoBehaviour
{
    public static InteractableManager instance;

    [SerializeField] private List<GameObject> candidates; // 상호작용 가능 오브젝트 목록
    [SerializeField] private List<GameObject> stairs;   // 계단 목록
    [SerializeField] private float interactionChance = 0.1f;    // 상호작용 적용 확률

    [SerializeField] private string[] nameFilters = { "Cabinet", "vending machine V2", "water purifier" };
    [SerializeField] private string[] stairFilters = { "UpStairs", "DownStairs" };

    [Header("Object")]
    public GameObject party;
    public GameObject partyCam;

    [Header("Interact UI")]
    public Button interactionUp;
    public Button interactionDown;
    public GameObject interactionObj;

    // ▼ 상호작용 보상 파라미터(필요 시 인스펙터 조절)
    [Header("Interact Reward Settings")]
    public Vector2Int coinRangeOnInteract = new Vector2Int(80, 300);
    public bool fixedSoulOneOnInteract = true;
    public float stairNudge = 150f;

    [Header("Outline Settings (Common)")]
    [Tooltip("직접 지정하면 Provider보다 이 머티리얼을 우선 사용")]
    public Material outlineMaterial;                  // 수동 직결 머티리얼
    public Color outlineColor = Color.black;
    [Range(0.01f, 0.5f)] public float outlineWidth = 0.1f;

    [Header("Debug")]
    public bool debugAlwaysOn = false;                // 스캔 직후 무조건 Enable → 눈으로 확인
    public bool debugLogTargets = true;
#if UNITY_EDITOR
    public bool editorPreviewAll = false;
#endif

    // ▼ Scanner가 세팅해줄 현재 타깃
    private Interactable currentObjectTarget;

    void Awake()
    {
        if (instance == null) instance = this; else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (outlineMaterial) OutlineMaterialProvider.GetShared();
        AutoFindFloorAndScan();
        AssignInteractables();

        if (interactionUp) interactionUp.onClick.AddListener(() => Nudge(-stairNudge));
        if (interactionDown) interactionDown.onClick.AddListener(() => Nudge(+stairNudge));

        var objBtn = interactionObj?.GetComponent<Button>();
        if (objBtn) objBtn.onClick.AddListener(GrantObjectRewardAndToast);

        // ✅ 시작 시 전체 OFF (스캔되면 개별로 ON)
        ForceAllOutlines(false);

        // (선택) 에디터에서만 전부 미리보기
#if UNITY_EDITOR
        if (editorPreviewAll) ForceAllOutlines(true);
#endif
    }

    // 상호작용 가능 오브젝트 스캔
    void AssignInteractables()
    {
        foreach (GameObject obj in candidates)
        {
            if (Random.value <= interactionChance)
            {
                if (!obj.TryGetComponent(out Interactable interactable))
                    obj.AddComponent<Interactable>();
                EnsureOutlineSetup(obj);
            }
        }

        foreach (GameObject obj in stairs)
        {
            if (!obj.TryGetComponent(out Interactable interactable))
                obj.AddComponent<Interactable>();
            EnsureOutlineSetup(obj);
        }

        // 전역 설정 일괄 반영
        ApplyGlobalOutlineSettings(false);
    }

    // 계단 오브젝트 스캔
    void AutoFindFloorAndScan()
    {
        GameObject[] floorObject = GameObject.FindGameObjectsWithTag("Floor");
        if (floorObject == null || floorObject.Length == 0)
        {
            Debug.LogError("❌ 'Floor' 태그 오브젝트 없음");
            return;
        }

        foreach (GameObject floor in floorObject)
        {
            foreach (Transform child in floor.transform)
            {
                string objName = child.name.ToLower();
                foreach (string keyword in nameFilters)
                {
                    if (objName.Contains(keyword.ToLower()) && !candidates.Contains(child.gameObject))
                    {
                        candidates.Add(child.gameObject);
                        break;
                    }
                }
                foreach (string keyword in stairFilters)
                {
                    if (objName.Contains(keyword.ToLower()) && !stairs.Contains(child.gameObject))
                    {
                        stairs.Add(child.gameObject);
                        break;
                    }
                }
            }
        }
    }

    // 핵심: 수동 머티리얼 우선 → Provider 폴백
    void EnsureOutlineSetup(GameObject obj)
    {
        // [역할] 머티리얼 확보(Provider > Shader.Find)
        Material mat = outlineMaterial;
        if (!mat && OutlineMaterialProvider.Instance)
            mat = OutlineMaterialProvider.Instance.GetSharedMaterial();
        if (!mat || !mat.shader)
            mat = new Material(Shader.Find("Custom/Outline_Mobile_URP"));

        if (!mat || !mat.shader || !mat.shader.isSupported)
        {
            // [역할] 빌드 환경에서 실패시 이유를 더 자세히 로그
            Debug.LogError($"[InteractableManager] Outline material invalid: mat={mat}, shaderOk={(mat ? mat.shader : null)}, isSupported={(mat && mat.shader ? mat.shader.isSupported : false)}");
            return; // 이 오브젝트만 스킵하고 매니저는 계속 동작
        }

        if (!obj.TryGetComponent(out OutlineDuplicator od))
            od = obj.AddComponent<OutlineDuplicator>();

        od.outlineMaterial = mat;
        od.RebuildIfNeeded();
        od.SetProperties(outlineColor, outlineWidth, enableNow: debugAlwaysOn);
    }

    /// <summary>전역 색/두께 반영, 필요 시 전부 켜서 시각 확인</summary>
    public void ApplyGlobalOutlineSettings(bool enableAllNow = false)
    {
        var list = FindObjectsOfType<OutlineDuplicator>(true);
        foreach (var od in list)
        {
            if (!od) continue;
            od.SetProperties(outlineColor, outlineWidth, enableAllNow);
            if (enableAllNow) od.EnableOutline(true);
        }
    }

    void Nudge(float dx)
    {
        if (!party || !partyCam) return;
        var p = party.transform.position; p.x += dx; party.transform.position = p;
        var c = partyCam.transform.position; c.x += dx; partyCam.transform.position = c;
    }

    public void SetCurrentObjectTarget(Interactable it) => currentObjectTarget = it;

    // 상호작용 UI 표시
    public void SetInteractButtonsVisible(bool up, bool down, bool obj)
    {
        if (interactionUp) interactionUp.gameObject.SetActive(up);
        if (interactionDown) interactionDown.gameObject.SetActive(down);
        if (interactionObj) interactionObj.SetActive(obj);
    }

    // 상호작용 보상 토스트
    void GrantObjectRewardAndToast()
    {
        if (currentObjectTarget == null) return;
        if (!currentObjectTarget.IsEligibleForReward)
        {
            SetInteractButtonsVisible(
                interactionUp && interactionUp.gameObject.activeSelf,
                interactionDown && interactionDown.gameObject.activeSelf,
                false
            );
            return;
        }

        int soulTypeCount = System.Enum.GetValues(typeof(SoulType)).Length;
        var soulType = (SoulType)Random.Range(0, soulTypeCount);
        int soulAmount = fixedSoulOneOnInteract ? 1 : 1;
        int coins = Random.Range(coinRangeOnInteract.x, coinRangeOnInteract.y + 1);

        if (RunReward.Instance == null) new GameObject("RunReward").AddComponent<RunReward>();
        RunReward.Instance.AddBattleDrop(soulType, soulAmount, coins);
        DungeonManager.instance?.ShowBattleRewardToast(soulType, soulAmount, coins);

        currentObjectTarget.MarkClaimed();
        SetInteractButtonsVisible(
            interactionUp && interactionUp.gameObject.activeSelf,
            interactionDown && interactionDown.gameObject.activeSelf,
            false
        );
    }

    // 전투 시작 시 호출
    public void EnterBattleMode()
    {
        // 버튼/타깃/오브젝트 UI 전부 OFF
        SetInteractButtonsVisible(false, false, false);
        if (currentObjectTarget) { currentObjectTarget.ShowUI(false); currentObjectTarget = null; }

        // 씬 내 모든 Interactable의 UI/아웃라인 비활성화
        var all = FindObjectsOfType<Interactable>(true);
        foreach (var it in all) it.ShowUI(false);

        // 아웃라인 전부 강제 OFF
        SetAllOutlines(false);

        // 스캐너 중단
        var scanners = FindObjectsOfType<InteractionScanner>(true);
        foreach (var s in scanners) s.enabled = false;
    }

    // 전투 종료 시
    public void ExitBattleMode()
    {
        // 스캐너 다시 켜기 (Update에서 즉시 스캔됨)
        var scanners = FindObjectsOfType<InteractionScanner>(true);
        foreach (var s in scanners) s.enabled = true;

        StartCoroutine(ForceScanNextFrame(scanners));
    }

    // 강제 스캔
    private IEnumerator ForceScanNextFrame(InteractionScanner[] scanners)
    {
        yield return null; // 다음 프레임까지 대기
        foreach (var s in scanners) if (s && s.isActiveAndEnabled) s.ForceOneScan();
    }

    // 공개 래퍼 (기존 ForceAllOutlines를 감싸서 외부에서도 호출 가능)
    public void SetAllOutlines(bool on)
    {
        var list = FindObjectsOfType<OutlineDuplicator>(true);
    }

    /// <summary>
    /// [역할] 씬의 모든 OutlineDuplicator를 한 번에 켜거나 끈다(초기화/디버그용)
    /// </summary>
    void ForceAllOutlines(bool on)
    {
        var list = FindObjectsOfType<OutlineDuplicator>(true);
        foreach (var od in list)
        {
            if (!od) continue;
            od.EnableOutline(on);
        }
    }
}

