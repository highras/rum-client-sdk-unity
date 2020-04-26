using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using com.fpnn.msgpack;

namespace com.fpnn.rum
{
    internal static class RUMFile
    {
        static private string persistentDataPath;

        static private readonly string rumRootFolder = "/com.fpnn.rum/";
        static private readonly string rumDataFolder = "rumdata/";
        static private readonly string rumIdFilename = "rumid";
        static private readonly string rumConfigFilename = "rumconfig";

        static private readonly string rumSessionDirectoryPrefix = "s_";
        static private readonly string rumSessionDataFilePrefix = "f_";
        static private readonly string rumSessionSendingCacheFileName = "sed";

        //-- session cache folder:
        //--    Application.persistentDataPath/rumRootFolder/rumDataFolder/rumSessionDirectoryPrefix + sessionId/
        //--
        //-- session sending cache file:
        //--    rumSessionDataFilePrefix + rumSessionSendingCacheFileName
        //--
        //-- session cache file:
        //--    rumSessionDataFilePrefix + priority + "_" + sectionIndex
        //--
        //-- session cache file structure:
        //--    msgpack-list<byte[]> + msgpack-list<byte[]> + ...

        //----------------------------------------------//
        //--                   Init                   --//
        //----------------------------------------------//
        internal static void Init()
        {
            persistentDataPath = Application.persistentDataPath;
        }

        private static void CheckDirectory(string path)
        {
            if (Directory.Exists(path))
                return;

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Create path for RUM failed. Path: " + path, e);

                throw e;
            }
        }

        internal static void InitDirectories(CoreInfo coreInfo)
        {
            string path = persistentDataPath + rumRootFolder;

            try
            {
                CheckDirectory(path);
            }
            catch (Exception e)
            {
                RUMCenter.AddInternalErrorInfo("Init root path for RUM failed. All data cannot be cached on disk. Path: " + path, e);
                return;
            }

            path += rumDataFolder;
            try
            {
                CheckDirectory(path);
            }
            catch (Exception e)
            {
                RUMCenter.AddInternalErrorInfo("Init data path for RUM failed. All data cannot be cached on disk. Path: " + path, e);
                return;
            }
        }

        //----------------------------------------------//
        //--                 RUM ID                   --//
        //----------------------------------------------//
        internal static void RetrieveRumId(CoreInfo coreInfo)
        {
            string path = persistentDataPath + rumRootFolder + rumIdFilename;
            string reserveRumId = coreInfo.rumId;

            try
            {
                coreInfo.rumId = File.ReadAllText(path);
                return;
            }
            catch (FileNotFoundException)
            {
                //-- MayBe New Installed.
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Load RumId file failed. Path: " + path, e);

                RUMCenter.AddInternalErrorInfo("Load RumId file failed. A new rumid will be Generated. Path: " + path, e);
            }

            coreInfo.rumId = reserveRumId;

            try
            {
                File.WriteAllText(path, coreInfo.rumId);
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Save RumId file failed. Path: " + path, e);

                RUMCenter.AddInternalErrorInfo("Save RumId file failed. Path: " + path, e);
            }
        }

        //----------------------------------------------//
        //--             File Utilities               --//
        //----------------------------------------------//
        private static byte[] LoadBinaryFile(string path, bool notFoundNotError = false)
        {
            try
            {
                return File.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                if (notFoundNotError && e is FileNotFoundException)
                    return null;

                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Load binary file failed. Path: " + path, e);

                RUMCenter.AddInternalErrorInfo("Load binary file failed. Path: " + path, e);
            }

            return null;
        }

        private static bool SaveBinaryFile(string path, byte[] data)
        {
            try
            {
                File.WriteAllBytes(path, data);
                return true;
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Save binary file failed. Path: " + path, e);

                RUMCenter.AddInternalErrorInfo("Save binary file failed. Path: " + path, e);
            }

            return false;
        }

        private static bool DeleteFile(string fullPath, string errorInfo, bool recordInternalError)
        {
            try
            {
                File.Delete(fullPath);
                return true;
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError(errorInfo + " File: " + fullPath, e);

                if (recordInternalError)
                    RUMCenter.AddInternalErrorInfo(errorInfo + " File: " + fullPath, e);

                return false;
            }
        }

        private static void DeleteInvalidCacheFile(string fullPath)
        {
            if (RUMCenter.errorRecorder != null)
                RUMCenter.errorRecorder.RecordError("Find invalid cache file: " + fullPath);

            DeleteFile(fullPath, "Delete invalid cache file failed.", false);
        }

        private static long GetFileSize(string fullPath)
        {
            try
            {
                FileInfo info = new FileInfo(fullPath);
                return info.Length;
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Get file size failed. Path: " + fullPath, e);

                return -1;
            }
        }

        //----------------------------------------------//
        //--         Session Folder & Files           --//
        //----------------------------------------------//
        private static string GetPathLastSegment(string path)
        {
            string lastSegment;
            int idx = path.LastIndexOf('/');
            if (idx == -1)
                lastSegment = path;
            else
                lastSegment = path.Substring(idx + 1);
            
            //-- For windows
            idx = lastSegment.LastIndexOf('\\');
            if (idx == -1)
                return lastSegment;
            else
                return lastSegment.Substring(idx + 1);
        }

        internal static string[] GetCachedSessions()
        {
            string[] fullPaths;
            string path = persistentDataPath + rumRootFolder + rumDataFolder;

            try
            {
                fullPaths = Directory.GetDirectories(path, rumSessionDirectoryPrefix + "*");
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Load cached sessions' directories failed. Path: " + path, e);

                RUMCenter.AddInternalErrorInfo("Load cached sessions' directories failed. Path: " + path, e);

                return null;
            }

            if (fullPaths.Length == 0)
                return fullPaths;

            string[] rev = new string[fullPaths.Length];

            for (int i = 0; i < fullPaths.Length; i++)
            {
                string sidDirectory = GetPathLastSegment(fullPaths[i]);
                rev[i] = sidDirectory.Substring(rumSessionDirectoryPrefix.Length);
            }

            return rev;
        }

        private static bool ParseSessionFileToPriorityAndIndex(string fullPath, out int priority, out int index)
        {
            priority = 0;
            index = 0;

            string filename = GetPathLastSegment(fullPath);
            string infoPart = filename.Substring(rumSessionDataFilePrefix.Length);

            string[] segments = infoPart.Split('_');
            if (segments == null || segments.Length != 2)
            {
                //-- DeleteInvalidCacheFile(fullPath);      //-- for sending cache file.
                return false;
            }

            if (!Int32.TryParse(segments[0], out priority))
            {
                DeleteInvalidCacheFile(fullPath);
                return false;
            }

            if (!Int32.TryParse(segments[1], out index))
            {
                DeleteInvalidCacheFile(fullPath);
                return false;
            }

            return true;
        }

        internal static SessionCacheInfo LoadCacheInfo(string sessionId)
        {
            //-----------[ Fetch Files List ]----------------//
            string[] fullNames;
            string path = persistentDataPath + rumRootFolder + rumDataFolder;
            path += rumSessionDirectoryPrefix + sessionId + "/";

            try
            {
                fullNames = Directory.GetFiles(path, rumSessionDataFilePrefix + "*");
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Load session cached files failed. Path: " + path, e);

                RUMCenter.AddInternalErrorInfo("Load session cached files failed. Path: " + path, e);

                return null;
            }

            if (fullNames.Length == 0)
                return null;

            //-----------[ Reform file name format ]----------------//

            SessionCacheInfo info = new SessionCacheInfo();
            int maxPriority = 0;
            Dictionary<int, Dictionary<int, long>> priorityOrderFileSizes = new Dictionary<int, Dictionary<int, long>>();

            for (int i = 0; i < fullNames.Length; i++)
            {
                if (ParseSessionFileToPriorityAndIndex(fullNames[i], out int priority, out int index))
                {
                    if (priority >= RUMLimitation.maxPriorityLevel)
                    {
                        DeleteInvalidCacheFile(fullNames[i]);
                        continue;
                    }

                    long fileSize = GetFileSize(fullNames[i]);
                    if (fileSize <= 0)
                    {
                        DeleteInvalidCacheFile(fullNames[i]);
                        continue;
                    }

                    if (priority > maxPriority)
                        maxPriority = priority;

                    if (priorityOrderFileSizes.TryGetValue(priority, out Dictionary<int, long> orderFiles))
                    {
                        orderFiles.Add(index, fileSize);
                    }
                    else
                    {
                        priorityOrderFileSizes.Add(priority, new Dictionary<int, long>() { { index, fileSize } });
                    }

                    info.totalSize += (int)fileSize;
                }
                else
                {
                    string filename = GetPathLastSegment(fullNames[i]);
                    string realName = filename.Substring(rumSessionDataFilePrefix.Length);

                    if (realName == rumSessionSendingCacheFileName)
                    {
                        info.sendingCacheSize = (int)GetFileSize(fullNames[i]);

                        if (info.sendingCacheSize > 0)
                            info.totalSize += info.sendingCacheSize;
                        else
                            DeleteInvalidCacheFile(fullNames[i]);
                    }
                    else
                    {
                        DeleteInvalidCacheFile(fullNames[i]);
                    }
                }
            }

            //-----------[ Reform file sizes ]----------------//

            if (info.totalSize == 0)
                return null;
            //if (maxPriority == 0 && priorityOrderFileSizes.ContainsKey(0) == false)
            //    return info;

            info.priorityFilesSize = new int[maxPriority + 1][];

            for (int i = 0; i <= maxPriority; i++)
            {
                if (!priorityOrderFileSizes.TryGetValue(i, out Dictionary<int, long> orderFile))
                {
                    info.priorityFilesSize[i] = null;
                }
                else
                {
                    int maxIndex = 0;
                    foreach (KeyValuePair<int, long> kvp in orderFile)
                    {
                        if (kvp.Key > maxIndex)
                            maxIndex = kvp.Key;
                    }

                    if (maxIndex == 0 && orderFile.ContainsKey(0) == false)
                        info.priorityFilesSize[i] = null;
                    else
                    {
                        info.priorityFilesSize[i] = new int[maxIndex + 1];
                        for (int idx = 0; idx <= maxIndex; idx++)
                        {
                            if (orderFile.TryGetValue(idx, out long s))
                                info.priorityFilesSize[i][idx] = (int)s;
                            else
                                info.priorityFilesSize[i][idx] = 0;
                        }
                    }
                }
            }

            return info;
        }

        //----------------------------------------------//
        //--       Delete Session Cache Folder        --//
        //----------------------------------------------//
        internal static void DeleteSessionCache(string sessionId)
        {
            string path = persistentDataPath + rumRootFolder + rumDataFolder;
            path += rumSessionDirectoryPrefix + sessionId + "/";

            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Delete session cache directory failed. Path: " + path, e);

                RUMCenter.AddInternalErrorInfo("Delete session cache directory failed. Path: " + path, e);
            }
        }

        //----------------------------------------------//
        //--            Session Data File             --//
        //----------------------------------------------//
        internal static byte[] LoadCachedFile(string sessionIdStr, int priority, int idx)
        {
            string path = persistentDataPath + rumRootFolder + rumDataFolder;
            path += rumSessionDirectoryPrefix + sessionIdStr + "/";
            path += rumSessionDataFilePrefix + priority + "_" + idx;

            return LoadBinaryFile(path);
        }

        internal static bool SaveCacheFile(string sessionIdStr, int priority, int idx, byte[] data)
        {
            string path = persistentDataPath + rumRootFolder + rumDataFolder;
            path += rumSessionDirectoryPrefix + sessionIdStr + "/";

            try
            {
                CheckDirectory(path);
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Save cache failed. Session "
                        + sessionIdStr + ", priority " + priority + ", section "
                        + idx + ". Check directory failed.", e);

                return false;
            }

            path += rumSessionDataFilePrefix + priority + "_" + idx;
            return SaveBinaryFile(path, data);
        }

        internal static bool AppendDataToCacheFile(string sessionIdStr, int priority, int idx, byte[] data)
        {
            string path = persistentDataPath + rumRootFolder + rumDataFolder;
            path += rumSessionDirectoryPrefix + sessionIdStr + "/";

            try
            {
                CheckDirectory(path);
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Append data to cache file failed. Session "
                        + sessionIdStr + ", priority " + priority + ", section "
                        + idx + ". Check directory failed.", e);

                return false;
            }

            path += rumSessionDataFilePrefix + priority + "_" + idx;

            try
            {
                using (FileStream fs = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    fs.Write(data, 0, data.Length);
                }
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Append data to cache file failed. Path: " + path, e);

                return false;
            }
            
            return true;
        }

        internal static void DeleteCacheFile(string sessionIdStr, int priority, int idx)
        {
            string path = persistentDataPath + rumRootFolder + rumDataFolder;
            path += rumSessionDirectoryPrefix + sessionIdStr + "/";
            path += rumSessionDataFilePrefix + priority + "_" + idx;

            DeleteFile(path, "Delete cache file failed.", false);
        }

        //----------------------------------------------//
        //--          Session Sending Cache           --//
        //----------------------------------------------//
        internal static Queue<byte[]> LoadSendingCache(string sessionIdStr, out HashSet<byte[]> sendingCache)
        {
            sendingCache = null;
            string path = persistentDataPath + rumRootFolder + rumDataFolder;
            path += rumSessionDirectoryPrefix + sessionIdStr + "/";
            path += rumSessionDataFilePrefix + rumSessionSendingCacheFileName;

            byte[] data = LoadBinaryFile(path);
            if (data == null)
                return null;

            try
            {
                List<object> list = (List<object>)MsgUnpacker.Unpack(data, 0, out int _);
                Queue<byte[]> queue = new Queue<byte[]>();
                sendingCache = new HashSet<byte[]>();

                foreach (object obj in list)
                {
                    byte[] bin = (byte[])obj;
                    queue.Enqueue(bin);
                    sendingCache.Add(bin);
                }

                return queue;
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Load sending cache for session " + sessionIdStr + " failed. MsgPack pack exception.", e);
            }

            return null;
        }

        internal static bool SaveSendingCache(string sessionIdStr, HashSet<byte[]> sendingCache)
        {
            byte[] rawData;

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    MsgPacker.Pack(stream, sendingCache);
                    rawData = stream.ToArray();
                }
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Save sending cache for session " + sessionIdStr + " failed. MsgPack pack exception.", e);

                return false;
            }

            string path = persistentDataPath + rumRootFolder + rumDataFolder;
            path += rumSessionDirectoryPrefix + sessionIdStr + "/";

            try
            {
                CheckDirectory(path);
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Save sending cache for session " + sessionIdStr + " failed. Check directory failed.", e);

                return false;
            }


            path += rumSessionDataFilePrefix + rumSessionSendingCacheFileName;

            return SaveBinaryFile(path, rawData);
        }

        //----------------------------------------------//
        //--              RUM Configs                 --//
        //----------------------------------------------//
        internal static Dictionary<string, int> LoadConfig(out int configVersion, out int maxPriority)
        {
            string path = persistentDataPath + rumRootFolder + rumConfigFilename;
            configVersion = 0;
            maxPriority = 0;

            byte[] data = LoadBinaryFile(path, true);
            if (data == null)
                return null;

            try
            {
                int offset = 0;
                object obj = MsgUnpacker.Unpack(data, offset, out int endOffset);

                configVersion = (int)Convert.ChangeType(obj, TypeCode.Int32);


                offset = endOffset;
                obj = MsgUnpacker.Unpack(data, offset, out endOffset);

                if (obj is Dictionary<object, object> dict)
                {
                    Dictionary<string, int> config = new Dictionary<string, int>();
                    foreach (KeyValuePair<object, object> kvp in dict)
                    {
                        int priority = (int)Convert.ChangeType(kvp.Value, TypeCode.Int32);
                        config.Add((string)Convert.ChangeType(kvp.Key, TypeCode.String), priority);

                        if (priority > maxPriority)
                            maxPriority = priority;
                    }

                    return config;
                }
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Load RUM Event Config file failed. Path: " + path, e);

                RUMCenter.AddInternalErrorInfo("RUM Event Config failed. Path: " + path, e);
            }

            return null;
        }

        internal static bool SaveConfig(Dictionary<string, int> config, int configVersion)
        {
            byte[] rawData;

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    MsgPacker.Pack(stream, configVersion);
                    MsgPacker.Pack(stream, config);
                    rawData = stream.ToArray();
                }
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Save RUM Event Config file failed. MsgPack pack exception.", e);

                RUMCenter.AddInternalErrorInfo("Save RUM Event Config file failed. MsgPack pack exception.", e);

                return false;
            }

            string path = persistentDataPath + rumRootFolder + rumConfigFilename;
            return SaveBinaryFile(path, rawData);
        }
    }
}
