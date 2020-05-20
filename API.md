# RUM Unity SDK API Docs

# Index

[TOC]

## Current Version

	public static readonly string com.fpnn.rum.RUMConfig.SDKVersion = "2.0.2";

## Init & Config SDK

**Please init FPNN SDK before initing RUM SDK.**

### Init FPNN SDK

Please refer：[Init & Config FPNN SDK](https://github.com/highras/fpnn-sdk-unity/blob/master/API.md#init--config-sdk)

### Init RUM SDK

	using com.fpnn.rum;

	//-- Quick initialize
	public static void RUM.Init(string endpoint, int pid, string secretKey, string appVersion, string uid = null, com.fpnn.common.ErrorRecorder errorRecorder = null);

	//-- Complex initialize
	public static void RUM.Init(RUMConfig config);

**Parameters:**

* **endpoint**

	RUM server endpoint. Can be retrieved on the web console.

* **pid**

	RUM project id. Can be retrieved on the web console.

* **secretKey**

	RUM secret key. Can be retrieved on the web console.

* **appVersion**

	Version for user's application.

* **uid**

	User's ID. Can be null.

* **errorRecorder**

	An error recorder will be used inside of RUM SDK, which is an instance of com.fpnn.common.ErrorRecorder. Can be null.

	More detail refer: [Definition of ErrorRecorder](https://github.com/highras/fpnn-sdk-unity/blob/master/Assets/Plugins/fpnn/common/ErrorRecorder.cs)

* **config**

	An instance of RUMConfig. Please refer [RUMConfig](#RUMConfig)


### RUMConfig

#### Definition

	public class RUMConfig
    {
        public NetworkSetting network;
        public CacheSetting cache;
        public ErrorRecorder errorRecorder;
        public bool autoClearCrashReports;
        public bool autoRecordUnityDebugErrorLog;
    }

#### Constructors

	public RUMConfig(string endpoint, int pid, string secretKey, string appVersion, string uid = null);

**Parameters:**

* **endpoint**

	RUM server endpoint. Can be retrieved on the web console.

* **pid**

	RUM project id. Can be retrieved on the web console.

* **secretKey**

	RUM secret key. Can be retrieved on the web console.

* **appVersion**

	Version for user's application.

* **uid**

	User's ID. Can be null.

#### Network Section

Network section including the network parameters, and a RegressiveStrategy section.

* **public int network.globalConnectTimeout**

	The connecting timeout for RUM client. Default is 30 seconds. Unit: second(s).

* **public int network.globalQuestTimeout**

	The event quest timeout for RUM client. Default is 30 seconds. Unit: second(s).

* **public int network.bandwidthInKBPS**

	The bandwidth can be used by RUM client. Default is 256 KB/s. Unit: KB/s (not kb/s).

	**This parameter can be reconfigured dynamically in RUM console, even if the application has been releaseed or installed on user's devices.**


#### Network.RegressiveStrategy Section

Network.RegressiveStrategy section is including the strategy for the RUM client reconnecting action when the network is reachable. 

* **public int network.regressiveStrategy.connectFailedMaxIntervalMilliseconds**

	The threshold for the connection flash off.

	From the connection established to broken, how long time can be identified as a flash off.

	A flash off is recognized as an invalid connection.

	Default is 2000 milliseconds. Unit: millisecond(s).

* **public int network.regressiveStrategy.startConnectFailedCount**

	How many times of invalid connecting later, start exectuing this strategy.

	Default is 2 times.

* **public int network.regressiveStrategy.maxIntervalSeconds**

	The max interval for RUM SDK client reconnecting. Default is 600 seconds (10 minutes). Unit: second(s).

* **public int network.regressiveStrategy.linearRegressiveCount**

	How many times can be retried before the reconnecting interval touching the max threshold. Default is 5 times.


#### Cache Section

* **public int cache.maxDiskCachedSizeInMB**

	Max disk cached size in MB. Default is 200 MB, max limitation is 1024 MB. Unit: MB.

* **public int cache.maxMemoryCachedSizeInMB**

	Max memory cached size in MB. Default is 24 MB, mix and max limitation are 8 MB and 64 MB. Unit: MB.

* **public int cache.maxFileSizeForCacheFileInKB**

	Max size for cached file in KB. Default is 4096 KB, mix and max limitation are 256 KB and 64 MB. Unit: KB.

* **public int cache.memorySyncToDiskIntervalMilliseconds**

	Interval for memory & disk size checking, and sync data from memory to disk. Default is 1000 milliseconds. Unit: millisecond(s).

#### Other Section

* **public com.fpnn.common.ErrorRecorder errorRecorder**

	An error recorder will be used inside of RUM SDK, which is an instance of com.fpnn.common.ErrorRecorder. Default is null.

	More detail refer: [Definition of ErrorRecorder](https://github.com/highras/fpnn-sdk-unity/blob/master/Assets/Plugins/fpnn/common/ErrorRecorder.cs)

* **public bool autoClearCrashReports**

	Auto clear the unity gathered crash reports after UnityEngine.CrashReport checked. Default is true.

* **public bool autoRecordUnityDebugErrorLog**

	Auto report the log recored by Debug.LogError(). Default is false.


## Actions

### Set & Change Uid

	public static void RUM.SetUid(string uid);

* **uid**

	User Id.

### Start New Session

	public static void RUM.NewSession();

Close current session, and start new session.

Typical scenario:

	+ User logout
	+ User relogin
	+ Reactived after long time idle

## Manual Events

### Custom Event

	static public void RUM.CustomEvent(string eventName, IDictionary<string, object> attributes);

* **eventName**

	Customized event name. Cannot be null or empty. Cannot using the reserved event names of RUM.

* **attributes**

	Customized information. Can be null.

### Custom Error Event

	static public void RUM.CustomError(string tag, string message, System.Exception exception = null, IDictionary<string, object> attributes = null);

* **tag**

	Customized error type or tag.

* **message**

	Error message.

* **exception**

	Exception if exist.

* **attributes**

	Customized information. Can be null.

### Http Event

	static public void RUM.HttpEvent(string url, string method, int status, long reqsize, long respsize, int latency, IDictionary<string, object> attributes = null);

	static public void RUM.HttpEvent(System.Net.Http.HttpResponseMessage res, int latency, IDictionary<string, object> attributes = null);

	static public void RUM.HttpEvent(UnityEngine.Networking.UnityWebRequest req, int latency, IDictionary<string, object> attributes = null);

* **method**

	"GET" or "POST".

* **status**

	Http status code.

* **reqsize**

	Request package length.

* **respsize**

	Response package length.

* **latency**

	Request time cost in milliseconds.

* **attributes**

	Customized information. Can be null.

### Loading Event

	static public void RUM.LoadingEvent(string loadingType, int percentageProcess);
	static public void RUM.InitialLoadingEvent(int percentageProcess);
	static public void RUM.InitialLoadingBeginEvent();
	static public void RUM.InitialLoadingFinishEvent();

* **loadingType**

	Loading type. Value "initial" for the primary loading.

* **percentageProcess**

	The process or step for loading. 0 means start a loading process, 100 means a loading process is finished.

	Value range [0, 100] are recommended.


### Register Event

	static public void RUM.UserRegisterBeginEvent(string channel = null);
	static public void RUM.UserRegisterFinishedEvent(string uid, string channel = null);

* **channel**

	The user accessed channel, or promotion channel.

* **uid**

	Registered uid. Can be null when register failed.

### Login Event

	static public void RUM.UserLoginBeginEvent();
	static public void RUM.UserLoginFinishEvent(string uid, bool success, int userLevel = -1);

* **uid**

	Logined uid. Can be null when login failed.

* **success**

	Login whether successful.

* **userLevel**

	User role level. -1 means ignore.

### Tutorial Event

	static public void RUM.TutorialEvent(string tutorialType, int stage, int userLevel = -1);

* **tutorialType**

	Tutorial type.

* **stage**

	Tutorial triggered stage id/order.

* **userLevel**

	User role level. -1 means ignore.


### Task Event

	static public void RUM.TaskBeginEvent(string taskType, string taskId, int userLevel = -1);
	static public void RUM.TaskFinishEvent(string taskType, string taskId, bool success, int userLevel = -1);

* **taskType**

	Task type.

* **taskId**

	Task id.

* **success**

	Task executed result whether successful.

* **userLevel**

	User role level. -1 means ignore.
