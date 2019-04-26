using System;

namespace com.rum {

    public class RUMConfig {

        public static string VERSION = "1.0.0";
        
        public static int PING_INTERVAL = 20 * 1000;
        public static int SENT_INTERVAL = 1 * 1000;
        public static int CONNCT_INTERVAL = 20 * 1000;
        public static int SENT_TIMEOUT = 20 * 1000;
        public static int EVENT_QUEUE_LIMIT = 1 * 1000;
        public static int STORAGE_SIZE_LIMIT = 2 * 1024 * 1024;
        public static int LOCAL_STORAGE_DELAY = 1 * 1000;
        public static int SENT_SIZE_LIMIT = 15 * 1024;
    }
}