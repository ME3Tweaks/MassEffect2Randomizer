using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class SkipMiniGames
    {

        private static bool CanApplySkip(ExportEntry export) => !export.IsDefaultObject && export.ClassName == "SequenceReference" && IsMiniGameRef(export);

        private static bool IsMiniGameRef(ExportEntry export)
        {
            var sRef = export.GetProperty<ObjectProperty>("oSequenceReference");
            if (sRef != null && export.FileRef.TryGetUExport(sRef.Value, out var referencedItem))
            {
                var objName = referencedItem.GetProperty<StrProperty>("ObjName");
                if (objName != null)
                {
                    if (objName == "REF_SkillGame_Bypass") return true;
                    if (objName == "REF_SkillGame_Decryption") return true;
                    if (objName == "REF_SkillGame_Hack") return true;

                }

            }
            return false;
        }

        public static bool DetectAndSkipMiniGameSeqRefs(ExportEntry exp, RandomizationOption option)
        {
            if (!CanApplySkip(exp)) return false;
            SeqTools.SkipSequenceElement(exp, 0); //Success is link 0
            return true;
        }
    }
}
