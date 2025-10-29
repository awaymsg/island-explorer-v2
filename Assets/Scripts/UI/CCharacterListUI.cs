using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

public class CCharacterListUI : MonoBehaviour
{
    [SerializeField]
    private VisualTreeAsset m_CharacterButtonTemplate;
    [SerializeField]
    private VisualTreeAsset m_CharacterShortInfoPanelTemplate;

    private UIDocument m_CharacterListUI;
    private VisualElement m_CharacterPanel;

    private VisualElement m_CurrentPopup;

    private void Awake()
    {
        m_CharacterListUI = GetComponent<UIDocument>();
        m_CharacterPanel = m_CharacterListUI.rootVisualElement.Q<VisualElement>("CharacterPanel");
    }

    public void AddCharacterButton(CPartyMemberRuntime partyMember)
    {
        TemplateContainer buttonInstance = m_CharacterButtonTemplate.Instantiate();
        Button newButton = buttonInstance.Q<Button>("CharacterButton");

        newButton.name = partyMember.CharacterName + "Button";
        newButton.iconImage = partyMember.PartyMemberPortrait.texture;
        
        newButton.style.display = DisplayStyle.Flex;

        newButton.clicked += () => OnButtonClicked(partyMember);

        newButton.RegisterCallback<MouseEnterEvent>(evt => OnMouseEnter(evt, partyMember, newButton));
        newButton.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

        m_CharacterPanel.Add(buttonInstance);
    }

    public void RemoveCharacterButton(string partyMemberName)
    {
        Button buttonToRemove = m_CharacterPanel.Q<Button>(partyMemberName + "Button");

        if (buttonToRemove == null)
        {
            Debug.Log(string.Format("RemoveCharacterButton - character {0} button not found!", partyMemberName));
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
        Debug.Log(partyMember.CharacterName);
        Debug.Log(partyMember.TotalCost);
    }

    private void OnMouseEnter(MouseEnterEvent mouseEnterEvent, CPartyMemberRuntime partyMember, Button button)
    {
        TemplateContainer popupStatsContainer = m_CharacterShortInfoPanelTemplate.Instantiate();
        m_CurrentPopup = popupStatsContainer.Q<VisualElement>("CharacterShortInfoPanel");

        if (m_CurrentPopup == null)
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

            m_CurrentPopup.style.left = localX + buttonWorldRect.width;
            m_CurrentPopup.style.top = localY;

            m_CurrentPopup.style.height = buttonWorldRect.height;
        }

        Label nameLabel = m_CurrentPopup.Q<Label>("CharacterName");
        Label shortStatsLabel = m_CurrentPopup.Q<Label>("CharacterShortStats");
        nameLabel.AddToClassList("white-text");
        shortStatsLabel.AddToClassList("white-text-small");
        
        nameLabel.text = partyMember.CharacterName;
        shortStatsLabel.text = GetShortStatsString(partyMember);

        m_CharacterPanel.Add(m_CurrentPopup);
    }

    private string GetShortStatsString(CPartyMemberRuntime partyMember)
    {
        string statsString = "Traits: ";
        int index = 0;

        foreach (string trait in partyMember.TraitDetails.Keys)
        {
            statsString += trait;
            if (index < partyMember.TraitDetails.Count - 1)
            {
                statsString += ", ";
            }

            index++;
        }

        return statsString;
    }

    private void OnMouseLeave(MouseLeaveEvent mouseLeaveEvent)
    {
        m_CharacterPanel.Remove(m_CurrentPopup);
        m_CurrentPopup = null;
    }
}
