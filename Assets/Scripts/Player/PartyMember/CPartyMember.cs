using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "PartyMember", menuName = "Scriptable Objects/PartyMember")]
public class CPartyMember : ScriptableObject
{
    [Tooltip("Name of this party member type")]
    public EPartyMemberType m_PartyMemberClass;

    public EPartyMemberGender m_PartyMemberGender;

    [Tooltip("Default stats for this party member")]
    public SPartyMemberStat[] m_BaseStats;

    [Tooltip("Special default skills of this party member type")]
    public List<CPartyMemberSkill> m_PartyMemberSkills;

    [Tooltip("Trait type bias for this party member type")]
    public EPartyMemberTraitType m_TraitTypeBais;

    [Tooltip("Trait bias amount, 0 is none, 1.0 is 100%")]
    public float m_TraitBiasAmount = 0f;
}

public class CPartyMemberRuntime
{
    protected CPartyMember m_PartyMemberSO;
    protected CPartyManager m_PartyManager;

    // name of the character
    protected string m_CharacterName;

    // portrait for UI
    private Sprite m_PartyMemberPortrait;

    protected List<CPartyMemberTraitRuntime> m_PartyMemberTraits = new List<CPartyMemberTraitRuntime>();
    protected List<CPartyMemberPersonalityTrait> m_PartyMemberPersonalityTraits = new List<CPartyMemberPersonalityTrait>();
    protected CBodyPart[] m_BodyParts;

    protected Dictionary<EPartyMemberStatType, float> m_PartyMemberStats;

    // player-facing
    private Dictionary<EBodyPart, List<string>> m_BodyPartConditions = new Dictionary<EBodyPart, List<string>>();
    private Dictionary<string, string> m_TraitDetails = new Dictionary<string, string>();

    protected UInt16 m_SkillLevel = 0;

    private float m_TotalCost = 0f;

    public CPartyMemberRuntime(CPartyMember partyMemberSO, CPartyManager partyManager)
    {
        m_PartyMemberSO = partyMemberSO;
        m_PartyManager = partyManager;
    }

    //-- getters
    public string CharacterName
    {
        get { return m_CharacterName; }
    }

    public EPartyMemberGender PartyMemberGender
    {
        get { return m_PartyMemberSO.m_PartyMemberGender; }
    }

    public Sprite PartyMemberPortrait
    {
        get { return m_PartyMemberPortrait; }
    }

    public float TotalCost
    {
        get { return m_TotalCost; }
    }
    //--

    public void InitializePartyMember()
    {
        if (m_PartyManager == null)
        {
            Debug.Log("InitializePartyMember - PartyManager is null!");
        }

        m_PartyMemberStats = GetBaseStats();

        // make a deep copy of this array
        m_BodyParts = m_PartyManager.DefaultBodyParts?.Select(bodyPart => new CBodyPart(bodyPart)).ToArray() ?? Array.Empty<CBodyPart>();

        // todo: prefixes

        if (m_PartyMemberSO.m_PartyMemberGender == EPartyMemberGender.Invalid)
        {
            Debug.Log("InitializePartyMember - party member gender is not set!");
        }

        if (m_PartyMemberSO.m_PartyMemberGender == EPartyMemberGender.Male)
        {
            string[] maleNames = m_PartyManager.MaleNamesPool;
            if (maleNames.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, maleNames.Length);
                m_CharacterName = maleNames[index];
            }
            else
            {
                Debug.Log("InitializePartyMember - there are no names in the male names pool!");
            }

            Sprite[] malePortraits = m_PartyManager.MalePortraitsPool;
            if (malePortraits.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, malePortraits.Length);
                m_PartyMemberPortrait = malePortraits[index];
            }
            else
            {
                Debug.Log("InitializePartyMember - male portraits pool is empty!");
            }
        }
        else if (m_PartyMemberSO.m_PartyMemberGender == EPartyMemberGender.Female)
        {
            string[] femaleNames = m_PartyManager.FemaleNamesPool;
            if (femaleNames.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, femaleNames.Length);
                m_CharacterName = femaleNames[index];
            }
            else
            {
                Debug.Log("InitializePartyMember - there are no names in the female names pool!");
            }

            Sprite[] femalePortraits = m_PartyManager.FemalePortraitsPool;
            if (femalePortraits.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, femalePortraits.Length);
                m_PartyMemberPortrait = femalePortraits[index];
            }
            else
            {
                Debug.Log("InitializePartyMember - female portraits pool is empty!");
            }
        }

        string[] surnames = m_PartyManager.Surnames;
        if (surnames.Length > 0)
        {
            int index = UnityEngine.Random.Range(0, surnames.Length);
            m_CharacterName += " " + surnames[index];
        }
        else
        {
            Debug.Log("InitializePartyMember - there are no names in the surnames pool!");
        }

        // todo: suffixes

        CPartyMemberTrait[] traitPool = m_PartyManager.TraitPool;

        // for now just add randomly
        // todo: selectively add a number of traits
        if (traitPool != null && traitPool.Length > 0)
        {
            int index = UnityEngine.Random.Range(0, traitPool.Length);
            AddTrait(traitPool[index]);
        }
        else
        {
            Debug.Log("InitializePartyMember - there are no traits in the pool!");
        }

        CalculateCost();
    }

    private Dictionary<EPartyMemberStatType, float> GetBaseStats()
    {
        SPartyMemberStat[] defaultStats = m_PartyMemberSO.m_BaseStats;

        Dictionary<EPartyMemberStatType, float> defaultStatsBook = new Dictionary<EPartyMemberStatType, float>();

        foreach (SPartyMemberStat defaultStat in defaultStats)
        {
            defaultStatsBook[defaultStat.StatType] = defaultStat.Value;
        }

        return defaultStatsBook;
    }

    public void AddBodyMod(CBodyPartModification bodyMod)
    {
        CBodyPart bodyPart = Array.Find<CBodyPart>(m_BodyParts, p => p.BodyPart == bodyMod.ModLocation);
        if (bodyPart == null)
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
        if (!m_BodyPartConditions.ContainsKey(bodyPart.BodyPart))
        {
            m_BodyPartConditions.Add(bodyPart.BodyPart, new List<string>());
        }

        m_BodyPartConditions[bodyPart.BodyPart].Add(bodyMod.ModificationContext);
    }

    public void RemoveBodyMod(CBodyPartModification bodyMod)
    {
        CBodyPart bodyPart = Array.Find(m_BodyParts, p => p.BodyPart == bodyMod.ModLocation);
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
        m_BodyPartConditions[bodyPart.BodyPart].Remove(bodyMod.ModificationContext);

        if (m_BodyPartConditions[bodyPart.BodyPart].Count() == 0)
        {
            m_BodyPartConditions.Remove(bodyPart.BodyPart);
        }
    }

    public void AddTrait(CPartyMemberTrait trait)
    {
        CPartyMemberTraitRuntime runtimeTrait = new CPartyMemberTraitRuntime(trait);

        foreach (SPartyMemberTraitEffect traitEffect in runtimeTrait.TraitEffects)
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

        m_PartyMemberTraits.Add(runtimeTrait);

        // add player facing details
        m_TraitDetails.Add(trait.m_TraitName, trait.m_TraitDescription);
    }

    public void RemoveTrait(CPartyMemberTraitRuntime trait)
    {
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

        m_PartyMemberTraits.Remove(trait);

        // remove player facing details
        m_TraitDetails.Remove(trait.TraitName);
    }

    public float CalculateCost()
    {
        float totalCost = 0f;

        foreach (CPartyMemberTraitRuntime trait in m_PartyMemberTraits)
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

        m_TotalCost = totalCost;
        return totalCost;
    }
}
