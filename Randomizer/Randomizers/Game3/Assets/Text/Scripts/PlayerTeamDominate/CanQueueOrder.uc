public function bool CanQueueOrder()
{
    local int nQueuedInstantOrders;
    
    if (m_nEnabledFlags != 0 || int(GetTeamNum()) != 0)
    {
        return FALSE;
    }
    nQueuedInstantOrders = GetInstantOrderCount();
    if (nQueuedInstantOrders > 1)
    {
        return FALSE;
    }
    return TRUE;
}