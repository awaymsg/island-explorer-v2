using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "PartyMember", menuName = "Scriptable Objects/PartyMember")]
public class CPartyMember : ScriptableObject
{
    [Tooltip("The overworld sprite of this character if they are party leader")]
    public Sprite m_OverworldSprite;

    [Tooltip("Party member type")]
    public EPartyMemberType m_PartyMemberClass;

    [Tooltip("Player facing class name")]
    public string m_PartyMemberClassName;

    public EPartyMemberGender m_PartyMemberGender;

    [Tooltip("Default stats for this party member")]
    public SPartyMemberDefaultStat[] m_BaseStats;

    [Tooltip("Default Starting items")]
    public CInventoryItem[] m_StartingItems;

    [Tooltip("Special default skills of this party member type")]
    public List<CPartyMemberSkill> m_PartyMemberSkills;

    [Tooltip("Trait type bias for this party member type")]
    public EPartyMemberTraitType m_TraitTypeBias;

    [Tooltip("Trait bias amount, 0 is none, 1.0 is 100%")]
    public float m_TraitBiasAmount = 0f;

    [Tooltip("Default amount of attitude toward other party members")]
    public float m_DefaultAttitude = 0f;

    [Tooltip("Default amount of attitude toward self")]
    public float m_DefaultSelfAttitude = 0f;
}

public class CPartyMemberRuntime : IDisposable
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

    protected Dictionary<EPartyMemberStatType, CPartyMemberStat> m_PartyMemberStats;
    protected CInventory m_ItemInventory;

    // player-facing
    private Dictionary<EBodyPart, List<string>> m_BodyPartConditions = new Dictionary<EBodyPart, List<string>>();
    private Dictionary<string, string> m_TraitDetails = new Dictionary<string, string>();
    private Dictionary<string, float> m_AttitudesBook = new Dictionary<string, float>();

    protected UInt16 m_SkillLevel = 0;

    private float m_DefaultAttitude = 0f;
    private float m_DefaultSelfAttitude = 0f;

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

    public Sprite OverworldSprite
    {
        get { return m_PartyMemberSO.m_OverworldSprite; }
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

    public Dictionary<string, string> TraitDetails
    {
        get { return m_TraitDetails; }
    }

    public Dictionary<EPartyMemberStatType, CPartyMemberStat> PartyMemberStats
    {
        get { return m_PartyMemberStats; }
    }

    public Dictionary<string, float> AttitudesBook
    {
        get { return m_AttitudesBook; }
    }

    public CInventory ItemInventory
    {
        get { return m_ItemInventory; }
    }

    public string PartyMemberClassName
    {
        get { return m_PartyMemberSO.m_PartyMemberClassName; }
    }
    //--

    public void InitializePartyMember()
    {
        if (m_PartyManager == null)
        {
            Debug.Log("InitializePartyMember - PartyManager is null!");
        }

        m_PartyMemberStats = SetBaseStats();
        m_DefaultAttitude = m_PartyMemberSO.m_DefaultAttitude;
        m_DefaultSelfAttitude = m_PartyMemberSO.m_DefaultSelfAttitude;

        // Initialize inventory
        m_ItemInventory = new CInventory();

        // Set inventory max weight and add callback when Fortitude changes
        if (m_PartyMemberStats.ContainsKey(EPartyMemberStatType.Fortitude))
        {
            m_PartyMemberStats[EPartyMemberStatType.Fortitude].OnStatChanged += UpdateInventoryMaxWeight;
            UpdateInventoryMaxWeight(0f, m_PartyMemberStats[EPartyMemberStatType.Fortitude].Value);
        }
        else
        {
            Debug.Log("InitializePartyMember - Party member missing Fortitude stat!");
        }

        AddDefaultItems();

        // make a deep copy of this array
        m_BodyParts = m_PartyManager.DefaultBodyParts?.Select(bodyPart => new CBodyPart(bodyPart)).ToArray() ?? Array.Empty<CBodyPart>();

        GenerateName();
        GeneratePortrait();

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

        CPartyManager.Instance.m_OnCharacterAdded += AddPartyMemberAttitude;
        CPartyManager.Instance.m_OnCharacterRemoved += RemovePartyMemberAttitude;

        CalculateCost();
    }

    public void Dispose()
    {
        if (CPartyManager.Instance == null)
        {
            return;
        }

        CPartyManager.Instance.m_OnCharacterAdded -= AddPartyMemberAttitude;
        CPartyManager.Instance.m_OnCharacterRemoved -= RemovePartyMemberAttitude;
    }

    private void GenerateName()
    {
        string name = string.Empty;

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
                name = maleNames[index];
            }
            else
            {
                Debug.Log("InitializePartyMember - there are no names in the male names pool!");
            }
        }
        else if (m_PartyMemberSO.m_PartyMemberGender == EPartyMemberGender.Female)
        {
            string[] femaleNames = m_PartyManager.FemaleNamesPool;
            if (femaleNames.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, femaleNames.Length);
                name = femaleNames[index];
            }
            else
            {
                Debug.Log("InitializePartyMember - there are no names in the female names pool!");
            }
        }

        string[] surnames = m_PartyManager.Surnames;
        if (surnames.Length > 0)
        {
            int index = UnityEngine.Random.Range(0, surnames.Length);
            name += " " + surnames[index];
        }
        else
        {
            Debug.Log("InitializePartyMember - there are no names in the surnames pool!");
        }

        // todo: suffixes

        if (CPartyManager.ExistingNames.Contains(name))
        {
            // Should be okay to be recursive given number of combinations of names and limited number of party members
            GenerateName();
        }

        m_CharacterName = name;
    }

    private void GeneratePortrait()
    {
        // TODO: actually generate portraits

        if (m_PartyMemberSO.m_PartyMemberGender == EPartyMemberGender.Male)
        {
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
    }

    private void AddPartyMemberAttitude(CPartyMemberRuntime partyMember)
    {
        if (partyMember == null)
        {
            Debug.Log("AddPartyMemberAttitude - partyMember is null!");
            return;
        }

        // Party members can have attitudes toward themselves
        m_AttitudesBook.Add(partyMember.CharacterName, (partyMember == this) ? m_DefaultSelfAttitude : m_DefaultAttitude);
    }

    private void RemovePartyMemberAttitude(CPartyMemberRuntime partyMember)
    {
        if (partyMember == null)
        {
            Debug.Log("RemovePartyMemberAttitude - partyMember is null!");
            return;
        }

        m_AttitudesBook.Remove(partyMember.CharacterName);
    }

    private void UpdateInventoryMaxWeight(float oldValue, float newValue)
    {
        if (m_ItemInventory == null)
        {
            Debug.Log("ItemInventory is null!");
            return;
        }

        m_ItemInventory.MaxWeight = m_PartyMemberStats[EPartyMemberStatType.Fortitude].Value;
    }

    private void AddDefaultItems()
    {
        foreach (CInventoryItem item in m_PartyMemberSO.m_StartingItems)
        {
            bool bSuccess = m_ItemInventory.TryAddItemToInventory(new CInventoryItemRuntime(item), this);

            if (!bSuccess)
            {
                Debug.Log("AddDefaultItems - default item failed to be added!");
            }
        }
    }

    private Dictionary<EPartyMemberStatType, CPartyMemberStat> SetBaseStats()
    {
        SPartyMemberDefaultStat[] defaultStats = m_PartyMemberSO.m_BaseStats;

        Dictionary<EPartyMemberStatType, CPartyMemberStat> defaultStatsBook = new Dictionary<EPartyMemberStatType, CPartyMemberStat>();

        foreach (SPartyMemberDefaultStat defaultStat in defaultStats)
        {
            CPartyMemberStat stat = new CPartyMemberStat(defaultStat.Value);
            defaultStatsBook[defaultStat.StatType] = stat;
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
        if (bodyPart == null)
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

            ApplyStatModifiers(traitEffect.StatModifiers);

            foreach (SPartyMemberTraitItem traitItem in traitEffect.GrantedItems)
            {
                m_ItemInventory.TryAddItemToInventory(new CInventoryItemRuntime(traitItem.InventoryItem), this);
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
            RemoveStatModifiers(traitEffect.StatModifiers);
        }

        m_PartyMemberTraits.Remove(trait);

        // remove player facing details
        m_TraitDetails.Remove(trait.TraitName);
    }

    public void ApplyStatModifiers(SPartyMemberStatModifier[] statMods)
    {
        // apply stat modifiers
        foreach (SPartyMemberStatModifier statMod in statMods)
        {
            m_PartyMemberStats[statMod.StatType].AddMod(statMod);
        }
    }

    public void RemoveStatModifiers(SPartyMemberStatModifier[] statMods)
    {
        foreach (SPartyMemberStatModifier statMod in statMods)
        {
            m_PartyMemberStats[statMod.StatType].RemoveMod(statMod);
        }
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
