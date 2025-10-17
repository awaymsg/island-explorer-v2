using System;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

public class CPartyManager : MonoBehaviour
{
    [SerializeField, Tooltip("Premade default party leaders by class")]
    private CPartyLeader[] m_DefaultPartyLeadersPool;

    [SerializeField, Tooltip("Premade default party members by class")]
    private CPartyMember[] m_DefaultPartyMembersPool;

    [SerializeField, Tooltip("Default stats for classes")]
    private SDefaultPartyMemberStats[] m_DefaultPartyMemberStats;

    [SerializeField, Tooltip("All body parts base stats")]
    private CBodyPart[] m_DefaultBodyParts;

    [SerializeField, Tooltip("All available traits")]
    private CPartyMemberTrait[] m_TraitPool;

    [SerializeField, Tooltip("All available personality traits")]
    private CPartyMemberPersonalityTrait[] m_PersonalityTraitPool;

    [SerializeField, Tooltip("Male names pool")]
    private string[] m_MaleNames;

    [SerializeField, Tooltip("Female names pool")]
    private string[] m_FemaleNames;

    [SerializeField, Tooltip("Surnames names pool")]
    private string[] m_Surnames;

    [SerializeField, Tooltip("Prefixes / honorifics pool")]
    private string[] m_Prefixes;

    [SerializeField, Tooltip("Suffixes pool")]
    private string[] m_Suffixes;

    [SerializeField, Tooltip("Male portraits pool")]
    private Sprite[] m_MalePortraits;

    [SerializeField, Tooltip("Female portraits pool")]
    private Sprite[] m_FemalePortraits;

    private CPartyPlayerCharacter m_PartyPlayerCharacter;

    private CCharacterListUI m_CharacterListUI;

    //-- getters
    public CPartyLeader[] DefaultPartyLeadersPool
    {
        get { return m_DefaultPartyLeadersPool; }
    }

    public CPartyMember[] DefaultPartyMembersPool
    {
        get { return m_DefaultPartyMembersPool; }
    }

    public CBodyPart[] DefaultBodyParts
    {
        get { return m_DefaultBodyParts; }
    }

    public CPartyMemberTrait[] TraitPool
    {
        get { return m_TraitPool; }
    }

    public CPartyMemberPersonalityTrait[] PersonalityTraitPool
    {
        get { return m_PersonalityTraitPool; }
    }

    public CPartyPlayerCharacter PartyPlayerCharacter
    {
        get { return m_PartyPlayerCharacter; }
    }

    public string[] MaleNamesPool
    {
        get { return m_MaleNames; }
    }

    public string[] FemaleNamesPool
    {
        get { return m_FemaleNames; }
    }

    public string[] Surnames
    {
        get { return m_Surnames; }
    }

    public string[] Prefixes
    { 
        get { return m_Prefixes; }
    }

    public string[] Suffixes
    {
        get { return m_Suffixes; }
    }

    public Sprite[] MalePortraitsPool
    {
        get { return m_MalePortraits; }
    }

    public Sprite[] FemalePortraitsPool
    {
        get { return m_FemalePortraits; }
    }
    //--

    public void Initialize()
    {
        m_CharacterListUI = FindFirstObjectByType<CCharacterListUI>();

        if (m_CharacterListUI == null)
        {
            Debug.Log("CPartyManager::Start - m_CharacterListUI is null!");
        }
    }

    public Dictionary<EPartyMemberStatType, float> GetDefaultPartyMemberStats(EPartyMemberType type)
    {
        SDefaultPartyMemberStats defaultStats = Array.Find<SDefaultPartyMemberStats>(m_DefaultPartyMemberStats, p => p.Class == type);
        if (defaultStats.Class == EPartyMemberType.Invalid)
        {
            return null;
        }

        Dictionary<EPartyMemberStatType, float> defaultStatsBook = new Dictionary<EPartyMemberStatType, float>();

        foreach (SPartyMemberStat defaultStat in defaultStats.BaseStats)
        {
            defaultStatsBook[defaultStat.StatType] = defaultStat.Value;
        }

        return defaultStatsBook;
    }

    public CPartyMemberRuntime CreatePartyMember(CPartyMember defaultMember)
    {
        CPartyMemberRuntime newPartyMember = new CPartyMemberRuntime(defaultMember, this);

        newPartyMember.InitializePartyMember();

        return newPartyMember;
    }
    
    public CPartyLeaderRuntime CreatePartyLeader(CPartyLeader defaultPartyLeader)
    {
        CPartyLeaderRuntime newPartyLeader = new CPartyLeaderRuntime(defaultPartyLeader, this);

        newPartyLeader.InitializePartyMember();

        return newPartyLeader;
    }

    public CPartyPlayerCharacter CreatePartyPlayerCharacter(CPartyPlayerCharacter defaultPlayerCharacter, CPartyLeaderRuntime partyLeader, List<CPartyMemberRuntime> partyMembers)
    {
        defaultPlayerCharacter.InitializePartyPlayerCharacter(partyLeader, partyMembers);
        m_PartyPlayerCharacter = defaultPlayerCharacter;

        if (m_CharacterListUI != null)
        {
            m_CharacterListUI.AddCharacterButton(partyLeader);

            foreach (CPartyMemberRuntime partyMember in partyMembers)
            {
                m_CharacterListUI.AddCharacterButton(partyMember);
            }
        }

        return defaultPlayerCharacter;
    }

    public void AddMemberToParty(CPartyMemberRuntime partyMember)
    {
        m_PartyPlayerCharacter.AddPartyMember(partyMember);

        if (m_CharacterListUI != null)
        {
            m_CharacterListUI.AddCharacterButton(partyMember);
        }
    }
}
