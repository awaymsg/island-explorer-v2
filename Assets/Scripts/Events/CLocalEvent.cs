using UnityEngine;

[CreateAssetMenu(fileName = "CLocalEvent", menuName = "Scriptable Objects/CLocalEvent")]
public class CLocalEvent : ScriptableObject
{
    [Tooltip("Player facing name for event")]
    public string m_Name;

    [Tooltip("Description text")]
    public string m_DescriptionText;

    [Tooltip("Encountered Entity")]
    public EEventEntityType m_EntityType;

    [Tooltip("Event Options")]
    public SEventOption[] m_EventOptions;
}

public class CLocalEventRuntime
{
    private CLocalEvent m_LocalEventSO;
    private CPartyMemberRuntime m_Self;
    private CPartyMemberRuntime m_Target;

    // Getters
    public CLocalEvent LocalEventSO
    {
        get { return m_LocalEventSO; }
    }

    public CPartyMemberRuntime Self
    {
        get { return m_Self; }
    }

    public CPartyMemberRuntime Target
    {
        get { return m_Target; }
    }
    //--

    public CLocalEventRuntime(CLocalEvent localEventSO, CPartyMemberRuntime self, CPartyMemberRuntime target)
    {
        m_LocalEventSO = localEventSO;
        m_Self = self;
        m_Target = target;
    }
}
