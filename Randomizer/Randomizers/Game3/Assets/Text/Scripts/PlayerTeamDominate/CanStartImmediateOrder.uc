public function bool CanStartImmediateOrder()
{
    local int nQueuedInstantOrders;
    
    if (m_nEnabledFlags != 0 || int(GetTeamNum()) != 0)
    {
        return FALSE;
    }
    nQueuedInstantOrders = GetInstantOrderCount();
    if (nQueuedInstantOrders > 0)
    {
        return FALSE;
    }
    return TRUE;
}