using UnityEngine;

public class CUIManager : MonoBehaviour
{
    private static CUIManager m_Instance;

    private CCharacterListUI m_CharacterListUI;
    private CWorldInfoUI m_WorldInfoUI;
    private CPopupsUI m_PopupsUI;

    //-- Getters
    public CPopupsUI PopupsUI
    {
        get { return m_PopupsUI; }
    }

    public CWorldInfoUI WorldInfoUI
    {
        get { return m_WorldInfoUI; }
    }

    public CCharacterListUI CharacterListUI
    {
        get { return m_CharacterListUI; }
    }

    public static CUIManager Instance
    {
        get {
            if (m_Instance == null)
            {
                m_Instance = FindFirstObjectByType<CUIManager>();
            }

            return m_Instance;
        }
    }
    //--

    private void Awake()
    {
        m_CharacterListUI = GetComponentInChildren<CCharacterListUI>(true);
        m_WorldInfoUI = GetComponentInChildren<CWorldInfoUI>(true);
        m_PopupsUI = GetComponentInChildren<CPopupsUI>(true);
    }

    public void ClearCharacterListUI()
    {
        m_CharacterListUI.RemoveAllElements();
    }

    public void InitializeWorldInfoPanel()
    {
        m_WorldInfoUI.InitializeWorldInfoPanel();
    }
}
