using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CPartyPlayerCharacter : MonoBehaviour
{
    private CPartyMemberRuntime m_PartyLeader;
    private List<CPartyMemberRuntime> m_PartyMembers;
    private Vector3Int m_CurrentLocation;
    private float m_PartyMorale = 0f;

    //-- getters
    public CPartyMemberRuntime PartyLeader
    {  
        get { return m_PartyLeader; }
    }

    public List<CPartyMemberRuntime> PartyMembers
    {
        get { return m_PartyMembers; }
    }

    public Vector3Int CurrentLocation
    {
        get { return m_CurrentLocation; }
        set { m_CurrentLocation = value; }
    }

    public float PartyMorale
    {
        get { return m_PartyMorale; }
    }
    //--

    public void InitializePartyPlayerCharacter(CPartyMemberRuntime partyLeader, Queue<CPartyMemberRuntime> partyMembers)
    {
        m_PartyLeader = partyLeader;
        m_PartyMembers = partyMembers.ToList();
    }

    public void AddPartyMember(CPartyMemberRuntime partyMember)
    {
        m_PartyMembers.Add(partyMember);
    }

    public void RemovePartyMember(CPartyMemberRuntime partyMember)
    {
        m_PartyMembers.Remove(partyMember);
    }

    public void CalculatePartyMorale()
    {
        foreach (CPartyMemberRuntime partyMember in m_PartyMembers)
        {

        }
    }

    private float CalculatePartyMemberMorale(bool bIsLeader)
    {
        return 100f;
    }
}
