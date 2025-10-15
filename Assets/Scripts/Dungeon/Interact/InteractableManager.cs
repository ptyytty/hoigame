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

    // 아웃라인 공통 설정
    [Header("Outline Settings (Common)")]
    public Material outlineMaterial;
    public Color outlineColor = Color.black;
    [Range(0.01f, 0.5f)] public float outlineWidth = 0.1f;

    // ▼ Scanner가 세팅해줄 현재 타깃
    private Interactable currentObjectTarget;

    void Awake()
    {
        if (instance == null) instance = this; else { Destroy(gameObject); return; }
    }

    void Start()
    {
        AutoFindFloorAndScan();
        AssingInteractables();

        // ✅ 전역 버튼 리스너는 여기서 '딱 한 번' 등록
        if (interactionUp) interactionUp.onClick.AddListener(() => Nudge(-stairNudge));  // 보상 없음
        if (interactionDown) interactionDown.onClick.AddListener(() => Nudge(+stairNudge)); // 보상 없음

        var objBtn = interactionObj?.GetComponent<Button>();
        if (objBtn) objBtn.onClick.AddListener(GrantObjectRewardAndToast);
    }

    /// <summary>
    /// candidates 리스트에 있는 오브젝트 중 확률에 따라 Interactable 컴포넌트 부여
    /// </summary>
    // 상호작용 랜덤 배정
    void AssingInteractables()
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

    }
    /// <summary>
    /// "Floor" 태그를 가진 오브젝트를 찾아 그 하위 오브젝트들을 대상으로 candidates와 stairs 자동 등록
    /// </summary>
    void AutoFindFloorAndScan()
    {
        GameObject[] floorObject = GameObject.FindGameObjectsWithTag("Floor");

        if (floorObject == null || floorObject.Length == 0)
        {
            Debug.LogError("❌ 'Floor' 태그를 가진 오브젝트를 찾을 수 없습니다.");
            return;
        }

        int objCount = 0;
        int stairCount = 0;

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
                        objCount++;
                        break;
                    }
                }

                foreach (string keyword in stairFilters)
                {
                    if (objName.Contains(keyword.ToLower()) && !stairs.Contains(child.gameObject))
                    {
                        stairs.Add(child.gameObject);
                        stairCount++;
                        break;
                    }
                }
            }
        }
        //Debug.Log($"✅ 자동 등록 완료: 상호작용 오브젝트 {objCount}개, 계단 {stairCount}개");
    }

    // 대상 오브젝트에 OutlineDuplicator 구성
    void EnsureOutlineSetup(GameObject obj)
    {
        var mat = OutlineMaterialProvider.GetShared();
        if (!mat) return;

        if (!obj.TryGetComponent(out OutlineDuplicator od))
            od = obj.AddComponent<OutlineDuplicator>();

        od.outlineMaterial = mat;
        od.autoEnableOnSetProperties = false;             // 세팅단계에선 자동 ON 금지
        od.SetProperties(outlineColor, outlineWidth, false);

        // [진단] 대상 오브젝트의 월드 위치 출력
        Debug.Log($"[OL-TARGET] {obj.name} worldPos={obj.transform.position}");
    }

    void Nudge(float dx)
    {
        if (!party || !partyCam) return;
        var p = party.transform.position; p.x += dx; party.transform.position = p;
        var c = partyCam.transform.position; c.x += dx; partyCam.transform.position = c;
    }

    public void SetCurrentObjectTarget(Interactable it) => currentObjectTarget = it;

    public void SetInteractButtonsVisible(bool up, bool down, bool obj)
    {
        if (interactionUp) interactionUp.gameObject.SetActive(up);
        if (interactionDown) interactionDown.gameObject.SetActive(down);
        if (interactionObj) interactionObj.SetActive(obj);
    }

    // 상호작용 오브젝트 보상 지급
    void GrantObjectRewardAndToast()
    {
        if (currentObjectTarget == null) return;
        if (!currentObjectTarget.IsEligibleForReward)
        {
            // 이미 받은 대상이면 오브젝트 버튼 숨김
            SetInteractButtonsVisible(
                interactionUp && interactionUp.gameObject.activeSelf,
                interactionDown && interactionDown.gameObject.activeSelf,
                false
            );
            return;
        }

        // ✅ 소울 1개 + 코인 랜덤
        int soulTypeCount = System.Enum.GetValues(typeof(SoulType)).Length;
        var soulType = (SoulType)Random.Range(0, soulTypeCount);
        int soulAmount = fixedSoulOneOnInteract ? 1 : 1;
        int coins = Random.Range(coinRangeOnInteract.x, coinRangeOnInteract.y + 1);

        if (RunReward.Instance == null) new GameObject("RunReward").AddComponent<RunReward>();
        RunReward.Instance.AddBattleDrop(soulType, soulAmount, coins);

        DungeonManager.instance?.ShowBattleRewardToast(soulType, soulAmount, coins);

        // 1회성 마킹 + 버튼 숨김
        currentObjectTarget.MarkClaimed();
        SetInteractButtonsVisible(
            interactionUp && interactionUp.gameObject.activeSelf,
            interactionDown && interactionDown.gameObject.activeSelf,
            false
        );
    }

    private Material GetOrCreateOutlineMat()
    {
        return OutlineMaterialProvider.GetShared();
    }
}

