using System.Collections.Generic;
using Game.Visual;
using UnityEngine;

/// <summary>
/// [역할] PartyBridge의 ActiveParty를 읽어
/// 각 슬롯에 프리팹을 스폰하고 애니메이션 바인딩을 수행
/// </summary>
public class DungeonPartySpawner : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public Transform root;                 // 캐릭터가 놓일 부모
        public HeroVisualBinder binder;        // 바인더(없으면 자동 Add)
    }

    [Header("Party Slots (좌→우, 앞→뒤)")]
    public List<Slot> slots = new();           // 4칸 가정

    public string combatantLayerName = "Combatant";
    int CombatantLayer => LayerMask.NameToLayer(combatantLayerName);

    [Header("Hero Definition Lookup")]
    public List<HeroDefinition> heroDefs = new();
    private Dictionary<int, HeroDefinition> _byId;

    void Awake()
    {
        _byId = new Dictionary<int, HeroDefinition>(heroDefs.Count);
        foreach (var def in heroDefs)
        {
            if (!def) continue;
            if (_byId.ContainsKey(def.jobId))
                Debug.LogWarning($"[PartyVisualSpawner] 중복 jobId 감지: {def.jobId} ({def.name})");
            else
                _byId.Add(def.jobId, def);
        }
    }

    void Start()
    {
        SpawnParty();
    }

    // [역할] id/name 등으로 HeroDefinition을 찾아오는 간단 매퍼
    HeroDefinition FindDefFor(Job job)
    {
        if (job == null) return null;
        return _byId != null && _byId.TryGetValue(job.id_job, out var def) ? def : null;
    }

    /// <summary>
    /// [역할] 파티 정보를 읽어 슬롯에 스폰/바인딩
    /// </summary>
    public void SpawnParty()
    {
        if (!PartyBridge.Instance || !PartyBridge.Instance.HasParty())
        {
            Debug.LogWarning("[PartyVisualSpawner] 파티가 비었습니다.");
            return;
        }

        var party = PartyBridge.Instance.ActiveParty;
        for (int i = 0; i < slots.Count && i < party.Count; i++)
        {
            var job = party[i];
            var slot = slots[i];


            if (!slot.binder)
                slot.binder = slot.root.gameObject.GetComponent<HeroVisualBinder>()
                              ?? slot.root.gameObject.AddComponent<HeroVisualBinder>();

            var def = FindDefFor(job);
            slot.binder.SetHeroDefinition(def);
            slot.binder.SpawnAndBind(); // ← 프리팹 생성/애니메이션 바인딩

            // === Combatant/레이어/콜라이더 보장 시작 ===
            var go = slot.binder.Instance;
            if (!go) continue;

            var combatant = go.GetComponent<Combatant>() ?? go.AddComponent<Combatant>();
            combatant.AutoInitByHierarchy(heroCandidate: job);

            // 1) Combatant 부착 + 영웅 데이터 주입
            var c = go.GetComponent<Combatant>() ?? go.AddComponent<Combatant>();
            // 역할: 상위 태그 기반 진영/행 등 자동 판단 + 이 슬롯의 영웅을 heroCandidate로 주입
            c.AutoInitByHierarchy(heroCandidate: job); // ← BattleManager가 FindByHero로 찾을 수 있게 연결 :contentReference[oaicite:4]{index=4}

            // 2) 레이어(자식 포함) 일괄 적용 — BattleInput 레이캐스트 정확도/성능
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = CombatantLayer;

            // // 3) 레이캐스트용 콜라이더 보장
            // if (addCapsuleIfMissing && !go.GetComponentInChildren<Collider>())
            // {
            //     var col = go.AddComponent<CapsuleCollider>();
            //     col.center = new Vector3(0, 1.0f, 0);
            //     col.radius = 0.35f;
            //     col.height = 2.0f;
            //     col.direction = 1; // Y축
            // }
            // === Combatant/레이어/콜라이더 보장 끝 ===
        }
    }

    /// <summary>
    /// [역할] 외부(버튼/전투 로직)에서 호출: i번 슬롯을 달리기/대기 블렌딩
    /// </summary>
    public void SetRun(int index, bool run)
    {
        if (index < 0 || index >= slots.Count) return;
        slots[index].binder?.SetMoveSpeed01(run ? 1f : 0f); // Speed 파라미터 제어 :contentReference[oaicite:5]{index=5}
    }

    /// <summary>
    /// [역할] 외부에서 호출: i번 슬롯 공격 모션
    /// </summary>
    public void PlayAttack(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        slots[index].binder?.PlayAttack(); // Trigger("Attack") :contentReference[oaicite:6]{index=6}
    }

    /// <summary>
    /// [역할] 외부에서 호출: i번 슬롯 피격 모션
    /// </summary>
    public void PlayHit(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        slots[index].binder?.PlayHit(); // Trigger("Hit") :contentReference[oaicite:7]{index=7}
    }
}
