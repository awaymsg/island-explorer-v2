using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CPartyPlayerCharacter : MonoBehaviour
{
    private CPartyLeaderRuntime m_PartyLeader;
    private List<CPartyMemberRuntime> m_PartyMembers;

    //-- getters
    public CPartyLeaderRuntime PartyLeader
    {  
        get { return m_PartyLeader; }
    }

    public List<CPartyMemberRuntime> PartyMembers
    {
        get { return m_PartyMembers; }
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

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
