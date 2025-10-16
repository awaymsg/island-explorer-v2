using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CPartyPlayerCharacter : MonoBehaviour
{
    private CPartyLeader m_PartyLeader;
    private List<CPartyMember> m_PartyMembers;

    public void InitializePartyPlayerCharacter(CPartyLeader partyLeader, List<CPartyMember> partyMembers)
    {
        m_PartyLeader = partyLeader;
        m_PartyMembers = partyMembers;
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
