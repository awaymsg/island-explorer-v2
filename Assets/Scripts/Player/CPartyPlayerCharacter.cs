using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CPartyPlayerCharacter : MonoBehaviour
{
    private CPartyLeaderRuntime m_PartyLeader;
    private List<CPartyMemberRuntime> m_PartyMembers;
    private Vector3Int m_CurrentLocation;

    //-- getters
    public CPartyLeaderRuntime PartyLeader
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

    public void InitializePartyPlayerCharacter(CPartyLeaderRuntime partyLeader, List<CPartyMemberRuntime> partyMembers)
    {
        m_PartyLeader = partyLeader;
        m_PartyMembers = partyMembers;
    }

    public void AddPartyMember(CPartyMemberRuntime partyMember)
    {
        m_PartyMembers.Add(partyMember);
    }
}
