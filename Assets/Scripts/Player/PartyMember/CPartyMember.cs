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
    protected readonly CPartyMember m_PartyMemberSO;
    protected CPartyManager m_PartyManager;

    // Name of the character
    protected string m_CharacterName;

    // Portrait for UI
    private Sprite m_PartyMemberPortrait;

    protected List<CPartyMemberTraitRuntime> m_PartyMemberTraits = new List<CPartyMemberTraitRuntime>();
    protected CBodyPart[] m_BodyParts;

    protected Dictionary<EPartyMemberStatType, CPartyMemberStat> m_PartyMemberStats;
    protected CInventory m_ItemInventory;

    // Player-facing and also used for internal data / counts
    private Dictionary<EBodyPart, List<string>> m_BodyPartConditions = new Dictionary<EBodyPart, List<string>>();
    private Dictionary<string, string> m_TraitDetails = new Dictionary<string, string>();
    private Dictionary<string, float> m_AttitudesBook = new Dictionary<string, float>();
    private List<SPartyMemberMoodlet> m_Moodlets = new List<SPartyMemberMoodlet>();
    // TODO: Memories
    private Dictionary<string, string[]> m_BadMemories = new Dictionary<string, string[]>();
    private Dictionary<string, string[]> m_GoodMemories = new Dictionary<string, string[]>();

    protected UInt16 m_SkillLevel = 0;

    // Important non-stat values
    private float m_DefaultAttitude = 0f;
    private float m_DefaultSelfAttitude = 0f;
    private float m_HungerRateScalar = 1f;
    private float m_Hunger = 0f;
    private float m_Happiness = 0f;
    private float m_Angst = 0f;
    private float m_Sanity = 0f;
    private float m_DaysOnIsland = 0f;

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

    public float Hunger
    {
        get { return m_Hunger; }
    }

    public float Happiness
    {
        get { return m_Happiness; }
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

        // Set inventory max weight and add callback for when Fortitude changes
        if (m_PartyMemberStats.ContainsKey(EPartyMemberStatType.Fortitude))
        {
            m_PartyMemberStats[EPartyMemberStatType.Fortitude].OnStatChanged += UpdateInventoryMaxWeight;
            UpdateInventoryMaxWeight(0, m_PartyMemberStats[EPartyMemberStatType.Fortitude].Value);
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
        CGameManager.Instance.m_OnTick += OnTick;

        CalculateCost();
    }

    public void Dispose()
    {
        if (CPartyManager.Instance != null)
        {
            CPartyManager.Instance.m_OnCharacterAdded -= AddPartyMemberAttitude;
            CPartyManager.Instance.m_OnCharacterRemoved -= RemovePartyMemberAttitude;
        }

        if (CGameManager.Instance != null)
        {
            CGameManager.Instance.m_OnTick -= OnTick;
        }

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
            return;
        }

        CPartyManager.ExistingNames.Add(name);

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
        CalculateHappiness();
    }

    private void RemovePartyMemberAttitude(CPartyMemberRuntime partyMember)
    {
        if (partyMember == null)
        {
            Debug.Log("RemovePartyMemberAttitude - partyMember is null!");
            return;
        }

        m_AttitudesBook.Remove(partyMember.CharacterName);
        CalculateHappiness();
    }

    private void UpdateInventoryMaxWeight(int oldValue, int newValue)
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
            int randomVariation = UnityEngine.Random.Range(CGameManager.Instance.StatRandomizationRange.x, CGameManager.Instance.StatRandomizationRange.y + 1);
            CPartyMemberStat stat = new CPartyMemberStat(defaultStat.Value + randomVariation);

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
        if (bodyMod == null || bodyMod.bPermanent)
        {
            Debug.Log("RemoveBodyMod - tried to remove null or permanent bodymod!");
            return;
        }

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

        foreach (CBodyPartModification bodyMod in runtimeTrait.TraitEffect.BodyPartModifications)
        {
            AddBodyMod(bodyMod);
        }

        ApplyStatModifiers(runtimeTrait.TraitEffect.StatModifiers);

        foreach (CInventoryItem traitItem in runtimeTrait.TraitEffect.GrantedItems)
        {
            m_ItemInventory.TryAddItemToInventory(new CInventoryItemRuntime(traitItem), this);
        }

        m_DefaultAttitude += runtimeTrait.TraitEffect.AttitudeModifier.Value;
        m_DefaultSelfAttitude += runtimeTrait.TraitEffect.SelfAttitudeModifier.Value;

        m_PartyMemberTraits.Add(runtimeTrait);

        // add player facing details
        if (!runtimeTrait.bIsHidden)
        {
            m_TraitDetails.Add(trait.m_TraitName, trait.m_TraitDescription);
        }
    }

    public void RemoveTrait(CPartyMemberTraitRuntime trait)
    {
        // revert stat modifiers
        RemoveStatModifiers(trait.TraitEffect.StatModifiers);

        // revert non-permanent bodypart modifications
        foreach (CBodyPartModification bodyMod in trait.TraitEffect.BodyPartModifications)
        {
            if (!bodyMod.bPermanent)
            {
                RemoveBodyMod(bodyMod);
            }
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

        CalculateHappiness();
        CalculateHungerRateScalar();
    }

    public void RemoveStatModifiers(SPartyMemberStatModifier[] statMods)
    {
        foreach (SPartyMemberStatModifier statMod in statMods)
        {
            m_PartyMemberStats[statMod.StatType].RemoveMod(statMod);
        }

        CalculateHappiness();
        CalculateHungerRateScalar();
    }

    // TEMP: happiness and hunger rate calculations should be reworked
    private void CalculateHappiness()
    {
        // For now, relationshipSatisfaction is simply the average of all attitudes toward other party members
        float relationshipSatisfaction = 0f;
        
        foreach (var attitude in m_AttitudesBook)
        {
            relationshipSatisfaction += attitude.Value;
        }

        relationshipSatisfaction /= m_AttitudesBook.Count == 0 ? 1f : m_AttitudesBook.Count;

        // Happiness is max at 100, affected by moodlets, reduced by hunger, but helped by relationship satisfaction and serenity stat
        float maxStatValue = CGameManager.Instance.MaxStatValue;
        m_Happiness = maxStatValue * 0.5f;

        foreach (SPartyMemberMoodlet moodlet in m_Moodlets)
        {
            if (moodlet.MoodletType == EMoodletType.Happiness)
            {
                m_Happiness += moodlet.AdditiveHappinessAmount;
            }
        }

        m_Happiness *= 1f - m_Hunger / maxStatValue;
        m_Happiness *= 1f + relationshipSatisfaction / maxStatValue;

        if (m_PartyMemberStats.ContainsKey(EPartyMemberStatType.Serenity))
        {
            m_Happiness *= 1f + m_PartyMemberStats[EPartyMemberStatType.Serenity].Value / maxStatValue;
        }

        // Selfworth counts twice as much
        if (m_AttitudesBook.ContainsKey(m_CharacterName))
        {
            m_Happiness *= 1f + (m_AttitudesBook[m_CharacterName] / maxStatValue) * 2f;
        }

        // Spending too much time on the island reduces happiness (should introduce sanity later)
        m_Happiness *= Math.Max(1f - m_DaysOnIsland / CGameManager.Instance.DaysOnIslandMaxValue, 0.5f);

        m_Happiness = Math.Min(m_Happiness, maxStatValue);
    }

    private void CalculateHungerRateScalar()
    {
        m_HungerRateScalar = 1f * (1f - m_PartyMemberStats[EPartyMemberStatType.Stamina].Value / CGameManager.Instance.MaxStatValue);
        m_HungerRateScalar *= 1f - m_Happiness / CGameManager.Instance.MaxStatValue;
    }

    private void OnTick()
    {
        m_DaysOnIsland += 1f /CGameManager.Instance.StepsInADay;

        IncrementHunger();
    }

    private void IncrementHunger()
    {
        m_Hunger += CGameManager.Instance.HungerRatePerTick * m_HungerRateScalar;
        CalculateHappiness();
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

        m_TotalCost = totalCost;
        return totalCost;
    }
}
