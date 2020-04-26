using System;
using System.IO;
using System.Collections.Generic;
using com.fpnn.msgpack;

namespace com.fpnn.rum
{
    internal class RUMCache
    {
        private SessionCache currentSession;
        private LinkedList<SessionCache> oldSessions;

        //---------[ Constructor ]---------//
        public RUMCache(CoreInfo coreInfo)
        {
            currentSession = new SessionCache(coreInfo.sessionId);
            InitOldSessionCaches();
            LoadOldSessionCacheInfos();
        }

        //---------[ Initialization ]---------//
        private void InitOldSessionCaches()
        {
            oldSessions = new LinkedList<SessionCache>();

            string[] sessions = RUMFile.GetCachedSessions();
            if (sessions == null || sessions.Length == 0)
                return;

            HashSet<string> invalidSessionIds = new HashSet<string>();
            SortedDictionary<long, string> orderDict = new SortedDictionary<long, string>();

            for (int i = 0; i < sessions.Length; i++)
            {
                try
                {
                    long sid = Int64.Parse(sessions[i]);
                    orderDict.Add(sid, sessions[i]);
                }
                catch (Exception e)
                {
                    if (RUMCenter.errorRecorder != null)
                        RUMCenter.errorRecorder.RecordError("Convert cached session failed. Session: " + sessions[i], e);

                    RUMCenter.AddInternalErrorInfo("Convert cached session failed. Session: " + sessions[i], e);

                    invalidSessionIds.Add(sessions[i]);
                }
            }

            foreach (KeyValuePair<long, string> kvp in orderDict)
            {
                SessionCache sessionCache = new SessionCache(kvp.Key, kvp.Value);
                oldSessions.AddFirst(sessionCache);
            }

            foreach (string session in invalidSessionIds)
            {
                SessionCache sessionCache = new SessionCache(0, session);
                oldSessions.AddLast(sessionCache);
            }
        }

        private void LoadOldSessionCacheInfos()
        {
            HashSet<LinkedListNode<SessionCache>> invalidNodes = new HashSet<LinkedListNode<SessionCache>>();

            LinkedListNode<SessionCache> node = oldSessions.First;
            while (node != null)
            {
                SessionCacheInfo info = node.Value.LoadCacheInfo();
                if (info == null)
                {
                    invalidNodes.Add(node);
                }
                else if (info.totalSize == 0)
                {
                    invalidNodes.Add(node);
                }

                node = node.Next;
            }

            foreach (LinkedListNode<SessionCache> lnode in invalidNodes)
            {
                lnode.Value.DeleteCache();
                oldSessions.Remove(lnode);
            }
        }

        //---------[ unclassified functions ]---------//
        public void AddEvent(int priority, byte[] binaryData)
        {
            currentSession.AddEvent(priority, binaryData);
        }

        public void UpdatePriority(int maxPriority)
        {
            currentSession.UpdateMaxPriority(maxPriority);
        }

        public void GenNewSession(long sessionId)
        {
            oldSessions.AddFirst(currentSession);
            currentSession = new SessionCache(sessionId);
        }

        public void Destroy()
        {
            currentSession.Destroy();
            if (oldSessions.Count > 0)
            {
                SessionCache header = oldSessions.First.Value;
                header.Destroy();
            }
        }

        //---------[ Send Events ]---------//
        public long SendEvents(CoreInfo coreInfo, TCPClient client, long quota)
        {
            long tokenByte = 0;
            
            bool status = currentSession.SendEvents(coreInfo, client, ref quota, ref tokenByte);

            List<LinkedListNode<SessionCache>> emptySessions = new List<LinkedListNode<SessionCache>>();
            LinkedListNode<SessionCache> node = oldSessions.First;
            while (status && quota > 0 && node != null)
            {
                long old = tokenByte;
                
                status = node.Value.SendEvents(coreInfo, client, ref quota, ref tokenByte);
                if (status && quota > 0 && tokenByte - old == 0)
                {
                    emptySessions.Add(node);
                }

                node = node.Next;
            }

            foreach (LinkedListNode<SessionCache> lnode in emptySessions)
            {
                lnode.Value.DeleteCache();
                oldSessions.Remove(lnode);
            }

            return tokenByte;
        }

        //---------[ Resources Control ]---------//
        public void CheckResourceLimitation()
        {
            long memoryQuota = RUMLimitation.maxMemoryCachedSize;
            long diskQuota = RUMLimitation.maxDiskCachedSize;

            currentSession.CheckResourceQuota(ref memoryQuota, ref diskQuota, true);

            List<LinkedListNode<SessionCache>> dropSessions = new List<LinkedListNode<SessionCache>>();
            LinkedListNode<SessionCache> node = oldSessions.First;
            while (node != null)
            {
                if (diskQuota > 0)
                    node.Value.CheckResourceQuota(ref memoryQuota, ref diskQuota, false);
                else
                    dropSessions.Add(node);

                node = node.Next;
            }

            foreach (LinkedListNode<SessionCache> droppedNode in dropSessions)
            {
                droppedNode.Value.DeleteCache();
                oldSessions.Remove(droppedNode);
            }
        }

        public void SyncMemoryToDisk()
        {
            currentSession.SyncToDisk();

            foreach (SessionCache session in oldSessions)
                session.SyncToDisk();
        }
    }
}

