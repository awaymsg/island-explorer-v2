using System;
using System.Collections.Generic;
using UnityEngine;

public class CPartyManager : MonoBehaviour
{
    [SerializeField, Tooltip("Premade default party members")]
    private CPartyMember[] m_DefaultPartyMembersPool;

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

    private static CPartyManager m_Instance;

    //-- Events
    public event Action<CPartyMemberRuntime> m_OnCharacterAdded;
    public event Action<CPartyMemberRuntime> m_OnCharacterRemoved;
    //--

    //-- getters
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

    public static CPartyManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = FindFirstObjectByType<CPartyManager>();
            }

            return m_Instance;
        }
    }
    //--

    public CPartyMemberRuntime CreatePartyMember(CPartyMember defaultMember)
    {
        CPartyMemberRuntime newPartyMember = new CPartyMemberRuntime(defaultMember, this);

        newPartyMember.InitializePartyMember();

        return newPartyMember;
    }

    public CPartyPlayerCharacter CreatePartyPlayerCharacter(CPartyPlayerCharacter defaultPlayerCharacter, Queue<CPartyMemberRuntime> partyMembers)
    {
        CPartyMemberRuntime partyLeader = partyMembers.Dequeue();

        defaultPlayerCharacter.InitializePartyPlayerCharacter(partyLeader, partyMembers);
        m_PartyPlayerCharacter = defaultPlayerCharacter;

        m_OnCharacterAdded?.Invoke(partyLeader);
        
        foreach (CPartyMemberRuntime partyMember in partyMembers)
        {
            m_OnCharacterAdded?.Invoke(partyMember);
        }

        return defaultPlayerCharacter;
    }

    public void AddMemberToParty(CPartyMemberRuntime partyMember)
    {
        m_PartyPlayerCharacter.AddPartyMember(partyMember);

        m_OnCharacterAdded?.Invoke(partyMember);
    }

    public void OnDestroy()
    {
        m_OnCharacterAdded = null;
        m_OnCharacterRemoved = null;
    }
}
