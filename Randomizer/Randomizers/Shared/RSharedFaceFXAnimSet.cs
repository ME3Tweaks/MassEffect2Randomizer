using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers.Shared
{
    public class RSharedFaceFXAnimSet
    {
#if __GAME1__ || __GAME2__
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == "FaceFXAnimSet";
#elif __GAME3__
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName is "FaceFXAnimSet" or "FaceFXAsset";
#endif
        public static bool RandomizeExport(GameTarget target, ExportEntry exp,RandomizationOption option)
        {
            if (!CanRandomize(exp)) return false;
            //if (exp.UIndex != 1999) return false;
            try
            {
                //Log.Information($"[{Path.GetFileNameWithoutExtension(exp.FileRef.FilePath)}] Randomizing FaceFX export {exp.UIndex}");
                //var d = exp.Data;
      
                List<FaceFXLine> lines = null;
                ObjectBinary facefx = ObjectBinary.From(exp);
                if (facefx is FaceFXAsset fxa)
                {
                    lines = fxa.Lines;
                }
                else if (facefx is FaceFXAnimSet fxas)
                {
                    lines = fxas.Lines;
                }
                else
                {
                    throw new Exception("How did this happen? Invalid object bin for FaceFX");
                }
                for (int i = 0; i < lines.Count; i++)
                {
                    var faceFxline = lines[i];
                    //if (true)

                    bool randomizedBoneList = false;
                    if (ThreadSafeRandom.Next(10 - (int)option.SliderValue) == 0)
                    {
                        //Randomize the names used for animation
                        faceFxline.AnimationNames.Shuffle();
                        randomizedBoneList = true;
                    }
                    if (!randomizedBoneList || ThreadSafeRandom.Next(16 - (int)option.SliderValue) == 0) // if bonelist was shuffled, or if 1 in 16 chance while it was also shuffled.
                    {
                        //Randomize the points
                        for (int j = 0; j < faceFxline.Points.Count; j++)
                        {
                            bool isLast = j == faceFxline.Points.Count - 1;
                            var currentPoint = faceFxline.Points[j];
                            switch (option.SliderValue)
                            {
                                case 1: //A few broken bones
                                    currentPoint.weight += ThreadSafeRandom.NextFloat(-.25, .25);
                                    break;
                                case 2: //A significant option.SliderValue of broken bones
                                    currentPoint.weight += ThreadSafeRandom.NextFloat(-.5, .5);
                                    break;
                                case 3: //That's not how the face is supposed to work
                                    if (ThreadSafeRandom.Next(5) == 0)
                                    {
                                        currentPoint.weight = ThreadSafeRandom.NextFloat(-3, 3);
                                    }
                                    else if (isLast && ThreadSafeRandom.Next(3) == 0)
                                    {
                                        currentPoint.weight = ThreadSafeRandom.NextFloat(.7, .7);
                                    }
                                    else
                                    {
                                        currentPoint.weight *= 8;
                                    }

                                    break;
                                case 4: //:O
                                    if (ThreadSafeRandom.Next(12) == 0)
                                    {
                                        currentPoint.weight = ThreadSafeRandom.NextFloat(-5, 5);
                                    }
                                    else if (isLast && ThreadSafeRandom.Next(2) == 0)
                                    {
                                        currentPoint.weight = ThreadSafeRandom.NextFloat(.9, .9);
                                    }
                                    else
                                    {
                                        currentPoint.weight *= 5;
                                    }
                                    //Debug.WriteLine(currentPoint.weight);

                                    break;
                                case 5: //Utter madness
                                    currentPoint.weight = ThreadSafeRandom.NextFloat(-10, 10);
                                    break;
                                default:
                                    Debugger.Break();
                                    break;
                            }
                            //if (currentPoint.weight == 0)
                                //Debug.WriteLine($"Weight is 0, {faceFxline.NameAsString}, exp {exp.UIndex} {exp.FullPath}");
                            faceFxline.Points[j] = currentPoint; //Reassign the struct
                        }
                    }

                    //Debugging only: Get list of all animation names
                    //for (int j = 0; j < faceFxline.animations.Length; j++)
                    //{
                    //    var animationName = animSet.Header.Names[faceFxline.animations[j].index]; //animation name
                    //    faceFxBoneNames.Add(animationName);
                    //}
                }

                //var dataBefore = exp.Data;
                exp.WriteBinary(facefx);
                //var dataAfter = exp.Data;
                //if (dataBefore.SequenceEqual(dataAfter))
                //{
                //Debugger.Break();
                //}
                //else
                //{
                //    MERLog.Information($"[{Path.GetFileNameWithoutExtension(exp.FileRef.FilePath)}] Randomized FaceFX for export " + exp.UIndex);
                // }
                return true;
            }
            catch (Exception e)
            {
                //Do nothing for now.
                MERLog.Exception(e, "AnimSet Error");
            }
            return false;
        }
    }
}
