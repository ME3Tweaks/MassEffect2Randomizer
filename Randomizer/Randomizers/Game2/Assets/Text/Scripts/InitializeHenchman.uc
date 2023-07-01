public function InitializeHenchman(int DesiredLevel)
{
    local BioWorldInfo BWI;
    local BioPlayerController PC;
    local SFXEngine Engine;
    local int ExpectedTalentPoints;
    local int I;
    local int J;
    local int AssignedPoints;
    local int IsLoyalPlotCond;
    local SFXPower pow;
    local bool IsLoyal;
    local string henchCodename;
    local BioGlobalVariableTable GVars;
    
    BWI = BioWorldInfo(WorldInfo);
    PC = BWI.GetLocalPlayerController();
    if (PC != None)
    {
        Engine = SFXEngine(PC.Player.Outer);
    }
    Engine.CurrentSaveGame.LoadHenchman(Self);
    SpawnDefaultWeapons();
    if (CharacterLevel != DesiredLevel)
    {
        Class'BioLevelUpSystem'.static.LevelUpPawn(Self, DesiredLevel);
    }
    ExpectedTalentPoints = 1;
    for (I = 2; I <= 5; I++)
    {
        if (I <= DesiredLevel)
        {
            ExpectedTalentPoints += 2;
            LogInternal((((("Talent points for " $ Tag) $ ": ") $ ExpectedTalentPoints) $ ", level ") $ I, );
        }
    }
    for (I = 6; I < 20; I++)
    {
        if (I <= DesiredLevel && I %  2 == 1)
        {
            ExpectedTalentPoints += 2;
            LogInternal((((("Talent points for " $ Tag) $ ": ") $ ExpectedTalentPoints) $ ", level ") $ I, );
        }
    }
    for (I = 21; I < 30; I++)
    {
        if (I <= DesiredLevel && I %  2 == 1)
        {
            ExpectedTalentPoints += 1;
            LogInternal((((("Talent points for " $ Tag) $ ": ") $ ExpectedTalentPoints) $ ", level ") $ I, );
        }
    }
    if (DesiredLevel == 30)
    {
        ExpectedTalentPoints += 1;
    }
    GVars = BioWorldInfo(WorldInfo).GetGlobalVariables();
    if (Tag == 'hench_vixen')
    {
        ExpectedTalentPoints += 1;
        IsLoyal = GVars.GetBoolByName('IsLoyalVixen');
    }
    else if (Tag == 'hench_leading')
    {
        ExpectedTalentPoints += 1;
        IsLoyal = GVars.GetBoolByName('IsLoyalLeading');
    }
    else if (Tag == 'hench_convict')
    {
        IsLoyal = GVars.GetBoolByName('IsLoyalConvict');
    }
    else if (Tag == 'hench_geth')
    {
        IsLoyal = GVars.GetBoolByName('IsLoyalGeth');
    }
    else if (Tag == 'hench_thief')
    {
        IsLoyal = GVars.GetBoolByName('IsLoyalThief');
    }
    else if (Tag == 'hench_garrus')
    {
        IsLoyal = GVars.GetBoolByName('IsLoyalGarrus');
    }
    else if (Tag == 'hench_assassin')
    {
        IsLoyal = GVars.GetBoolByName('IsLoyalAssassin');
    }
    else if (Tag == 'hench_tali')
    {
        IsLoyal = GVars.GetBoolByName('IsLoyalTali');
    }
    else if (Tag == 'hench_professor')
    {
        IsLoyal = GVars.GetBoolByName('IsLoyalProfessor');
    }
    else if (Tag == 'hench_grunt')
    {
        IsLoyal = GVars.GetBoolByName('IsLoyalGrunt');
    }
    else if (Tag == 'hench_mystic')
    {
        IsLoyal = GVars.GetBoolByName('IsLoyalMystic');
    }
    else if (Tag == 'hench_veteran')
    {
        IsLoyal = GVars.GetBoolByName('IsLoyalVeteran');
    }
    if (IsLoyal)
    {
        LogInternal("Squadmember is loyal", );
        ExpectedTalentPoints += 1;
    }
    else
    {
        LogInternal("Squad member is not loyal", );
    }
    LogInternal((((("Expected talent points for " $ Tag) $ ": ") $ ExpectedTalentPoints) $ ", level ") $ DesiredLevel, );
    LogInternal("Current points: " $ TalentPoints, );
    LogInternal("Sum of points + assigned powers:", );
    for (I = 0; I < PowerManager.Powers.Length; I++)
    {
        pow = PowerManager.Powers[I];
        if (pow.DisplayInCharacterRecord)
        {
            LogInternal((pow.DisplayName $ ": Rank ") $ pow.Rank, );
            for (J = int(pow.Rank); J > 0; J--)
            {
                AssignedPoints += J;
            }
        }
    }
    LogInternal("Total assigned points: " $ AssignedPoints, );
    I = AssignedPoints + TalentPoints;
    LogInternal("Need to add: " $ ExpectedTalentPoints - I, );
    if (ExpectedTalentPoints - I > 0)
    {
        AddTalentPoints(ExpectedTalentPoints - I);
    }
}