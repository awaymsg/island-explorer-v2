using System.ComponentModel;
using UnityEngine;

[CreateAssetMenu(fileName = "PartyMemberTrait", menuName = "Scriptable Objects/PartyMemberTrait")]
public class CPartyMemberTrait : ScriptableObject
{
    [SerializeField, Tooltip("Trait name (player-facing)")]
    private string m_TraitName;

    [SerializeField, Tooltip("Player-facing description")]
    private string m_Description;

    [SerializeField]
    private EPartyMemberTraitType m_TraitType;

    [SerializeField, Tooltip("This trait cannot exist with these other traits, if any. For example, someone can't be both Tall and Short")]
    private CPartyMemberTrait[] m_InvalidatesTraits;

    [SerializeField, Tooltip("Effects of this trait")]
    private SPartyMemberTraitEffect[] m_TraitEffects;

    [SerializeField, ReadOnly(true)]
    private float m_Cost = 0f;

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
        }

        m_Cost = totalCost;
        return totalCost;
    }

    public SPartyMemberTraitEffect[] GetTraitEffects()
    {
        return m_TraitEffects;
    }

    private void OnValidate()
    {
        CalculateCosts();
    }
}
