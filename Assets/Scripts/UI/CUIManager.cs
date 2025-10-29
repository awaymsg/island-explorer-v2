using UnityEngine;

public class CUIManager : MonoBehaviour
{
    private CCharacterListUI m_CharacterListUI;
    private CWorldInfoUI m_WorldInfoUI;

    private void Awake()
    {
        m_CharacterListUI = FindFirstObjectByType<CCharacterListUI>();
        m_WorldInfoUI = FindFirstObjectByType<CWorldInfoUI>();
    }

    public void AddCharacterButton(CPartyMemberRuntime partyMember)
    {
        m_CharacterListUI.AddCharacterButton(partyMember);
    }

    public void RemoveCharacterButton(string partyMemberName)
    {
        m_CharacterListUI.RemoveCharacterButton(partyMemberName);
    }

    public void ClearCharacterListUI()
    {
        m_CharacterListUI.RemoveAllElements();
    }

    public void InitializeWorldInfoPanel()
    {
        m_WorldInfoUI.InitializeWorldInfoPanel();
    }

    public void UpdateDayInfo(string dayInfo)
    {
        m_WorldInfoUI.UpdateDayInfo(dayInfo);
    }

    public void UpdateWorldInfo(string worldInfo)
    {
        m_WorldInfoUI.UpdateWorldInfo(worldInfo);
    }
}
