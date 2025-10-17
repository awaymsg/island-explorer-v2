using UnityEngine;

[CreateAssetMenu(fileName = "CPartyLeader", menuName = "Scriptable Objects/CPartyLeader")]
public class CPartyLeader : CPartyMember
{
    [SerializeField]
    private Sprite m_OverworldSprite;

    // add unique skills
}

public class CPartyLeaderRuntime : CPartyMemberRuntime
{
    public CPartyLeaderRuntime(CPartyMember partyMemberSO, CPartyManager partyManager) : base(partyMemberSO, partyManager) { }
}
