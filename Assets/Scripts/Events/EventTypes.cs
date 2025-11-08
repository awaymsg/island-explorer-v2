using System;
using UnityEngine;

[Serializable]
public struct SEventOption
{
    [Tooltip("Name to show on UI button")]
    public string OptionName;
    [Tooltip("Stats to roll")]
    public SEventRollStats[] RollStats;
    [Tooltip("Trait requirements")]
    public CPartyMemberTrait[] Traits;
    [Tooltip("Requires all traits?")]
    public bool bRequireAllTraits;
    [Tooltip("Modifiers on success")]
    public SEventModifiers ModifiersOnSuccess;
    [Tooltip("Modifiers on failure")]
    public SEventModifiers ModifiersOnFailure;
    [Tooltip("Trigger another event on success")]
    public CLocalEvent SecondaryEventOnSuccess;
    [Tooltip("Trigger another event on failure")]
    public CLocalEvent SecondaryEventOnFailure;
}

[Serializable]
public struct SEventRollStats
{
    [Tooltip("Stat that this event rolls against")]
    public EPartyMemberStatType RollStat;
    [Tooltip("Value to roll against")]
    public float Value;
}

[Serializable]
public struct SEventModifiers
{
    public SPartyMemberStatModifier[] StatModifiers;
    public CBodyPartModification[] BodyPartModifications;
    public SEventModifierTarget ModifierTarget;
    public float AttitudeModifier;
}

public enum SEventModifierTarget
{
    Invalid = 0,
    Self,
    Target,
    AllExceptSelf,
    AllExceptTarget
}

public enum EEventEntityType
{
    Invalid = 0,
    Plant,
    Animal,
    Human,
    Artifact,
    Paranormal
}

public abstract class CEventEntity
{
    public abstract void Initialize();
}

public class CEventEntityPlant : CEventEntity
{
    public override void Initialize()
    {

    }
}

public class CEventEntityAnimal : CEventEntity
{
    public override void Initialize()
    {
        
    }
}

public class CEventEntityHuman : CEventEntity
{
    public override void Initialize()
    {

    }
}

public class CEventEntityArtifact : CEventEntity
{
    public override void Initialize()
    {

    }
}

public class CEventEntityParanormal : CEventEntity
{
    public override void Initialize()
    {

    }
}
