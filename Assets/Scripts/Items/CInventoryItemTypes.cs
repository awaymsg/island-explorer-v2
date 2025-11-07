[System.Flags]
public enum EItemType
{
    Invalid = 0,
    Consumable = 1 << 0,
    Useable = 1 << 1,
    Equippable = 1 << 2,
    Held = 1 << 3
}

public enum EItemCategory
{
    Invalid,
    Food,
    Medicine,
    Wearable,
    Weapon,
    Occult
}

public enum EItemQuality
{
    Invalid,
    Broken,
    Shoddy,
    Normal,
    Craftsman,
    Artisan,
    Exemplary,
    Legendary
}
