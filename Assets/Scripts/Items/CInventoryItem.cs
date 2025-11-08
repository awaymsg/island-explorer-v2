using System.Linq;
using System.ComponentModel;
using UnityEngine;
using static UnityEditor.Progress;

[CreateAssetMenu(fileName = "CInventoryItem", menuName = "Scriptable Objects/CItem")]
public class CInventoryItem : ScriptableObject
{
    [Tooltip("Item name (player-facing)")]
    public string m_ItemName;

    [Tooltip("Item description (player-facing)")]
    public string m_ItemDescription;

    public EItemType m_ItemType;

    public EItemCategory m_ItemCategory;

    [Tooltip("Stat modifiers on item use, equip, or consume")]
    public SPartyMemberStatModifier[] m_StatModifiers;

    public float m_ItemWeight;

    [ReadOnly(true)]
    public float m_TotalCost;

    public void CalculateCosts()
    {
        float totalCost = 0f;

        foreach (SPartyMemberStatModifier statMod in m_StatModifiers)
        {
            totalCost += statMod.Cost;
        }

        m_TotalCost = totalCost;
    }

    private void OnValidate()
    {
        CalculateCosts();
    }
}

public class CInventoryItemRuntime
{
    private readonly CInventoryItem m_InventoryItemSO;
    private string m_ItemName;
    private string m_ItemDescription;
    private EItemType m_ItemType;
    private EItemCategory m_ItemCategory;
    private EItemQuality m_ItemQuality;
    private SPartyMemberStatModifier[] m_StatModifiers;
    private float m_ItemWeight;

    // Getters
    public string ItemName
    {
        get { return m_ItemName; }
    }

    public string ItemDescription
    {
        get { return m_ItemDescription; }
    }

    public EItemType ItemType
    {
        get { return m_ItemType; }
    }

    public EItemCategory ItemCategory
    {
        get { return m_ItemCategory; }
    }

    public EItemQuality ItemQuality
    {
        get { return m_ItemQuality; }
    }

    public SPartyMemberStatModifier[] StatModifiers
    {
        get { return m_StatModifiers; }
    }

    public float ItemWeight
    {
        get { return m_ItemWeight; }
    }
    //--

    public CInventoryItemRuntime(CInventoryItem itemBase)
    {
        m_InventoryItemSO = itemBase;
        m_ItemName = itemBase.m_ItemName;
        m_ItemDescription = itemBase.m_ItemDescription;
        m_ItemType = itemBase.m_ItemType;
        m_ItemCategory = itemBase.m_ItemCategory;
        m_ItemWeight = itemBase.m_ItemWeight;

        // TODO: item quality

        m_StatModifiers = itemBase.m_StatModifiers?.Select(statMod => new SPartyMemberStatModifier(statMod)).ToArray();
    }
}
