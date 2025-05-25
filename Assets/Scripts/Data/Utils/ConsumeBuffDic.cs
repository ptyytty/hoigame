using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConsumeBuffDic
{
    public static string ToKorean(this ConsumeBuffType type)
    {
        switch (type)
        {
            case ConsumeBuffType.Damage: return "피해";
            case ConsumeBuffType.Heal: return "회복";
            case ConsumeBuffType.Remove: return "디버프 제거";
            case ConsumeBuffType.Poison: return "중독";
            case ConsumeBuffType.Bleeding: return "출혈";
            case ConsumeBuffType.Burn: return "화상";
            case ConsumeBuffType.Sign: return "표식";
            case ConsumeBuffType.Faint: return "기절";
            case ConsumeBuffType.Taunt: return "도발";
            case ConsumeBuffType.AbilityBuff: return "능력 강화";
            case ConsumeBuffType.AbilityDebuff: return "능력 약화";
            case ConsumeBuffType.Special: return "특수 효과";
            default: return "알 수 없음";
        }
    }
}
