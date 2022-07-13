PawnShield = SFXShield_Base(MyBP.InvManager.FindInventoryType(Class'SFXShield_Base', TRUE));
if (PawnShield != None)
{
    PawnShield.InitializeMaxShields((FRand() * 2.0) * PawnShield.CurrentShields);
    PawnShield.MaxEnemyShieldRecharge = FRand();
    if (PawnShield.MaxEnemyShieldRecharge > 0.0)
    {
        PawnShield.bRechargeable = TRUE;
        PawnShield.ShieldRegenPct = FRand() * 0.5;
        PawnShield.ShieldRegenDelay.X = FRand() * 5.0;
        PawnShield.ShieldRegenDelay.Y = PawnShield.ShieldRegenDelay.X + FRand() * 5.0;
        PawnShield.ShieldRegenDelay.Value = FClamp(FRand() * (PawnShield.ShieldRegenDelay.Y - PawnShield.ShieldRegenDelay.X) + PawnShield.ShieldRegenDelay.X, PawnShield.ShieldRegenDelay.X, PawnShield.ShieldRegenDelay.Y);
    }
}