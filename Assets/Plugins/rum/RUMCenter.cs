using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;
using com.fpnn.msgpack;
using com.fpnn.proto;

namespace com.fpnn.rum
{
    internal static class Crypto
    {
        public static string GetMD5(string str, bool upper)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(str);
            return GetMD5(inputBytes, upper);
        }

        public static string GetMD5(byte[] bytes, bool upper)
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(bytes);
            string f = "x2";

            if (upper)
            {
                f = "X2";
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString(f));
            }

            return sb.ToString();
        }
    }

    internal class CoreInfo
    {
        public string rumId;
        public Int64 sessionId;

        public int pid;
        private string uid;
        private string secretKey;
        private long delaySeconds;

        public CoreInfo(RUMConfig config)
        {
            rumId = SystemInfo.deviceUniqueIdentifier;      //-- Init rumId with reserve id. It will be rewriten when RumId file exist.
            sessionId = ClientEngine.GetCurrentMilliseconds();

            pid = config.pid;
            uid = config.uid;
            secretKey = config.secretKey;
            delaySeconds = 0;
        }

        public long ConfigForNewSession()
        {
            long newSessionId = ClientEngine.GetCurrentMilliseconds();
            Interlocked.Exchange(ref sessionId, newSessionId);
            return newSessionId;
        }

        public string GenSign(out long salt)
        {
            salt = ClientEngine.GetCurrentSeconds() - Interlocked.Read(ref delaySeconds);

            string src = pid.ToString() + ":" + secretKey + ":" + salt;

            return Crypto.GetMD5(src, true);
        }

        public long GetAdjustedTimestamp()
        {
            return ClientEngine.GetCurrentSeconds() - Interlocked.Read(ref delaySeconds);
        }

        public void ConfigDelay(long delay)
        {
            Interlocked.Exchange(ref delaySeconds, delay);
        }

        public string Uid
        {
            get
            {
                lock (this)
                    return uid;
            }
            set
            {
                bool trigger = false;

                lock (this)
                {
                    trigger = (uid == null || uid.Length == 0) && value != null && value.Length > 0;
                    uid = value;
                }

                if (trigger)
                {
                    Dictionary<string, object> eventDict = new Dictionary<string, object>()
                    {
                        { "type", "uid" },
                        { "uid", value },
                    };

                    RUMCenter.Instance.AddEvent("append", eventDict);
                }
            }
        }
    }

    internal class OnlineStatus
    {
        private bool online;
        private long onlineMS;
        private long offlineMS;
        private long lastSwitchMS;

        public OnlineStatus()
        {
            Reset(false);
        }

        public void Reset(bool onlineStatus)
        {
            lock (this)
            {
                online = onlineStatus;
                onlineMS = 0;
                offlineMS = 0;
                lastSwitchMS = ClientEngine.GetCurrentMilliseconds();
            }
        }

        public void Online(bool onlineStatus)
        {
            long now = ClientEngine.GetCurrentMilliseconds();

            lock (this)
            {
                if (online == onlineStatus)
                    return;

                if (online)
                    onlineMS += now - lastSwitchMS;
                else
                    offlineMS += now - lastSwitchMS;

                online = onlineStatus;
                lastSwitchMS = now;
            }
        }

        public void EnterBackground()
        {
            long now = ClientEngine.GetCurrentMilliseconds();

            lock (this)
            {
                if (online)
                    onlineMS += now - lastSwitchMS;
                else
                    offlineMS += now - lastSwitchMS;

                lastSwitchMS = now;
            }
        }

        public void EnterFrontground()
        {
            lock (this)
            {
                lastSwitchMS = ClientEngine.GetCurrentMilliseconds();
            }
        }

        public void ReportStatus()      //-- Only calling this function in frontground.
        {
            Dictionary<string, object> eventDict = new Dictionary<string, object>();
            eventDict.Add("type", "onlinetime");

            lock (this)
            {
                eventDict.Add("online", onlineMS / 1000);
                eventDict.Add("offline", offlineMS / 1000);
            }

            RUMCenter.Instance.AddEvent("append", eventDict);
        }
    }

    internal class UsingStat
    {
        private readonly long SDKCreatedMS;                   //-- Readonly in logic.
        private long lastEnterBackgroundMS;                   //-- If in FG, this is zero.
        private long lastEnterFrontgroundMS;                  //-- If in BG, this is zero.
        private long accumulatedPlayingMS;

        public UsingStat()
        {
            SDKCreatedMS = ClientEngine.GetCurrentMilliseconds();
            lastEnterBackgroundMS = 0;
            lastEnterFrontgroundMS = SDKCreatedMS;
            accumulatedPlayingMS = 0;
        }

        public void EnterBackground()
        {
            long now = ClientEngine.GetCurrentMilliseconds();

            lock (this)
            {
                accumulatedPlayingMS += now - lastEnterFrontgroundMS;
                lastEnterFrontgroundMS = 0;
                lastEnterBackgroundMS = now;
            }
        }

        public void EnterFrontground()
        {
            long now = ClientEngine.GetCurrentMilliseconds();

            lock (this)
            {
                lastEnterBackgroundMS = 0;
                lastEnterFrontgroundMS = now;
            }
        }

        public long CurrentUsedMS()
        {
            long now = ClientEngine.GetCurrentMilliseconds();

            lock (this)
            {
                if (lastEnterFrontgroundMS == 0)
                    return accumulatedPlayingMS;

                return accumulatedPlayingMS + (now - lastEnterFrontgroundMS);
            }
        }

        public long EnterBackgroundMS()
        {
            lock (this)
                return lastEnterBackgroundMS;
        }

        public void Reset()
        {
            long now = ClientEngine.GetCurrentMilliseconds();

            lock (this)
            {
                accumulatedPlayingMS = 0;

                if (lastEnterBackgroundMS != 0)
                    lastEnterBackgroundMS = now;
                else
                    lastEnterFrontgroundMS = now;
            }
        }
    }

    internal class FPSStatus
    {
        private List<float> timeSinceStartup;
        private List<float> fps;

        private float timeBenchmark;
        private float lastTimeRecord;
        private int lastFrameCount;

        private volatile bool requireReset;

        public FPSStatus()
        {
            timeSinceStartup = new List<float>();
            fps = new List<float>();

            timeBenchmark = 0;
            lastTimeRecord = 0;
            lastFrameCount = 0;

            requireReset = false;
        }

        public void Reset()
        {
            requireReset = true;
        }

        private void RealReset()
        {
            timeSinceStartup.Clear();
            fps.Clear();

            lastTimeRecord = Time.realtimeSinceStartup;
            lastFrameCount = Time.frameCount;

            timeBenchmark = lastTimeRecord;
        }

        public void EnterFrontground()
        {
            lock (this)
            {
                if (requireReset)
                {
                    RealReset();
                    requireReset = false;
                }

                lastTimeRecord = Time.realtimeSinceStartup;
                lastFrameCount = Time.frameCount;
            }
        }

        public void EnterBackground()
        {
            if (requireReset)
            {
                lock (this)
                {
                    RealReset();
                    requireReset = false;
                }
            }
        }

        public void Gather()
        {
            const float minGatherInterval = 0.5f;
            const int reportCount = 120;            //-- means about 2 mintues.

            float rss = Time.realtimeSinceStartup;
            int frameCount = Time.frameCount;

            List<float> timsSeq = null;
            List<float> fpsSeq = null;

            lock (this)
            {
                if (requireReset)
                {
                    RealReset();
                    requireReset = false;
                    return;
                }

                float timeDelta = rss - lastTimeRecord;
                int frameDelta = frameCount - lastFrameCount;

                if (timeDelta < minGatherInterval)
                    return;

                float rate;
                try
                {
                    rate = frameDelta / timeDelta;
                }
                catch (Exception)
                {
                    return;
                }

                if (float.IsNaN(rate) || float.IsInfinity(rate))
                    return;

                timeSinceStartup.Add(rss - timeBenchmark);
                fps.Add(rate);
                
                lastTimeRecord = rss;
                lastFrameCount = frameCount;

                if (fps.Count >= reportCount)
                {
                    timsSeq = timeSinceStartup;
                    fpsSeq = fps;

                    timeSinceStartup = new List<float>();
                    fps = new List<float>();
                }
            }

            if (fpsSeq != null)
                Report(timsSeq, fpsSeq);
        }

        private void Report(List<float> secFromBeginSeq, List<float> fpsSeq)
        {
            Dictionary<string, object> eventDict = new Dictionary<string, object>();
            eventDict.Add("type", "unity_fps");
            eventDict.Add("sec", secFromBeginSeq);
            eventDict.Add("fps", fpsSeq);

            RUMCenter.Instance.AddEvent("info", eventDict);
        }
    }

    internal class PlatformInfo
    {
        public string appVersion;
        public string systemLanguage;
        public string deviceModel;
        public string operatingSystem;
        public string installMode;
        public int screenHeight;
        public int screenWidth;

        public void Init(string appVer)
        {
            appVersion = Normalize(appVer);
            systemLanguage = Normalize(Application.systemLanguage.ToString());
            deviceModel = Normalize(SystemInfo.deviceModel);
            operatingSystem = Normalize(SystemInfo.operatingSystem);
            installMode = Normalize(Application.installMode.ToString());

            screenHeight = Screen.height;
            screenWidth = Screen.width;
        }

        private string Normalize(string value)
        {
            if (value == null)
                return null;

            if (value.Length == 0)
                return null;

            return value;
        }
    }

    internal class EventsSendState
    {
        public const int sendIntervalMilliseconds = 200;
        public int sendFrequency;
        public long bandwidthInBPS;
        public long statSecond;
        public long currSentBytes;
        public int currSentTimes;

        public EventsSendState(long bwInBps)
        {
            sendFrequency = 1000 / sendIntervalMilliseconds;
            if (sendFrequency < 1)
                sendFrequency = 1;

            bandwidthInBPS = bwInBps;
            statSecond = 0;
            currSentBytes = 0;
            currSentTimes = 0;
        }

        public long getGuota(long nowSeconds)
        {
            if (nowSeconds != statSecond)
            {
                statSecond = nowSeconds;
                currSentBytes = 0;
                currSentTimes = 0;
            }

            long remain = bandwidthInBPS - currSentBytes;
            if (remain <= 0)
                return 0;

            int remainCount = sendFrequency - currSentTimes;
            if (remainCount > 0)
            {
                return remain / remainCount;
            }
            else if (remainCount == 0)
                return remain;
            else
                return 0;
        }

        public void Adjust(long consumedBytes)
        {
            currSentBytes += consumedBytes;
            currSentTimes += 1;
        }
    }

    public class RUMCenter : Singleton<RUMCenter>
    {
        private volatile bool inited;
        private volatile bool stopped;
        private object interLocker;
        private Thread routineThread;

        private RUMCache cache;
        internal CoreInfo coreInfo;
        private PlatformInfo platformInfo;
        private RUMClientHolder clientHolder;

        private FPSStatus fpsStatus;
        private UsingStat usingStat;
        private OnlineStatus onlineStatus;
        internal static common.ErrorRecorder errorRecorder;

        //------- Network & Misc Status --------//
        private NetworkReachability networkReachability;
        private volatile bool networkReachable;
        private long lastPingCostMilliseconds;

        private EventsSendState eventsSendState;

        //------- Temp Event Caches ---------//
        private int eventIdBase;
        private int configVersion;
        private Dictionary<string, int> priorityDict;
        private List<Dictionary<string, object>> internalErrors;
        private Dictionary<string, Queue<byte[]>> tempEventCache;
        private Dictionary<string, Queue<byte[]>> primaryEventsCache;

        //------- Special Events ----------//
        private volatile bool requireNewSession;
        private volatile bool requireReportSystemInfo;
        private volatile bool requireUpdateConfig;
        private Action updateConfigAction;

        //------- readonly configs ----------//
        private bool autoRecordUnityDebugErrorLog;

        //------- BG &FG Status ---------//
        private bool _isPause;
        private bool _isFocus;
        private volatile bool _isBackground;

        void OnEnable()
        {
            _isPause = false;
            _isFocus = true;
            _isBackground = false;

            Application.lowMemory += OnLowMemory;
            Application.logMessageReceivedThreaded += LogUnityMessage;

            StartCoroutine(PerSecondCoroutine());
        }

        public RUMCenter()
        {
            clientHolder = new RUMClientHolder();

            fpsStatus = new FPSStatus();
            usingStat = new UsingStat();
            onlineStatus = new OnlineStatus();

            inited = false;
            interLocker = new object();
            coreInfo = null;
            eventIdBase = 0;
            configVersion = 0;
            priorityDict = null;
            internalErrors = new List<Dictionary<string, object>>();
            tempEventCache = new Dictionary<string, Queue<byte[]>>();
            primaryEventsCache = new Dictionary<string, Queue<byte[]>>();

            requireReportSystemInfo = false;
            autoRecordUnityDebugErrorLog = false;
        }

        private long GenEventId()
        {
            /*
             * 这里有个 defect，当 eventIdBase 溢出后，成为负数，会导致 eid 回退变小。
             * 但假设客户端 1 秒生成 1000 个，也就是 1 毫秒生成 1 个，进程需要持续运行4年才会触发该 defect。
             * 因此不打算修改。
             */
            return (ClientEngine.GetCurrentSeconds() << 32) + Interlocked.Increment(ref eventIdBase);
        }

        public void StartNewSession()
        {
            requireNewSession = true;
        }
        private void GenNewSession()
        {
            DumpEventCache();
            long newSessionId = coreInfo.ConfigForNewSession();
            cache.GenNewSession(newSessionId);

            GenOpenEvent();
        }

        internal void Init(RUMConfig config)
        {
            if (inited)
                return;

            lock (interLocker)
            {
                if (inited)
                    return;

                autoRecordUnityDebugErrorLog = config.autoRecordUnityDebugErrorLog;

                stopped = false;
                errorRecorder = config.errorRecorder;
                networkReachability = Application.internetReachability;
                networkReachable = networkReachability != NetworkReachability.NotReachable;

                onlineStatus.Reset(networkReachable);

                RUMLimitation.Init(config.cache);
                RUMFile.Init();

                coreInfo = new CoreInfo(config);
                platformInfo = new PlatformInfo();
                platformInfo.Init(config.appVersion);

                routineThread = new Thread(RoutineFunc)
                {
                    Name = "RUM.RUMCenter.RoutineThread",
                    IsBackground = false
                };
                routineThread.Start(config);

                inited = true;
            }

            CheckCrashReport(config.autoClearCrashReports);
        }

        private void SecondaryInit(RUMConfig config)
        {
            lastPingCostMilliseconds = 0;

            RUMFile.InitDirectories(coreInfo);
            RUMFile.RetrieveRumId(coreInfo);

            RUMCache tmp = new RUMCache(coreInfo);
            lock (interLocker)
            {
                cache = tmp;
            }
            GenOpenEvent();

            Dictionary<string, int> eventConfig = RUMFile.LoadConfig(out configVersion, out int maxPriority);
            if (eventConfig == null)
                eventConfig = BuildDefaultConfigMap(out maxPriority);

            UpdateConfig(eventConfig, maxPriority);
            eventsSendState = new EventsSendState(config.network.bandwidthInKBPS * 1024);

            requireNewSession = false;
            requireReportSystemInfo = true;
            requireUpdateConfig = false;
            updateConfigAction = null;

            DumpPrimaryEventsCache();
            clientHolder.Init(config);
        }

        private void UpdateConfig(Dictionary<string, int> eventConfig, int maxPriority)
        {
            cache.UpdatePriority(maxPriority);
            lock (interLocker)
            {
                priorityDict = eventConfig;
            }
        }

        private Dictionary<string, int> BuildDefaultConfigMap(out int maxPriority)
        {
            Dictionary<string, int> config = new Dictionary<string, int>()
            {
                { "open", 0 },
                { "nwswitch", 1 },
                { "bg", 1 },
                { "fg", 1 },
                { "crash", 1 },
                { "append", 1 },
                { "http", 2 },
                { "error", 2 },
                { "warn", 2 },
                { "info", 3 },

                { "channel", 2 },       //-- only server-end SDK using
                { "loading", 2 },
                { "register", 2 },
                { "login", 2 },
                { "level", 2 },         //-- only server-end SDK using
                { "payment", 2 },       //-- only server-end SDK using
                { "tutorial", 2 },
                { "task", 3 },
                { "source", 3 },        //-- only server-end SDK using
                { "order", 3 },         //-- only server-end SDK using
            };

            maxPriority = 3;
            return config;
        }

        public void OnDestroy()
        {
            lock (interLocker)
            {
                if (!inited || stopped)
                    return;

                stopped = true;
            }

            Application.lowMemory -= OnLowMemory;
            Application.logMessageReceivedThreaded -= LogUnityMessage;
            
            StopAllCoroutines();
            routineThread.Join();
        }

        //==========[ Routine Functionss ]===========//

        private void RoutineFunc(object obj)
        {
            RUMConfig config = (RUMConfig)obj;

            SecondaryInit(config);

            //----------[ Loop Parameters ]------------//

            const long pingIntervalSeconds = 20;
            long lastPingSecond = 0;

            const long onlineStatReportInteervalSeconds = 60;
            long lastOnlineStatReportSecond = ClientEngine.GetCurrentSeconds() + onlineStatReportInteervalSeconds;

            int memorySyncToDiskIntervalMilliseconds = config.cache.memorySyncToDiskIntervalMilliseconds;
            if (memorySyncToDiskIntervalMilliseconds < 200)
                memorySyncToDiskIntervalMilliseconds = 200;

            long lastDiskSyncedMS = ClientEngine.GetCurrentMilliseconds();

            while (!stopped)
            {
                if (requireUpdateConfig)
                {
                    Action updateAction;
                    lock (interLocker)
                    {
                        updateAction = updateConfigAction;
                        updateConfigAction = null;
                    }

                    updateAction();
                    requireUpdateConfig = false;
                }

                if (!_isBackground)
                {
                    long nowSeconds = ClientEngine.GetCurrentSeconds();
                    if (nowSeconds - lastOnlineStatReportSecond >= onlineStatReportInteervalSeconds)
                    {
                        lastOnlineStatReportSecond = nowSeconds;
                        onlineStatus.ReportStatus();
                    }
                }

                DumpEventCache();

                if (requireNewSession)
                {
                    GenNewSession();

                    fpsStatus.Reset();
                    usingStat.Reset();
                    onlineStatus.Reset(networkReachable);

                    requireReportSystemInfo = true;
                    requireNewSession = false;
                }

                if (networkReachable)
                {
                    TCPClient client = clientHolder.GetClient();
                    if (client != null)
                    {
                        //---- Send Ping
                        long nowSeconds = ClientEngine.GetCurrentSeconds();
                        bool triggerPing = true;

                        if (_isBackground)
                        {
                            if (nowSeconds - usingStat.EnterBackgroundMS() / 1000 > RUMLimitation.stopPingWhenAppInBackgorundMoreThanSeconds)
                                triggerPing = false;
                        }

                        if (triggerPing && lastPingSecond + pingIntervalSeconds <= nowSeconds)
                        {
                            SendPing(client);
                            lastPingSecond = nowSeconds;
                        }

                        //---- Send events
                        {
                            long quota = eventsSendState.getGuota(nowSeconds);
                            long consumedBytes = cache.SendEvents(coreInfo, client, quota);
                            if (consumedBytes > 0)
                                eventsSendState.Adjust(consumedBytes);
                        }
                    }
                }
                else
                    clientHolder.NetworkUnreachable();

                //-------------[ Resource Checking ]-------------//
                {
                    long nowMilliseconds = ClientEngine.GetCurrentMilliseconds();
                    if (lastDiskSyncedMS + memorySyncToDiskIntervalMilliseconds <= nowMilliseconds)
                    {
                        cache.CheckResourceLimitation();
                        cache.SyncMemoryToDisk();
                        lastDiskSyncedMS = nowMilliseconds;
                    }
                }

                Thread.Sleep(EventsSendState.sendIntervalMilliseconds);
            }

            cache.Destroy();
            clientHolder.Close();
        }

        private IEnumerator PerSecondCoroutine()
        {
            yield return new WaitForSeconds(1.0f);

            while (true)
            {
                CheckNetworkChange();

                if (!_isBackground)
                {
                    fpsStatus.Gather();
                }

                if (requireReportSystemInfo)
                {
                    ReportSystemInfo();
                    requireReportSystemInfo = false;
                }

                yield return new WaitForSeconds(1.0f);
            }
        }

        private void CheckNetworkChange()
        {
            NetworkReachability oldType, newType;

            lock (interLocker)
            {
                oldType = networkReachability;
                networkReachability = Application.internetReachability;
                networkReachable = networkReachability != NetworkReachability.NotReachable;
                newType = networkReachability;
            }

            if (oldType != newType)
            {
                Dictionary<string, object> eventDict = new Dictionary<string, object>();

                if (newType == NetworkReachability.ReachableViaCarrierDataNetwork)
                    eventDict.Add("nw", "cellular");
                else if (newType == NetworkReachability.ReachableViaLocalAreaNetwork)
                    eventDict.Add("nw", "WiFi");
                else
                    eventDict.Add("nw", "Disable");

                AddEvent("nwswitch", eventDict);

                if (oldType == NetworkReachability.NotReachable)
                    onlineStatus.Online(true);
                else if (newType == NetworkReachability.NotReachable)
                    onlineStatus.Online(false);
            }
        }

        //---------[ BG & FG Ciontrolling ]---------//
        private void CheckInBackground()
        {
            if (_isPause && !_isFocus)
            {
                if (_isBackground == false)
                {
                    _isBackground = true;

                    fpsStatus.EnterBackground();
                    usingStat.EnterBackground();
                    onlineStatus.EnterBackground();
                    onlineStatus.ReportStatus();
                    AddEvent("bg", new Dictionary<string, object>());
                }
            }
            else
            {
                if (_isBackground)
                {
                    _isBackground = false;

                    fpsStatus.EnterFrontground();
                    usingStat.EnterFrontground();
                    onlineStatus.EnterFrontground();
                    clientHolder.EnterFrontground();
                    AddEvent("fg", new Dictionary<string, object>());
                }
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            _isPause = pauseStatus;

            CheckInBackground();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            _isFocus = hasFocus;

            CheckInBackground();
        }

        //---------[ Process Events ]---------//
        internal void PushInternalErrorInfo(string errorInfo, Exception ex)
        {
            Dictionary<string, object> info = new Dictionary<string, object>();
            info.Add("message", errorInfo);

            if (ex != null)
                info.Add("ex", ex.ToString());

            lock (interLocker)
            {
                if (coreInfo != null)
                    info.Add("ts", coreInfo.GetAdjustedTimestamp());
                else
                    info.Add("ts", ClientEngine.GetCurrentSeconds());

                internalErrors.Add(info);
            }
        }

        internal static void AddInternalErrorInfo(string errorInfo, Exception ex)
        {
            Instance.PushInternalErrorInfo(errorInfo, ex);
        }

        private void AddPrimaryEvent(string eventName, Dictionary<string, object> eventDict)
        {
            eventDict.Add("ts", ClientEngine.GetCurrentSeconds());

            byte[] binaryData = SerializeEventDict(eventName, eventDict);
            if (binaryData == null)
                return;

            if (primaryEventsCache.TryGetValue(eventName, out Queue<byte[]> queue))
            {
                queue.Enqueue(binaryData);
            }
            else
            {
                queue = new Queue<byte[]>();
                queue.Enqueue(binaryData);
                primaryEventsCache.Add(eventName, queue);
            }
        }

        internal void AddEvent(string eventName, Dictionary<string, object> eventDict)
        {
            lock (interLocker)
            {
                if (coreInfo == null)
                {
                    AddPrimaryEvent(eventName, eventDict);
                    return;
                }

                if (priorityDict != null)
                {
                    if (!priorityDict.TryGetValue(eventName, out int _))
                        return;
                }
            }

            eventDict.Add("ev", eventName);
            eventDict.Add("ts", coreInfo.GetAdjustedTimestamp());
            eventDict.Add("pid", coreInfo.pid);
            eventDict.Add("eid", GenEventId());
            eventDict.Add("sid", Interlocked.Read(ref coreInfo.sessionId));     //-- Only this read coreInfo.sessionId in other thread, not in RoutineFunc()' loop.
            eventDict.Add("rid", coreInfo.rumId);

            if (!eventDict.ContainsKey("uid"))      //-- For append event for uid changed or append.
            {
                string userId = coreInfo.Uid;
                if (userId != null)
                    eventDict.Add("uid", userId);
            }

            byte[] binaryData = SerializeEventDict(eventName, eventDict);
            if (binaryData == null)
                return;

            lock (interLocker)
            {
                if (tempEventCache.TryGetValue(eventName, out Queue<byte[]> queue))
                {
                    queue.Enqueue(binaryData);
                }
                else
                {
                    queue = new Queue<byte[]>();
                    queue.Enqueue(binaryData);
                    tempEventCache.Add(eventName, queue);
                }
            }
        }

        private void DumpPrimaryEventsCache()
        {
            Dictionary<string, int> eventPriorities = new Dictionary<string, int>();
            Dictionary<string, Queue<byte[]>> eventCache = new Dictionary<string, Queue<byte[]>>();

            lock (interLocker)
            {
                foreach (KeyValuePair<string, Queue<byte[]>> kvp in primaryEventsCache)
                {
                    if (priorityDict.TryGetValue(kvp.Key, out int priority))
                    {
                        eventPriorities.Add(kvp.Key, priority);
                        eventCache.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            string userId = coreInfo.Uid;
            long sessionId = coreInfo.sessionId;

            foreach (KeyValuePair<string, Queue<byte[]>> kvp in eventCache)
            {
                int priority = eventPriorities[kvp.Key];

                foreach (byte[] data in kvp.Value)
                {
                    Dictionary<string, object> eventDict;

                    try
                    {
                        Dictionary<object, object> dict = MsgUnpacker.Unpack(data);
                        if (dict == null)
                            continue;

                        eventDict = new Dictionary<string, object>();
                        foreach (KeyValuePair<object, object> dictkv in dict)
                        {
                            eventDict.Add((string)dictkv.Key, dictkv.Value);
                        }

                    }
                    catch (Exception e)
                    {
                        if (errorRecorder != null)
                            errorRecorder.RecordError("Dump primaty event " + kvp.Key + " failed. MsgPack unpack exception.", e);

                        continue;
                    }


                    eventDict.Add("ev", kvp.Key);

                    eventDict.Add("pid", coreInfo.pid);
                    eventDict.Add("eid", GenEventId());
                    eventDict.Add("sid", sessionId);
                    eventDict.Add("rid", coreInfo.rumId);

                    if (userId != null)
                        eventDict.Add("uid", userId);

                    byte[] binaryData = SerializeEventDict(kvp.Key, eventDict);
                    if (binaryData != null)
                        cache.AddEvent(priority, binaryData);
                }
            }

            primaryEventsCache = null;
        }

        private void DumpEventCache()
        {
            DumpTempEventCache();
            DumpInternalErrors();
        }

        private void DumpTempEventCache()
        {
            Dictionary<string, int> eventPriorities = new Dictionary<string, int>();
            Dictionary<string, Queue<byte[]>> eventCache = new Dictionary<string, Queue<byte[]>>();

            lock (interLocker)
            {
                if (tempEventCache.Count == 0)
                    return;

                foreach (KeyValuePair<string, Queue<byte[]>> kvp in tempEventCache)
                {
                    if (priorityDict.TryGetValue(kvp.Key, out int priority))
                    {
                        eventPriorities.Add(kvp.Key, priority);
                        eventCache.Add(kvp.Key, kvp.Value);
                    }
                }

                tempEventCache = new Dictionary<string, Queue<byte[]>>();
            }

            foreach (KeyValuePair<string, Queue<byte[]>> kvp in eventCache)
            {
                int priority = eventPriorities[kvp.Key];

                foreach (byte[] data in kvp.Value)
                { 
                    cache.AddEvent(priority, data);
                }
            }
        }

        private void DumpInternalErrors()
        {
            int priority;
            List<Dictionary<string, object>> dumpInternalErrors;
            lock (interLocker)
            {
                if (internalErrors.Count == 0)
                    return;

                dumpInternalErrors = internalErrors;
                internalErrors = new List<Dictionary<string, object>>();

                if (!priorityDict.TryGetValue("error", out priority))
                    priority = 2;
            }

            string userId = coreInfo.Uid;
            long sessionId = coreInfo.sessionId;

            foreach (Dictionary<string, object> dict in dumpInternalErrors)
            {
                dict.Add("ev", "error");
                dict.Add("type", "unity_sdk");

                dict.Add("pid", coreInfo.pid);
                dict.Add("eid", GenEventId());
                dict.Add("sid", sessionId);
                dict.Add("rid", coreInfo.rumId);

                if (userId != null)
                    dict.Add("uid", userId);

                byte[] binaryData = SerializeEventDict("internal error", dict);
                if (binaryData != null)
                    cache.AddEvent(priority, binaryData);
            }
        }

        private byte[] SerializeEventDict(string eventName, Dictionary<string, object> dict)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    MsgPacker.Pack(stream, dict);
                    return stream.ToArray();
                }
            }
            catch (Exception e)
            {
                if (RUMCenter.errorRecorder != null)
                    RUMCenter.errorRecorder.RecordError("Serialize " + eventName + " event failed. MsgPack pack exception.", e);

                RUMCenter.AddInternalErrorInfo("Serialize " + eventName + " event failed. MsgPack pack exception.", e);

                return null;
            }
        }

        private void GenOpenEvent()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            dict.Add("ev", "open");
            dict.Add("ts", coreInfo.GetAdjustedTimestamp());
            dict.Add("pid", coreInfo.pid);
            dict.Add("eid", GenEventId());
            dict.Add("sid", coreInfo.sessionId);
            dict.Add("rid", coreInfo.rumId);

            string userId = coreInfo.Uid;
            if (userId != null)
                dict.Add("uid", userId);

            dict.Add("v", RUMConfig.SDKVersion);

            if (platformInfo.screenHeight != 0)
                dict.Add("sh", platformInfo.screenHeight);
            if (platformInfo.screenWidth != 0)
                dict.Add("sw", platformInfo.screenWidth);

            if (platformInfo.deviceModel != null)
                dict.Add("model", platformInfo.deviceModel);
            if (platformInfo.operatingSystem != null)
                dict.Add("os", platformInfo.operatingSystem);
            if (platformInfo.systemLanguage != null)
                dict.Add("lang", platformInfo.systemLanguage);

            lock (interLocker)
            {
                if (networkReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
                    dict.Add("nw", "WiFi");
                else if (networkReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
                    dict.Add("nw", "Cellular");
                else
                    dict.Add("nw", "offline");
            }

            int timeZone = TimeZoneInfo.Local.BaseUtcOffset.Hours;
            if (timeZone > 0)
                dict.Add("tz", "+" + timeZone);
            else
                dict.Add("tz", timeZone.ToString());

            byte[] binaryData = SerializeEventDict("open", dict);
            if (binaryData != null)
                cache.AddEvent(0, binaryData);
        }

        private void PackStringValue(Dictionary<string, object> dict, string key, string value)
        {
            if (value != null && value.Length > 0)
                dict.Add(key, value);
        }

        private void ReportSystemInfo()
        {
            Dictionary<string, object> sysInfo = new Dictionary<string, object>();

            if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
                sysInfo.Add("network", "WiFi");
            else if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
                sysInfo.Add("network", "Cellular");
            else
                sysInfo.Add("network", "offline");

            PackStringValue(sysInfo, "systemLanguage", Application.systemLanguage.ToString());
            PackStringValue(sysInfo, "deviceModel", SystemInfo.deviceModel);
            PackStringValue(sysInfo, "operatingSystem", SystemInfo.operatingSystem);

            sysInfo.Add("screenHeight", Screen.height);
            sysInfo.Add("screenWidth", Screen.width);

            sysInfo.Add("isMobile", Application.isMobilePlatform);
            sysInfo.Add("systemMemorySize", SystemInfo.systemMemorySize);

            PackStringValue(sysInfo, "unityVersion", Application.unityVersion);

            sysInfo.Add("installMode", Application.installMode.ToString());
            sysInfo.Add("copyTextureSupport", SystemInfo.copyTextureSupport.ToString());
            sysInfo.Add("deviceType", SystemInfo.deviceType.ToString());
            sysInfo.Add("graphicsDeviceType", SystemInfo.graphicsDeviceType.ToString());
            sysInfo.Add("npotSupport", SystemInfo.npotSupport.ToString());

            {
                int timeZone = TimeZoneInfo.Local.BaseUtcOffset.Hours;
                if (timeZone > 0)
                    sysInfo.Add("timeZone", "+" + timeZone);
                else
                    sysInfo.Add("timeZone", timeZone.ToString());
            }

            PackStringValue(sysInfo, "deviceName", SystemInfo.deviceName);
            sysInfo.Add("deviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier);
            sysInfo.Add("graphicsDeviceID", SystemInfo.graphicsDeviceID);
            PackStringValue(sysInfo, "graphicsDeviceName", SystemInfo.graphicsDeviceName);

            PackStringValue(sysInfo, "graphicsDeviceVendor", SystemInfo.graphicsDeviceVendor);
            sysInfo.Add("graphicsDeviceVendorID", SystemInfo.graphicsDeviceVendorID);
            PackStringValue(sysInfo, "graphicsDeviceVersion", SystemInfo.graphicsDeviceVersion);

            sysInfo.Add("graphicsMemorySize", SystemInfo.graphicsMemorySize);
            sysInfo.Add("graphicsMultiThreaded", SystemInfo.graphicsMultiThreaded);
            sysInfo.Add("graphicsShaderLevel", SystemInfo.graphicsShaderLevel);
            sysInfo.Add("maxTextureSize", SystemInfo.maxTextureSize);

            sysInfo.Add("processorCount", SystemInfo.processorCount);
            sysInfo.Add("processorFrequency", SystemInfo.processorFrequency);
            PackStringValue(sysInfo, "processorType", SystemInfo.processorType);

            sysInfo.Add("supportedRenderTargetCount", SystemInfo.supportedRenderTargetCount);
            sysInfo.Add("supports2DArrayTextures", SystemInfo.supports2DArrayTextures);
            sysInfo.Add("supports3DTextures", SystemInfo.supports3DTextures);
            sysInfo.Add("supportsAccelerometer", SystemInfo.supportsAccelerometer);
            sysInfo.Add("supportsAudio", SystemInfo.supportsAudio);
            sysInfo.Add("supportsComputeShaders", SystemInfo.supportsComputeShaders);
            sysInfo.Add("supportsGyroscope", SystemInfo.supportsGyroscope);
            sysInfo.Add("supportsInstancing", SystemInfo.supportsInstancing);
            sysInfo.Add("supportsLocationService", SystemInfo.supportsLocationService);
            sysInfo.Add("supportsMotionVectors", SystemInfo.supportsMotionVectors);
            sysInfo.Add("supportsRawShadowDepthSampling", SystemInfo.supportsRawShadowDepthSampling);
            sysInfo.Add("supportsShadows", SystemInfo.supportsShadows);
            sysInfo.Add("supportsSparseTextures", SystemInfo.supportsSparseTextures);
            sysInfo.Add("supportsVibration", SystemInfo.supportsVibration);

            PackStringValue(sysInfo, "unsupportedIdentifier", SystemInfo.unsupportedIdentifier);

#if UNITY_IPHONE
            {
                byte[] deviceToken = UnityEngine.iOS.NotificationServices.deviceToken;
                if (deviceToken != null)
                {
                    string token = BitConverter.ToString(deviceToken);
                    if (token != null && token.Length > 0)
                        sysInfo.Add("deviceToken", token.Replace("-", ""));
                }
            }

            PackStringValue(sysInfo, "vendorIdentifier", UnityEngine.iOS.Device.vendorIdentifier);
#endif

            Dictionary<string, object> eventDict = new Dictionary<string, object>
            {
                { "type", "unity_system_info" },
                { "system_info", sysInfo }
            };

            AddEvent("info", eventDict);
        }

        private void AddUnityLogEvent(string eventName, string type, string message, string stackTrace)
        {
            Dictionary<string, object> eventDict = new Dictionary<string, object>();
            eventDict.Add("type", type);
            eventDict.Add("message", message);
            eventDict.Add("stack", stackTrace);

            AddEvent(eventName, eventDict);
        }

        private void LogUnityMessage(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
                AddUnityLogEvent("error", "unity_error", message, stackTrace);
            else if (type == LogType.Assert)
                AddUnityLogEvent("crash", "unity_assert", message, stackTrace);
            else if (type == LogType.Error && autoRecordUnityDebugErrorLog)
                AddUnityLogEvent("error", "unity_custom_error_log", message, stackTrace);
        }

        private long lastLowMemoryReportSeconds = 0;
        private void OnLowMemory()
        {
            const long reportIntervalSeconds = 60;
            long now = ClientEngine.GetCurrentSeconds();
            if (now - lastLowMemoryReportSeconds < reportIntervalSeconds)
                return;

            lastLowMemoryReportSeconds = now;
            Dictionary<string, object> eventDict = new Dictionary<string, object>()
            {
                { "type", "unity_low_memory" },
                { "system_memory", SystemInfo.systemMemorySize }
            };
            AddEvent("warn", eventDict);
        }

        private void CheckCrashReport(bool autoClearCrashReports)
        {
            CrashReport[] reports = CrashReport.reports;
            if (reports != null)
            {
                DateTime originDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

                foreach (CrashReport report in reports)
                {
                    Dictionary<string, object> eventDict = new Dictionary<string, object>();
                    eventDict.Add("type", "unity_last");
                    eventDict.Add("message", report.text);

                    TimeSpan span = report.time.ToUniversalTime() - originDateTime;
                    long occurredTime = (Int64)Math.Floor(span.TotalSeconds);
                    eventDict.Add("cts", occurredTime);

                    AddEvent("crash", eventDict);
                }

                if (autoClearCrashReports)
                    CrashReport.RemoveAll();
            }
        }

        //---------[ Process Ping ]---------//
        private void SendPing(TCPClient client)
        {
            Quest quest = new Quest("ping");
            quest.Param("pid", coreInfo.pid);
            quest.Param("cv", configVersion);

            string userId = coreInfo.Uid;
            if (userId != null)
                quest.Param("uid", userId);

            quest.Param("rid", coreInfo.rumId);
            quest.Param("sid", coreInfo.sessionId);
            quest.Param("pt", Interlocked.Read(ref lastPingCostMilliseconds));

            string sign = coreInfo.GenSign(out long salt);
            quest.Param("sign", sign);
            quest.Param("salt", salt);

            long curr = ClientEngine.GetCurrentMilliseconds();
            bool success = client.SendQuest(quest, (Answer answer, int errorCode) => {
                if (errorCode == ErrorCode.FPNN_EC_OK)
                {
                    //-- lastPingCostMilliseconds
                    long now = ClientEngine.GetCurrentMilliseconds();
                    long costMS = now - curr;
                    Interlocked.Exchange(ref lastPingCostMilliseconds, costMS);

                    try
                    {
                        //-- adjust client time
                        long ts = answer.Want<long>("ts");
                        long delay = (now - costMS / 2) / 1000 - ts;
                        coreInfo.ConfigDelay(delay);

                        //-- bandwith
                        int bw = answer.Want<int>("bw");
                        if (bw > 0)
                            Interlocked.Exchange(ref eventsSendState.bandwidthInBPS, bw);

                        //-- config version
                        int cv = answer.Want<int>("cv");
                        if (cv >= 0 && cv != configVersion)
                            FetchConfig(client, cv);
                    }
                    catch (Exception)
                    {
                        //-- Do nothing. Server send illegal data.
                    }
                }
                else
                {
                    if (errorRecorder != null)
                        errorRecorder.RecordError("Send ping failed. ErrorCode: " + errorCode + ", Ts: " + ClientEngine.GetCurrentSeconds());
                }
            });

            if (!success)
            {
                if (errorRecorder != null)
                    errorRecorder.RecordError("Send ping failed. Ts: " + ClientEngine.GetCurrentSeconds());
            }
        }

        private void FetchConfig(TCPClient client, int cv)
        {
            Quest quest = new Quest("getconfig");
            quest.Param("pid", coreInfo.pid);
            quest.Param("rid", coreInfo.rumId);

            string userId = coreInfo.Uid;
            if (userId != null)
                quest.Param("uid", userId);

            if (platformInfo.systemLanguage != null)
                quest.Param("lang", platformInfo.systemLanguage);

            if (platformInfo.deviceModel != null)
                quest.Param("model", platformInfo.deviceModel);

            if (platformInfo.operatingSystem != null)
                quest.Param("os", platformInfo.operatingSystem);

            NetworkReachability nwType;
            lock (interLocker)
            {
                nwType = networkReachability;
            }
            if (nwType == NetworkReachability.ReachableViaCarrierDataNetwork)
                quest.Param("nw", "cellular");
            else if (nwType == NetworkReachability.ReachableViaLocalAreaNetwork)
                quest.Param("nw", "WiFi");

            if (platformInfo.appVersion != null)
                quest.Param("appv", platformInfo.appVersion);

            string sign = coreInfo.GenSign(out long salt);
            quest.Param("sign", sign);
            quest.Param("salt", salt);

            bool success = client.SendQuest(quest, (Answer answer, int errorCode) =>
            {
                if (errorCode == ErrorCode.FPNN_EC_OK)
                {
                    try
                    {
                        int maxPriority = 0;
                        Dictionary<string, int> eventConfig = new Dictionary<string, int>();

                        Dictionary<object, object> events = (Dictionary<object, object>)answer.Want("events");
                        foreach (KeyValuePair<object, object> kvp in events)
                        {
                            string priorityStr = (string)kvp.Key;
                            List<object> eventNames = (List<object>)kvp.Value;

                            if (!Int32.TryParse(priorityStr, out int priority))
                                priority = RUMLimitation.defaultPriorityLevel;

                            if (priority > maxPriority)
                                maxPriority = priority;

                            foreach (object obj in eventNames)
                                eventConfig.Add((string)obj, priority);
                        }

                        lock (interLocker)
                        {
                            updateConfigAction = () => {
                                configVersion = cv;
                                UpdateConfig(eventConfig, maxPriority);
                                RUMFile.SaveConfig(eventConfig, cv);
                            };
                            requireUpdateConfig = true;
                        }
                    }
                    catch (Exception)
                    {
                        //-- Do nothing. Server send illegal data.
                    }
                }
                else
                {
                    if (errorRecorder != null)
                        errorRecorder.RecordError("Send getconfig failed. ErrorCode: " + errorCode
                            + " Curr cv: " + configVersion + ", want cv: " + cv
                            + ", Ts: " + ClientEngine.GetCurrentSeconds());

                    //-- 20 秒一次ping，根据ping的结果决定是否需要更新config。因此实时性已经不高了，所以如果失败，等待20秒后，下一次ping的处理即可。不做重试。
                }
            });

            if (!success)
            {
                if (errorRecorder != null)
                    errorRecorder.RecordError("Send getconfig failed. Curr cv: " + configVersion
                        + ", want cv: "+ cv + ", Ts: " + ClientEngine.GetCurrentSeconds());
            }
        }
    }
}
