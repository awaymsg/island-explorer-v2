using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "PartyMember", menuName = "Scriptable Objects/PartyMember")]
public class CPartyMember : ScriptableObject
{
    [SerializeField, Tooltip("Name of this party member type")]
    protected EPartyMemberType m_PartyMemberClass;

    // name of the character
    protected string m_CharacterName;

    [SerializeField, Tooltip("Special default skills of this party member type")]
    protected List<CPartyMemberSkill> m_PartyMemberSkills;

    [SerializeField, Tooltip("Trait type bias for this party member type")]
    protected EPartyMemberTraitType m_TraitTypeBais;

    [SerializeField, Tooltip("Trait bias amount, 0 is none, 1.0 is 100%")]
    protected float m_TraitBiasAmount = 0f;

    // portrait for UI
    private Sprite m_PartyMemberPortrait;

    protected List<CPartyMemberTrait> m_PartyMemberTraits;
    protected List<CPartyMemberPersonalityTrait> m_PartyMemberPersonalityTraits;
    protected List<CBodyPart> m_BodyParts;

    protected Dictionary<EPartyMemberStatType, float> m_PartyMemberStats;

    // player-facing
    private Dictionary<EBodyPart, List<string>> m_BodyPartConditions;
    private Dictionary<string, string> m_TraitDetails;

    protected UInt16 m_SkillLevel = 0;

    private float m_Cost = 0f;

    //-- getters
    public string CharacterName
    {
        get { return m_CharacterName; }
    }

    public Sprite PartyMemberPortrait
    {
        get { return m_PartyMemberPortrait; }
    }

    public float Cost
    {
        get { return m_Cost; }
    }
    //--

    public void InitializePartyMember()
    {
        CPartyManager partyManagerReference = FindFirstObjectByType<CPartyManager>();
        m_PartyMemberStats = partyManagerReference.GetDefaultPartyMemberStats(m_PartyMemberClass);

        CPartyMemberTrait[] traitPool = partyManagerReference.TraitPool;

        // for now just add randomly
        // todo: selectively add a number of traits
        if (traitPool != null && traitPool.Length > 0)
        {
            int index = UnityEngine.Random.Range(0, traitPool.Length);
            AddTrait(traitPool[index]);
        }
        else
        {
            Debug.Log("InitializePartyMember, there are no traits in the pool!");
        }

        CalculateCost();
    }

    public void AddBodyMod(CBodyPartModification bodyMod)
    {
        CBodyPart bodyPart = m_BodyParts.Find(p => p.BodyPart == bodyMod.ModLocation);
        if (bodyPart != null)
        {
            Debug.Log("AddBodyMod - bodymod does not correspond to a bodypart!");
            return;
        }

        bodyPart.Modifications.Add(bodyMod);

        if (bodyMod.bMultiplicative)
        {
            bodyPart.Health *= bodyMod.ModAmount;

            if (bodyMod.bPermanent)
            {
                bodyPart.MaxHealth *= bodyMod.ModAmount;
            }
        }
        else
        {
            bodyPart.Health += bodyMod.ModAmount;

            if (bodyMod.bPermanent)
            {
                bodyPart.MaxHealth += bodyMod.ModAmount;
            }
        }

        // add player facing details
        List<string> conditionContext = m_BodyPartConditions[bodyPart.BodyPart];
        conditionContext.Add(bodyMod.ModificationContext);
    }

    public void RemoveBodyMod(CBodyPartModification bodyMod)
    {
        CBodyPart bodyPart = m_BodyParts.Find(p => p.BodyPart == bodyMod.ModLocation);
        if (bodyPart != null)
        {
            Debug.Log("RemoveBodyMod - bodymod does not correspond to a bodypart!");
            return;
        }

        if (bodyMod.bMultiplicative)
        {
            bodyPart.Health /= bodyMod.ModAmount;

            if (bodyMod.bPermanent)
            {
                bodyPart.MaxHealth /= bodyMod.ModAmount;
            }
        }
        else
        {
            bodyPart.Health -= bodyMod.ModAmount;

            if (bodyMod.bPermanent)
            {
                bodyPart.MaxHealth -= bodyMod.ModAmount;
            }
        }

        // remove player facing details
        List<string> conditionContext = m_BodyPartConditions[bodyPart.BodyPart];
        conditionContext.Remove(bodyMod.ModificationContext);

        if (conditionContext.Count() == 0)
        {
            m_BodyPartConditions.Remove(bodyPart.BodyPart);
        }
    }

    public void AddTrait(CPartyMemberTrait trait)
    {
        m_PartyMemberTraits.Add(trait);

        foreach (SPartyMemberTraitEffect traitEffect in trait.TraitEffects)
        {
            // essentially move the bodymod from the trait to the actual bodypart
            List<CBodyPartModification> bodyModsToRemove = new List<CBodyPartModification>();

            foreach (CBodyPartModification bodyMod in traitEffect.BodyPartModifications)
            {
                AddBodyMod(bodyMod);

                bodyModsToRemove.Add(bodyMod);
            }

            foreach (CBodyPartModification bodyModToRemove in bodyModsToRemove)
            {
                traitEffect.BodyPartModifications.Remove(bodyModToRemove);
            }

            // apply stat modifiers
            foreach (SPartyMemberStatModifier statMod in traitEffect.StatModifiers)
            {
                if (statMod.bMultiplicative)
                {
                    m_PartyMemberStats[statMod.StatType] *= statMod.ModAmount;
                }
                else
                {
                    m_PartyMemberStats[statMod.StatType] += statMod.ModAmount;
                }
            }
        }

        // add player facing details
        m_TraitDetails[trait.TraitName] = trait.TraitDescription;
    }

    public void RemoveTrait(CPartyMemberTrait trait)
    {
        m_PartyMemberTraits.Remove(trait);

        foreach (SPartyMemberTraitEffect traitEffect in trait.TraitEffects)
        {
            // revert stat modifiers
            foreach (SPartyMemberStatModifier statMod in traitEffect.StatModifiers)
            {
                if (statMod.bMultiplicative)
                {
                    m_PartyMemberStats[statMod.StatType] /= statMod.ModAmount;
                }
                else
                {
                    m_PartyMemberStats[statMod.StatType] -= statMod.ModAmount;
                }
            }
        }

        // remove player facing details
        m_TraitDetails.Remove(trait.TraitName);
    }

    public float CalculateCost()
    {
        float totalCost = 0f;

        foreach (CPartyMemberTrait trait in m_PartyMemberTraits)
        {
            totalCost += trait.CalculateCosts();
        }
        foreach (CBodyPart bodyPart in m_BodyParts)
        {
            foreach (CBodyPartModification bodyMod in bodyPart.Modifications)
            {
                totalCost += bodyMod.CalculateCost();
            }
        }
        // todo: personality trait

        return totalCost;
    }
}
