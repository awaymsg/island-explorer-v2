using System;
using System.Collections.Generic;
using UnityEngine;

public class CPartyManager : MonoBehaviour
{
    private static CPartyManager m_Instance;

    [SerializeField, Tooltip("Premade default party members")]
    private CPartyMember[] m_DefaultPartyMembersPool;

    [SerializeField, Tooltip("All body parts base stats")]
    private CBodyPart[] m_DefaultBodyParts;

    [SerializeField, Tooltip("All available traits")]
    private CPartyMemberTrait[] m_TraitPool;

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

    private static HashSet<string> m_ExistingNames = new HashSet<string>();

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

    public static HashSet<string> ExistingNames
    {
        get { return m_ExistingNames; }
    }
    //--

    public CPartyMemberRuntime CreatePartyMember(CPartyMember defaultMember)
    {
        if (defaultMember == null)
        {
            Debug.Log("CreatePartyMember - defaultMember is null!");
            return null;
        }

        CPartyMemberRuntime newPartyMember = new CPartyMemberRuntime(defaultMember, this);

        newPartyMember.InitializePartyMember();

        return newPartyMember;
    }

    // TODO: rework this part of the code so it's not separate from AddMemberToParty
    public CPartyPlayerCharacter CreatePartyPlayerCharacter(CPartyPlayerCharacter defaultPlayerCharacter, Queue<CPartyMemberRuntime> partyMembers)
    {
        if (defaultPlayerCharacter == null)
        {
            Debug.Log("CreatePartyPlayerCharacter - defaultPlayerCharacter is null!");
            return null;
        }

        if (partyMembers == null || partyMembers.Count == 0)
        {
            Debug.Log("CreatePartyPlayerCharacter - partyMembers is null or count is 0!");
            return null;
        }

        CPartyMemberRuntime partyLeader = partyMembers.Dequeue();
        if (partyLeader == null)
        {
            Debug.Log("CreatePartyPlayerCharacter - partyLeader is null!");
        }

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
        if (partyMember == null)
        {
            Debug.Log("AddMemberToParty - partyMember is null!");
            return;
        }

        m_PartyPlayerCharacter.AddPartyMember(partyMember);

        m_OnCharacterAdded?.Invoke(partyMember);
    }

    public void RemoveMemberFromParty(CPartyMemberRuntime partyMember)
    {
        if (partyMember == null)
        {
            Debug.Log("RemoveMemberFromParty - partyMember is null!");
            return;
        }

        m_PartyPlayerCharacter.RemovePartyMember(partyMember);
        m_ExistingNames.Remove(partyMember.CharacterName);

        m_OnCharacterRemoved?.Invoke(partyMember);
    }

    public void OnDestroy()
    {
        m_OnCharacterAdded = null;
        m_OnCharacterRemoved = null;
    }
}
