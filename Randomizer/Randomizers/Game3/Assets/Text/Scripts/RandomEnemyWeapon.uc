public function bool CreateWeapon(Class<SFXWeapon> WeaponClass, optional bool bEquipWeapon = FALSE)
{
    local int GroupIdx;
    local int EntryIdx;
    local SFXWeapon Wpn;
    local int randNum;
    local string weaponAsset;
    local ScaledFloat OldDmg;
    local int weaponPoolSize;
    local bool keepDamageAsIs;

    if (SFXPawn_PlayerParty(Self) != None)
    {
        Class'SFXPlayerSquadLoadoutData'.static.GetWeaponCategory(WeaponClass, GroupIdx, EntryIdx);
        ReplaceWeapon(byte(GroupIdx), WeaponClass, bEquipWeapon);
    }
    else
    {
        weaponPoolSize = 71;
        if (SFXPawn(self) != None && !SFXPawn(self).bSupportsVisibleWeapons){
            weaponPoolSize = %FULLWEAPONPOOLSIZE%;
        }
        randNum = Rand(weaponPoolSize);
        switch (randNum)
        {
            case 0:
                weaponAsset = "SFXGameContent.SFXWeapon_Heavy_GrenadeLauncher";
                break;
            case 1:
                weaponAsset = "SFXGameContent.SFXWeapon_Heavy_MissileLauncher";
                break;
            case 2:
                weaponAsset = "SFXGameContent.SFXWeapon_Heavy_RocketLauncher";
                break;
            case 3:
                weaponAsset = "SFXGameContent.SFXWeapon_Heavy_TitanMissileLauncher";
                break;
            case 4:
                weaponAsset = "SFXGameContent.SFXWeapon_Heavy_Cain";
                break;
            case 5:
                weaponAsset = "SFXGameContent.SFXWeapon_Heavy_ParticleBeam";
                break;
            case 6:
                weaponAsset = "SFXGameContent.SFXWeapon_Heavy_Avalanche";
                break;
            case 7:
                weaponAsset = "SFXGameContent.SFXWeapon_Heavy_Flamethrower_Player";
                break;
            case 8:
                weaponAsset = "SFXGameContent.SFXWeapon_Heavy_MiniGun";
                break;
            case 9:
                weaponAsset = "SFXGameContent.SFXWeapon_Heavy_Blackstar";
                break;
            case 10:
                weaponAsset = "SFXGameContent.SFXWeapon_Heavy_ArcProjector";
                break;
            case 11:
                weaponAsset = "SFXGameContent.SFXWeapon_AssaultRifle_Avenger";
                break;
            case 12:
                weaponAsset = "SFXGameContent.SFXWeapon_AssaultRifle_Revenant";
                break;
            case 13:
                weaponAsset = "SFXGameContent.SFXWeapon_AssaultRifle_Collector";
                break;
            case 14:
                weaponAsset = "SFXGameContent.SFXWeapon_AssaultRifle_Geth";
                break;
            case 15:
                weaponAsset = "SFXGameContent.SFXWeapon_AssaultRifle_Vindicator";
                break;
            case 16:
                weaponAsset = "SFXGameContent.SFXWeapon_AssaultRifle_Mattock";
                break;
            case 17:
                weaponAsset = "SFXGameContent.SFXWeapon_AssaultRifle_Cobra";
                break;
            case 18:
                weaponAsset = "SFXGameContent.SFXWeapon_AssaultRifle_Falcon";
                break;
            case 19:
                weaponAsset = "SFXGameContent.SFXWeapon_AssaultRifle_Saber";
                break;
            case 20:
                weaponAsset = "SFXGameContent.SFXWeapon_AssaultRifle_Argus";
                break;
            case 21:
                weaponAsset = "SFXGameContent.SFXWeapon_AssaultRifle_Valkyrie";
                break;
            case 21:
                weaponAsset = "SFXGameContent.SFXWeapon_AssaultRifle_Reckoning";
                break;
            case 22:
                weaponAsset = "SFXGameContent.SFXWeapon_SMG_Shuriken";
                break;
            case 23:
                weaponAsset = "SFXGameContent.SFXWeapon_SMG_Tempest";
                break;
            case 24:
                weaponAsset = "SFXGameContent.SFXWeapon_SMG_Locust";
                break;
            case 25:
                weaponAsset = "SFXGameContent.SFXWeapon_SMG_Hornet";
                break;
            case 26:
                weaponAsset = "SFXGameContent.SFXWeapon_SMG_Hurricane";
                break;
            case 27:
                weaponAsset = "SFXGameContent.SFXWeapon_Pistol_Predator";
                break;
            case 28:
                weaponAsset = "SFXGameContent.SFXWeapon_Pistol_Carnifex";
                break;
            case 29:
                weaponAsset = "SFXGameContent.SFXWeapon_Pistol_EndGameCarnifex";
                break;
            case 30:
                weaponAsset = "SFXGameContent.SFXWeapon_Pistol_Phalanx";
                break;
            case 31:
                weaponAsset = "SFXGameContent.SFXWeapon_Pistol_Talon";
                break;
            case 32:
                weaponAsset = "SFXGameContent.SFXWeapon_Pistol_Thor";
                break;
            case 33:
                weaponAsset = "SFXGameContent.SFXWeapon_Pistol_Scorpion";
                break;
            case 34:
                weaponAsset = "SFXGameContent.SFXWeapon_Pistol_Ivory";
                break;
            case 35:
                weaponAsset = "SFXGameContent.SFXWeapon_Pistol_Eagle";
                break;
            case 36:
                weaponAsset = "SFXGameContent.SFXWeapon_Shotgun_Katana";
                break;
            case 37:
                weaponAsset = "SFXGameContent.SFXWeapon_Shotgun_Scimitar";
                break;
            case 38:
                weaponAsset = "SFXGameContent.SFXWeapon_Shotgun_Claymore";
                break;
            case 39:
                weaponAsset = "SFXGameContent.SFXWeapon_Shotgun_Eviscerator";
                break;
            case 40:
                weaponAsset = "SFXGameContent.SFXWeapon_Shotgun_Geth";
                break;
            case 41:
                weaponAsset = "SFXGameContent.SFXWeapon_Shotgun_Graal";
                break;
            case 42:
                weaponAsset = "SFXGameContent.SFXWeapon_Shotgun_Disciple";
                break;
            case 43:
                weaponAsset = "SFXGameContent.SFXWeapon_Shotgun_Striker";
                break;
            case 44:
                weaponAsset = "SFXGameContent.SFXWeapon_Shotgun_Crusader";
                break;
            case 45:
                weaponAsset = "SFXGameContent.SFXWeapon_Shotgun_Raider";
                break;
            case 46:
                weaponAsset = "SFXGameContent.SFXWeapon_SniperRifle_Mantis";
                break;
            case 47:
                weaponAsset = "SFXGameContent.SFXWeapon_SniperRifle_Viper";
                break;
            case 48:
                weaponAsset = "SFXGameContent.SFXWeapon_SniperRifle_Widow";
                break;
            case 49:
                weaponAsset = "SFXGameContent.SFXWeapon_SniperRifle_Incisor";
                break;
            case 50:
                weaponAsset = "SFXGameContent.SFXWeapon_SniperRifle_Raptor";
                break;
            case 51:
                weaponAsset = "SFXGameContent.SFXWeapon_SniperRifle_Javelin";
                break;
            case 52:
                weaponAsset = "SFXGameContent.SFXWeapon_SniperRifle_BlackWidow";
                break;
            case 53:
                weaponAsset = "SFXGameContent.SFXWeapon_SniperRifle_Indra";
                break;
            case 54:
                weaponAsset = "SFXGameContent.SFXWeapon_SniperRifle_Valiant";
                break;
            case 55:
                weaponAsset = "SFXGameContentDLC_HEN_PR.SFXWeapon_AssaultRifle_Prothean";
                break;
            case 56:
                weaponAsset = "SFXGameContentDLC_EXP_Pack003.SFXWeapon_Pistol_Silencer";
                break;
            case 57:
                weaponAsset = "SFXGameContentDLC_EXP_Pack003.SFXWeapon_AssaultRifle_Lancer";
                break;
            case 58:
                weaponAsset = "SFXGameContentDLC_EXP_Pack003.SFXWeapon_Heavy_Spitfire_Cit001";
                break;
            case 59:
                weaponAsset = "SFXGameContentDLC_CON_GUN02.SFXWeapon_Pistol_Bloodpack";
                break;
            case 60:
                weaponAsset = "SFXGameContentDLC_CON_GUN02.SFXWeapon_Shotgun_Salarian";
                break;
            case 61:
                weaponAsset = "SFXGameContentDLC_CON_GUN02.SFXWeapon_Sniperrifle_Batarian_GUN02";
                break;
            case 62:
                weaponAsset = "SFXGameContentDLC_CON_GUN02.SFXWeapon_Shotgun_Assault_GUN02";
                break;
            case 63:
                weaponAsset = "SFXGameContentDLC_CON_GUN02.SFXWeapon_Pistol_Asari_GUN02";
                break;
            case 64:
                weaponAsset = "SFXGameContentDLC_CON_GUN02.SFXWeapon_AssaultRifle_LMG_GUN02";
                break;
            case 65:
                weaponAsset = "SFXGameContentDLC_CON_GUN02.SFXWeapon_AssaultRifle_Krogan_GUN02";
                break;
            case 66:
                weaponAsset = "SFXGameContentDLC_CON_GUN01.SFXWeapon_AssaultRifle_Cerb_GUN01";
                break;
            case 67:
                weaponAsset = "SFXGameContentDLC_CON_GUN01.SFXWeapon_Shotgun_Quarian_GUN01";
                break;
            case 68:
                weaponAsset = "SFXGameContentDLC_CON_GUN01.SFXWeapon_SMG_Geth_GUN01";
                break;
            case 69:
                weaponAsset = "SFXGameContentDLC_CON_GUN01.SFXWeapon_SniperRifle_Turian_GUN01";
                break;
            case 70:
                weaponAsset = "SFXGameContentDLC_CON_GUN01.SFXWeapon_AssaultRifle_Quarian";
                break;
            case 70:
                weaponAsset = "SFXGameContentDLC_CON_GUN01.SFXWeapon_SMG_Bloodpack";
                break;
            %ADDITIONALCASESTATEMENTS%
            default:
                LogInternal("NONE SET!", );
                break;
        }
        WeaponClass = Class<SFXWeapon>(Class'SFXEngine'.static.LoadSeekFreeObjectBlocking(weaponAsset, Class'Class'));
        Wpn = SFXWeapon(CreateInventory(WeaponClass));
        if (bCombatPawn && Weapon == None)
        {
            if (Wpn != None && !keepDamageAsIs)
            {
                OldDmg = Wpn.Damage;
                OldDmg.X = (Wpn.Damage.X * Wpn.DamageHench) * 0.5;
                OldDmg.Y = (Wpn.Damage.Y * Wpn.DamageHench) * 0.5;
                Wpn.Damage = OldDmg;
            }
            SetWeaponImmediately(Wpn);
        }
    }
    return TRUE;
}