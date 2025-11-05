using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CInventory
{
    private Dictionary<string, CInventoryItemRuntime> m_ItemsBook = new Dictionary<string, CInventoryItemRuntime>();
    private Dictionary<string, int> m_ItemStacks = new Dictionary<string, int>();
    private List<CInventoryItemRuntime> m_SortedItems = new List<CInventoryItemRuntime>();
    private bool m_bIsSortedListDirty = false;
    private float m_MaxWeight = 0f;
    private float m_CurrentWeight = 0f;

    // Getters
    public float CurrentWeight => m_CurrentWeight;

    public float MaxWeight {
        get { return m_MaxWeight; }
        set { m_MaxWeight = value; }
    }
    //--

    public bool CanAddItemToInventory(CInventoryItemRuntime item)
    {
        if (item == null)
        {
            Debug.Log("CanAddItemToInventory - item is null!");

            return false;
        }

        return m_CurrentWeight + item.ItemWeight <= m_MaxWeight;
    }

    public bool TryAddItemToInventory(CInventoryItemRuntime item, CPartyMemberRuntime owner)
    {
        if (item == null)
        {
            Debug.Log("TryAddItemToInventory - item is null!");
            return false;
        }

        if (!CanAddItemToInventory(item))
        {
            Debug.Log("TryAddItemToInventory - Could not add item to inventory, item exceeds max carrying weight!");
            return false;
        }

        if (m_ItemStacks.ContainsKey(item.ItemName))
        {
            m_ItemStacks[item.ItemName]++;
        }
        else
        {
            m_ItemsBook.Add(item.ItemName, item);
            m_ItemStacks.Add(item.ItemName, 1);
            m_SortedItems.Add(item);
        }

        m_CurrentWeight += item.ItemWeight;

        if ((item.ItemType & EItemType.Held) != 0)
        {
            if (owner == null)
            {
                Debug.Log("TryAddItemToInventory - owner is null!");
            }

            owner.ApplyStatModifiers(item.StatModifiers);
        }

        m_bIsSortedListDirty = true;

        return true;
    }

    public bool TryRemoveItemFromInventory(CInventoryItemRuntime item, CPartyMemberRuntime owner)
    {
        if (item == null)
        {
            Debug.Log("TryRemoveItemFromInventory - item is null!");
            return false;
        }

        if (!m_ItemStacks.ContainsKey(item.ItemName))
        {
            Debug.Log("TryRemoveItemFromInventory - item is not in dictionary!");
            return false;
        }
        
        m_ItemStacks[item.ItemName]--;
        m_CurrentWeight -= item.ItemWeight;

        if (m_ItemStacks[item.ItemName] == 0)
        {
            m_ItemStacks.Remove(item.ItemName);
            m_ItemsBook.Remove(item.ItemName);
            m_SortedItems.Remove(item);
        }

        if ((item.ItemType & EItemType.Held) != 0)
        {
            if (owner == null)
            {
                Debug.Log("TryRemoveItemToInventory - owner is null!");
            }

            owner.RemoveStatModifiers(item.StatModifiers);
        }

        m_bIsSortedListDirty = true;

        return true;
    }

    public bool TryUseItem(string itemName, CPartyMemberRuntime owner, CPartyMemberRuntime target)
    {
        if (!m_ItemsBook.ContainsKey(itemName))
        {
            Debug.Log("TryUseItem - tried to use item that is not in dictionary!");

            return false;
        }

        CInventoryItemRuntime item = m_ItemsBook[itemName];
     
        if (item == null)
        {
            Debug.Log("TryUseItem - item is null!");
            return false;
        }

        if (target == null)
        {
            Debug.Log("TryUseItem - target is null!");
            return false;
        }

        target.ApplyStatModifiers(item.StatModifiers);

        if ((item.ItemType & EItemType.Consumable) != 0)
        {
            TryRemoveItemFromInventory(item, owner);
        }

        return true;
    }

    private List<CInventoryItemRuntime> GetSortedItemsList()
    {
        if (m_bIsSortedListDirty)
        {
            m_SortedItems = m_ItemsBook.Values
                .OrderBy(item => item.ItemCategory)
                .ThenBy(item => item.ItemQuality)
                .ThenBy(item => item.ItemName)
                .ToList();
            m_bIsSortedListDirty = false;
        }

        return m_SortedItems;
    }

    private int GetItemCount(string itemName)
    {
        return m_ItemStacks.ContainsKey(itemName) ? m_ItemStacks[itemName] : 0;
    }

    public Dictionary<CInventoryItemRuntime, int> GetItemsWithCounts()
    {
        return GetSortedItemsList().ToDictionary(item => item, item => GetItemCount(item.ItemName));
    }
}
