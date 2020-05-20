using System;
using System.IO;
using System.Collections.Generic;
using com.fpnn.msgpack;
using com.fpnn.proto;

namespace com.fpnn.rum
{
    internal class EventsCache
    {
        public readonly int fileIdx;
        public Queue<byte[]> cache;
        public int cacheSize;       //-- only in memary
        public int fileSize;

        public EventsCache(int index)       //-- For current session
        {
            fileIdx = index;
            cache = new Queue<byte[]>();
            cacheSize = 0;
            fileSize = 0;
        }

        public EventsCache(int index, int dataSize)    //-- For old sessions
        {
            fileIdx = index;
            cache = null;
            cacheSize = 0;
            fileSize = dataSize;
        }

        public void AddData(byte[] data)
        {
            cache.Enqueue(data);
            cacheSize += data.Length;
        }

        private byte[] DumpCache()      //-- MUST catch exception.
        {
            using (MemoryStream stream = new MemoryStream())
            {
                MsgPacker.Pack(stream, cache);
                return stream.ToArray();
            }
        }

        public void LoadFromFile(string sessionIdStr, int priority)
        {
            byte[] data = RUMFile.LoadCachedFile(sessionIdStr, priority, fileIdx);
            if (data != null)
            {
                Queue<byte[]> oldQueue = cache;
                cache = new Queue<byte[]>();

                try
                {
                    int offset = 0;
                    while (offset < data.Length)
                    {
                        List<object> list = (List<object>)MsgUnpacker.Unpack(data, offset, out int endOffset);

                        foreach (object obj in list)
                        {
                            byte[] binary = (byte[])obj;
                            cache.Enqueue(binary);
                            cacheSize += binary.Length;
                        }

                        offset = endOffset;
                    }
                }
                catch (Exception e)
                {
                    if (RUMCenter.errorRecorder != null)
                        RUMCenter.errorRecorder.RecordError("Load session cache file failed. MsgPack unpack exception. Sid: "
                            + sessionIdStr + ", priority: " + priority + ", index: " + fileIdx, e);
                }

                if (oldQueue != null && oldQueue.Count > 0)
                    foreach (byte[] bin in oldQueue)
                        cache.Enqueue(bin);
            }
        }

        public void SaveToFile(string sessionIdStr, int priority)
        {
            if (cache == null || cache.Count == 0)
                return;

            byte[] data;
            try
            {
                data = DumpCache();
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Save data to session cache file failed. Sid: "
                        + sessionIdStr + ", priority: " + priority + ", index: " + fileIdx, e);

                return;
            }

            if (RUMFile.SaveCacheFile(sessionIdStr, priority, fileIdx, data))
            {
                fileSize = cacheSize;
            }
        }

        public void AppendToFile(string sessionIdStr, int priority)
        {
            if (cache == null || cache.Count == 0)
                return;

            byte[] data;
            try
            {
                data = DumpCache();
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Append data to session cache file failed. Sid: "
                        + sessionIdStr + ", priority: " + priority + ", index: " + fileIdx, e);

                return;
            }

            if (RUMFile.AppendDataToCacheFile(sessionIdStr, priority, fileIdx, data))
            {
                fileSize += cacheSize;
                cacheSize = 0;
                cache.Clear();
            }
        }

        public void PrepareSendData(ref List<byte[]> binaryList, ref long quota, ref long tokenBytes)
        {
            while (cache.Count > 0 && quota > 0)
            {
                byte[] data = cache.Peek();
                if (!RUMLimitation.CheckSendVolume(data, quota))
                {
                    quota = 0;
                    return;
                }

                data = cache.Dequeue();
                cacheSize -= data.Length;

                binaryList.Add(data);
                tokenBytes += data.Length;
                quota -= data.Length;
            }
        }

        public void Enable(string sessionIdStr, int priority)
        {
            LoadFromFile(sessionIdStr, priority);
        }

        public void Disable(string sessionIdStr, int priority, bool isHeader)
        {
            if (isHeader)
                SaveToFile(sessionIdStr, priority);
            else
                AppendToFile(sessionIdStr, priority);

            cacheSize = 0;
            cache = null;
        }
    }

    //===============[ PriorityCache ]================//
    internal class PriorityCache
    {
        private string sessionIdStr;
        private int priority;

        private LinkedList<EventsCache> sections;
        private EventsCache header;
        private EventsCache tail;
        private long totalSize;

        public PriorityCache(string sessionIdStr, int priority)         //-- For current session
        {
            this.sessionIdStr = sessionIdStr;
            this.priority = priority;

            sections = new LinkedList<EventsCache>();
            header = new EventsCache(0);
            tail = header;

            sections.AddFirst(header);
            totalSize = 0;
        }

        public PriorityCache(string sessionIdStr, int priority, int[] sizeList)     //-- For old sessions
        {
            this.sessionIdStr = sessionIdStr;
            this.priority = priority;

            sections = new LinkedList<EventsCache>();
            if (sizeList == null)
            {
                header = new EventsCache(0);
                tail = header;

                sections.AddFirst(header);
                totalSize = 0;
            }
            else
            {
                Enable(sizeList);
            }
        }

        public void SaveToFile()
        {
            header.SaveToFile(sessionIdStr, priority);
            if (header != tail)
                tail.AppendToFile(sessionIdStr, priority);
        }

        public void AddData(byte[] data)
        {
            tail.AddData(data);
            totalSize += data.Length;

            if (tail.cacheSize + tail.fileSize >= RUMLimitation.maxFileSizeForCacheFile)
            {
                if (tail != header)
                    tail.Disable(sessionIdStr, priority, false);

                int newIdx = tail.fileIdx + 1;
                tail = new EventsCache(newIdx);
                sections.AddLast(tail);
            }
        }

        public void PrepareSendData(ref List<byte[]> binaryList,ref long quota, ref long tokenBytes, ref List<int> emptyFileIndex)
        {
            if (header.cache == null)
                header.Enable(sessionIdStr, priority);

            long diff = tokenBytes;
            while (quota > 0 && header.cacheSize > 0)
            {
                header.PrepareSendData(ref binaryList, ref quota, ref tokenBytes);
                if (header.cacheSize == 0)
                {
                    if (header != tail)
                    {
                        emptyFileIndex.Add(header.fileIdx);
                        sections.RemoveFirst();
                        header = sections.First.Value;

                        header.Enable(sessionIdStr, priority);
                    }
                }
            }

            diff = tokenBytes - diff;
            totalSize -= diff;
        }

        public void Disable()
        {
            if (header != tail)
                header.Disable(sessionIdStr, priority, true);
        }

        public long TotalSize()
        {
            return totalSize;
        }

        public long MemorySize()
        {
            if (header == tail)
                return header.cacheSize;
            else
                return header.cacheSize + tail.cacheSize;
        }

        private void Enable(int[] dataSize)
        {
            EventsCache node;
            totalSize = 0;

            for (int i = 0; i < dataSize.Length; i++)
            {
                node = new EventsCache(i, dataSize[i]);
                totalSize += dataSize[i];

                sections.AddLast(node);
            }

            header = sections.First.Value;
            tail = sections.Last.Value;
        }

        public void DropSecions(ref long diskQuota, ref long memoryQuota)
        {
            LinkedListNode<EventsCache> node = sections.First;
            while (node != null && diskQuota < 0)
            {
                memoryQuota += node.Value.cacheSize;
                long sectionSize = node.Value.cacheSize + node.Value.fileSize;
                diskQuota += sectionSize;
                totalSize -= sectionSize;

                RUMFile.DeleteCacheFile(sessionIdStr, priority, node.Value.fileIdx);
                sections.RemoveFirst();
                node = sections.First;
            }

            if (node == null)
            {
                header = new EventsCache(0);
                tail = header;

                sections.AddFirst(header);
                totalSize = 0;
            }
            else
            {
                header = node.Value;
            }
        }

        public void DropAllSections()
        {
            foreach (EventsCache cache in sections)
                RUMFile.DeleteCacheFile(sessionIdStr, priority, cache.fileIdx);

            sections.Clear();

            header = new EventsCache(0);
            tail = header;

            sections.AddFirst(header);
            totalSize = 0;
        }
    }

    //===============[ SessionCacheInfo ]================//
    internal class SessionCacheInfo
    {
        public int sendingCacheSize;
        public int[][] priorityFilesSize;
        public int totalSize;

        public SessionCacheInfo()
        {
            sendingCacheSize = 0;
            priorityFilesSize = null;
            totalSize = 0;
        }
    }

    //===============[ SessionCache ]================//
    internal class SessionCache
    {
        private readonly long sessionId;
        private readonly string sessionIdStr;

        private int maxPriority;
        private PriorityCache[] priorityList;

        private SessionCacheInfo cacheInfo;

        //-- Only the following three fileds in multi-threads.
        private HashSet<byte[]> sendingCache;    //-- need to be locked
        private int sendingCacheSize;            //-- need to be locked
        private Queue<byte[]> sentFailedCache;   //-- need to be locked

        //-- For new session
        public SessionCache(long sid)
        {
            sessionId = sid;
            sessionIdStr = sid.ToString();

            maxPriority = 3;
            priorityList = new PriorityCache[maxPriority + 1];
            for (int i = 0; i <= maxPriority; i++)
                priorityList[i] = new PriorityCache(sessionIdStr, i);

            cacheInfo = null;

            sendingCache = new HashSet<byte[]>();
            sendingCacheSize = 0;

            sentFailedCache = new Queue<byte[]>();
        }

        //-- For old & cached session
        public SessionCache(long sid, string sidstr)
        {
            sessionId = sid;
            sessionIdStr = sidstr;

            maxPriority = 0;
            priorityList = null;

            cacheInfo = null;

            sendingCache = null;
            sendingCacheSize = 0;
            sentFailedCache = null;
        }

        public void AddEvent(int priority, byte[] data)
        {
            if (priority > maxPriority)
                priority = maxPriority;

            priorityList[priority].AddData(data);
        }

        public void UpdateMaxPriority(int maxPri)
        {
            if (maxPriority >= maxPri)
                return;

            PriorityCache[] tmp = new PriorityCache[maxPri + 1];
            for (int i = 0; i <= maxPriority; i++)
                tmp[i] = priorityList[i];

            for (int i = maxPriority + 1; i <= maxPri; i++)
                tmp[i] = new PriorityCache(sessionIdStr, i);

            priorityList = tmp;
            maxPriority = maxPri;
        }

        public SessionCacheInfo LoadCacheInfo()
        {
            cacheInfo = RUMFile.LoadCacheInfo(sessionIdStr);
            return cacheInfo;
        }

        private void FetchDataFromSentFailedQueue(ref List<byte[]> binaryList, ref long quota, ref long tokenByte)
        {
            lock (this)
            {
                while (quota > 0 && sentFailedCache.Count > 0)
                {
                    byte[] data = sentFailedCache.Peek();
                    if (!RUMLimitation.CheckSendVolume(data, quota))
                    {
                        quota = 0;
                        return;
                    }

                    data = sentFailedCache.Dequeue();
                    binaryList.Add(data);
                    tokenByte += data.Length;
                    quota -= data.Length;
                }
            }
        }

        public bool SendEvents(CoreInfo coreInfo, TCPClient client, ref long quota, ref long tokenByte)
        {
            if (cacheInfo != null)
                Enable();

            List<byte[]> binaryList = new List<byte[]>();
            Dictionary<int, List<int>> emptyFileDict = null;

            FetchDataFromSentFailedQueue(ref binaryList, ref quota, ref tokenByte);
            if (quota > 0)
            {
                emptyFileDict = new Dictionary<int, List<int>>();

                for (int i = 0; i <= maxPriority; i++)
                {
                    List<int> emptyFileIndex = new List<int>();
                    priorityList[i].PrepareSendData(ref binaryList, ref quota, ref tokenByte, ref emptyFileIndex);

                    if (emptyFileIndex.Count > 0)
                        emptyFileDict.Add(i, emptyFileIndex);

                    if (quota <= 0)
                        break;
                }
            }

            lock (this)
            {
                foreach (byte[] data in binaryList)
                {
                    if (sendingCache.Add(data))
                        sendingCacheSize += data.Length;
                }
            }

            bool status = SendEventData(coreInfo, client, binaryList);

            if (emptyFileDict != null)
            {
                foreach (KeyValuePair<int, List<int>> kvp in emptyFileDict)
                {
                    foreach (int fileIdx in kvp.Value)
                    {
                        RUMFile.DeleteCacheFile(sessionIdStr, kvp.Key, fileIdx);
                    }
                }
            }

            return status;
        }

        private bool SendEventData(CoreInfo coreInfo, TCPClient client, List<byte[]> binaryList)
        {
            if (binaryList.Count == 0)
                return true;

            List<Dictionary<object, object>> events = new List<Dictionary<object, object>>();
            foreach (byte[] data in binaryList)
            {
                try
                {
                    Dictionary<Object, Object> dict = MsgUnpacker.Unpack(data);
                    if (dict != null)
                    {
                        if (!dict.ContainsKey("ts"))
                        {
                            object eidObj = dict["eid"];
                            long eid = 0;

                            try
                            {
                                ulong v = (ulong)eidObj;
                                eid = (long)v;
                            }
                            catch (Exception)
                            {
                                eid = (long)eidObj;
                            }

                            dict.Add("ts", coreInfo.AdjustTimestamp(eid));
                        }
                        
                        events.Add(dict);
                    }
                    else
                        sendingCache.Remove(data);
                }
                catch (Exception e)
                {
                    sendingCache.Remove(data);

                    if (RUMCenter.errorRecorder != null)
                        RUMCenter.errorRecorder.RecordError("Unpack event for sending failed. MsgPack unpack exception. Sid: " + sessionIdStr, e);
                }
            }

            Quest quest = new Quest("adds");
            quest.Param("pid", coreInfo.pid);
            quest.Param("events", events);

            string sign = coreInfo.GenSign(out long salt);
            quest.Param("sign", sign);
            quest.Param("salt", salt);

            if (RealSend(client, quest, binaryList, true))
                return true;

            return RealSend(client, quest, binaryList, false);     //-- retry.
        }

        private bool RealSend(TCPClient client, Quest quest, List<byte[]> binaryList, bool firstSend)
        {
            return client.SendQuest(quest, (Answer answer, int errorCode) => {
                if (errorCode == ErrorCode.FPNN_EC_OK)
                {
                    ClearSentData(binaryList);
                    return;
                }

                if (firstSend && (errorCode == ErrorCode.FPNN_EC_CORE_CONNECTION_CLOSED
                    || errorCode == ErrorCode.FPNN_EC_CORE_INVALID_CONNECTION))
                {
                    RealSend(client, quest, binaryList, false);
                    return;
                }

                CacheFailedSentData(binaryList);
            });
        }

        private void CacheFailedSentData(List<byte[]> binaryList)
        {
            lock (this)
            {
                foreach (byte[] data in binaryList)
                    sentFailedCache.Enqueue(data);
            }
        }

        private void ClearSentData(List<byte[]> binaryList)
        {
            lock (this)
            {
                foreach (byte[] data in binaryList)
                {
                    sendingCache.Remove(data);
                    sendingCacheSize -= data.Length;
                }
            }
        }

        private void Enable()
        {
            if (cacheInfo == null)
                return;

            //-- For priority caches
            maxPriority = cacheInfo.priorityFilesSize.Length - 1;
            priorityList = new PriorityCache[maxPriority + 1];
            for (int i = 0; i < maxPriority + 1; i++)
            {
                priorityList[i] = new PriorityCache(sessionIdStr, i, cacheInfo.priorityFilesSize[i]);
            }

            //-- For sending caches
            sentFailedCache = RUMFile.LoadSendingCache(sessionIdStr, out sendingCache);
            sendingCacheSize = cacheInfo.sendingCacheSize;

            if (sendingCache == null)
                sendingCache = new HashSet<byte[]>();

            if (sentFailedCache == null)
                sentFailedCache = new Queue<byte[]>();

            //-- clean
            cacheInfo = null;
        }

        public void CheckResourceQuota(ref long memoryQuota, ref long diskQuota, bool isCurrentSession)
        {
            if (isCurrentSession)
                CheckResourceQuotaForCurrentSession(ref memoryQuota, ref diskQuota);
            else
                CheckResourceQuotaForOldSession(ref memoryQuota, ref diskQuota);
        }

        private void CheckResourceQuotaForCurrentSession(ref long memoryQuota, ref long diskQuota)
        {
            for (int i = 0; i <= maxPriority; i++)
            {
                if (diskQuota <= 0)
                {
                    priorityList[i].DropAllSections();
                }
                else
                {
                    if (memoryQuota <= 0)
                        priorityList[i].Disable();

                    long memSize = priorityList[i].MemorySize();
                    memoryQuota -= memSize;

                    diskQuota -= priorityList[i].TotalSize();
                    if (diskQuota < 0)
                        priorityList[i].DropSecions(ref diskQuota, ref memoryQuota);
                }
            }
        }

        private void CheckResourceQuotaForOldSession(ref long memoryQuota, ref long diskQuota)
        {
            if (cacheInfo == null)
            {
                CheckResourceQuotaForCurrentSession(ref memoryQuota, ref diskQuota);
            }
            else
            {
                diskQuota -= cacheInfo.totalSize;
            }
        }

        public void SyncToDisk()
        {
            if (cacheInfo == null)
            {
                for (int i = 0; i <= maxPriority; i++)
                    priorityList[i].SaveToFile();

                lock (this)
                    RUMFile.SaveSendingCache(sessionIdStr, sendingCache);
            }
        }

        public void DeleteCache()
        {
            RUMFile.DeleteSessionCache(sessionIdStr);
        }

        public void Destroy()
        {
            SyncToDisk();
        }
    }
}

