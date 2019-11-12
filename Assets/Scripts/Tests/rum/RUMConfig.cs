using System;

namespace com.rum {

    public class RUMConfig {

        public static string VERSION = "1.3.4";

        public static int PING_INTERVAL = 20 * 1000;                    //PING间隔(ms)
        public static int CONNCT_INTERVAL = 40 * 1000;                  //尝试重新连接的时间间隔(ms)
        public static int CONNCT_TIMEOUT = 20 * 1000;                   //建立连接超时时间(ms)
        public static int SENT_CONCURRENT = 1;                          //上报事件并发限制
        public static int SENT_INTERVAL = 1 * 1000;                     //事件上报时间间隔(ms)
        public static int SENT_TIMEOUT = 20 * 1000;                     //事件上报超时时间(ms)
        public static int SENT_SIZE_LIMIT = 15 * 1024;                  //事件上报带宽限制(B)
        public static int EVENT_QUEUE_LIMIT = 20 * 1000;                //事件队列长度限制(2 * 1024 * 1024B / 100B)
        public static int STORAGE_SIZE_MAX = 1 * 1024 * 1024;           //本地存储上限阀值(B)
        public static int STORAGE_SIZE_MIN = 256 * 1024;                //本地存储下限阀值(B)
        public static int LOCAL_STORAGE_DELAY = 1 * 1000;               //内存到本地存储快照的时间间隔(ms)
        public static int LOCAL_FILE_SIZE = 512 * 1024;                 //本地文件大小(B)
        public static int LOCAL_FILE_COUNT = 200;                       //本地文件存储数量上限(1 ~ count)
    }
}