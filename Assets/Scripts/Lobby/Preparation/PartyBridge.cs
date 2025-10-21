using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Save;

///<summary>
/// 씬 간 Party 정보 전달
/// ActiveParty로 외부에서 읽기
/// </summary>
public sealed class PartyBridge : MonoBehaviour
{
    public static PartyBridge Instance { get; private set; }

    // 씬 간 전달용 파티 (null 제외, 최대 4명)
    private readonly List<Job> _activeParty = new(4);
    public IReadOnlyList<Job> ActiveParty => _activeParty;

    // 던전 진입 시 DungeonManager가 1회 적용 후 null 로 클리어
    public List<DungeonInventory.SlotDTO> dungeonLoadoutSnapshot;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary> 로비에서 선택된 파티를 그대로 보관(참조 공유). </summary>
    public void SetParty(IEnumerable<Job> party)
    {
        _activeParty.Clear();
        if (party == null) return;
        foreach (var c in party)
            if (c != null) _activeParty.Add(c);
    }

    /// <summary> 파티가 비었는지 간단 체크. </summary>
    public bool HasParty() => _activeParty.Count > 0;


    
}
