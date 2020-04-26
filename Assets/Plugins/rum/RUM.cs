using System.Collections.Generic;

namespace com.fpnn.rum
{
    public static class RUM
    {
        static RUM()
        {
        }

        public static void Init(string endpoint, int pid, string secretKey, string appVersion, string uid = null, com.fpnn.common.ErrorRecorder errorRecorder = null)
        {
            RUMConfig config = new RUMConfig(endpoint, pid, secretKey, appVersion, uid);
            config.errorRecorder = errorRecorder;

            Init(config);
        }

        public static void Init(RUMConfig config)
        {
            RUMCenter.Instance.Init(config);
        }

        public static void SetUid(string uid)
        {
            RUMCenter.Instance.coreInfo.Uid = uid;            
        }

        public static void NewSession()
        {
            RUMCenter.Instance.StartNewSession();
        }

        //----------------------------------------------//
        //--         Custom Event Interfaces          --//
        //----------------------------------------------//
        static public void CustomEvent(string eventName, IDictionary<string, object> attributes)
        {
            if (eventName == null || eventName.Length == 0)
                return;

            Dictionary<string, object> eventDict = new Dictionary<string, object>();
            
            if (attributes != null && attributes.Count > 0)
                eventDict.Add("attrs", attributes);

            RUMCenter.Instance.AddEvent(eventName, eventDict);
        }

        static public void CustomError(string tag, string message, System.Exception exception = null, IDictionary<string, object> attributes = null)
        {
            if ((tag == null || tag.Length == 0)
                && (message == null || message.Length == 0)
                && exception == null
                && (attributes == null || attributes.Count == 0))
                return;

            Dictionary<string, object> eventDict = new Dictionary<string, object>()
            {
                { "type", "unity_custom_error" },
            };

            if (tag != null)
                eventDict.Add("tag", tag);
            else
                eventDict.Add("tag", "");

            if (message != null && message.Length > 0)
                eventDict.Add("message", message);

            if (exception != null)
            {
                if (exception.Message.Length > 0)
                {
                    eventDict.Add("ex_message", exception.Message);

                    if (message == null || message.Length == 0)
                        eventDict.Add("message", exception.Message);
                }

                eventDict.Add("stack", exception.StackTrace);
            }

            if (attributes != null && attributes.Count > 0)
                eventDict.Add("attrs", attributes);

            RUMCenter.Instance.AddEvent("error", eventDict);
        }

        //----------------------------------------------//
        //--          Http Event Interface            --//
        //----------------------------------------------//
        static public void HttpEvent(string url, string method, int status, long reqsize, long respsize, int latency, IDictionary<string, object> attributes = null)
        {
            if (url == null || url.Length == 0)
                return;

            Dictionary<string, object> eventDict = new Dictionary<string, object>()
            {
                { "url", url },
                { "method", method },
                { "status", status },
                { "reqsize", reqsize },
                { "respsize", respsize },
                { "latency", latency }
            };

            if (attributes != null && attributes.Count > 0)
                eventDict.Add("attrs", attributes);

            RUMCenter.Instance.AddEvent("http", eventDict);
        }

        static public void HttpEvent(System.Net.Http.HttpResponseMessage res, int latency, IDictionary<string, object> attributes = null)
        {
           System.Net.Http.HttpRequestMessage req = res.RequestMessage;
            Dictionary<string, object> eventDict = new Dictionary<string, object>()
            {
                { "url", req.RequestUri },
                { "method", req.Method },
                { "status", res.StatusCode.ToString("D") },
                { "latency", latency }
            };

            if (attributes != null && attributes.Count > 0)
                eventDict.Add("attrs", attributes);

            RUMCenter.Instance.AddEvent("http", eventDict);
        }

        static public void HttpEvent(UnityEngine.Networking.UnityWebRequest req, int latency, IDictionary<string, object> attributes = null)
        {
            Dictionary<string, object> eventDict = new Dictionary<string, object>()
            {
                { "url", req.url },
                { "method", req.method },
                { "reqsize", req.uploadedBytes },
                { "respsize", req.downloadedBytes },
                { "status", req.responseCode },
                { "latency", latency },
            };

            if (attributes != null && attributes.Count > 0)
                eventDict.Add("attrs", attributes);

            RUMCenter.Instance.AddEvent("http", eventDict);
        }

        //----------------------------------------------//
        //--         PRD 2 Events Interface           --//
        //----------------------------------------------//
        //----------------- loading -------------------//
        //-- percentageProcess: [0, 100]
        static public void LoadingEvent(string loadingType, int percentageProcess)
        {
            if (loadingType == null || loadingType.Length == 0)
                return;

            Dictionary<string, object> eventDict = new Dictionary<string, object>();
            eventDict.Add("ldty", loadingType);
            eventDict.Add("ldtg", percentageProcess);

            RUMCenter.Instance.AddEvent("loading", eventDict);
        }

        static public void InitialLoadingEvent(int percentageProcess)
        {
            LoadingEvent("initial", percentageProcess);
        }

        static public void InitialLoadingBeginEvent()
        {
            LoadingEvent("initial", 0);
        }

        static public void InitialLoadingFinishEvent()
        {
            LoadingEvent("initial", 100);
        }

        //----------------- register -------------------//
        static public void UserRegisterBeginEvent(string channel = null)
        {
            Dictionary<string, object> eventDict = new Dictionary<string, object>();
            eventDict.Add("retg", 0);
            if (channel != null && channel.Length > 0)
                eventDict.Add("chname", channel);

            RUMCenter.Instance.AddEvent("register", eventDict);
        }

        static public void UserRegisterFinishedEvent(string uid, string channel = null)
        {
            Dictionary<string, object> eventDict = new Dictionary<string, object>();
            eventDict.Add("retg", 1);

            if (uid != null && uid.Length > 0)
                eventDict.Add("uid", uid);

            if (channel != null && channel.Length > 0)
                eventDict.Add("chname", channel);

            RUMCenter.Instance.AddEvent("register", eventDict);
        }

        //----------------- login -------------------//
        static public void UserLoginBeginEvent()
        {
            Dictionary<string, object> eventDict = new Dictionary<string, object>();
            eventDict.Add("lotg", 0);

            RUMCenter.Instance.AddEvent("login", eventDict);
        }

        static public void UserLoginFinishEvent(string uid, bool success, int userLevel = -1)
        {
            Dictionary<string, object> eventDict = new Dictionary<string, object>();
            eventDict.Add("lotg", 1);
            eventDict.Add("logsuc", success ? 1 : 0);

            if (userLevel >= 0)
                eventDict.Add("uslev", userLevel);

            if (uid != null && uid.Length > 0)
                eventDict.Add("uid", uid);

            RUMCenter.Instance.AddEvent("login", eventDict);
        }

        //----------------- tutorial -------------------//
        static public void TutorialEvent(string tutorialType, int stage, int userLevel = -1)
        {
            if (tutorialType == null || tutorialType.Length == 0)
                return;

            Dictionary<string, object> eventDict = new Dictionary<string, object>();
            eventDict.Add("tuty", tutorialType);
            eventDict.Add("tust", stage);

            if (userLevel >= 0)
                eventDict.Add("uslev", userLevel);
        }

        //----------------- task -------------------//
        static public void TaskBeginEvent(string taskType, string taskId, int userLevel = -1)
        {
            Dictionary<string, object> eventDict = new Dictionary<string, object>();

            if (taskType != null && taskType.Length != 0)
                eventDict.Add("taskty", taskType);

            if (taskId != null && taskId.Length != 0)
                eventDict.Add("taskid", taskId);

            if (eventDict.Count == 0)
                return;

            if (userLevel >= 0)
                eventDict.Add("uslev", userLevel);

            eventDict.Add("isbegin", 0);
            RUMCenter.Instance.AddEvent("task", eventDict);
        }

        static public void TaskFinishEvent(string taskType, string taskId, bool success, int userLevel = -1)
        {
            Dictionary<string, object> eventDict = new Dictionary<string, object>();

            if (taskType != null && taskType.Length != 0)
                eventDict.Add("taskty", taskType);

            if (taskId != null && taskId.Length != 0)
                eventDict.Add("taskid", taskId);

            if (eventDict.Count == 0)
                return;

            if (userLevel >= 0)
                eventDict.Add("uslev", userLevel);

            eventDict.Add("isbegin", 1);
            eventDict.Add("issuc", success ? 1 : 0);

            RUMCenter.Instance.AddEvent("task", eventDict);
        }
    }
}
