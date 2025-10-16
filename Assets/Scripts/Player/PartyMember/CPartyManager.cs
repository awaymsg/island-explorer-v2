using System;
using System.Collections.Generic;
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

    private CPartyPlayerCharacter m_PartyPlayerCharacter;

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
    //--

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

    public CPartyMember CreatePartyMember(CPartyMember defaultMember)
    {
        defaultMember.InitializePartyMember();

        return defaultMember;
    }
    
    public CPartyLeader CreatePartyLeader(CPartyLeader defaultPartyLeader)
    {
        defaultPartyLeader.InitializePartyMember();

        return defaultPartyLeader;
    }

    public CPartyPlayerCharacter CreatePartyPlayerCharacter(CPartyPlayerCharacter defaultPlayerCharacter, CPartyLeader partyLeader, List<CPartyMember> partyMembers)
    {
        defaultPlayerCharacter.InitializePartyPlayerCharacter(partyLeader, partyMembers);
        m_PartyPlayerCharacter = defaultPlayerCharacter;

        return defaultPlayerCharacter;
    }
}
