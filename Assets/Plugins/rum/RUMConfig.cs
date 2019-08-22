using System;

namespace com.rum {

    public class RUMConfig {

        public static string VERSION = "1.2.5";
        
        public static int PING_INTERVAL = 20 * 1000;                    //与Agent之间的连通性检查以及config更新检查的时间间隔(ms)
        public static int SENT_CONCURRENT = 3;                          //发送事件并发限制
        public static int SENT_INTERVAL = 1 * 1000;                     //事件发送时间间隔(ms)
        public static int CONNCT_INTERVAL = 40 * 1000;                  //客户端尝试重新连接的时间间隔(ms)
        public static int SENT_TIMEOUT = 20 * 1000;                     //事件发送的超时时间(ms)
        public static int EVENT_QUEUE_LIMIT = 11 * 1000;                //事件队列长度限制(1024 * 1024B / 100B)
        public static int STORAGE_SIZE_MAX = 1 * 1024 * 1024;           //本地存储上限阀值(B)
        public static int STORAGE_SIZE_MIN = 256 * 1024;                //本地存储下限阀值(B)
        public static int LOCAL_STORAGE_DELAY = 1 * 1000;               //内存到本地存储快照的时间间隔(ms)
        public static int SENT_SIZE_LIMIT = 15 * 1024;                  //事件发送的带宽限制(B)
        public static int LOCAL_FILE_SIZE = 512 * 1024;                 //本地文件大小(B)
        public static int LOCAL_FILE_COUNT = 200;                       //本地文件存储数量上限
    }
}