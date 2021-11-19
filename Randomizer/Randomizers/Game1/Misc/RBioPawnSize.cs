using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using Serilog;

namespace Randomizer.Randomizers.Game1.Misc
{
    class RBioPawnSize
    {
        private void RandomizeBioPawnSize(ExportEntry export, Random random, double amount)
        {
            Log.Information("Randomizing pawn size for " + export.UIndex + ": " + export.InstancedFullPath);
            var props = export.GetProperties();
            StructProperty sp = props.GetProp<StructProperty>("DrawScale3D");
            if (sp == null)
            {
                var structprops = GlobalUnrealObjectInfo.getDefaultStructValue(export.Game, "Vector", true);
                sp = new StructProperty("Vector", structprops, "DrawScale3D", GlobalUnrealObjectInfo.IsImmutable("Vector", export.Game));
                props.Add(sp);
            }

            if (sp != null)
            {
                //Debug.WriteLine("Randomizing morph face " + Path.GetFileName(export.FileRef.FileName) + " " + export.UIndex + " " + export.GetFullPath + " vPos");
                FloatProperty x = sp.GetProp<FloatProperty>("X");
                FloatProperty y = sp.GetProp<FloatProperty>("Y");
                FloatProperty z = sp.GetProp<FloatProperty>("Z");
                if (x.Value == 0) x.Value = 1;
                if (y.Value == 0) y.Value = 1;
                if (z.Value == 0) z.Value = 1;
                x.Value = x.Value * ThreadSafeRandom.NextFloat(1 - amount, 1 + amount);
                y.Value = y.Value * ThreadSafeRandom.NextFloat(1 - amount, 1 + amount);
                z.Value = z.Value * ThreadSafeRandom.NextFloat(1 - amount, 1 + amount);
            }

            export.WriteProperties(props);
            //export.GetProperties(true);
            //ArrayProperty<StructProperty> m_aMorphFeatures = props.GetProp<ArrayProperty<StructProperty>>("m_aMorphFeatures");
            //if (m_aMorphFeatures != null)
            //{
            //    foreach (StructProperty morphFeature in m_aMorphFeatures)
            //    {
            //        FloatProperty offset = morphFeature.GetProp<FloatProperty>("Offset");
            //        if (offset != null)
            //        {
            //            //Debug.WriteLine("Randomizing morph face " + Path.GetFileName(export.FileRef.FileName) + " " + export.UIndex + " " + export.GetFullPath + " offset");
            //            offset.Value = offset.Value * ThreadSafeRandom.NextFloat(1 - (amount / 3), 1 + (amount / 3));
            //        }
            //    }
            //}
        }

    }
}
