public function bool MoveToCoverNearHoldLocation(bool bInitialOrder)
{
    local Actor oMoveIndicator;
    
    if (int(GetTeamNum()) != 0)
    {
        return FALSE;
    }
    if (MyBP == None || MyBP.Squad == None)
    {
        AILog_Internal("Invalid pawn or squad", 'Move');
        return FALSE;
    }
    oMoveIndicator = MyBP.Squad.GetMemberMoveIndicator(MyBP.Squad.Members.Find(MyBP));
    if (oMoveIndicator == None)
    {
        AILog_Internal("Unable to get the move indicator", 'Move');
        return FALSE;
    }
    Class'SFXAICmd_AcquireCoverNearHoldLoc'.static.AcquireCoverNearHoldLoc(Self, AtCover_NearMoveGoal, oMoveIndicator, bInitialOrder, FALSE);
    return TRUE;
}