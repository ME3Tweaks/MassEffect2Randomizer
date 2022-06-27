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
    Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, Outer.MeleeMoveOffset);
    if (CanBombingRun())
    {
        Outer.Focus = Outer.FireTarget;
        Outer.SetDesiredRotation(Rotator(Outer.FireTarget.location - Outer.Pawn.location));
        Outer.FinishRotation();
        BioPawn(Outer.FireTarget).SetAbilityTimeStamp(Outer.BombingRunAbilityName);
        Outer.MyBP.StartCustomAction(205);
        Outer.LastBombingRunTime = Outer.WorldInfo.GameTimeSeconds;
    }
    else if (Outer.IsBomberInRange(Outer.EnemyDistance_Melee) && Outer.ShouldMelee(Outer.FireTarget))
    {
        Outer.SetDesiredRotation(Rotator(Outer.FireTarget.location - Outer.Pawn.location));
        Outer.FinishRotation();
        Outer.MyBP.StartCustomAction(133);
    }
    else
    {
        Outer.Attack();
        Outer.Sleep(0.5);
    }
    Outer.Sleep(0.200000003);
    goto 'Begin';
    stop;
};