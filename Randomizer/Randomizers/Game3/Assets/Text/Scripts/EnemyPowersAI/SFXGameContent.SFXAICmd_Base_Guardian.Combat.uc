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
    MoveTimeout = RandRange(MoveTimeoutInterval.X, MoveTimeoutInterval.Y);
    TotalMoveTime = 0.0;
    Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, 100.0, TRUE, , TRUE);
    Outer.Attack();
    Outer.Sleep(0.100000001);
    goto 'Begin';
    stop;
};