//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Management;
//using System.Numerics;
//using System.Runtime.InteropServices;
//using System.Security.Principal;
//using System.Text;
//using System.Threading;
//using System.Windows;
//using System.Windows.Media.Imaging;
//using System.Xml;
//using System.Xml.Linq;
//using LegendaryExplorerCore.Packages;
//using LegendaryExplorerCore.Unreal;
//using Microsoft.Win32;
//using RandomizerUI.Classes.Randomizers.Utility;
////This namespace is used to work with Registry editor.

//namespace RandomizerUI.Classes
//{
//    public class MERUtilities
//    {
//        public const uint MEMI_TAG = 0x494D454D;

//        public const int WIN32_EXCEPTION_ELEVATED_CODE = -98763;
//        [DllImport("kernel32.dll")]
//        static extern uint GetLastError();
//        [DllImport("kernel32.dll", SetLastError = true)]
//        [return: MarshalAs(UnmanagedType.Bool)]
//        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);
//        public static string GetOperatingSystemInfo()
//        {
//            StringBuilder sb = new StringBuilder();
//            //Create an object of ManagementObjectSearcher class and pass query as parameter.
//            ManagementObjectSearcher mos = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
//            foreach (ManagementObject managementObject in mos.Get())
//            {
//                if (managementObject["Caption"] != null)
//                {
//                    sb.AppendLine("Operating System Name  :  " + managementObject["Caption"].ToString());   //Display operating system caption
//                }
//                if (managementObject["OSArchitecture"] != null)
//                {
//                    sb.AppendLine("Operating System Architecture  :  " + managementObject["OSArchitecture"].ToString());   //Display operating system architecture.
//                }
//                if (managementObject["CSDVersion"] != null)
//                {
//                    sb.AppendLine("Operating System Service Pack   :  " + managementObject["CSDVersion"].ToString());     //Display operating system version.
//                }
//            }
//            sb.AppendLine("\nProcessor Information-------");
//            RegistryKey processor_name = Registry.LocalMachine.OpenSubKey(@"Hardware\Description\System\CentralProcessor\0", RegistryKeyPermissionCheck.ReadSubTree);   //This registry entry contains entry for processor info.

//            if (processor_name != null)
//            {
//                if (processor_name.GetValue("ProcessorNameString") != null)
//                {
//                    sb.AppendLine((string)processor_name.GetValue("ProcessorNameString"));   //Display processor ingo.
//                }
//            }
//            return sb.ToString();
//        }

//        internal static string GetAppDataFolder()
//        {
//            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MassEffect2Randomizer");
//            Directory.CreateDirectory(folder);
//            return folder;
//        }

//        public static string GetEmbeddedStaticFilesTextFile(string filename)
//        {
//            string result = string.Empty;
//            var items = typeof(MainWindow).Assembly.GetManifestResourceNames();
//            using (Stream stream = typeof(MainWindow).Assembly.GetManifestResourceStream("ME2Randomizer.staticfiles.text." + filename))
//            {
//                using (StreamReader sr = new StreamReader(stream))
//                {
//                    result = sr.ReadToEnd();
//                }
//            }
//            return result;
//        }

//        /// <summary>
//        /// Fetches a file from the staticfiles resource folder
//        /// </summary>
//        /// <param name="filename"></param>
//        /// <param name="fullName"></param>
//        /// <returns></returns>
//        public static byte[] GetEmbeddedStaticFile(string filename, bool fullName = false)
//        {
//            var items = typeof(MainWindow).Assembly.GetManifestResourceNames();
//            using (Stream stream = typeof(MainWindow).Assembly.GetManifestResourceStream(fullName ? filename : "ME2Randomizer.staticfiles." + filename))
//            {
//                byte[] ba = new byte[stream.Length];
//                stream.Read(ba, 0, ba.Length);
//                return ba;
//            }
//        }

//        /// <summary>
//        /// Fetches a file from the staticfiles/binary resource folder
//        /// </summary>
//        /// <param name="filename"></param>
//        /// <param name="fullName"></param>
//        /// <returns></returns>
//        public static byte[] GetEmbeddedStaticFilesBinaryFile(string filename, bool fullName = false)
//        {
//            return GetEmbeddedStaticFile(fullName ? filename : ("binary." + filename), fullName);
//        }

//        public static string ExtractInternalFile(string internalResourceName, bool fullname, string destination, bool overwrite)
//        {
//            return ExtractInternalFile(internalResourceName, fullname, destination, overwrite, null);
//        }

//        public static void ExtractInternalFileToMemory(string internalResourceName, bool fullname, MemoryStream stream)
//        {
//            ExtractInternalFile(internalResourceName, fullname, destStream: stream);
//        }

//        private static string ExtractInternalFile(string internalResourceName, bool fullname, string destination = null, bool overwrite = false, Stream destStream = null)
//        {
//            MERLog.Information("Extracting file: " + internalResourceName);
//            if (destStream != null || (destination != null && (!File.Exists(destination) || overwrite)))
//            {
//                // Todo: might need adjusted for ME3
//                using Stream stream = MERUtilities.GetResourceStream(fullname ? internalResourceName : "ME2Randomizer.staticfiles." + internalResourceName);
//                bool close = destStream != null;
//                if (destStream == null)
//                {
//                    destStream = new FileStream(destination, FileMode.Create, FileAccess.Write);
//                }
//                stream.CopyTo(destStream);
//                if (close) stream.Close();
//            }
//            else if (destination != null && !overwrite)
//            {
//                MERLog.Warning("File already exists");
//            }
//            else
//            {
//                MERLog.Warning("Invalid extraction parameters!");
//            }
//            return destination;
//        }

//        public static bool IsDirectoryWritable2(string dirPath)
//        {
//            try
//            {
//                using (FileStream fs = File.Create(
//                    Path.Combine(
//                        dirPath,
//                        Path.GetRandomFileName()
//                    ),
//                    1,
//                    FileOptions.DeleteOnClose)
//                )
//                { }
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public static bool IsAdministrator()
//        {
//            var identity = WindowsIdentity.GetCurrent();
//            var principal = new WindowsPrincipal(identity);
//            return principal.IsInRole(WindowsBuiltInRole.Administrator);
//        }

//        /// <summary>
//        /// Gets the currently origin or steam game path.
//        /// </summary>
//        /// <param name="allowMissing">Allow directory to be missing if registry key still exists</param>
//        /// <returns></returns>
//        //public static string GetGamePath(bool allowMissing = false)
//        //{
//        //    MERUtilities.WriteDebugLog("Looking up game path for Mass Effect.");

//        //    //does not exist in ini (or ini does not exist).
//        //    string softwareKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\";
//        //    string key64 = @"Wow6432Node\";
//        //    string gameKey = @"BioWare\Mass Effect 2";
//        //    string entry = "Path";

//        //    string path = (string)Registry.GetValue(softwareKey + gameKey, entry, null);
//        //    if (path == null)
//        //    {
//        //        path = (string)Registry.GetValue(softwareKey + key64 + gameKey, entry, null);
//        //    }
//        //    if (path != null)
//        //    {
//        //        WriteDebugLog("Found game path via registry: " + path);
//        //        path = path.TrimEnd(Path.DirectorySeparatorChar);
//        //        if (allowMissing) return path; //don't do the rest of the check. we don't care

//        //        string GameEXEPath = Path.Combine(path, @"Binaries\MassEffect2.exe");
//        //        WriteDebugLog("GetGamePath Registry EXE Check Path: " + GameEXEPath);

//        //        if (File.Exists(GameEXEPath))
//        //        {
//        //            WriteDebugLog("EXE file exists - returning this path: " + GameEXEPath);
//        //            return path; //we have path now
//        //        }
//        //    }
//        //    else
//        //    {
//        //        WriteDebugLog("Could not find game via registry. Game is not installed, has not yet been run, or is not legitimate.");
//        //    }
//        //    WriteDebugLog("No path found. Returning null");
//        //    return null;
//        //}

//        //public static string GetGameEXEPath()
//        //{
//        //    string path = GetGamePath();
//        //    if (path == null) { return null; }
//        //    WriteDebugLog("GetEXE ME2 Path: " + Path.Combine(path, @"Binaries\MassEffect2.exe"));
//        //    return Path.Combine(path, @"Binaries\MassEffect2.exe");
//        //}

//        public static bool IsDirectoryEmpty(string path)
//        {
//            return !Directory.EnumerateFileSystemEntries(path).Any();
//        }

//        internal static void WriteRegistryKey(RegistryKey subkey, string subpath, string value, string data)
//        {
//            int i = 0;
//            string[] subkeys = subpath.Split('\\');
//            while (i < subkeys.Length)
//            {
//                subkey = subkey.CreateSubKey(subkeys[i]);
//                i++;
//            }
//            subkey.SetValue(value, data);
//        }

//        internal static void WriteRegistryKey(RegistryKey subkey, string subpath, string value, bool data)
//        {
//            WriteRegistryKey(subkey, subpath, value, data ? 1 : 0);
//        }

//        internal static void WriteRegistryKey(RegistryKey subkey, string subpath, string value, int data)
//        {
//            int i = 0;
//            string[] subkeys = subpath.Split('\\');
//            while (i < subkeys.Length)
//            {
//                subkey = subkey.CreateSubKey(subkeys[i]);
//                i++;
//            }
//            subkey.SetValue(value, data);
//        }

//        public static string GetBackupRegistrySettingString(string name)
//        {
//            string softwareKey = @"HKEY_CURRENT_USER\" + App.BACKUP_REGISTRY_KEY;
//            return (string)Registry.GetValue(softwareKey, name, null);
//        }

//        /// <summary>
//        /// Gets a list of filenames at the specified staticfiles subdir path. Returns full asset names.
//        /// </summary>
//        /// <param name="assetPath"></param>
//        /// <returns></returns>
//        public static List<string> ListStaticAssets(string assetRootPath, bool includeSubitems = false, bool includemerPrefix = true)
//        {
//            var items = typeof(MainWindow).Assembly.GetManifestResourceNames();
//            string prefix = $"ME2Randomizer.staticfiles.{assetRootPath}";
//            List<string> itemsL = new List<string>();
//            foreach (var item in items)
//            {
//                if (item.StartsWith(prefix))
//                {
//                    var iName = item.Substring(prefix.Length + 1);
//                    if (includeSubitems || iName.Count(x => x == '.') == 1) //Only has extension
//                    {
//                        itemsL.Add(iName);
//                    }
//                }
//            }

//            prefix = includemerPrefix ? prefix : $"staticfiles.{assetRootPath}";
//            return itemsL.Select(x => prefix + '.' + x).ToList();
//        }
//        public static string GetRegistrySettingString(string name)
//        {
//            string softwareKey = @"HKEY_CURRENT_USER\" + App.REGISTRY_KEY;
//            return (string)Registry.GetValue(softwareKey, name, null);
//        }

//        public static string GetRegistrySettingString(string key, string name)
//        {
//            return (string)Registry.GetValue(key, name, null);
//        }

//        public static bool? GetRegistrySettingBool(string name)
//        {
//            string softwareKey = @"HKEY_CURRENT_USER\" + App.REGISTRY_KEY;

//            int? value = (int?)Registry.GetValue(softwareKey, name, null);
//            if (value != null)
//            {
//                return value > 0;
//            }
//            return null;
//        }

//        public static int GetPartitionDiskBackingType(string partitionLetter)
//        {
//            using (var partitionSearcher = new ManagementObjectSearcher(
//                @"\\localhost\ROOT\Microsoft\Windows\Storage",
//                $"SELECT DiskNumber FROM MSFT_Partition WHERE DriveLetter='{partitionLetter}'"))
//            {
//                try
//                {
//                    var partition = partitionSearcher.Get().Cast<ManagementBaseObject>().Single();
//                    using (var physicalDiskSearcher = new ManagementObjectSearcher(
//                        @"\\localhost\ROOT\Microsoft\Windows\Storage",
//                        $"SELECT Size, Model, MediaType FROM MSFT_PhysicalDisk WHERE DeviceID='{ partition["DiskNumber"] }'"))
//                    {
//                        var physicalDisk = physicalDiskSearcher.Get().Cast<ManagementBaseObject>().Single();
//                        return
//                            (UInt16)physicalDisk["MediaType"];/*||
//                        SSDModelSubstrings.Any(substring => result.Model.ToLower().Contains(substring)); ;*/


//                    }
//                }
//                catch (Exception e)
//                {
//                    MERLog.Error("Error reading partition type on " + partitionLetter + ": " + e.Message);
//                    return -1;
//                }
//            }
//        }

//        /// <summary>
//        /// Checks if a hash string is in the list of supported hashes.
//        /// </summary>
//        /// <param name="game">Game ID</param>
//        /// <param name="hash">Executable hash</param>
//        /// <returns>True if found, false otherwise</returns>
//        public static bool CheckIfHashIsSupported(string hash)
//        {
//            foreach (KeyValuePair<string, string> hashPair in SUPPORTED_HASHES_ME2)
//            {
//                if (hashPair.Key == hash)
//                {
//                    return true;
//                }
//            }
//            return false;
//        }

//        public static string GetCPUString()
//        {
//            string str = "";
//            ManagementObjectSearcher mosProcessor = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
//            try
//            {
//                foreach (ManagementObject moProcessor in mosProcessor.Get())
//                {
//                    if (str != "")
//                    {
//                        str += "\n";
//                    }

//                    if (moProcessor["name"] != null)
//                    {
//                        str += moProcessor["name"].ToString();
//                        str += "\n";
//                    }
//                    if (moProcessor["maxclockspeed"] != null)
//                    {
//                        str += "Maximum reported clock speed: ";
//                        str += moProcessor["maxclockspeed"].ToString();
//                        str += " Mhz\n";
//                    }
//                    if (moProcessor["numberofcores"] != null)
//                    {
//                        str += "Cores: ";

//                        str += moProcessor["numberofcores"].ToString();
//                        str += "\n";
//                    }
//                    if (moProcessor["numberoflogicalprocessors"] != null)
//                    {
//                        str += "Logical processors: ";
//                        str += moProcessor["numberoflogicalprocessors"].ToString();
//                        str += "\n";
//                    }

//                }
//                return str
//                   .Replace("(TM)", "™")
//                   .Replace("(tm)", "™")
//                   .Replace("(R)", "®")
//                   .Replace("(r)", "®")
//                   .Replace("(C)", "©")
//                   .Replace("(c)", "©")
//                   .Replace("    ", " ")
//                   .Replace("  ", " ").Trim();
//            }
//            catch
//            {
//                return "Access denied: Not authorized to get CPU information\n";
//            }
//        }



//        public static List<KeyValuePair<string, string>> SUPPORTED_HASHES_ME2 = new List<KeyValuePair<string, string>>();

//        public static bool IsWindows10OrNewer()
//        {
//            var os = Environment.OSVersion;
//            return os.Platform == PlatformID.Win32NT &&
//                   (os.Version.Major >= 10);
//        }

//        private static Stream GetResourceStream(string assemblyResource)
//        {
//            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
//#if DEBUG
//            var resources = assembly.GetManifestResourceNames();
//#endif
//            return assembly.GetManifestResourceStream(assemblyResource);
//        }

//        public static void OpenWebPage(string link)
//        {
//            try
//            {
//                ProcessStartInfo psi = new ProcessStartInfo
//                {
//                    FileName = link,
//                    UseShellExecute = true
//                };
//                Process.Start(psi);
//            }
//            catch (Exception e)
//            {
//                MERLog.Error("Exception trying to open web page from system (typically means browser default is incorrectly configured by Windows): " + e.Message + ". Try opening the URL manually: " + link);
//            }
//        }

//        // Pinvoke for API function
//        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
//        [return: MarshalAs(UnmanagedType.Bool)]
//        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
//        out ulong lpFreeBytesAvailable,
//        out ulong lpTotalNumberOfBytes,
//        out ulong lpTotalNumberOfFreeBytes);

//        public static bool DriveFreeBytes(string folderName, out ulong freespace)
//        {
//            freespace = 0;
//            if (string.IsNullOrEmpty(folderName))
//            {
//                throw new ArgumentNullException("folderName");
//            }

//            if (!folderName.EndsWith("\\"))
//            {
//                folderName += '\\';
//            }

//            ulong free = 0, dummy1 = 0, dummy2 = 0;

//            if (GetDiskFreeSpaceEx(folderName, out free, out dummy1, out dummy2))
//            {
//                freespace = free;
//                return true;
//            }
//            else
//            {
//                return false;
//            }
//        }

//        public static string GetRelativePath(string filespec, string folder)
//        {
//            Uri pathUri = new Uri(filespec);
//            // Folders must end in a slash
//            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
//            {
//                folder += Path.DirectorySeparatorChar;
//            }
//            Uri folderUri = new Uri(folder);
//            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
//        }

//        public static string FlattenException(Exception exception)
//        {
//            var stringBuilder = new StringBuilder();

//            while (exception != null)
//            {
//                stringBuilder.AppendLine(exception.Message);
//                stringBuilder.AppendLine(exception.StackTrace);

//                exception = exception.InnerException;
//            }

//            return stringBuilder.ToString();
//        }

//        //public static ALOTVersionInfo GetInstalledALOTInfo()
//        //{
//        //    string gamePath = MERUtilities.GetALOTMarkerFilePath();
//        //    if (gamePath != null && File.Exists(gamePath))
//        //    {
//        //        try
//        //        {
//        //            using (FileStream fs = new FileStream(gamePath, System.IO.FileMode.Open, FileAccess.Read))
//        //            {
//        //                fs.SeekEnd();
//        //                long endPos = fs.Position;
//        //                fs.Position = endPos - 4;
//        //                uint memi = fs.ReadUInt32();

//        //                if (memi == MEMI_TAG)
//        //                {
//        //                    //ALOT has been installed
//        //                    fs.Position = endPos - 8;
//        //                    int installerVersionUsed = fs.ReadInt32();
//        //                    int perGameFinal4Bytes = 0;

//        //                    if (installerVersionUsed >= 10 && installerVersionUsed != perGameFinal4Bytes) //default bytes before 178 MEMI Format
//        //                    {
//        //                        fs.Position = endPos - 12;
//        //                        short ALOTVER = fs.ReadInt16();
//        //                        byte ALOTUPDATEVER = (byte)fs.ReadByte();
//        //                        byte ALOTHOTFIXVER = (byte)fs.ReadByte();

//        //                        //unused for now
//        //                        fs.Position = endPos - 16;
//        //                        int MEUITMVER = fs.ReadInt32();

//        //                        return new ALOTVersionInfo(ALOTVER, ALOTUPDATEVER, ALOTHOTFIXVER, MEUITMVER);
//        //                    }
//        //                    else
//        //                    {
//        //                        return new ALOTVersionInfo(0, 0, 0, 0); //MEMI tag but no info we know of
//        //                    }
//        //                }
//        //            }
//        //        }
//        //        catch (Exception e)
//        //        {
//        //            MERLog.Error("Error reading marker file for Mass Effect. ALOT Info will be returned as null (nothing installed). " + e.Message);
//        //            return null;
//        //        }
//        //    }
//        //    return null;
//        //}


//        public static int runProcess(string exe, string args, bool standAlone = false)
//        {
//            MERLog.Information("Running process: " + exe + " " + args);
//            using (Process p = new Process())
//            {
//                p.StartInfo.CreateNoWindow = true;
//                p.StartInfo.FileName = exe;
//                p.StartInfo.UseShellExecute = false;
//                p.StartInfo.Arguments = args;
//                p.StartInfo.RedirectStandardOutput = true;
//                p.StartInfo.RedirectStandardError = true;


//                StringBuilder output = new StringBuilder();
//                StringBuilder error = new StringBuilder();

//                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
//                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
//                {
//                    p.OutputDataReceived += (sender, e) =>
//                    {
//                        if (e.Data == null)
//                        {
//                            outputWaitHandle.Set();
//                        }
//                        else
//                        {
//                            output.AppendLine(e.Data);
//                        }
//                    };
//                    p.ErrorDataReceived += (sender, e) =>
//                    {
//                        if (e.Data == null)
//                        {
//                            errorWaitHandle.Set();
//                        }
//                        else
//                        {
//                            error.AppendLine(e.Data);
//                        }
//                    };

//                    p.Start();
//                    if (!standAlone)
//                    {
//                        int timeout = 600000;
//                        p.BeginOutputReadLine();
//                        p.BeginErrorReadLine();

//                        if (p.WaitForExit(timeout) &&
//                            outputWaitHandle.WaitOne(timeout) &&
//                            errorWaitHandle.WaitOne(timeout))
//                        {
//                            // Process completed. Check process.ExitCode here.
//                            MERLog.Information("Process standard output of " + exe + " " + args + ":");
//                            if (output.ToString().Length > 0)
//                            {
//                                MERLog.Information("Standard:\n" + output.ToString());
//                            }
//                            if (error.ToString().Length > 0)
//                            {
//                                MERLog.Error("Error output:\n" + error.ToString());
//                            }
//                            return p.ExitCode;
//                        }
//                        else
//                        {
//                            // Timed out.
//                            MERLog.Error("Process timed out: " + exe + " " + args);
//                            return -1;
//                        }
//                    }
//                    else
//                    {
//                        return 0; //standalone
//                    }
//                }
//            }
//        }

//        public static int runProcessAsAdmin(string exe, string args, bool standAlone = false)
//        {
//            MERLog.Information("Running process as admin: " + exe + " " + args);
//            using (Process p = new Process())
//            {
//                p.StartInfo.CreateNoWindow = true;
//                p.StartInfo.FileName = exe;
//                p.StartInfo.UseShellExecute = true;
//                p.StartInfo.Arguments = args;
//                p.StartInfo.Verb = "runas";
//                try
//                {
//                    p.Start();
//                    if (!standAlone)
//                    {
//                        p.WaitForExit(60000);
//                        try
//                        {
//                            return p.ExitCode;
//                        }
//                        catch (Exception e)
//                        {
//                            MERLog.Error("Error getting return code from admin process. It may have timed out.\n" + FlattenException(e));
//                            return -1;
//                        }
//                    }
//                    else
//                    {
//                        return 0;
//                    }
//                }
//                catch (System.ComponentModel.Win32Exception e)
//                {
//                    MERLog.Error("Error running elevated process: " + e.Message);
//                    return WIN32_EXCEPTION_ELEVATED_CODE;
//                }
//            }
//        }

//        public static void SetLocation(ExportEntry export, float x, float y, float z)
//        {
//            StructProperty prop = export.GetProperty<StructProperty>("location");
//            SetLocation(prop, x, y, z);
//            export.WriteProperty(prop);
//        }

//        public static void SetLocation(StructProperty prop, float x, float y, float z)
//        {
//            prop.GetProp<FloatProperty>("X").Value = x;
//            prop.GetProp<FloatProperty>("Y").Value = y;
//            prop.GetProp<FloatProperty>("Z").Value = z;
//        }

//        private static void RepointAllVariableReferencesToNode(ExportEntry targetNode, ExportEntry newNode, List<ExportEntry> exceptions = null)
//        {
//            var sequence = targetNode.FileRef.GetUExport(targetNode.GetProperty<ObjectProperty>("ParentSequence").Value);
//            var sequenceObjects = sequence.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects");
//            foreach (var seqObjRef in sequenceObjects)
//            {
//                var saveProps = false;
//                var seqObj = targetNode.FileRef.GetUExport(seqObjRef.Value);
//                var props = seqObj.GetProperties();
//                var variableLinks = props.GetProp<ArrayProperty<StructProperty>>("VariableLinks");
//                if (variableLinks != null)
//                {
//                    foreach (var variableLink in variableLinks)
//                    {
//                        var linkedVars = variableLink.GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables");
//                        if (linkedVars != null)
//                        {
//                            foreach (var linkedVar in linkedVars)
//                            {
//                                if (linkedVar.Value == targetNode.UIndex)
//                                {
//                                    linkedVar.Value = newNode.UIndex; //repoint
//                                    saveProps = true;
//                                }
//                            }
//                        }
//                    }
//                }

//                if (saveProps)
//                {
//                    seqObj.WriteProperties(props);
//                }
//            }
//        }

//        public static void SetRotation(ExportEntry export, float newDirectionDegrees)
//        {
//            StructProperty prop = export.GetProperty<StructProperty>("rotation");
//            if (prop == null)
//            {
//                PropertyCollection p = new PropertyCollection();
//                p.Add(new IntProperty(0, "Pitch"));
//                p.Add(new IntProperty(0, "Yaw"));
//                p.Add(new IntProperty(0, "Roll"));
//                prop = new StructProperty("Rotator", p, "Rotation", true);
//            }
//            SetRotation(prop, newDirectionDegrees);
//            export.WriteProperty(prop);
//        }

//        public static void SetRotation(StructProperty prop, float newDirectionDegrees)
//        {
//            int newYaw = (int)((newDirectionDegrees / 360) * 65535);
//            prop.GetProp<IntProperty>("Yaw").Value = newYaw;
//        }

//        private static void SetAttrSafe(XmlNode node, params XmlAttribute[] attrList)
//        {
//            foreach (var attr in attrList)
//            {
//                if (node.Attributes[attr.Name] != null)
//                {
//                    node.Attributes[attr.Name].Value = attr.Value;
//                }
//                else
//                {
//                    node.Attributes.Append(attr);
//                }
//            }
//        }

//        public static long GetInstalledRamAmount()
//        {
//            long memKb;
//            GetPhysicallyInstalledSystemMemory(out memKb);
//            if (memKb == 0L)
//            {
//                uint errorcode = GetLastError();
//                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
//                MERLog.Warning("Failed to get RAM amount. This may indicate a potential (or soon coming) hardware problem. The error message was: " + errorMessage);
//            }
//            return memKb;
//        }

//        public static bool TestXMLIsValid(string inputXML)
//        {
//            try
//            {
//                XDocument.Parse(inputXML);
//                return true;
//            }
//            catch (XmlException)
//            {
//                return false;
//            }
//        }

//        public static string sha256(string randomString)
//        {
//            System.Security.Cryptography.SHA256Managed crypt = new System.Security.Cryptography.SHA256Managed();
//            System.Text.StringBuilder hash = new System.Text.StringBuilder();
//            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString), 0, Encoding.UTF8.GetByteCount(randomString));
//            foreach (byte theByte in crypto)
//            {
//                hash.Append(theByte.ToString("x2"));
//            }
//            return hash.ToString();
//        }

//        public static bool OpenAndSelectFileInExplorer(string filePath)
//        {
//            if (!System.IO.File.Exists(filePath))
//            {
//                return false;
//            }
//            //Clean up file path so it can be navigated OK
//            filePath = System.IO.Path.GetFullPath(filePath);
//            System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));
//            return true;

//        }

//        public static bool IsWindowOpen<T>(string name = "") where T : Window
//        {
//            return string.IsNullOrEmpty(name)
//               ? Application.Current.Windows.OfType<T>().Any()
//               : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
//        }

//        public static long DirSize(DirectoryInfo d)
//        {
//            long size = 0;
//            // Add file sizes.
//            FileInfo[] fis = d.GetFiles();
//            foreach (FileInfo fi in fis)
//            {
//                size += fi.Length;
//            }
//            // Add subdirectory sizes.
//            DirectoryInfo[] dis = d.GetDirectories();
//            foreach (DirectoryInfo di in dis)
//            {
//                size += DirSize(di);
//            }
//            return size;
//        }

//        public static bool IsSubfolder(string parentPath, string childPath)
//        {
//            var parentUri = new Uri(parentPath);
//            var childUri = new DirectoryInfo(childPath).Parent;
//            while (childUri != null)
//            {
//                if (new Uri(childUri.FullName) == parentUri)
//                {
//                    return true;
//                }
//                childUri = childUri.Parent;
//            }
//            return false;
//        }

//        public static void GetAntivirusInfo()
//        {
//            ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntivirusProduct");
//            ManagementObjectCollection data = wmiData.Get();

//            foreach (ManagementObject virusChecker in data)
//            {
//                var virusCheckerName = virusChecker["displayName"];
//                var productState = virusChecker["productState"];
//                uint productVal = (uint)productState;
//                var bytes = BitConverter.GetBytes(productVal);
//                MERLog.Information("Antivirus info: " + virusCheckerName + " with state " + bytes[1].ToString("X2") + " " + bytes[2].ToString("X2") + " " + bytes[3].ToString("X2"));
//            }
//        }

//        internal static void SetLocation(ExportEntry bioPawn, Vector3 position)
//        {
//            SetLocation(bioPawn, position.X, position.Y, position.Z);
//        }

//        public static bool IsGameRunning(MEGame game)
//        {
//#if __ME2__
//            return Process.GetProcessesByName("MassEffect2").Any() || Process.GetProcessesByName("ME2Game").Any();
//#elif __ME3__
//            return true; // FIX ME
//#else
//            return false;
//#endif
//        }

//        public static bool IsSupportedLocale()
//        {
//            var target = Locations.GetTarget(MERFileSystem.Game);
//            if (target != null)
//            {

//                var locintfile1 = Path.Combine(target.TargetPath, @"BioGame\CookedPC\Startup_INT.pcc");
//                var locintfile2 = Path.Combine(target.TargetPath, @"BioGame\CookedPC\BioD_QuaTlL_321AgriDomeTrial1_LOC_INT.pcc");
//                var locintfile3 = Path.Combine(target.TargetPath, @"BioGame\CookedPC\ss_global_hench_geth_S_INT.afc");
//                var locintfile4 = Path.Combine(target.TargetPath, @"BioGame\CookedPC\BioD_ProFre_500Warhouse_LOC_INT.pcc");

//                return File.Exists(locintfile1) && File.Exists(locintfile2) && File.Exists(locintfile3) && File.Exists(locintfile4);
//            }

//            return false;
//        }
//        internal static string GetAppCrashHandledFile()
//        {
//            return Path.Combine(MERUtilities.GetAppDataFolder(), "APP_CRASH_HANDLED");
//        }

//        internal static string GetAppCrashFile()
//        {
//            return Path.Combine(MERUtilities.GetAppDataFolder(), "APP_CRASH");
//        }

//        internal static object ExtractInteralFileToMemory(string v)
//        {
//            throw new NotImplementedException();
//        }

//        public static string GetFilenameFromAssetName(string assetName)
//        {
//            var parts = assetName.Split('.');
//            return string.Join('.', parts[^2], parts[^1]);
//        }

//        /// <summary>
//        /// Loads an image from the specified data array
//        /// </summary>
//        /// <param name="imageData"></param>
//        /// <returns></returns>
//        public static BitmapImage LoadImage(byte[] imageData)
//        {
//            if (imageData == null || imageData.Length == 0) return null;
//            var image = new BitmapImage();
//            using (var mem = new MemoryStream(imageData))
//            {
//                mem.Position = 0;
//                image.BeginInit();
//                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
//                image.CacheOption = BitmapCacheOption.OnLoad;
//                image.UriSource = null;
//                image.StreamSource = mem;
//                image.EndInit();
//            }
//            image.Freeze();
//            return image;
//        }
//    }
//}