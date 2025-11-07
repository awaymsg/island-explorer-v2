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
    public SPartyMemberTraitEffect[] m_TraitEffects;

    [Tooltip("If true, this trait can only be assigned when a new party member is generated.")]
    public bool m_bIsGenerationOnly = true;

    [Tooltip("If true, this trait is hidden, until it is discovered later")]
    public bool m_bIsHidden = false;

    [ReadOnly(true)]
    public float m_TotalCost = 0f;

    public float CalculateCosts()
    {
        float totalCost = 0f;

        foreach (SPartyMemberTraitEffect traitEffect in m_TraitEffects)
        {
            foreach (CBodyPartModification bodyMod in traitEffect.BodyPartModifications)
            {
                totalCost += bodyMod.CalculateCost();
            }

            foreach (SPartyMemberStatModifier statMod in traitEffect.StatModifiers)
            {
                totalCost += statMod.Cost;
            }

            foreach (SPartyMemberTraitItem grantedItem in traitEffect.GrantedItems)
            {
                totalCost += grantedItem.Cost;
            }
        }

        m_TotalCost = totalCost;
        return totalCost;
    }

    private void OnValidate()
    {
        CalculateCosts();
    }
}

public class CPartyMemberTraitRuntime
{
    private CPartyMemberTrait m_PartyMemberTraitSO;
    private SPartyMemberTraitEffect[] m_TraitEffects;
    private bool m_bIsHidden = false;

    //-- getters
    public string TraitName
    {
        get { return m_PartyMemberTraitSO.m_TraitName; }
    }

    public SPartyMemberTraitEffect[] TraitEffects
    {
        get { return m_TraitEffects; }
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

        // todo: it would be better if we didn't have to deep copy, think about how
        m_TraitEffects = m_PartyMemberTraitSO.m_TraitEffects
            .Select(effect => DeepCopyTraitEffect(effect))
            .ToArray();

        m_bIsHidden = partyMemberTraitSO.m_bIsHidden;
    }

    private SPartyMemberTraitEffect DeepCopyTraitEffect(SPartyMemberTraitEffect original)
    {
        return new SPartyMemberTraitEffect
        {
            // Copy all properties
            BodyPartModifications = original.BodyPartModifications?
                .Select(mod => new CBodyPartModification(mod))
                .ToList() ?? new List<CBodyPartModification>(),

            StatModifiers = original.StatModifiers?
                .Select(statMod => new SPartyMemberStatModifier(statMod))
                .ToArray(),

            GrantedItems = original.GrantedItems?.ToList()
        };
    }

    public float CalculateCosts()
    {
        float totalCost = 0f;

        foreach (SPartyMemberTraitEffect traitEffect in m_TraitEffects)
        {
            foreach (CBodyPartModification bodyMod in traitEffect.BodyPartModifications)
            {
                totalCost += bodyMod.CalculateCost();
            }

            foreach (SPartyMemberStatModifier statMod in traitEffect.StatModifiers)
            {
                totalCost += statMod.Cost;
            }

            foreach (SPartyMemberTraitItem grantedItem in traitEffect.GrantedItems)
            {
                totalCost += grantedItem.Cost;
            }
        }

        return totalCost;
    }
}
