using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
/// <summary>
/// PartyManager
/// Party 이동 제어, 전투 UI 제어
/// </summary>

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager instance { get; private set; }

    [Header("Inventory")]
    [SerializeField] private DungeonInventory dungeonInventory;
    [SerializeField] private DungeonInventoryBinder inventoryBinder;

    [Header("Dungeon UI")]
    public GameObject moveLeft;
    public GameObject moveRight;

    [Header("Battle UI")]
    public GameObject battleUI;
    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // ✅ 전투 시작 이벤트 구독(게임 시작부터 살아있게)
        EnemySpawner.OnBattleStart += HandleBattleStart;
    }

    void Start()
    {
        // 자동 찾기(인스펙터 비워도 OK)
        if (!dungeonInventory) dungeonInventory = FindObjectOfType<DungeonInventory>(true);

        // 1) PartyBridge → DungeonInventory 스냅샷 1회 적용
        var snap = PartyBridge.Instance?.dungeonLoadoutSnapshot;
        if (dungeonInventory && snap != null && snap.Count > 0)
        {
            dungeonInventory.ApplySnapshot(snap);              // 6칸 재초기화 후 채움 :contentReference[oaicite:10]{index=10}
            PartyBridge.Instance.dungeonLoadoutSnapshot = null;
        }

        // 1) PartyBridge → DungeonInventory로 스냅샷 1회 적용
        //ApplyInitialInventoryFromBridge_Once();

        // 2) 바인더 연결(이벤트 구독 + 즉시 그리기)
        if (inventoryBinder && dungeonInventory)
            inventoryBinder.Bind(dungeonInventory);
    }

    void OnDestroy()
    {
        // 🔒 누수 방지
        EnemySpawner.OnBattleStart -= HandleBattleStart;
    }

    public Transform partyTransform;
    public float moveSpeed = 50f;  // 이동 속도
    private bool isMoving = false;
    private bool isInFrontRow = true; // 앞열인지 뒷열인지 구분하는 변수

    public MoveDirection currentDir { get; private set; }

    // ─────────────────────────────────────
    // ▼ 전투 시작 => 이동 정지, UI 전환
    void HandleBattleStart(IReadOnlyList<Job> heroes, IReadOnlyList<GameObject> enemies)
    {
        StopMoveHard();        // 이동 중이면 즉시 정지
                               // 필요하면 여기서 입력 UI 숨김, 카메라 홀드 등도 같이 처리 가능

        moveLeft.SetActive(false);
        moveRight.SetActive(false);

        battleUI.SetActive(true);
    }

    // 전투 종료 => UI 전환
    public void ShowDungeonUIAfterBattle()
    {
        // TODO: 전투 보상 화면 추가

        if (battleUI) battleUI.SetActive(false);
        if (moveLeft) moveLeft.SetActive(true);
        if (moveRight) moveRight.SetActive(true);
    }
    // ─────────────────────────────────────

    void Update()
    {
        if (isMoving)
        {
            Vector3 dir = GetMoveVector(currentDir);
            partyTransform.Translate(dir * moveSpeed * Time.deltaTime);
        }
    }

    public void StartMove(int dir)
    {
        currentDir = (MoveDirection)dir;  //정수 -> 열거형 캐스팅
        isMoving = true;

        if (currentDir == MoveDirection.Left && !isInFrontRow)
        {
            transform.Rotate(0, -180, 0);
            isInFrontRow = true;
        }
        else if (currentDir == MoveDirection.Right && isInFrontRow)
        {
            transform.Rotate(0, 180, 0);
            isInFrontRow = false;
        }
    }

    public void StopMove() { isMoving = false; }
    public void StopMoveHard() { isMoving = false; /* 필요 시 추가로 속도/트위닝도 여기서 끊기 */ }
    public void ResumeMove() { isMoving = true; }           // 전투 끝나고 다시 움직일 때 호출
    public void ResumeMoveIfNeeded() { /* 조건부 재개가 필요하면 여기에 로직 */ }

    Vector3 GetMoveVector(MoveDirection dir)
    {
        if (isInFrontRow)
            return dir == MoveDirection.Left ? Vector3.back : Vector3.forward;
        else
            return dir == MoveDirection.Left ? Vector3.forward : Vector3.back;
    }

    // ======= 인벤토리 =======
    void ApplyInitialInventoryFromBridge_Once()
    {
        if (!dungeonInventory)
        {
            Debug.LogWarning("[DungeonManager] DungeonInventory 미할당");
            return;
        }

        var bridge = PartyBridge.Instance;
        if (bridge == null)
        {
            Debug.LogWarning("[DungeonManager] PartyBridge 미존재");
            return;
        }

        var snap = bridge.dungeonLoadoutSnapshot;
        if (snap == null || snap.Count == 0)
        {
            Debug.Log("[DungeonManager] 적용할 인벤토리 스냅샷 없음(빈 상태 유지)");
            return;
        }

        dungeonInventory.ApplySnapshot(snap);   // ← 던전 6칸에 직접 반영
        bridge.dungeonLoadoutSnapshot = null;   // 재적용 방지
        Debug.Log("[DungeonManager] 브릿지 스냅샷을 DungeonInventory에 적용 완료");
    }
}

// 이동 방향 열
public enum MoveDirection
{
    Left = 0,
    Right = 1
}
