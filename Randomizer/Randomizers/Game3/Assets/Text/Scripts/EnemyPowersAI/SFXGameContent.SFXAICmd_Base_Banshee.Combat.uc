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
    if (Outer.ShouldChargeUp())
    {
        Outer.MyBP.StartCustomAction(149);
        Outer.SetCombatMood(4);
    }
    else if (Outer.ShouldBlast())
    {
        Outer.Focus = Outer.FireTarget;
        Outer.SetDesiredRotation(Rotator(Outer.FireTarget.location - Outer.Pawn.location));
        Outer.FinishRotation();
        Outer.MyBP.StartCustomAction(169);
        Outer.LastBlastTime = Outer.WorldInfo.GameTimeSeconds;
    }
    Outer.Attack();
    Outer.Sleep(0.200000003);
    goto 'Begin';
    stop;
};