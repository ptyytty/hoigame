using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonEquipApply : MonoBehaviour
{
    void Awake()
    {
        var pb = PartyBridge.Instance;
        if (pb == null || !pb.HasParty()) return;

        foreach (var job in pb.ActiveParty)
            EquipmentRuntime.ApplyToJobOnEnterDungeon(job);  // 역할: 입장 즉시 1회 가산
    }

    void OnDestroy()
    {
        var pb = PartyBridge.Instance;
        if (pb == null || !pb.HasParty()) return;

        foreach (var job in pb.ActiveParty)
            EquipmentRuntime.RevertFromJobOnExitDungeon(job); // 역할: 퇴장 시 회수
    }
}
