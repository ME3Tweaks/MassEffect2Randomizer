auto state Combat extends InCombat 
{
    // State Functions
    
    // State code
Begin:
    if (Outer.FireTarget == None)
    {
        while (Outer.SelectTarget() == FALSE)
        {
            Outer.Sleep(1.0);
        }
    }
    if (Outer.InRange(Outer.FireTarget.location, 500.0) == FALSE)
    {
        Class'SFXAICmd_MoveToMeleeRange'.static.MoveToMeleeRange(Outer, Outer.FireTarget, 350.0);
    }
    else
    {
        Outer.Attack();
        Outer.Sleep(1.0 + FRand());
    }
    Outer.Sleep(0.200000003);
    goto 'Begin';
    stop;
};