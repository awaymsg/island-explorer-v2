using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public enum EPartyMemberType
{
    Invalid = 0,
    Warrior,
    Scout,
    Hunter
}

[Serializable]
public enum EPartyMemberSkillType
{
    Invalid = 0,
    None,
    Buff,
    Event
}

[Serializable]
public enum EPartyMemberTraitType
{
    Invalid = 0,
    None,
    Warrior,
    Social,
    Scientific,
    Exploration
}

[Serializable]
public enum EPartyMemberGender
{
    Invalid = 0,
    Male,
    Female
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
        Cost = other.Cost;
    }

    public EPartyMemberStatType StatType;
    public bool bMultiplicative;
    public float ModAmount;

    [Tooltip("How valuable this modifier is")]
    public float Cost;
}

[Serializable]
public enum EPartyMemberStatType
{
    Invalid = 0,
    None,
    Attack,
    Defense,
    Hunting,
    Nutrition,
    Science,
    Social,
    History,
    Occult,
    Serenity,
    Morale,
    Attractiveness,
    Gayness
}

[Serializable]
public struct SPartyMemberStat
{
    public EPartyMemberStatType StatType;
    public float Value;
}

[Serializable]
public enum EBodyPart
{
    Invalid = 0,
    None,
    Soul,
    Mind,
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

[Serializable]
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
    public float Cost;

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
        AttachedTo = other.AttachedTo;

        StatModifiers = other.StatModifiers?.Select(mod => new SBodyPartStatModifier(mod)).ToArray();
        Modifications = other.Modifications?.Select(mod => new CBodyPartModification(mod)).ToList();
    }

    [Header("Basic Info")]
    public EBodyPart BodyPart;
    public float MaxHealth = 100;
    public float Health = 100;
    [Tooltip("Party member stats this bodypart affects")]
    public SBodyPartStatModifier[] StatModifiers;
    [Tooltip("Modifications to this bodypart")]
    public List<CBodyPartModification> Modifications;
    [Tooltip("If this is a vital body part, party member would die if it was destroyed")]
    public bool bIsVital = false;
    [Header("Attachment")]
    public EBodyPart AttachedTo;
}

[Serializable]
public struct SPartyMemberTraitEffect
{
    public List<CBodyPartModification> BodyPartModifications;
    public SPartyMemberStatModifier[] StatModifiers;
}
