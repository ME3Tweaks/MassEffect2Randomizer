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
            merRandomPowerId = Rand(%NUMPOWERSINPOOL%);
            switch (merRandomPowerId)
            {
                %POWERSLIST%
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