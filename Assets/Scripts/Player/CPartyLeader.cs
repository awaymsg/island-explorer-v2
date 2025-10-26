using UnityEngine;

[CreateAssetMenu(fileName = "CPartyLeader", menuName = "Scriptable Objects/CPartyLeader")]
public class CPartyLeader : CPartyMember
{
    public Sprite m_OverworldSprite;

    // add unique skills
}

public class CPartyLeaderRuntime : CPartyMemberRuntime
{
    private CPartyLeader m_PartyLeaderSO;

    public CPartyLeaderRuntime(CPartyMember partyMemberSO, CPartyManager partyManager) : base(partyMemberSO, partyManager)
    {
        m_PartyLeaderSO = m_PartyMemberSO as CPartyLeader;

        if (m_PartyLeaderSO == null)
        {
            Debug.Log("CPartyLeaderRunTime - partyMemberSO was not a CPartyLeader!");
        }
    }
}
