using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class STraitLabelData
{
    public Button Button;
    public CPartyMemberRuntime PartyMember;
}

public class CCharacterListUI : MonoBehaviour
{
    [SerializeField]
    private VisualTreeAsset m_CharacterButtonTemplate;
    [SerializeField]
    private VisualTreeAsset m_CharacterShortInfoPanelTemplate;
    [SerializeField]
    private VisualTreeAsset m_CharacterFullInfoPanelTabsTemplate;
    [SerializeField]
    private VisualTreeAsset m_CharacterFullInfoPanelGeneralTabTemplate;
    [SerializeField]
    private VisualTreeAsset m_CharacterFullInfoPanelInventoryTabTemplate;
    [SerializeField]
    private VisualTreeAsset m_CharacterFullInfoPanelHealthTabTemplate;
    [SerializeField]
    private VisualTreeAsset m_SmallTextPopupTemplate;

    private UIDocument m_CharacterListUI;
    private VisualElement m_CharacterPanel;
    private VisualElement m_CharacterPopup;
    private TemplateContainer m_CharacterFullInfoTemplateContainer;
    private VisualElement m_CharacterFullInfoPanelTabs;
    private VisualElement m_CharacterFullInfoGeneralTabPanel;
    private VisualElement m_CharacterFullInfoInventoryTabPanel;
    private VisualElement m_CharacterFullInfoHealthTabPanel;
    private CPartyMemberRuntime m_CurrentCharacterFullPanelCharacter;
    private CWorldInfoUI m_WorldInfoUI;

    private VisualElement m_CurrentSmallPopup;

    private List<Label> m_AddedTraitLabels = new List<Label>();

    private void OnEnable()
    {
        m_CharacterListUI = GetComponent<UIDocument>();
        m_CharacterPanel = m_CharacterListUI.rootVisualElement.Q<VisualElement>("CharacterPanel");
        m_WorldInfoUI = CUIManager.Instance.WorldInfoUI;

        CPartyManager.Instance.m_OnCharacterAdded += AddCharacterButton;
        CPartyManager.Instance.m_OnCharacterRemoved += RemoveCharacterButton;

        if (m_WorldInfoUI == null)
        {
            Debug.Log("CCharacterListUI::Awake - WorldInfoUI is null!");
        }
    }

    private void OnDisable()
    {
        if (CPartyManager.Instance == null)
        {
            return;
        }

        CPartyManager.Instance.m_OnCharacterAdded -= AddCharacterButton;
        CPartyManager.Instance.m_OnCharacterRemoved -= RemoveCharacterButton;

        foreach (Label traitLabel in m_AddedTraitLabels)
        {
            if (traitLabel != null)
            {
                traitLabel.UnregisterCallback<MouseEnterEvent>(OnMouseEnterCreateDescriptionLabel);
                traitLabel.UnregisterCallback<MouseLeaveEvent>(OnMouseLeaveTraitLabel);
                traitLabel.UnregisterCallback<MouseMoveEvent>(OnMouseMoveUpdateSmallPopupPosition);
            }
        }

        DeinitFullInfoPanel();
    }

    public void AddCharacterButton(CPartyMemberRuntime partyMember)
    {
        TemplateContainer buttonInstance = m_CharacterButtonTemplate.Instantiate();
        Button newButton = buttonInstance.Q<Button>("CharacterButton");

        newButton.name = partyMember.CharacterName + "Button";
        newButton.iconImage = partyMember.PartyMemberPortrait.texture;
        newButton.style.display = DisplayStyle.Flex;

        newButton.userData = partyMember;

        newButton.RegisterCallback<ClickEvent>(OnCharacterButtonClicked);
        newButton.RegisterCallback<MouseEnterEvent>(OnMouseEnterPortrait);
        newButton.RegisterCallback<MouseLeaveEvent>(OnMouseLeavePortrait);

        m_CharacterPanel.Add(buttonInstance);
    }

    public void RemoveCharacterButton(CPartyMemberRuntime partyMember)
    {
        Button buttonToRemove = m_CharacterPanel.Q<Button>(partyMember.CharacterName + "Button");

        if (buttonToRemove == null)
        {
            Debug.Log(string.Format("RemoveCharacterButton - character {0} button not found!", partyMember.CharacterName));
            return;
        }

        // Unregister events
        buttonToRemove.UnregisterCallback<ClickEvent>(OnCharacterButtonClicked);
        buttonToRemove.UnregisterCallback<MouseEnterEvent>(OnMouseEnterPortrait);
        buttonToRemove.UnregisterCallback<MouseLeaveEvent>(OnMouseLeavePortrait);

        buttonToRemove.RemoveFromHierarchy();
    }

    private void OnMouseEnterPortrait(MouseEnterEvent mouseEnterEvent)
    {
        Button button = mouseEnterEvent.currentTarget as Button;
        if (button == null)
        {
            Debug.Log("OnMouseEnterPortrait - button from clickEvent is null!");
            return;
        }

        CPartyMemberRuntime partyMember = button.userData as CPartyMemberRuntime;
        if (partyMember == null)
        {
            Debug.Log("OnMouseEnterPortrait - partyMember is null!");
            return;
        }

        TemplateContainer popupStatsContainer = m_CharacterShortInfoPanelTemplate.Instantiate();
        m_CharacterPopup = popupStatsContainer.Q<VisualElement>("CharacterShortInfoPanel");

        if (m_CharacterPopup == null)
        {
            Debug.Log("Popup event OnMouseEnter - failed to find popup!");
            return;
        }

        if (button != null)
        {
            Rect buttonWorldRect = button.worldBound;
            Rect panelWorldRect = m_CharacterPanel.worldBound;

            float localX = buttonWorldRect.x - panelWorldRect.x;
            float localY = buttonWorldRect.y - panelWorldRect.y;

            m_CharacterPopup.style.left = localX + buttonWorldRect.width;
            m_CharacterPopup.style.top = localY;

            m_CharacterPopup.style.height = buttonWorldRect.height;
        }

        Label nameLabel = m_CharacterPopup.Q<Label>("CharacterName");
        Label shortStatsLabel = m_CharacterPopup.Q<Label>("CharacterShortStats");
        nameLabel.AddToClassList("white-text");
        shortStatsLabel.AddToClassList("white-text-small");

        nameLabel.text = partyMember.CharacterName;
        shortStatsLabel.text = GetShortStatsString(partyMember);

        m_CharacterPanel.Add(m_CharacterPopup);
    }

    private void OnMouseLeavePortrait(MouseLeaveEvent mouseLeaveEvent)
    {
        m_CharacterPanel.Remove(m_CharacterPopup);
        m_CharacterPopup = null;
    }

    private string GetShortStatsString(CPartyMemberRuntime partyMember)
    {
        string statsString = "Mood: ";

        switch (partyMember.Happiness)
        {
            case > 90f:
                statsString += "Immaculate\n";
                break;
            case > 75f:
                statsString += "Great\n";
                break;
            case > 50f:
                statsString += "Good\n";
                break;
            case > 25:
                statsString += "Bad\n";
                break;
            default:
                statsString += "Awful\n";
                break;
        }

        statsString += "Hunger: " + (float)Math.Round(partyMember.Hunger, 1) + "\n";

        return statsString;
    }

    private void OnCharacterButtonClicked(ClickEvent clickEvent)
    {
        Button button = clickEvent.currentTarget as Button;
        if (button == null)
        {
            Debug.Log("OnCharacterButtonClicked - button from clickEvent is null!");
            return;
        }

        CPartyMemberRuntime partyMember = button.userData as CPartyMemberRuntime;
        if (partyMember == null)
        {
            Debug.Log("OnCharacterButtonClicked - partyMember is null!");
            return;
        }

        // Reset panel and show world info again, if we clicked the same character
        // NOTE: need to clean up this code;
        if (m_CurrentCharacterFullPanelCharacter != null)
        {
            if (m_CharacterFullInfoTemplateContainer != null)
            {
                DeinitFullInfoPanel();

                if (partyMember == m_CurrentCharacterFullPanelCharacter)
                {
                    m_CurrentCharacterFullPanelCharacter = null;
                    if (m_WorldInfoUI != null)
                    {
                        m_WorldInfoUI.ShowWorldInfo();
                    }

                    return;
                }
            }
        }

        if (m_WorldInfoUI != null)
        {
            m_WorldInfoUI.HideWorldInfo();
        }

        m_CurrentCharacterFullPanelCharacter = partyMember;

        if (m_CharacterFullInfoPanelTabs == null)
        {
            TemplateContainer characterFullStatsContainer = m_CharacterFullInfoPanelTabsTemplate.Instantiate();
            m_CharacterFullInfoPanelTabs = characterFullStatsContainer.Q<VisualElement>("TabsPanel");
        }

        if (m_CharacterFullInfoPanelTabs == null)
        {
            Debug.Log("CharacterFullInfoPanel is null!");
            return;
        }

        if (m_CharacterFullInfoTemplateContainer == null)
        {
            m_CharacterFullInfoTemplateContainer = m_CharacterFullInfoPanelGeneralTabTemplate.Instantiate();
        }

        if (m_CharacterFullInfoTemplateContainer == null)
        {
            Debug.Log("CharacterFullStatsGeneralTabContainer is null!");
            return;
        }

        m_CharacterFullInfoTemplateContainer.style.alignSelf = Align.FlexEnd;
        m_CharacterFullInfoTemplateContainer.style.position = Position.Absolute;
        m_CharacterFullInfoTemplateContainer.style.right = 0;
        m_CharacterFullInfoTemplateContainer.style.top = 0;
        m_CharacterFullInfoTemplateContainer.style.width = new Length(30, LengthUnit.Percent);

        Button generalTabButton = m_CharacterFullInfoPanelTabs.Q<Button>("GeneralTabButton");
        if (generalTabButton == null)
        {
            Debug.Log("GeneralTabButton is null!");
            return;
        }

        Button inventoryTabButton = m_CharacterFullInfoPanelTabs.Q<Button>("InventoryTabButton");
        if (inventoryTabButton == null)
        {
            Debug.Log("InventoryTabButton is null!");
            return;
        }

        Button healthTabButton = m_CharacterFullInfoPanelTabs.Q<Button>("HealthTabButton");
        if (healthTabButton == null)
        {
            Debug.Log("HealthTabButton is null!");
            return;
        }

        m_CharacterFullInfoTemplateContainer.Add(m_CharacterFullInfoPanelTabs);
        m_CharacterListUI.rootVisualElement.Add(m_CharacterFullInfoTemplateContainer);

        // Setup the tabs
        CreateGeneralInfoPanelTab(partyMember);

        generalTabButton.clicked += AddGeneralInfoTab;

        AddGeneralInfoTab();
    }

    private void CreateGeneralInfoPanelTab(CPartyMemberRuntime partyMember)
    {
        if (m_CharacterFullInfoGeneralTabPanel == null)
        {
            m_CharacterFullInfoGeneralTabPanel = m_CharacterFullInfoTemplateContainer.Q<VisualElement>("CharacterFullInfoGeneralPanel");
        }

        if (m_CharacterFullInfoGeneralTabPanel == null)
        {
            Debug.Log("CharacterFullInfoGeneralPanel is null!");
            return;
        }

        VisualElement characterPortrait = m_CharacterFullInfoGeneralTabPanel.Q<VisualElement>("Portrait");
        if (characterPortrait == null)
        {
            Debug.Log("characterPortrait is null!");
            return;
        }

        characterPortrait.style.backgroundImage = new StyleBackground(partyMember.PartyMemberPortrait);
        characterPortrait.style.width = partyMember.PartyMemberPortrait.rect.width;
        characterPortrait.style.height = partyMember.PartyMemberPortrait.rect.height;

        VisualElement portraitPanel = m_CharacterFullInfoGeneralTabPanel.Q<VisualElement>("PortraitPanel");
        if (portraitPanel == null)
        {
            Debug.Log("portraitPanel is null!");
            return;
        }

        Label nameInfo = m_CharacterFullInfoGeneralTabPanel.Q<Label>("NameInfo");
        if (nameInfo == null)
        {
            Debug.Log("nameInfo is null!");
            return;
        }

        nameInfo.text = string.Format("{0}\n({1})", partyMember.CharacterName, partyMember.PartyMemberClassName);

        foreach (var trait in partyMember.TraitDetails)
        {
            Label traitLabel = new Label();
            portraitPanel.Add(traitLabel);
            traitLabel.style.color = Color.white;
            traitLabel.style.fontSize = 12;
            traitLabel.text = trait.Key;
            traitLabel.userData = trait.Value;
            traitLabel.RegisterCallback<MouseEnterEvent>(OnMouseEnterCreateDescriptionLabel);
            traitLabel.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveTraitLabel);

            m_AddedTraitLabels.Add(traitLabel);
        }

        Label statsInfo = m_CharacterFullInfoGeneralTabPanel.Q<Label>("StatsInfo");
        if (statsInfo == null)
        {
            Debug.Log("statsInfo is null!");
            return;
        }
        statsInfo.style.fontSize = 12;

        string statsString = "";
        foreach (var stat in partyMember.PartyMemberStats)
        {
            statsString += string.Format("{0} - {1}\n", stat.Key.ToString(), stat.Value.Value);
        }

        statsInfo.text = statsString;

        // TODO: change to show status conditions
        Label statusInfo = m_CharacterFullInfoGeneralTabPanel.Q<Label>("StatusInfo");
        if (statusInfo == null)
        {
            Debug.Log("statusInfo is null!");
            return;
        }
        statusInfo.style.fontSize = 12;

        string inventoryString = string.Format("Inventory: {0}/{1}\n", partyMember.ItemInventory.CurrentWeight, partyMember.ItemInventory.MaxWeight);
        Dictionary<CInventoryItemRuntime, int> items = partyMember.ItemInventory.GetItemsWithCounts();
        foreach (var item in items)
        {
            inventoryString += item.Key.ItemName;
            if (item.Value > 1)
            {
                inventoryString += string.Format(" ({0})\n", item.Value);
            }
        }

        statusInfo.text = inventoryString;
    }

    private void AddGeneralInfoTab()
    {
        RemoveFullInfoTabs();

        if (m_CharacterFullInfoTemplateContainer != null && m_CharacterFullInfoGeneralTabPanel != null)
        {
            m_CharacterFullInfoTemplateContainer.Add(m_CharacterFullInfoGeneralTabPanel);
        }
    }

    private void RemoveFullInfoTabs()
    {
        if (m_CharacterFullInfoGeneralTabPanel != null)
        {
            m_CharacterFullInfoGeneralTabPanel.RemoveFromHierarchy();
        }
    }

    private void DeinitFullInfoPanel()
    {
        RemoveFullInfoTabs();

        if (m_CharacterFullInfoPanelTabs != null)
        {
            m_CharacterFullInfoPanelTabs.RemoveFromHierarchy();

            Button generalTabButton = m_CharacterFullInfoPanelTabs.Q<Button>("GeneralTabButton");

            if (generalTabButton != null)
            {
                generalTabButton.clicked -= AddGeneralInfoTab;
            }
        }

        if (m_CharacterFullInfoTemplateContainer != null)
        {
            m_CharacterFullInfoTemplateContainer.RemoveFromHierarchy();

            foreach (Label traitLabel in m_AddedTraitLabels)
            {
                if (traitLabel == null)
                {
                    continue;
                }

                traitLabel.UnregisterCallback<MouseEnterEvent>(OnMouseEnterCreateDescriptionLabel);
                traitLabel.UnregisterCallback<MouseLeaveEvent>(OnMouseLeaveTraitLabel);
            }

            m_AddedTraitLabels.Clear();
        }
        
        m_CharacterFullInfoGeneralTabPanel = null;
        m_CharacterFullInfoInventoryTabPanel = null;
        m_CharacterFullInfoHealthTabPanel = null;
        m_CharacterFullInfoPanelTabs = null;
        m_CharacterFullInfoTemplateContainer = null;
    }

    private void OnMouseEnterCreateDescriptionLabel(MouseEnterEvent mouseEnterEvent)
    {
        VisualElement currentMousedOverElement = mouseEnterEvent.currentTarget as VisualElement;
        if (currentMousedOverElement == null)
        {
            Debug.Log("OnMouseEnterCreateDescriptionLabel - currentMousedOverElement is null!");
            return;
        }

        string descriptionText = currentMousedOverElement.userData as string;
        if (descriptionText == null)
        {
            Debug.Log("OnMouseEnterCreateDescriptionLabel - descriptionText is null!");
            return;
        }

        if (m_CurrentSmallPopup != null)
        {
            m_CurrentSmallPopup.RemoveFromHierarchy();
            m_CurrentSmallPopup = null;
        }

        m_CurrentSmallPopup = m_SmallTextPopupTemplate.Instantiate();
        if (m_CurrentSmallPopup == null)
        {
            Debug.Log("OnMouseEnterCreateDescriptionLabel - CurrentSmallPopup is null!");
            return;
        }

        m_CharacterFullInfoPanelTabs.Add(m_CurrentSmallPopup);

        Label textLabel = m_CurrentSmallPopup.Q<Label>("SmallTextPopupLabel");

        if (textLabel == null)
        {
            Debug.Log("OnMouseEnterCreateDescriptionLabel - text label is still null!");
            return;
        }

        UpdateCurrentSmallPopupPosition(mouseEnterEvent.mousePosition);
        currentMousedOverElement.RegisterCallback<MouseMoveEvent>(OnMouseMoveUpdateSmallPopupPosition);

        textLabel.text = descriptionText;

        m_CharacterListUI.rootVisualElement.Add(m_CurrentSmallPopup);
    }

    private void OnMouseMoveUpdateSmallPopupPosition(MouseMoveEvent mouseMoveEvent)
    {
        if (m_CurrentSmallPopup != null)
        {
            UpdateCurrentSmallPopupPosition(mouseMoveEvent.mousePosition);
        }
    }

    private void UpdateCurrentSmallPopupPosition(Vector2 position)
    {
        if (m_CurrentSmallPopup == null)
        {
            Debug.Log("UpdateCurrentSmallPopupPosition - CurrentSmallPopup is null!");
            return;
        }

        m_CurrentSmallPopup.style.left = position.x + CUIManager.Instance.PopupMouseOffset.x;
        m_CurrentSmallPopup.style.top = position.y + CUIManager.Instance.PopupMouseOffset.y;
    }

    private void OnMouseLeaveTraitLabel(MouseLeaveEvent mouseLeaveEvent)
    {
        VisualElement currentMousedOverElement = mouseLeaveEvent.currentTarget as VisualElement;
        if (currentMousedOverElement == null)
        {
            Debug.Log("OnMouseLeaveTraitLabel - currentMousedOverElement is null!");
            return;
        }

        currentMousedOverElement.UnregisterCallback<MouseMoveEvent>(OnMouseMoveUpdateSmallPopupPosition);

        if (m_CurrentSmallPopup != null)
        {
            m_CurrentSmallPopup.RemoveFromHierarchy();
            m_CurrentSmallPopup = null;
        }
    }
}
