using com.fpnn.common;

namespace com.fpnn.rum
{
    public class RegressiveStrategy
    {
        public int connectFailedMaxIntervalMilliseconds = 2000;     //-- 1.5 seconds.       从连接到断开，多少豪秒内算是连接失败。
        public int startConnectFailedCount = 2;                     //-- 连续失败多少次后，开始退行性连接
        public int maxIntervalSeconds = 600;                        //-- 退行性连接最大间隔时间
        public int linearRegressiveCount = 5;                       //-- 从第一次退行性连接开始，到最大连接间隔时间，允许尝试几次连接？每次间隔时间会线性增大。

        public RegressiveStrategy() { }
        public RegressiveStrategy(RegressiveStrategy instance)
        {
            connectFailedMaxIntervalMilliseconds = instance.connectFailedMaxIntervalMilliseconds;
            startConnectFailedCount = instance.startConnectFailedCount;
            maxIntervalSeconds = instance.maxIntervalSeconds;
            linearRegressiveCount = instance.linearRegressiveCount;
        }
    }

    public class NetworkSetting
    {
        public RegressiveStrategy regressiveStrategy = new RegressiveStrategy();

        public int globalConnectTimeout = 30;
        public int globalQuestTimeout = 30;
        public int bandwidthInKBPS = 256;                             //-- Byte (KB/s), not bit(kb/s).
    }

    public class CacheSetting
    {
        public int maxDiskCachedSizeInMB = 200;                         //-- Max disk Cached size is 1024MB (1GB)
        public int maxMemoryCachedSizeInMB = 24;                        //-- Max memory Cached size is 64MB. [8MB ~ 64MB]
        public int maxFileSizeForCacheFileInKB = 4 * 1024;              //-- Max size is 64MB. [256KB ~ 64MB]

        public int memorySyncToDiskIntervalMilliseconds = 1000;         //-- Min is 200 ms.
    }

    public class RUMConfig
    {
        public static readonly string SDKVersion = "2.0.0";

        public NetworkSetting network;
        public CacheSetting cache;
        public ErrorRecorder errorRecorder;
        public bool autoClearCrashReports;
        public bool autoRecordUnityDebugErrorLog;

        internal string endpoint;
        internal int pid;
        internal string secretKey;
        internal string appVersion;
        internal string uid;

        public RUMConfig(string endpoint, int pid, string secretKey, string appVersion, string uid = null)
        {
            network = new NetworkSetting();
            cache = new CacheSetting();

            this.endpoint = endpoint;
            this.pid = pid;
            this.secretKey = secretKey;
            this.appVersion = appVersion;
            this.uid = uid;

            autoClearCrashReports = true;
            autoRecordUnityDebugErrorLog = false;
        }
    }

    internal static class RUMLimitation
    {
        static public readonly int defaultPriorityLevel = 3;
        static public readonly int maxPriorityLevel = 9;
        static public readonly int stopPingWhenAppInBackgorundMoreThanSeconds = 60;

        static public int maxDiskCachedSize;                //-- In Bytes
        static public int maxMemoryCachedSize;              //-- In Bytes
        static public int maxFileSizeForCacheFile;          //-- In Bytes

        internal static void Init(CacheSetting setting)
        {
            if (setting.maxDiskCachedSizeInMB > 1024)
                setting.maxDiskCachedSizeInMB = 1024;
            else if (setting.maxDiskCachedSizeInMB < 0)
                setting.maxDiskCachedSizeInMB = 200;

            maxDiskCachedSize = setting.maxDiskCachedSizeInMB * 1024 * 1024;

            if (setting.maxMemoryCachedSizeInMB > 64)
                setting.maxMemoryCachedSizeInMB = 64;
            else if (setting.maxMemoryCachedSizeInMB < 8)
                setting.maxMemoryCachedSizeInMB = 8;

            maxMemoryCachedSize = setting.maxMemoryCachedSizeInMB * 1024 * 1024;

            if (setting.maxFileSizeForCacheFileInKB > 64 * 1024)
                setting.maxFileSizeForCacheFileInKB = 64 * 1024;
            else if (setting.maxFileSizeForCacheFileInKB < 256)
                setting.maxFileSizeForCacheFileInKB = 256;

            maxFileSizeForCacheFile = setting.maxFileSizeForCacheFileInKB * 1024;
        }

        internal static bool CheckSendVolume(byte[] data, long quota)
        {
            return !(quota < 128 && data.Length > 2048);
        }
    }
}