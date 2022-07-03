using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Misc;
using ME3TweaksCore.Targets;
using Microsoft.Win32;

namespace Randomizer.MER
{
    public class MERUtilities
    {
        public const int WIN32_EXCEPTION_ELEVATED_CODE = -98763;

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        public static string GetOperatingSystemInfo()
        {
            StringBuilder sb = new StringBuilder();
            //Create an object of ManagementObjectSearcher class and pass query as parameter.
            ManagementObjectSearcher mos = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
            foreach (ManagementObject managementObject in mos.Get())
            {
                if (managementObject["Caption"] != null)
                {
                    sb.AppendLine("Operating System Name  :  " + managementObject["Caption"].ToString()); //Display operating system caption
                }

                if (managementObject["OSArchitecture"] != null)
                {
                    sb.AppendLine("Operating System Architecture  :  " + managementObject["OSArchitecture"].ToString()); //Display operating system architecture.
                }

                if (managementObject["CSDVersion"] != null)
                {
                    sb.AppendLine("Operating System Service Pack   :  " + managementObject["CSDVersion"].ToString()); //Display operating system version.
                }
            }

            sb.AppendLine("\nProcessor Information-------");
            RegistryKey processor_name = Registry.LocalMachine.OpenSubKey(@"Hardware\Description\System\CentralProcessor\0", RegistryKeyPermissionCheck.ReadSubTree); //This registry entry contains entry for processor info.

            if (processor_name != null)
            {
                if (processor_name.GetValue("ProcessorNameString") != null)
                {
                    sb.AppendLine((string)processor_name.GetValue("ProcessorNameString")); //Display processor ingo.
                }
            }

            return sb.ToString();
        }

        internal static string GetAppDataFolder()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MassEffect2Randomizer");
            Directory.CreateDirectory(folder);
            return folder;
        }



        //        /// <summary>
        //        /// Fetches a file from the staticfiles resource folder
        //        /// </summary>
        //        /// <param name="filename"></param>
        //        /// <param name="fullName"></param>
        //        /// <returns></returns>
        //        public static byte[] GetEmbeddedStaticFile(string filename, bool fullName = false)
        //        {
        //            var items = Assembly.GetExecutingAssembly().GetManifestResourceNames();
        //#if __GAME1__
        //// NEEDS GENERATION
        //            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName ? filename : "Randomizer.Randomizers.Game1.staticfiles." + filename))
        //#elif __GAME2__
        //// NEEDS GENERATION
        //            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName ? filename : "Randomizer.Randomizers.Game2.staticfiles." + filename))
        //#elif __GAME3__
        //            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName ? filename : "Randomizer.Randomizers.Game3.Assets." + filename))
        //#endif
        //            {
        //                byte[] ba = new byte[stream.Length];
        //                stream.Read(ba, 0, ba.Length);
        //                return ba;
        //            }
        //        }

        ///// <summary>
        ///// Fetches a file from the staticfiles/binary resource folder
        ///// </summary>
        ///// <param name="filename"></param>
        ///// <param name="fullName"></param>
        ///// <returns></returns>
        //public static byte[] GetEmbeddedBinaryFile(string filename, bool fullName = false)
        //{
        //    return GetEmbeddedAsset("Binary", $"{targetGame}.{packageName}");

        //    return GetEmbeddedAsset("Binary", fullName ? filename : ("Binary." + filename), fullName);
        //}

        

        public static bool IsDirectoryWritable2(string dirPath)
        {
            try
            {
                using (FileStream fs = File.Create(
                           Path.Combine(
                               dirPath,
                               Path.GetRandomFileName()
                           ),
                           1,
                           FileOptions.DeleteOnClose)
                      )
                {
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        /// <summary>
        /// Gets a list of filenames at the specified staticfiles subdir path. Returns full asset names.
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static List<string> ListStaticAssets(string assetRootPath, bool includeSubitems = false, bool includemerPrefix = true)
        {
            var items = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            string prefix = $"ME2Randomizer.staticfiles.{assetRootPath}";
            List<string> itemsL = new List<string>();
            foreach (var item in items)
            {
                if (item.StartsWith(prefix))
                {
                    var iName = item.Substring(prefix.Length + 1);
                    if (includeSubitems || iName.Count(x => x == '.') == 1) //Only has extension
                    {
                        itemsL.Add(iName);
                    }
                }
            }

            prefix = includemerPrefix ? prefix : $"staticfiles.{assetRootPath}";
            return itemsL.Select(x => prefix + '.' + x).ToList();
        }

        /// <summary>
        /// Lists packages for the specified game target that are embedded. Returned paths are full asset paths.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="includeSpecial"></param>
        /// <returns></returns>
        public static List<string> ListStaticPackageAssets(GameTarget target, string assetFolderName, bool includeSubitems)
        {
            var GameText = $"Game{target.Game.ToMEMGameNum()}"; // MEM Game num is 1 2 3 only.
            var items = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            string prefix = $"Randomizer.Randomizers.{GameText}.Assets.Binary.Packages.{target.Game}.{assetFolderName}";
            List<string> itemsL = new List<string>();
            foreach (var item in items)
            {
                if (item.StartsWith(prefix))
                {
                    var iName = item.Substring(prefix.Length + 1);
                    if (includeSubitems || iName.Count(x => x == '.') == 1) //Only has extension
                    {
                        itemsL.Add(iName);
                    }
                }
            }

            return itemsL.Select(x => prefix + '.' + x).ToList();
        }

        public static int GetPartitionDiskBackingType(string partitionLetter)
        {
            using (var partitionSearcher = new ManagementObjectSearcher(
                       @"\\localhost\ROOT\Microsoft\Windows\Storage",
                       $"SELECT DiskNumber FROM MSFT_Partition WHERE DriveLetter='{partitionLetter}'"))
            {
                try
                {
                    var partition = partitionSearcher.Get().Cast<ManagementBaseObject>().Single();
                    using (var physicalDiskSearcher = new ManagementObjectSearcher(
                               @"\\localhost\ROOT\Microsoft\Windows\Storage",
                               $"SELECT Size, Model, MediaType FROM MSFT_PhysicalDisk WHERE DeviceID='{partition["DiskNumber"]}'"))
                    {
                        var physicalDisk = physicalDiskSearcher.Get().Cast<ManagementBaseObject>().Single();
                        return
                            (UInt16)physicalDisk["MediaType"]; /*||
                        SSDModelSubstrings.Any(substring => result.Model.ToLower().Contains(substring)); ;*/


                    }
                }
                catch (Exception e)
                {
                    MERLog.Error("Error reading partition type on " + partitionLetter + ": " + e.Message);
                    return -1;
                }
            }
        }

        /// <summary>
        /// Checks if a hash string is in the list of supported hashes.
        /// </summary>
        /// <param name="game">Game ID</param>
        /// <param name="hash">Executable hash</param>
        /// <returns>True if found, false otherwise</returns>
        public static bool CheckIfHashIsSupported(string hash)
        {
            foreach (KeyValuePair<string, string> hashPair in SUPPORTED_HASHES_ME2)
            {
                if (hashPair.Key == hash)
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetCPUString()
        {
            string str = "";
            ManagementObjectSearcher mosProcessor = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            try
            {
                foreach (ManagementObject moProcessor in mosProcessor.Get())
                {
                    if (str != "")
                    {
                        str += "\n";
                    }

                    if (moProcessor["name"] != null)
                    {
                        str += moProcessor["name"].ToString();
                        str += "\n";
                    }

                    if (moProcessor["maxclockspeed"] != null)
                    {
                        str += "Maximum reported clock speed: ";
                        str += moProcessor["maxclockspeed"].ToString();
                        str += " Mhz\n";
                    }

                    if (moProcessor["numberofcores"] != null)
                    {
                        str += "Cores: ";

                        str += moProcessor["numberofcores"].ToString();
                        str += "\n";
                    }

                    if (moProcessor["numberoflogicalprocessors"] != null)
                    {
                        str += "Logical processors: ";
                        str += moProcessor["numberoflogicalprocessors"].ToString();
                        str += "\n";
                    }

                }

                return str
                    .Replace("(TM)", "™")
                    .Replace("(tm)", "™")
                    .Replace("(R)", "®")
                    .Replace("(r)", "®")
                    .Replace("(C)", "©")
                    .Replace("(c)", "©")
                    .Replace("    ", " ")
                    .Replace("  ", " ").Trim();
            }
            catch
            {
                return "Access denied: Not authorized to get CPU information\n";
            }
        }



        public static List<KeyValuePair<string, string>> SUPPORTED_HASHES_ME2 = new List<KeyValuePair<string, string>>();

        public static bool IsWindows10OrNewer()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32NT &&
                   (os.Version.Major >= 10);
        }

        public static void OpenWebPage(string link)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = link,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception e)
            {
                MERLog.Error("Exception trying to open web page from system (typically means browser default is incorrectly configured by Windows): " + e.Message + ". Try opening the URL manually: " + link);
            }
        }

        // Pinvoke for API function
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
            out ulong lpFreeBytesAvailable,
            out ulong lpTotalNumberOfBytes,
            out ulong lpTotalNumberOfFreeBytes);

        public static bool DriveFreeBytes(string folderName, out ulong freespace)
        {
            freespace = 0;
            if (string.IsNullOrEmpty(folderName))
            {
                throw new ArgumentNullException("folderName");
            }

            if (!folderName.EndsWith("\\"))
            {
                folderName += '\\';
            }

            ulong free = 0, dummy1 = 0, dummy2 = 0;

            if (GetDiskFreeSpaceEx(folderName, out free, out dummy1, out dummy2))
            {
                freespace = free;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }

            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        //public static ALOTVersionInfo GetInstalledALOTInfo()
        //{
        //    string gamePath = MERUtilities.GetALOTMarkerFilePath();
        //    if (gamePath != null && File.Exists(gamePath))
        //    {
        //        try
        //        {
        //            using (FileStream fs = new FileStream(gamePath, System.IO.FileMode.Open, FileAccess.Read))
        //            {
        //                fs.SeekEnd();
        //                long endPos = fs.Position;
        //                fs.Position = endPos - 4;
        //                uint memi = fs.ReadUInt32();

        //                if (memi == MEMI_TAG)
        //                {
        //                    //ALOT has been installed
        //                    fs.Position = endPos - 8;
        //                    int installerVersionUsed = fs.ReadInt32();
        //                    int perGameFinal4Bytes = 0;

        //                    if (installerVersionUsed >= 10 && installerVersionUsed != perGameFinal4Bytes) //default bytes before 178 MEMI Format
        //                    {
        //                        fs.Position = endPos - 12;
        //                        short ALOTVER = fs.ReadInt16();
        //                        byte ALOTUPDATEVER = (byte)fs.ReadByte();
        //                        byte ALOTHOTFIXVER = (byte)fs.ReadByte();

        //                        //unused for now
        //                        fs.Position = endPos - 16;
        //                        int MEUITMVER = fs.ReadInt32();

        //                        return new ALOTVersionInfo(ALOTVER, ALOTUPDATEVER, ALOTHOTFIXVER, MEUITMVER);
        //                    }
        //                    else
        //                    {
        //                        return new ALOTVersionInfo(0, 0, 0, 0); //MEMI tag but no info we know of
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            MERLog.Error("Error reading marker file for Mass Effect. ALOT Info will be returned as null (nothing installed). " + e.Message);
        //            return null;
        //        }
        //    }
        //    return null;
        //}


        public static int runProcess(string exe, string args, bool standAlone = false)
        {
            MERLog.Information("Running process: " + exe + " " + args);
            using (Process p = new Process())
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = exe;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.Arguments = args;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;


                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    p.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    p.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };

                    p.Start();
                    if (!standAlone)
                    {
                        int timeout = 600000;
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();

                        if (p.WaitForExit(timeout) &&
                            outputWaitHandle.WaitOne(timeout) &&
                            errorWaitHandle.WaitOne(timeout))
                        {
                            // Process completed. Check process.ExitCode here.
                            MERLog.Information("Process standard output of " + exe + " " + args + ":");
                            if (output.ToString().Length > 0)
                            {
                                MERLog.Information("Standard:\n" + output.ToString());
                            }

                            if (error.ToString().Length > 0)
                            {
                                MERLog.Error("Error output:\n" + error.ToString());
                            }

                            return p.ExitCode;
                        }
                        else
                        {
                            // Timed out.
                            MERLog.Error("Process timed out: " + exe + " " + args);
                            return -1;
                        }
                    }
                    else
                    {
                        return 0; //standalone
                    }
                }
            }
        }

        public static int runProcessAsAdmin(string exe, string args, bool standAlone = false)
        {
            MERLog.Information("Running process as admin: " + exe + " " + args);
            using (Process p = new Process())
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = exe;
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.Arguments = args;
                p.StartInfo.Verb = "runas";
                try
                {
                    p.Start();
                    if (!standAlone)
                    {
                        p.WaitForExit(60000);
                        try
                        {
                            return p.ExitCode;
                        }
                        catch (Exception e)
                        {
                            MERLog.Error("Error getting return code from admin process. It may have timed out.\n" + e.FlattenException());
                            return -1;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    MERLog.Error("Error running elevated process: " + e.Message);
                    return WIN32_EXCEPTION_ELEVATED_CODE;
                }
            }
        }

        private static void RepointAllVariableReferencesToNode(ExportEntry targetNode, ExportEntry newNode, List<ExportEntry> exceptions = null)
        {
            var sequence = targetNode.FileRef.GetUExport(targetNode.GetProperty<ObjectProperty>("ParentSequence").Value);
            var sequenceObjects = sequence.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects");
            foreach (var seqObjRef in sequenceObjects)
            {
                var saveProps = false;
                var seqObj = targetNode.FileRef.GetUExport(seqObjRef.Value);
                var props = seqObj.GetProperties();
                var variableLinks = props.GetProp<ArrayProperty<StructProperty>>("VariableLinks");
                if (variableLinks != null)
                {
                    foreach (var variableLink in variableLinks)
                    {
                        var linkedVars = variableLink.GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables");
                        if (linkedVars != null)
                        {
                            foreach (var linkedVar in linkedVars)
                            {
                                if (linkedVar.Value == targetNode.UIndex)
                                {
                                    linkedVar.Value = newNode.UIndex; //repoint
                                    saveProps = true;
                                }
                            }
                        }
                    }
                }

                if (saveProps)
                {
                    seqObj.WriteProperties(props);
                }
            }
        }

        private static void SetAttrSafe(XmlNode node, params XmlAttribute[] attrList)
        {
            foreach (var attr in attrList)
            {
                if (node.Attributes[attr.Name] != null)
                {
                    node.Attributes[attr.Name].Value = attr.Value;
                }
                else
                {
                    node.Attributes.Append(attr);
                }
            }
        }

        public static long GetInstalledRamAmount()
        {
            long memKb;
            GetPhysicallyInstalledSystemMemory(out memKb);
            if (memKb == 0L)
            {
                uint errorcode = GetLastError();
                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                MERLog.Warning("Failed to get RAM amount. This may indicate a potential (or soon coming) hardware problem. The error message was: " + errorMessage);
            }

            return memKb;
        }

        public static bool TestXMLIsValid(string inputXML)
        {
            try
            {
                XDocument.Parse(inputXML);
                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }

        public static bool OpenAndSelectFileInExplorer(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return false;
            }

            //Clean up file path so it can be navigated OK
            filePath = System.IO.Path.GetFullPath(filePath);
            System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));
            return true;

        }

        public static bool IsWindowOpen<T>(string name = "") where T : Window
        {
            return string.IsNullOrEmpty(name)
                ? Application.Current.Windows.OfType<T>().Any()
                : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
        }

        public static long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }

            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }

            return size;
        }

        public static bool IsSubfolder(string parentPath, string childPath)
        {
            var parentUri = new Uri(parentPath);
            var childUri = new DirectoryInfo(childPath).Parent;
            while (childUri != null)
            {
                if (new Uri(childUri.FullName) == parentUri)
                {
                    return true;
                }

                childUri = childUri.Parent;
            }

            return false;
        }

        public static void GetAntivirusInfo()
        {
            ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntivirusProduct");
            ManagementObjectCollection data = wmiData.Get();

            foreach (ManagementObject virusChecker in data)
            {
                var virusCheckerName = virusChecker["displayName"];
                var productState = virusChecker["productState"];
                uint productVal = (uint)productState;
                var bytes = BitConverter.GetBytes(productVal);
                MERLog.Information("Antivirus info: " + virusCheckerName + " with state " + bytes[1].ToString("X2") + " " + bytes[2].ToString("X2") + " " + bytes[3].ToString("X2"));
            }
        }

        public static bool IsGameRunning(MEGame game)
        {
            foreach (var exeName in MEDirectories.ExecutableNames(game))
            {
                if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exeName)).Any())
                    return true;
            }

            return false;
        }

        public static bool IsSupportedLocale(GameTarget target)
        {

            var locintfile1 = Path.Combine(M3Directories.GetCookedPath(target), "Startup_INT.pcc");
            var locintfile2 = Path.Combine(M3Directories.GetCookedPath(target), "BioD_QuaTlL_321AgriDomeTrial1_LOC_INT.pcc");
            var locintfile3 = Path.Combine(M3Directories.GetCookedPath(target), "ss_global_hench_geth_S_INT.afc");
            var locintfile4 = Path.Combine(M3Directories.GetCookedPath(target), "BioD_ProFre_500Warhouse_LOC_INT.pcc");

            return File.Exists(locintfile1) && File.Exists(locintfile2) && File.Exists(locintfile3) && File.Exists(locintfile4);
        }

        internal static string GetAppCrashHandledFile()
        {
            return Path.Combine(MERUtilities.GetAppDataFolder(), "APP_CRASH_HANDLED");
        }

        internal static string GetAppCrashFile()
        {
            return Path.Combine(MERUtilities.GetAppDataFolder(), "APP_CRASH");
        }


        /// <summary>
        /// Loads an image from the specified data array
        /// </summary>
        /// <param name="imageData"></param>
        /// <returns></returns>
        public static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }

        /// <summary>
        /// Gets the UI name for the build of the randomizer and generation
        /// </summary>
        /// <param name="originalTrilogy"></param>
        /// <returns></returns>
        public static string GetGameUIName(bool originalTrilogy)
        {
#if __GAME1__
            return originalTrilogy ? "Mass Effect" : "Mass Effect (Legendary Editon)";
#elif __GAME2__
            return originalTrilogy ? "Mass Effect 2" : "Mass Effect 2 (Legendary Editon)";
#else
            return "Mass Effect 3 Legendary Edition"; // We don't support OT for MER.
#endif
        }



        /// <summary>
        /// Returns MER/ME2R/ME3R
        /// </summary>
        /// <returns></returns>
        public static string GetRandomizerShortName()
        {
#if __GAME1__
            return "MER";
#elif __GAME2__
            return "ME2R";
#else
            return "ME3R";
#endif
        }

        /// <summary>
        /// Inventories a class export and adds it to the lookup system
        /// </summary>
        /// <param name="e"></param>
        public static void InventoryCustomClass(ExportEntry e)
        {
            if (e.ClassName != "Class")
                throw new Exception("Cannot inventory a non-class object");
            var classInfo = GlobalUnrealObjectInfo.generateClassInfo(e);
            var defaults = e.FileRef.GetUExport(ObjectBinary.From<UClass>(e).Defaults);
            Debug.WriteLine($@"Inventorying {e.InstancedFullPath}");
            GlobalUnrealObjectInfo.GenerateSequenceObjectInfoForClassDefaults(defaults);
            GlobalUnrealObjectInfo.InstallCustomClassInfo(e.ObjectName, classInfo, e.Game);
        }




    }
}