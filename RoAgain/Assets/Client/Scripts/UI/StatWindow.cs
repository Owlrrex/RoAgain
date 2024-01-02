using Client;
using OwlLogging;
using Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatWindow : MonoBehaviour
{
    [SerializeField]
    private TMP_Text StrBaseText;
    [SerializeField]
    private TMP_Text StrModText;
    [SerializeField]
    private TMP_Text StrCostText;
    [SerializeField]
    private Button StrIncreaseButton;

    [SerializeField]
    private TMP_Text AgiBaseText;
    [SerializeField]
    private TMP_Text AgiModText;
    [SerializeField]
    private TMP_Text AgiCostText;
    [SerializeField]
    private Button AgiIncreaseButton;

    [SerializeField]
    private TMP_Text VitBaseText;
    [SerializeField]
    private TMP_Text VitModText;
    [SerializeField]
    private TMP_Text VitCostText;
    [SerializeField]
    private Button VitIncreaseButton;

    [SerializeField]
    private TMP_Text IntBaseText;
    [SerializeField]
    private TMP_Text IntModText;
    [SerializeField]
    private TMP_Text IntCostText;
    [SerializeField]
    private Button IntIncreaseButton;

    [SerializeField]
    private TMP_Text DexBaseText;
    [SerializeField]
    private TMP_Text DexModText;
    [SerializeField]
    private TMP_Text DexCostText;
    [SerializeField]
    private Button DexIncreaseButton;

    [SerializeField]
    private TMP_Text LukBaseText;
    [SerializeField]
    private TMP_Text LukModText;
    [SerializeField]
    private TMP_Text LukCostText;
    [SerializeField]
    private Button LukIncreaseButton;

    [SerializeField]
    private TMP_Text AtkText;

    [SerializeField]
    private TMP_Text MatkText;

    [SerializeField]
    private TMP_Text HitText;

    [SerializeField]
    private TMP_Text CriticalText;

    [SerializeField]
    private TMP_Text DefText;

    [SerializeField]
    private TMP_Text MdefText;

    [SerializeField]
    private TMP_Text FleeText;

    [SerializeField]
    private TMP_Text AspdText;

    [SerializeField]
    private TMP_Text StatPointText;

    [SerializeField]
    private Button _closeButton;

    private LocalCharacterEntity _character;

    void Start()
    {
        OwlLogger.PrefabNullCheckAndLog(StrBaseText, "StrBaseText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(StrModText, "StrModText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(StrCostText, "StrCostText", this, GameComponent.UI);
        if(!OwlLogger.PrefabNullCheckAndLog(StrIncreaseButton, "StrIncreaseButton", this, GameComponent.UI))
            StrIncreaseButton.onClick.AddListener(OnStrIncreaseClicked);

        OwlLogger.PrefabNullCheckAndLog(AgiBaseText, "AgiBaseText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(AgiModText, "AgiModText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(AgiCostText, "AgiCostText", this, GameComponent.UI);
        if (!OwlLogger.PrefabNullCheckAndLog(AgiIncreaseButton, "AgiIncreaseButton", this, GameComponent.UI))
            AgiIncreaseButton.onClick.AddListener(OnAgiIncreaseClicked);

        OwlLogger.PrefabNullCheckAndLog(VitBaseText, "VitBaseText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(VitModText, "VitModText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(VitCostText, "VitCostText", this, GameComponent.UI);
        if (!OwlLogger.PrefabNullCheckAndLog(VitIncreaseButton, "VitIncreaseButton", this, GameComponent.UI))
            VitIncreaseButton.onClick.AddListener(OnVitIncreaseClicked);

        OwlLogger.PrefabNullCheckAndLog(IntBaseText, "IntBaseText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(IntModText, "IntModText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(IntCostText, "IntCostText", this, GameComponent.UI);
        if (!OwlLogger.PrefabNullCheckAndLog(IntIncreaseButton, "IntIncreaseButton", this, GameComponent.UI))
            IntIncreaseButton.onClick.AddListener(OnIntIncreaseClicked);

        OwlLogger.PrefabNullCheckAndLog(DexBaseText, "DexBaseText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(DexModText, "DexModText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(DexCostText, "DexCostText", this, GameComponent.UI);
        if (!OwlLogger.PrefabNullCheckAndLog(DexIncreaseButton, "DexIncreaseButton", this, GameComponent.UI))
            DexIncreaseButton.onClick.AddListener(OnDexIncreaseClicked);

        OwlLogger.PrefabNullCheckAndLog(LukBaseText, "LukBaseText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(LukModText, "LukModText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(LukCostText, "LukCostText", this, GameComponent.UI);
        if (!OwlLogger.PrefabNullCheckAndLog(LukIncreaseButton, "LukIncreaseButton", this, GameComponent.UI))
            LukIncreaseButton.onClick.AddListener(OnLukIncreaseClicked);

        OwlLogger.PrefabNullCheckAndLog(AtkText, "AtkText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(MatkText, "MatkText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(HitText, "HitText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(CriticalText, "CriticalText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(DefText, "DefText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(MdefText, "MdefText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(FleeText, "FleeText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(AspdText, "AspdText", this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(StatPointText, "StatPointText", this, GameComponent.UI);
        if (!OwlLogger.PrefabNullCheckAndLog(_closeButton, "closeButton", this, GameComponent.UI))
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    public int Initialize(LocalCharacterEntity character)
    {
        if (character == null)
        {
            OwlLogger.LogError($"Can't initialize StatWindow with null character!", GameComponent.UI);
            return -1;
        }

        _character = character;
        return 0;
    }

    private void OnStrIncreaseClicked()
    {
        PlayerMain.Instance.StatIncreaseRequest(EntityPropertyType.Str);
    }

    private void OnAgiIncreaseClicked()
    {
        PlayerMain.Instance.StatIncreaseRequest(EntityPropertyType.Agi);
    }

    private void OnVitIncreaseClicked()
    {
        PlayerMain.Instance.StatIncreaseRequest(EntityPropertyType.Vit);
    }

    private void OnIntIncreaseClicked()
    {
        PlayerMain.Instance.StatIncreaseRequest(EntityPropertyType.Int);
    }

    private void OnDexIncreaseClicked()
    {
        PlayerMain.Instance.StatIncreaseRequest(EntityPropertyType.Dex);
    }

    private void OnLukIncreaseClicked()
    {
        PlayerMain.Instance.StatIncreaseRequest(EntityPropertyType.Luk);
    }

    void Update()
    {
        if (_character == null)
            return;

        UpdateStrDisplay();
        UpdateAgiDisplay();
        UpdateVitDisplay();
        UpdateIntDisplay();
        UpdateDexDisplay();
        UpdateLukDisplay();
        UpdateAtkDisplay();
        UpdateMatkDisplay();
        UpdateHitDisplay();
        UpdateCritDisplay();
        UpdateDefDisplay();
        UpdateMdefDisplay();
        UpdateFleeDisplay();
        UpdateAspdDisplay();
        UpdateStatPointDisplay();
    }

    public void UpdateStrDisplay()
    {
        StrBaseText.text = _character.Str.Base.ToString();
        StrModText.text = (_character.Str.Total - _character.Str.Base).ToString();
        StrCostText.text = _character.StrIncreaseCost.ToString();
        StrIncreaseButton.gameObject.SetActive(_character.RemainingStatPoints >= _character.StrIncreaseCost);
    }

    public void UpdateAgiDisplay()
    {
        AgiBaseText.text = _character.Agi.Base.ToString();
        AgiModText.text = (_character.Agi.Total - _character.Agi.Base).ToString();
        AgiCostText.text = _character.AgiIncreaseCost.ToString();
        AgiIncreaseButton.gameObject.SetActive(_character.RemainingStatPoints >= _character.AgiIncreaseCost);
    }

    public void UpdateVitDisplay()
    {
        VitBaseText.text = _character.Vit.Base.ToString();
        VitModText.text = (_character.Vit.Total - _character.Vit.Base).ToString();
        VitCostText.text = _character.VitIncreaseCost.ToString();
        VitIncreaseButton.gameObject.SetActive(_character.RemainingStatPoints >= _character.VitIncreaseCost);
    }

    public void UpdateIntDisplay()
    {
        IntBaseText.text = _character.Int.Base.ToString();
        IntModText.text = (_character.Int.Total - _character.Int.Base).ToString();
        IntCostText.text = _character.IntIncreaseCost.ToString();
        IntIncreaseButton.gameObject.SetActive(_character.RemainingStatPoints >= _character.IntIncreaseCost);
    }

    public void UpdateDexDisplay()
    {
        DexBaseText.text = _character.Dex.Base.ToString();
        DexModText.text = (_character.Dex.Total - _character.Dex.Base).ToString();
        DexCostText.text = _character.DexIncreaseCost.ToString();
        DexIncreaseButton.gameObject.SetActive(_character.RemainingStatPoints >= _character.DexIncreaseCost);
    }

    public void UpdateLukDisplay()
    {
        LukBaseText.text = _character.Luk.Base.ToString();
        LukModText.text = (_character.Luk.Total - _character.Luk.Base).ToString();
        LukCostText.text = _character.LukIncreaseCost.ToString();
        LukIncreaseButton.gameObject.SetActive(_character.RemainingStatPoints >= _character.LukIncreaseCost);
    }

    public void UpdateAtkDisplay()
    {
        AtkText.text = $"{_character.AtkMin.Total} - {_character.AtkMax.Total}";
    }

    public void UpdateMatkDisplay()
    {
        MatkText.text = $"{_character.MatkMin.Total} - {_character.MatkMax.Total}";
    }

    public void UpdateHitDisplay()
    {
        HitText.text = _character.Hit.Total.ToString();
    }

    public void UpdateCritDisplay()
    {
        CriticalText.text = (_character.Crit.Total * 100).ToString("F1");
    }

    public void UpdateDefDisplay()
    {
        DefText.text = $"{_character.HardDef.Total} + {_character.SoftDef.Total}";
    }

    public void UpdateMdefDisplay()
    {
        MdefText.text = $"{_character.HardMdef.Total} + {_character.SoftMdef.Total}";
    }

    public void UpdateFleeDisplay()
    {
        FleeText.text = $"{_character.Flee.Total} + {_character.PerfectFlee.Total * 100:F0}";
    }

    public void UpdateAspdDisplay()
    {
        AspdText.text = "1";
    }

    public void UpdateStatPointDisplay()
    {
        StatPointText.text = _character.RemainingStatPoints.ToString();
    }

    private void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
    }
}
