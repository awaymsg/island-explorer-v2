using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EPartyMemberType
{
    Invalid = 0,
    Warrior,
    Social,
    Academic,
    Scout,
    Occult
}

public enum EPartyMemberSkillType
{
    Invalid = 0,
    Action,
    Buff,
    Event
}

public enum EPartyMemberTraitType
{
    Invalid = 0,
    Warrior,
    Social,
    Scientific,
    Exploration,
    Occult
}

public enum EPartyMemberGender
{
    Invalid = 0,
    Male,
    Female,
    Neutral
}

[Serializable]
public struct SPartyMemberStatModifier
{
    // copy constructor
    public SPartyMemberStatModifier(SPartyMemberStatModifier other)
    {
        StatType = other.StatType;
        bMultiplicative = other.bMultiplicative;
        ModAmount = other.ModAmount;
        bIsPermanent = other.bIsPermanent;
        RemovalTimeInDays = other.RemovalTimeInDays;
        Cost = other.Cost;
    }

    public EPartyMemberStatType StatType;
    public bool bMultiplicative;
    public float ModAmount;
    public bool bIsPermanent;
    public float RemovalTimeInDays;

    [Tooltip("How valuable this modifier is")]
    public int Cost;
}

public enum EPartyMemberStatType
{
    Invalid = 0,
    Attack,
    Defense,
    Hunting,
    Stamina,
    Fortitude,
    Vision,
    Mobility,
    Science,
    History,
    Medicine,
    Occult,
    Magic,
    Social,
    Serenity,
    Attractiveness,
    Gayness
}

public enum EVitalFunctions
{
    Invalid = 0,
    Thinking,
    Breathing,
    BloodPumping,
    BloodFiltration,
    ImmuneSystem,
    Sensing,
    Nutrition
}

[Serializable]
public struct SBodyPartVitalEffect
{
    public SBodyPartVitalEffect(SBodyPartVitalEffect other)
    {
        VitalFunction = other.VitalFunction;
        MaxEffect = other.MaxEffect;
    }

    public EVitalFunctions VitalFunction;
    [Tooltip("Maximum contribution (percentage) to vital function when at full health")]
    public float MaxEffect;
}

[Serializable]
public struct SPartyMemberDefaultStat
{
    public EPartyMemberStatType StatType;
    public int Value;
}

public class CPartyMemberStat
{
    private float m_Value;
    private readonly List<SPartyMemberStatModifier> m_CurrentModifiers;

    public event Action<int, int> OnStatChanged; // oldValue, newValue

    public int Value
    {
        get { return (int)Math.Round(Math.Clamp(m_Value, 0, CGameManager.Instance.MaxStatValue), 0); }
    }

    public IReadOnlyList<SPartyMemberStatModifier> CurrentModifiers
    {
        get { return m_CurrentModifiers; }
    }

    public CPartyMemberStat(int value)
    {
        m_Value = value;
        m_CurrentModifiers = new List<SPartyMemberStatModifier>();
    }

    public void AddMod(SPartyMemberStatModifier modifier)
    {
        int oldValue = Value;

        if (modifier.bMultiplicative)
        {
            m_Value *= modifier.ModAmount;
        }
        else
        {
            // Don't go below 0
            m_Value += modifier.ModAmount;
        }

        m_CurrentModifiers.Add(modifier);
        OnStatChanged?.Invoke(oldValue, Value);
    }

    public void RemoveMod(SPartyMemberStatModifier modifier)
    {
        int oldValue = Value;

        if (modifier.bMultiplicative)
        {
            m_Value /= modifier.ModAmount;
        }
        else
        {
            m_Value -= modifier.ModAmount;
        }

        m_CurrentModifiers.Remove(modifier);
        OnStatChanged?.Invoke(oldValue, Value);
    }
}

public enum EBodyPart
{
    Invalid = 0,
    Soul,
    Mind,
    Spirit,
    LeftEye,
    RightEye,
    Brain,
    Nose,
    LeftEar,
    RightEar,
    Head,
    Neck,
    LeftArm,
    RightArm,
    LeftForearm,
    RightForearm,
    LeftHand,
    RightHand,
    Chest,
    Heart,
    LeftLung,
    RightLung,
    Abdomen,
    Stomach,
    LargeIntestines,
    SmallIntestines,
    LeftKidney,
    RightKidney,
    Liver,
    Pelvis,
    LeftThigh,
    RightThigh,
    LeftKnee,
    RightKnee,
    LeftShin,
    RightShin,
    LeftAnkle,
    RightAnkle,
    LeftFoot,
    RightFoot
}

public enum EBodyPartModification
{
    Invalid = 0,
    Damaged,
    Broken,
    Pierced,
    Laceration,
    Cut,
    Amputated,
    Destroyed,
    Lame,
    Enhanced,
    Replaced
}

[Serializable]
public class CBodyPartModification
{
    // copy constructor
    public CBodyPartModification(CBodyPartModification other)
    {
        if (other == null)
        {
            return;
        }

        ModName = other.ModName;
        ModLocation = other.ModLocation;
        InjuryType = other.InjuryType;
        bPermanent = other.bPermanent;
        HealTimeInDays = other.HealTimeInDays;
        bMultiplicative = other.bMultiplicative;
        ModAmount = other.ModAmount;
        Cost = other.Cost;
        ModificationContext = other.ModificationContext;
    }

    [Tooltip("Player-facing name")]
    public string ModName;
    public EBodyPart ModLocation;
    public EBodyPartModification InjuryType;
    [Tooltip("Is this modification permanent?")]
    public bool bPermanent;
    [Tooltip("Ignored if permanent injury")]
    public float HealTimeInDays;
    [Tooltip("Is this modification multiplicative?")]
    public bool bMultiplicative;
    public float ModAmount;

    [Tooltip("Overall cost of this modification")]
    public int Cost;

    [Tooltip("Player facing description of this modification")]
    public string ModificationContext;

    public float CalculateCost()
    {
        return Cost;
    }
}

[Serializable]
public struct SBodyPartStatModifier
{
    public SBodyPartStatModifier(SBodyPartStatModifier other)
    {
        Stat = other.Stat;
        ModAmount = other.ModAmount;
    }

    public EPartyMemberStatType Stat;
    [Tooltip("Overall multiplicative effect on the stat")]
    public float ModAmount;
}

[Serializable]
public struct SBodyPartAffectedStat
{
    public SBodyPartAffectedStat(SBodyPartAffectedStat other)
    {
        StatType = other.StatType;
        MaxEffect = other.MaxEffect;
    }

    public EPartyMemberStatType StatType;
    [Tooltip("Maximum effect (percentage) at max health")]
    public float MaxEffect;
}

[Serializable]
public class CBodyPart
{
    // copy constructor
    public CBodyPart(CBodyPart other)
    {
        if (other == null)
        {
            return;
        }

        BodyPart = other.BodyPart;
        MaxHealth = other.MaxHealth;
        Health = other.Health;
        bIsVital = other.bIsVital;
        
        Attached = other.Attached != null ? other.Attached?.ToArray() : null;
        AffectedStats = other.AffectedStats?.Select(mod => new SBodyPartAffectedStat(mod)).ToArray();
        Modifications = other.Modifications?.Select(mod => new CBodyPartModification(mod)).ToList();
        VitalEffects = other.VitalEffects?.Select(mod => new SBodyPartVitalEffect(mod)).ToArray();
    }

    [Header("Basic Info")]
    public EBodyPart BodyPart;
    public float MaxHealth = 100;
    public float Health = 100;
    [Tooltip("Party member stats this bodypart affects")]
    public SBodyPartAffectedStat[] AffectedStats;
    [Tooltip("Modifications to this bodypart")]
    public List<CBodyPartModification> Modifications;
    [Tooltip("If this is a vital body part, party member would automatically die if it was destroyed")]
    public bool bIsVital = false;
    [Tooltip("What vital functions does this contribute to?")]
    public SBodyPartVitalEffect[] VitalEffects;
    [Header("Attachment")]
    public EBodyPart[] Attached;
}

[Serializable]
public struct SPartyMemberTraitEffect
{
    public List<CBodyPartModification> BodyPartModifications;
    public SPartyMemberStatModifier[] StatModifiers;
    public List<CInventoryItem> GrantedItems;
    [Tooltip("Additive")]
    public SPartyMemberAttitudeModifier SelfAttitudeModifier;
    [Tooltip("Additive")]
    public SPartyMemberAttitudeModifier AttitudeModifier;
}

[Serializable]
public struct SPartyMemberAttitudeModifier
{
    public float Value;
    public int Cost;
}

public enum EMoodletType
{
    Invalid = 0,
    Happiness,
    Angst,
    Sanity
}

[Serializable]
public struct SPartyMemberMoodlet
{
    public EMoodletType MoodletType;
    public float Duration;
    public float AdditiveHappinessAmount;
    public string Name;
    public string Description;
}
