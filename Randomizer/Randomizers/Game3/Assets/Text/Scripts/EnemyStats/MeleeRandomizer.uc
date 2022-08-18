    if (MyBP.CustomActionClasses[133] != None)
    {
        randIndex = Rand(%RANDCOUNTMELEE%);
        LogInternal("Randomizing EvadeLeft", );
        if (randIndex == 0)
        {
            MyBP.CustomActionClasses[54] = Class(Class'SFXEngine'.static.LoadSeekFreeObjectBlocking("MERGameContent.EvadeLeftActions.SFXCustomAction_AsariEvadeLeft", Class'BioCustomAction'));
        }
        %RANDMELEE%
    }
    if (MyBP.CustomActionClasses[134] != None)
    {
        randIndex = Rand(%RANDCOUNTMELEE%);
        LogInternal("Randomizing EvadeRight", );
        %RANDMELEE2%
     
     if (randIndex == 0)
        {
            MyBP.CustomActionClasses[55] = Class(Class'SFXEngine'.static.LoadSeekFreeObjectBlocking("MERGameContent.EvadeLeftActions.SFXCustomAction_AsariEvadeRight", Class'BioCustomAction'));
        }
    }
    if (MyBP.CustomActionClasses[135] != None)
    {
        randIndex = Rand(%RANDCOUNTSYNCMELEE%);
        LogInternal("Randomizing EvadeForwards", );
        %RANDSYNCMELEE%
    }

    // Todo: DeathReaction