using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

[CreateAssetMenu(fileName = "PartyMember", menuName = "Scriptable Objects/PartyMember")]
public class CPartyMember : ScriptableObject
{
    [SerializeField, Tooltip("Name of this party member type")]
    private EPartyMemberType m_Class;

    // name of the character
    private string m_CharacterName;

    [SerializeField, Tooltip("Special default skills of this party member type")]
    private List<CPartyMemberSkill> m_PartyMemberSkills;

    [SerializeField, Tooltip("Trait type bias for this party member type")]
    private EPartyMemberTraitType m_TraitTypeBais;

    [SerializeField, Tooltip("Trait bias amount, 0 is none, 1.0 is 100%")]
    private float m_TraitBiasAmount = 0f;

    private Sprite m_Sprite;

    private List<CPartyMemberTrait> m_PartyMemberTraits;
    private List<CPartyMemberPersonalityTrait> m_PartyMemberPersonalityTraits;
    private List<CBodyPart> m_BodyParts;

    private Dictionary<EPartyMemberStatType, float> m_PartyMemberStats;

    // player-facing
    private Dictionary<EBodyPart, string> m_BodyPartConditions;

    private UInt16 m_SkillLevel = 0;

    private float m_Cost = 0f;

    public void InitializePartyMember()
    {
        foreach (CPartyMemberTrait trait in m_PartyMemberTraits)
        {
            foreach (SPartyMemberTraitEffect traitEffect in trait.GetTraitEffects())
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
            }
        }

        CalculateStats();
        CalculateCost();
    }

    private void AddBodyMod(CBodyPartModification bodyMod)
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

        // if this bodypart already has a condition, replace it, and add onto the string
        if (m_BodyPartConditions.ContainsKey(bodyPart.BodyPart))
        {
            string conditionContext = m_BodyPartConditions[bodyPart.BodyPart];
            conditionContext = conditionContext + ", " + bodyMod.ModificationContext;
        }
        else // otherwise, add it to the dictionary
        {
            m_BodyPartConditions.Add(bodyPart.BodyPart, bodyMod.ModificationContext);
        }

        CalculateStats();
    }

    private void CalculateStats()
    {

    }

    private float CalculateCost()
    {
        float totalCost = 0f;

        foreach (CPartyMemberTrait trait in m_PartyMemberTraits)
        {
            totalCost += trait.CalculateCosts();
        }
        // todo: personality trait and bodyparts

        return totalCost;
    }
}
