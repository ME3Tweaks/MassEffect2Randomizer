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
    Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, 100.0, TRUE);
    if (Outer.ShouldSmokeTarget())
    {
        Class'SFXAICmd_Atlas_SmokeTarget'.static.SmokeBetweenUs(Outer, Outer.FireTarget);
        Outer.LastSmokeTime = Outer.WorldInfo.GameTimeSeconds;
    }
    else if (Outer.ShouldFireRocket())
    {
        Outer.Focus = Outer.FireTarget;
        if (Normal(Outer.FireTarget.location - Outer.MyBP.location) Dot Vector(Outer.MyBP.Rotation) > Outer.RocketConeAngle)
        {
            Class'SFXAICmd_UsePower'.static.UsePower(Outer, Outer.RocketAttack, Outer.FireTarget, , TRUE);
            Outer.LastRocketTime = Outer.WorldInfo.GameTimeSeconds;
            Outer.Sleep(1.5 + FRand());
        }
    }
    else
    {
        Outer.Attack();
    }
    Outer.Sleep(0.100000001);
    goto 'Begin';
    stop;
};