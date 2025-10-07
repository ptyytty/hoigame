// 삭제 예정
namespace Game.Skills
{
    public static class SkillKey
    {
        // heroId*BASE + localId 방식
        public const int BASE = 100;

        public static int Make(int heroId, int localSkillId) => heroId * BASE + localSkillId;
        public static int ExtractHeroId(int key)            => key / BASE;
        public static int ExtractLocalId(int key)           => key % BASE;
    }
}
