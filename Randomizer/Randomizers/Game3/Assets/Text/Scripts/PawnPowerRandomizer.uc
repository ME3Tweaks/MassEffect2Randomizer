public function InitializePowerList()
{
    local BioCustomAction CustomAction;
    local SFXPowerCustomActionBase Power;
    local Class<SFXPowerCustomActionBase> RandomPower;
    local SFXPawn_PlayerParty oPlayerPartyPawn;
    local SFXEngine Engine;
    local int numPowersToGive;
    local int powerGivingIdx;
    local string powerSeekFreeName;
    local int merRandomPowerId;
    local int PowerID;
    local int nIndex;
    local SFXPowerCustomActionBase PowerInList;
    local bool continueAddingPower;
    local bool gotAmmoPower;
    local bool isAmmoPower;
    
    if (MyPawn == None)
    {
        return;
    }
    LogInternal("MER InitializePowerList for " $ MyPawn, );
    Powers.Length = 0;
    if (SFXPawn_PlayerParty(MyPawn) == None)
    {
        numPowersToGive = Max(1, Rand(4));
        for (powerGivingIdx = 0; powerGivingIdx < numPowersToGive; powerGivingIdx++)
        {
            continueAddingPower = TRUE;
            isAmmoPower = FALSE;
            merRandomPowerId = Rand(47);
            switch (merRandomPowerId)
            {
                case 0:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_AdrenalineRush";
                    break;
                case 1:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_AIHacking";
                    break;
                case 2:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_ArmorPiercingAmmo";
                    isAmmoPower = TRUE;
                    break;
                case 3:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_KaiLengSlash";
                    break;
                case 4:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Barrier";
                    break;
                case 5:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Carnage";
                    break;
                case 6:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_CerberusGrenade";
                    break;
                case 7:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Cloak";
                    break;
                case 8:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_CombatDrone";
                    break;
                case 9:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_ConcussiveShot";
                    break;
                case 10:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_CryoAmmo";
                    isAmmoPower = TRUE;
                    break;
                case 11:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_CryoBlast";
                    break;
                case 12:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_DarkChannel";
                    break;
                case 13:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Decoy";
                    break;
                case 14:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_DefensiveShield";
                    break;
                case 15:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Discharge";
                    break;
                case 16:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_DisruptorAmmo";
                    isAmmoPower = TRUE;
                    break;
                case 17:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_EnergyDrain";
                    break;
                case 18:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Fortification";
                    break;
                case 19:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_FragGrenade";
                    break;
                case 20:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_GethPrimeShieldDrone";
                    break;
                case 21:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_GethPrimeTurret";
                    break;
                case 22:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_GethShieldBoost";
                    break;
                case 23:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_IncendiaryAmmo";
                    isAmmoPower = TRUE;
                    break;
                case 24:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Incinerate";
                    break;
                case 25:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_InfernoGrenade";
                    break;
                case 26:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_LiftGrenade";
                    break;
                case 27:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_StickyGrenade";
                    break;
                case 28:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_BioticGrenade";
                    break;
                case 29:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Marksman";
                    break;
                case 30:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Overload";
                    break;
                case 31:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_ProtectorDrone";
                    break;
                case 32:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_ProximityMine";
                    break;
                case 33:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Pull";
                    break;
                case 34:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Reave";
                    break;
                case 35:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_ReaperGrenade";
                    break;
                case 36:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_SentryTurret";
                    break;
                case 37:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Shockwave";
                    break;
                case 38:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Singularity";
                    break;
                case 39:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Slam";
                    break;
                case 40:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Stasis";
                    break;
                case 41:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_TechArmor";
                    break;
                case 42:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Throw";
                    break;
                case 43:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_TitanRocket";
                    break;
                case 44:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_Warp";
                    break;
                case 45:
                    powerSeekFreeName = "SFXGameContent.SFXPowerCustomAction_WarpAmmo";
                    isAmmoPower = TRUE;
                    break;
                case 46:
                    powerSeekFreeName = "MERGameContent.SFXPowerCustomAction_EnemyBioticCharge";
                    break;
                default:
                    LogInternal("MER Warning: Random power out of range: " $ merRandomPowerId, );
                    continue;
            }
            LogInternal((("MER: Attempting to give " $ MyPawn) $ " power ") $ powerSeekFreeName, );
            if (isAmmoPower && (SFXPawn(MyPawn).Loadout.Weapons.Length == 0 || gotAmmoPower))
            {
                LogInternal("MER: Re-rolling power, pawn has no weapon or was already given an ammo power", );
                powerGivingIdx--;
                continue;
            }
            RandomPower = Class<SFXPowerCustomActionBase>(Class'SFXEngine'.static.GetSeekFreeObject(powerSeekFreeName, Class'Class'));
            PowerID = RandomPower.default.PowerCustomActionID;
            if (PowerID == 0)
            {
                LogInternal("MER Warning: Cannot add random power: PowerID is 0!", );
                continue;
            }
            for (nIndex = 0; nIndex < Powers.Length; nIndex++)
            {
                PowerInList = Powers[nIndex];
                if (PowerInList != None && PowerInList.Class == RandomPower)
                {
                    LogInternal(((("MER Warning: " $ MyPawn) $ " already has ") $ RandomPower) $ " in the list. We will skip giving them a random power", );
                    continueAddingPower = FALSE;
                    continue;
                }
            }
            if (continueAddingPower)
            {
                MyPawn.PowerCustomActionClasses[PowerID] = RandomPower;
                MyPawn.VerifyCAHasBeenInstanced(132, PowerID);
                if (isAmmoPower)
                {
                    gotAmmoPower = TRUE;
                }
            }
        }
    }
    foreach MyPawn.PowerCustomActions(CustomAction, )
    {
        Power = SFXPowerCustomActionBase(CustomAction);
        if (Power != None)
        {
            if (SFXPawn_PlayerParty(MyPawn) == None)
            {
                Power.Rank = float(Max(1, Rand(4)));
            }
            Powers.AddItem(Power);
        }
    }
    oPlayerPartyPawn = SFXPawn_PlayerParty(MyPawn);
    if (oPlayerPartyPawn != None)
    {
        oPlayerPartyPawn.SetPowerStartingRanks();
        Engine = SFXEngine(Class'Engine'.static.GetEngine());
        if (Engine != None && Engine.CurrentSaveGame != None)
        {
            Engine.CurrentSaveGame.LoadPawnPowers(MyPawn);
        }
    }
    foreach Powers(Power, )
    {
        Power.OnPowersLoaded();
    }
}