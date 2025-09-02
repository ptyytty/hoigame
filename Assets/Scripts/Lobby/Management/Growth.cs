using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Growth : ListUIBase<Skill>
{
    [Header("Refs")]
    [SerializeField] private ListUpManager listUpManager;

    private static readonly HeroSkills heroSkills = new HeroSkills();

    private Job currentHero;

    protected override void OnEnable()
    {
        base.OnEnable();
        listUpManager.OnOwnedHeroSelected += OnHeroSelected;
        if (listUpManager.CurrentSelectedHero != null)
            OnHeroSelected(listUpManager.CurrentSelectedHero);
    }

    protected void OnDisable()
    {
        listUpManager.OnOwnedHeroSelected -= OnHeroSelected;
    }

    private void OnHeroSelected(Job hero)
    {
        currentHero = hero;
        RedrawSkills();
    }

    private void RedrawSkills()
    {
        ClearList();
        LoadList();
    }

    protected override void LoadList()
    {
        if (currentHero == null) return;        // 선택 없을 시 Load X

        foreach (var skill in LoadSkills(currentHero))
            CreateButton(skill);
            
    }

    protected override void SetLabel(Button button, Skill skill)
    {
        TMP_Text name = button.transform.Find("Name_skill").GetComponent<TMP_Text>();
        Image image = button.transform.Find("Image_skill").GetComponent<Image>();

        name.text = skill.skillName;
        
    }

    protected override void OnSelected(Skill skill)
    {
        
    }

    private IEnumerable<Skill> LoadSkills(Job hero)
        => hero == null ? System.Array.Empty<Skill>()
                        : heroSkills.GetHeroSkills(hero);
    

}
