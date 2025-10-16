using System.ComponentModel;
using UnityEngine;

[CreateAssetMenu(fileName = "PartyMemberTrait", menuName = "Scriptable Objects/PartyMemberTrait")]
public class CPartyMemberTrait : ScriptableObject
{
    [SerializeField, Tooltip("Trait name (player-facing)")]
    private string m_TraitName;

    [SerializeField, Tooltip("Player-facing description")]
    private string m_TraitDescription;

    [SerializeField]
    private EPartyMemberTraitType m_TraitType;

    [SerializeField, Tooltip("This trait cannot exist with these other traits, if any. For example, someone can't be both Tall and Short")]
    private CPartyMemberTrait[] m_InvalidatedTraits;

    [SerializeField, Tooltip("Effects of this trait")]
    private SPartyMemberTraitEffect[] m_TraitEffects;

    [SerializeField, Tooltip("If true, this trait can only be assigned when a new party member is generated.")]
    private bool m_bIsGenerationOnly = true;

    [SerializeField, ReadOnly(true)]
    private float m_Cost = 0f;

    //-- getters
    public string TraitName
    {
        get { return m_TraitName; }
    }

    public string TraitDescription
    {
        get { return m_TraitDescription; }
    }

    public EPartyMemberTraitType TraitType
    {
        get { return m_TraitType; }
    }

    public CPartyMemberTrait[] InvalidatedTraits
    {
        get { return m_InvalidatedTraits; }
    }

    public SPartyMemberTraitEffect[] TraitEffects
    {
        get { return m_TraitEffects; }
    }
    //--

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

    private void OnValidate()
    {
        CalculateCosts();
    }
}
