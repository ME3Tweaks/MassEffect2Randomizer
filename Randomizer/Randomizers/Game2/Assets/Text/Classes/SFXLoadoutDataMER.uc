Class SFXLoadoutDataMER extends SFXLoadoutData
    native
    config(Weapon);

// Variables
var config array<string> RandomWeaponOptions;
var array<Class<SFXWeapon>> OldReferences;

// Functions
function RandomizeWeapons()
{
    local array<string> newWeapons;
    local string weaponIFP;
    local int existingIndex;
    local int I;
    local Class<SFXWeapon> NewWeapon;
    
    if (RandomWeaponOptions.Length > Weapons.Length)
    {
        while (I < Weapons.Length)
        {
            weaponIFP = RandomWeaponOptions[Rand(RandomWeaponOptions.Length)];
            if (newWeapons.Find(weaponIFP) < 0)
            {
                NewWeapon = Class<SFXWeapon>(Class'SFXEngine'.static.LoadSeekFreeObject(weaponIFP, Class'Class'));
                if (NewWeapon == None)
                {
                    LogInternal("Dynamic loading new weapon failed! Weapon: " $ weaponIFP, );
                }
                else
                {
                    LogInternal("Set Weapon", );
                    OldReferences.AddItem(Weapons[I]);
                    Weapons[I] = NewWeapon;
                }
                I++;
            }
            continue;
        }
    }
}

//class default properties can be edited in the Properties tab for the class's Default__ object.
defaultproperties
{
    RandomWeaponOptions = ("SFXGameContent_Inventory.SFXWeapon_GethPulseRifle", "SFXGameContent_Inventory.SFXHeavyWeapon_MissileLauncher", "SFXGameContent_Inventory.SFXHeavyWeapon_GrenadeLauncher")
}