using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PartyMemberTrait", menuName = "Scriptable Objects/PartyMemberTrait")]
public class CPartyMemberTrait : ScriptableObject
{
    [Tooltip("Trait name (player-facing)")]
    public string m_TraitName;

    [Tooltip("Player-facing description")]
    public string m_TraitDescription;

    public EPartyMemberTraitType m_TraitType;

    [Tooltip("This trait cannot exist with these other traits, if any. For example, someone can't be both Tall and Short")]
    public CPartyMemberTrait[] m_InvalidatedTraits;

    [Tooltip("Effects of this trait")]
    public SPartyMemberTraitEffect m_TraitEffect;

    [Tooltip("If true, this trait can only be assigned when a new party member is generated.")]
    public bool m_bIsGenerationOnly = true;

    [Tooltip("If true, this trait is hidden, until it is discovered later")]
    public bool m_bIsHidden = false;

    [ReadOnly(true)]
    public float m_TotalCost = 0f;

    public float CalculateCosts()
    {
        float totalCost = 0f;

        foreach (CBodyPartModification bodyMod in m_TraitEffect.BodyPartModifications)
        {
            totalCost += bodyMod.CalculateCost();
        }

        foreach (SPartyMemberStatModifier statMod in m_TraitEffect.StatModifiers)
        {
            totalCost += statMod.Cost;
        }

        foreach (CInventoryItem grantedItem in m_TraitEffect.GrantedItems)
        {
            totalCost += grantedItem.m_TotalCost;
        }

        totalCost += m_TraitEffect.SelfAttitudeModifier.Cost;
        totalCost += m_TraitEffect.AttitudeModifier.Cost;

        m_TotalCost = totalCost;
        return totalCost;
    }

    public void OnValidate()
    {
        CalculateCosts();
    }
}

public class CPartyMemberTraitRuntime
{
    private readonly CPartyMemberTrait m_PartyMemberTraitSO;
    private bool m_bIsHidden = false;

    //-- getters
    public string TraitName
    {
        get { return m_PartyMemberTraitSO.m_TraitName; }
    }

    public SPartyMemberTraitEffect TraitEffect
    {
        get { return m_PartyMemberTraitSO.m_TraitEffect; }
    }

    public bool bIsHidden
    {
        get { return m_bIsHidden; }
        set { m_bIsHidden = value; }
    }
    //--

    public CPartyMemberTraitRuntime(CPartyMemberTrait partyMemberTraitSO)
    {
        m_PartyMemberTraitSO = partyMemberTraitSO;

        m_bIsHidden = partyMemberTraitSO.m_bIsHidden;
    }

    public float CalculateCosts()
    {
        return m_PartyMemberTraitSO.CalculateCosts();
    }
}
