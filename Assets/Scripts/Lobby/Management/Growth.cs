using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Skills;

public class Growth : ListUIBase<Skill>
{
    [Header("Refs")]
    [SerializeField] private ListUpManager listUpManager;

    [Header("Price")]
    [SerializeField] private GoodsImage currencyImage;
    [SerializeField] private GameObject pricePanel;
    [SerializeField] private Image currency;
    [SerializeField] private TMP_Text priceText;
    private int price = 3;

    [Header("Skill Info")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text skillName;

    [SerializeField] private Button growthButton;
    [SerializeField] private TMP_Text growthText;

    [Header("Extra Assets")]
    [SerializeField] private TestMoney testMoney;


    private static readonly HeroSkills heroSkills = new HeroSkills();

    private Job currentHero;
    private int currentSkillId;

    protected override void OnEnable()
    {
        base.OnEnable();
        listUpManager.OnOwnedHeroSelected += OnHeroSelected;
        if (listUpManager.CurrentSelectedHero != null)
            OnHeroSelected(listUpManager.CurrentSelectedHero);

        pricePanel.SetActive(false);

        growthButton.onClick.AddListener(GrowthSkill);
        growthText.text = "성장";
    }

    protected void OnDisable()
    {
        listUpManager.OnOwnedHeroSelected -= OnHeroSelected;
        ResetSelectedButton();
        ClearList();
        pricePanel.SetActive(false);
        infoPanel.SetActive(false);

        growthButton.onClick.RemoveListener(GrowthSkill);
    }

    private void OnHeroSelected(Job hero)
    {
        currentHero = hero;
        RedrawSkills();

        pricePanel.SetActive(false);
        infoPanel.SetActive(false);
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

    private IEnumerable<Skill> LoadSkills(Job hero)
        => hero == null ? System.Array.Empty<Skill>()
                        : heroSkills.GetHeroSkills(hero);

    protected override void OnSelected(Skill skill)
    {
        pricePanel.SetActive(true);
        infoPanel.SetActive(true);
        skillName.text = skill.skillName;

        currentSkillId = skill.skillId;

        switch (currentHero.jobCategory)
        {
            case JobCategory.Warrior:
                currency.sprite = currencyImage.warriorImage;
                break;

            case JobCategory.Ranged:
                currency.sprite = currencyImage.rangeImage;
                break;

            case JobCategory.Special:
                currency.sprite = currencyImage.specialImage;
                break;

            case JobCategory.Healer:
                currency.sprite = currencyImage.healerImage;
                break;
        }

        if (!testMoney.HasEnoughSoul(currentHero.jobCategory, 3))
        {
            priceText.color = Color.red;
            growthButton.interactable = false;
        }
        else growthButton.interactable = true;

        priceText.text = testMoney.SoulCost(currentHero.jobCategory, 3);

        growthButton.gameObject.SetActive(true);
    }

    void GrowthSkill()
    {
        TryUpgradeSkill(currentHero, currentSkillId);
    }

    public bool TryUpgradeSkill(Job hero, int localSkillId, int cost = 3, int maxLevel = 5)
    {
        int key = SkillKey.Make(hero.id_job, localSkillId);
        int have = 0;
        Debug.Log("성장 시작");

        // 비용 확인
        switch (hero.jobCategory)
        {
            case JobCategory.Warrior:
                have = InventoryRuntime.Instance.redSoul;
                break;

            case JobCategory.Ranged:
                have = InventoryRuntime.Instance.blueSoul;
                break;

            case JobCategory.Special:
                have = InventoryRuntime.Instance.purpleSoul;
                break;

            case JobCategory.Healer:
                have = InventoryRuntime.Instance.greenSoul;
                break;
        }
        if (have < cost) return false;
        Debug.Log("비용 있음");

        hero.skillLevels.TryGetValue(key, out int cur);
        if (cur >= maxLevel) return false;
        Debug.Log(have);
        have -= cost;
        Debug.Log(have);
        // 레벨 증가
        hero.skillLevels[key] = cur + 1;
        Debug.Log($"{hero.skillLevels[key]} / [{string.Join(",", hero.skillLevels.Values)}]");
        // 즉시 저장
        _ = PlayerProgressService.Instance.SaveAsync();
        Debug.Log("성장 실행");
        return true;
    }

}
