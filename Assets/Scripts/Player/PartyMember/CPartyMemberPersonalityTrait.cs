using UnityEngine;

[CreateAssetMenu(fileName = "PartyMemberPersonalityTrait", menuName = "Scriptable Objects/PartyMemberPersonalityTrait")]
public class CPartyMemberPersonalityTrait : CPartyMemberTrait
{

}

public class CPartyMemberPersonalityTraitRuntime : CPartyMemberTraitRuntime
{
    public CPartyMemberPersonalityTraitRuntime(CPartyMemberTrait PartyMemberTraitSO) : base(PartyMemberTraitSO) {}
}
