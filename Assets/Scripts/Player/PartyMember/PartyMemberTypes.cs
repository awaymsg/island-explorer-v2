using JetBrains.Annotations;
using System;
using System.Collections.Generic;
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
    Morale
}

[Serializable]
public struct SPartyMemberStat
{
    public EPartyMemberStatType StatType;
    public float Value;
}

[Serializable]
public struct SDefaultPartyMemberStats
{
    public EPartyMemberType Class;
    public SPartyMemberStat[] BaseStats;
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

    public string ModificationContext;

    public float CalculateCost()
    {
        return Cost;
    }
}

[Serializable]
public struct SBodyPartStatModifier
{
    public EPartyMemberStatType Stat;
    [Tooltip("Overall multiplicative effect on the stat")]
    public float ModAmount;
}

[Serializable]
public class CBodyPart
{
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
    public EBodyPart[] Attached;
}

[Serializable]
public struct SPartyMemberTraitEffect
{
    public List<CBodyPartModification> BodyPartModifications;
    public SPartyMemberStatModifier[] StatModifiers;
}
