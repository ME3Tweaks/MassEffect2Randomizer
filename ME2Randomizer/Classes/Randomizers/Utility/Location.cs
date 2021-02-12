using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    public static class Location
    {
        public static void SetLocation(ExportEntry export, CFVector3 location)
        {
            StructProperty prop = export.GetProperty<StructProperty>("location");
            SetLocation(prop, location);
            export.WriteProperty(prop);
        }

        public static void SetLocation(ExportEntry export, float x, float y, float z)
        {
            StructProperty prop = export.GetProperty<StructProperty>("location");
            SetLocation(prop, x, y, z);
            export.WriteProperty(prop);
        }
        public static void SetLocation(StructProperty prop, float x, float y, float z)
        {
            prop.GetProp<FloatProperty>("X").Value = x;
            prop.GetProp<FloatProperty>("Y").Value = y;
            prop.GetProp<FloatProperty>("Z").Value = z;
        }

        public static void SetLocation(StructProperty prop, CFVector3 vector)
        {
            prop.GetProp<FloatProperty>("X").Value = vector.X;
            prop.GetProp<FloatProperty>("Y").Value = vector.Y;
            prop.GetProp<FloatProperty>("Z").Value = vector.Z;
        }

        public static CFVector3? GetLocation(ExportEntry export)
        {
            float x = 0, y = 0, z = int.MinValue;
            var prop = export.GetProperty<StructProperty>("location");
            if (prop != null)
            {
                foreach (var locprop in prop.Properties)
                {
                    switch (locprop)
                    {
                        case FloatProperty fltProp when fltProp.Name == "X":
                            x = fltProp;
                            break;
                        case FloatProperty fltProp when fltProp.Name == "Y":
                            y = fltProp;
                            break;
                        case FloatProperty fltProp when fltProp.Name == "Z":
                            z = fltProp;
                            break;
                    }
                }

                return new CFVector3() { X = x, Y = y, Z = z };
            }

            return null;
        }

    }
}
