// Automatically adds back missing talent points
public function OnPowersLoaded()
{
    local int ExpectedTalentPoints;
    local int I;
    local BioGlobalVariableTable GVars;
    local int J;
    local SFXPower pow;
    local int AssignedPoints;
    local bool HasFoundBonusPower;
    
    Super.OnPowersLoaded();
    ExpectedTalentPoints = 0;
    for (I = 1; I <= 30; I++)
    {
        if (I <= CharacterLevel)
        {
            if (I > 20){
            ExpectedTalentPoints +=  1;
                
                } else {
            ExpectedTalentPoints +=2;
                
                }
            LogInternal((((("Talent points for " $ Tag) $ ": ") $ ExpectedTalentPoints) $ ", level ") $ I, );
        }
    }
    GVars = BioWorldInfo(WorldInfo).GetGlobalVariables();
    LogInternal((((("Expected talent points for " $ Tag) $ ": ") $ ExpectedTalentPoints) $ ", level ") $ CharacterLevel, );
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
        if (pow.IsHenchmenUnique && !HasFoundBonusPower){
            HasFoundBonusPower = true;
            LogInternal("Found bonus power, adding expected point");
            ExpectedTalentPoints += 1;
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