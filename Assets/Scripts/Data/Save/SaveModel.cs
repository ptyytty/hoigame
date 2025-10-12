using System;
using System.Collections.Generic;

namespace Save
{
    [Serializable]
    public class SaveGame
    {
        // 스키마 버전 (마이그레이션용)
        public int version = 1;

        // 플레이어 식별
        public string playerId;

        // 영웅 보유 및 성장 상태
        public List<HeroSave> heroes = new();

        // 보유 아이템
        public InventorySave inventory = new();

        // 추가: 게임 내 화폐/자원 등
        public int gold;
        public int redSoul;
        public int blueSoul;
        public int purpleSoul;
        public int greenSoul;
    }

    [Serializable]
    public class HeroSave           //  보유 영웅 정보
    {
        public string heroUid;      // 영웅 인스턴스 고유 식별자 (중복 영웅)
        public int heroId;           // 마스터 데이터에 존재하는 id
        public string displayName;      // 플레이어가 바꾼 이름
        public int level;
        public int exp;

        public int currentHp;

        // 스킬 업그레이드 상황  skillId->level(최대 5)
        public Dictionary<int, int> skillLevels = new();

        // 영웅의 성장(능력치 강화 등) 세부를 따로 둬도 됨
        public Dictionary<string, int> growthStats = new(); // 예: "hp": 5, "def": 2 ...
    }

    [Serializable]
    public class InventorySave
    {
        // 슬롯 기반 인벤(각 슬롯은 같은 아이템만, 최대 N개 스택)
        public List<Item> slots = new();
    }

    [Serializable]
    public class Item
    {
        public int itemId;      // ConsumeItem.id_item / EquipItem.id_item
        public int num;         // 보유 번호(장비 아이템 구분용)
        public ItemType type;   // Consume / Equipment
        public int count;       // 장비는 한 개씩, 소비는 증가
    }
}
