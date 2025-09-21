// PartyAdapter.cs
using System.Collections.Generic;
using UnityEngine;

public class PartyAdapter : MonoBehaviour,
    EnemySpawner.IHeroPartyProvider      // 파티(영웅) 목록 제공
{
    [Header("Heroes")]
    [SerializeField] List<Job> heroes = new();          // 파티 영웅 참조들(필수)

    [Header("Slots (optional for MirrorPartyGrid)")]
    [SerializeField] List<Transform> slots = new();     // 파티 2x2 슬롯(선택)

    public IReadOnlyList<Job> GetParty() => heroes;
    public IReadOnlyList<Transform> GetHeroSlots() => slots;
}