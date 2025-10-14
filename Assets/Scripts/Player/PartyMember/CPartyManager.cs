using UnityEngine;

public class CPartyManager : MonoBehaviour
{
    [SerializeField, Tooltip("All body parts base stats")]
    private CBodyPart[] m_DefaultBodyParts;

    [SerializeField, Tooltip("All body part connections")]
    private SBodyPartAttachment[] m_DefaultBodyPartConnections;

    [SerializeField, Tooltip("All available traits")]
    private CPartyMemberTrait[] m_TraitPool;

    [SerializeField, Tooltip("All available personality traits")]
    private CPartyMemberPersonalityTrait[] m_PersonalityTraitPool;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
