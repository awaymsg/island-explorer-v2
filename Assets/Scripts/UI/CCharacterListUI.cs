using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CCharacterListUI : MonoBehaviour
{
    [SerializeField]
    private VisualTreeAsset m_CharacterButtonTemplate;
    [SerializeField]
    private VisualTreeAsset m_CharacterShortInfoPanelTemplate;
    [SerializeField]
    private VisualTreeAsset m_CharacterFullInfoPanelTemplate;
    [SerializeField]
    private VisualTreeAsset m_SmallTextPopupTemplate;

    private UIDocument m_CharacterListUI;
    private VisualElement m_CharacterPanel;
    private VisualElement m_CharacterPopup;
    private VisualElement m_CharacterFullInfoPanel;
    private CPartyMemberRuntime m_CurrentCharacterFullPanelCharacter;
    private CWorldInfoUI m_WorldInfoUI;

    private VisualElement m_CurrentSmallPopup;

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
    }

    public void AddCharacterButton(CPartyMemberRuntime partyMember)
    {
        TemplateContainer buttonInstance = m_CharacterButtonTemplate.Instantiate();
        Button newButton = buttonInstance.Q<Button>("CharacterButton");

        newButton.name = partyMember.CharacterName + "Button";
        newButton.iconImage = partyMember.PartyMemberPortrait.texture;
        
        newButton.style.display = DisplayStyle.Flex;

        newButton.clicked += () => OnButtonClicked(partyMember);

        newButton.RegisterCallback<MouseEnterEvent>(evt => OnMouseEnterPortrait(evt, partyMember, newButton));
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

        buttonToRemove.RemoveFromHierarchy();
    }

    public void RemoveAllElements()
    {
        m_CharacterPanel.Clear();
    }

    private void OnButtonClicked(CPartyMemberRuntime partyMember)
    {
        if (m_CurrentCharacterFullPanelCharacter != null)
        {
            if (m_CharacterFullInfoPanel != null)
            {
                m_CharacterFullInfoPanel.RemoveFromHierarchy();
                m_CharacterFullInfoPanel = null;

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

        TemplateContainer characterFullStatsContainer = m_CharacterFullInfoPanelTemplate.Instantiate();
        m_CharacterFullInfoPanel = characterFullStatsContainer.Q<VisualElement>("FullCharacterPanel");

        if (m_CharacterFullInfoPanel == null)
        {
            Debug.Log("CharacterFullInfoPanel is null!");
            return;
        }

        m_CharacterFullInfoPanel.style.alignSelf = Align.FlexEnd;
        m_CharacterFullInfoPanel.style.position = Position.Absolute;
        m_CharacterFullInfoPanel.style.right = 0;
        m_CharacterFullInfoPanel.style.top = 0;
        m_CharacterFullInfoPanel.style.width = new Length(30, LengthUnit.Percent);

        VisualElement characterPortrait = m_CharacterFullInfoPanel.Q<VisualElement>("Portrait");
        if (characterPortrait == null)
        {
            Debug.Log("characterPortrait is null!");
            return;
        }

        characterPortrait.style.backgroundImage = new StyleBackground(partyMember.PartyMemberPortrait);
        characterPortrait.style.width = partyMember.PartyMemberPortrait.rect.width;
        characterPortrait.style.height = partyMember.PartyMemberPortrait.rect.height;

        VisualElement portraitPanel = m_CharacterFullInfoPanel.Q<VisualElement>("PortraitPanel");
        if (portraitPanel == null)
        {
            Debug.Log("portraitPanel is null!");
            return;
        }

        Label nameInfo = m_CharacterFullInfoPanel.Q<Label>("NameInfo");
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
            traitLabel.RegisterCallback<MouseEnterEvent>(evt => OnMouseEnterCreateDescriptionLabel(evt, traitLabel, trait.Value));
            traitLabel.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveTraitLabel);
        }

        Label statsInfo = m_CharacterFullInfoPanel.Q<Label>("StatsInfo");
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

        // TODO: make inventory individual labels and interactable
        Label inventoryInfo = m_CharacterFullInfoPanel.Q<Label>("InventoryInfo");
        if (inventoryInfo == null)
        {
            Debug.Log("inventoryInfo is null!");
            return;
        }
        inventoryInfo.style.fontSize = 12;

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

        inventoryInfo.text = inventoryString;

        m_CharacterListUI.rootVisualElement.Add(m_CharacterFullInfoPanel);
    }

    private void OnMouseEnterPortrait(MouseEnterEvent mouseEnterEvent, CPartyMemberRuntime partyMember, Button button)
    {
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

    private void OnMouseEnterCreateDescriptionLabel(MouseEnterEvent mouseEnterEvent, VisualElement currentMousedOverElement, string descriptionText)
    {
        m_CurrentSmallPopup = m_SmallTextPopupTemplate.Instantiate();
        m_CharacterFullInfoPanel.Add(m_CurrentSmallPopup);

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
        m_CurrentSmallPopup.style.left = position.x + CUIManager.Instance.PopupMouseOffset.x;
        m_CurrentSmallPopup.style.top = position.y + CUIManager.Instance.PopupMouseOffset.y;
    }

    private void OnMouseLeaveTraitLabel(MouseLeaveEvent moustLeaveEvent)
    {
        if (m_CurrentSmallPopup != null)
        {
            m_CurrentSmallPopup.RemoveFromHierarchy();
            m_CurrentSmallPopup = null;
        }
    }
}
