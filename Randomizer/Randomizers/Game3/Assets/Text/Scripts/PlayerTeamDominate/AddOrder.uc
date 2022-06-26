public function bool AddOrder(HenchmanOrderType eOrder, Actor oTargetActor, Vector vTargetLocation, Name nmPower, SFXWeapon oWeapon, optional int nQueue)
{
    local int nIndex;
    local bool bInstantOrder;
    local int nCurrentIndex;
    local int nQueuePosition;
    
    if ((MyBP == None || MyBP.IsDead()) || int(GetTeamNum()) != 0)
    {
        return FALSE;
    }
    if (m_RequestedActorToFollow != None)
    {
        if (eOrder == HenchmanOrderType.HENCHMAN_ORDER_FOLLOW || eOrder == HenchmanOrderType.HENCHMAN_ORDER_HOLD_POSITION)
        {
            return FALSE;
        }
    }
    if (eOrder == HenchmanOrderType.HENCHMAN_ORDER_USE_POWER)
    {
        bInstantOrder = TRUE;
        nQueuePosition = 0;
        nIndex = -1;
        for (nCurrentIndex = 0; nCurrentIndex < m_Orders.Length; nCurrentIndex++)
        {
            if (m_Orders[nCurrentIndex].eOrderType == HenchmanOrderType.HENCHMAN_ORDER_USE_POWER)
            {
                if (nQueuePosition == nQueue)
                {
                    nIndex = nCurrentIndex;
                    break;
                    continue;
                }
                nQueuePosition++;
            }
        }
        if (nIndex == -1)
        {
            m_Orders.Add(1);
            nIndex = m_Orders.Length - 1;
        }
    }
    else if (eOrder == HenchmanOrderType.HENCHMAN_ORDER_SWITCH_WEAPON)
    {
        bInstantOrder = TRUE;
        RemoveOldSwitchWeaponOrders();
        if (MyBP.Weapon == oWeapon)
        {
            return FALSE;
        }
        m_Orders.Add(1);
        nIndex = m_Orders.Length - 1;
    }
    else
    {
        m_Orders.Add(1);
        nIndex = m_Orders.Length - 1;
    }
    m_Orders[nIndex].eOrderType = eOrder;
    m_Orders[nIndex].oTargetActor = oTargetActor;
    m_Orders[nIndex].vTargetLocation = vTargetLocation;
    m_Orders[nIndex].nmPower = nmPower;
    m_Orders[nIndex].bInstantOrder = bInstantOrder;
    m_Orders[nIndex].oWeapon = oWeapon;
    m_Orders[nIndex].bExecutingOrder = FALSE;
    m_Orders[nIndex].bPowerUseIsInstant = TRUE;
    if (eOrder == HenchmanOrderType.HENCHMAN_ORDER_USE_POWER)
    {
        SetTimer(0.00999999978, FALSE, 'InstantUsePower', );
    }
    return TRUE;
}