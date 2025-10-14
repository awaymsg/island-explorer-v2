using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum EPartyMemberType
{
    Warrior,
    Scout,
    Hunter
}

[Serializable]
public enum EPartyMemberSkillType
{
    Invalid,
    None,
    Buff,
    Event
}

[Serializable]
public enum EPartyMemberTraitType
{
    Invalid,
    None,
    Warrior,
    Social,
    Scientific,
    Exploration
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
    Invalid,
    None,
    Attack,
    Defense,
    Food,
    Nutrition,
    Science,
    Social,
    History,
    Occult
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
    Invalid,
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
public struct SBodyPartAttachment
{
    public EBodyPart AttachedTo;
    public EBodyPart[] Attached;
}

[Serializable]
public enum EBodyPartModification
{
    Invalid,
    Broken,
    Pierced,
    Laceration,
    Cut,
    Amputated,
    Destroyed,
    Lame,
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
    public EBodyPart BodyPart;
    public float MaxHealth = 100;
    public float Health = 100;
    public SBodyPartStatModifier[] StatModifiers;
    public List<CBodyPartModification> Modifications;
    [Tooltip("If this is a vital body part, party member would die if it was destroyed")]
    public bool bIsVital = false;
}

[Serializable]
public struct SPartyMemberTraitEffect
{
    public List<CBodyPartModification> BodyPartModifications;
    public SPartyMemberStatModifier[] StatModifiers;
}
