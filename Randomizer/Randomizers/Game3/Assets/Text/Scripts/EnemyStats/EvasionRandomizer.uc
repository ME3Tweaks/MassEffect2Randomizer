    if (MyBP.CustomActionClasses[54] != None)
    {
        randIndex = Rand(1);
        LogInternal("Randomizing EvadeLeft", );
        if (randIndex == 0)
        {
            MyBP.CustomActionClasses[54] = Class(Class'SFXEngine'.static.LoadSeekFreeObjectBlocking("MERGameContent.EvadeLeftActions.SFXCustomAction_AsariEvadeLeft", Class'BioCustomAction'));
        }
    }
    if (MyBP.CustomActionClasses[55] != None)
    {
        randIndex = Rand(1);
        LogInternal("Randomizing EvadeRight", );
        if (randIndex == 0)
        {
            MyBP.CustomActionClasses[55] = Class(Class'SFXEngine'.static.LoadSeekFreeObjectBlocking("MERGameContent.EvadeLeftActions.SFXCustomAction_AsariEvadeRight", Class'BioCustomAction'));
        }
    }
    if (MyBP.CustomActionClasses[56] != None)
    {
        randIndex = Rand(1);
        LogInternal("Randomizing EvadeForwards", );
        if (randIndex == 0)
        {
            MyBP.CustomActionClasses[55] = Class(Class'SFXEngine'.static.LoadSeekFreeObjectBlocking("MERGameContent.EvadeForwardActions.SFXCustomAction_AsariEvadeForward", Class'BioCustomAction'));
        }
    }
    if (MyBP.CustomActionClasses[57] != None)
    {
        randIndex = Rand(1);
        LogInternal("Randomizing EvadeBackwards", );
        if (randIndex == 0)
        {
            MyBP.CustomActionClasses[55] = Class(Class'SFXEngine'.static.LoadSeekFreeObjectBlocking("MERGameContent.EvadeBackwardsActions.SFXCustomAction_AsariEvadeBackwards", Class'BioCustomAction'));
        }
    }