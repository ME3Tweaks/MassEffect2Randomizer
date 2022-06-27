public function bool ShouldUsePowerOnShields(BioPawn Target, Class<SFXDamageType> DamageType, out string sOptionalInfo)
{
    if (SFXPawn_PlayerParty(Target) != None)
    { return TRUE; }
    if (int(Target.GetCurrentResistance()) == 1)
    {
        if (DamageType.default.Resistance.Shield < 1.5)
        {
            sOptionalInfo = string(NotRecommended_TargetHasShields);
            return FALSE;
        }
    }
    else if (int(Target.GetCurrentResistance()) == 2)
    {
        if (DamageType.default.Resistance.Biotic < 1.5)
        {
            sOptionalInfo = string(NotRecommended_TargetHasBiotics);
            return FALSE;
        }
    }
    else if (int(Target.GetCurrentResistance()) == 3)
    {
        if (DamageType.default.Resistance.Armour < 1.5)
        {
            sOptionalInfo = string(NotRecommended_TargetHasArmor);
            return FALSE;
        }
    }
    return TRUE;
}