using UnityEngine;
using UnityEngine.UIElements;

public class CCharacterListUI : MonoBehaviour
{
    [SerializeField]
    private VisualTreeAsset m_CharacterButtonTemplate;

    private UIDocument m_CharacterListUI;
    private VisualElement m_CharacterPanel;

    private void Start()
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

    public void OnButtonClicked(CPartyMemberRuntime partyMember)
    {
        Debug.Log(partyMember.CharacterName);
        Debug.Log(partyMember.CalculateCost());
    }
}
