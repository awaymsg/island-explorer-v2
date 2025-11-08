using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CPartyPlayerCharacter : MonoBehaviour
{
    private CPartyMemberRuntime m_PartyLeader;
    private List<CPartyMemberRuntime> m_PartyMembers;
    private Vector3Int m_CurrentLocation;

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
}
