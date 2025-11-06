using System;

public static class SelectionEvents
{
    /// <summary>역할: 영웅이 선택되었을 때 브로드캐스트</summary>
    public static event Action<Job> OnHeroSelected;

    /// <summary>역할: 영웅의 장비가 변경되었을 때 브로드캐스트</summary>
    public static event Action<Job> OnHeroEquipChanged;

    public static void RaiseHeroSelected(Job hero)     => OnHeroSelected?.Invoke(hero);
    public static void RaiseHeroEquipChanged(Job hero) => OnHeroEquipChanged?.Invoke(hero);
}