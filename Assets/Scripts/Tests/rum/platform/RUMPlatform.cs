using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using GameDevWare.Serialization;
using com.fpnn;

using UnityEngine;
using UnityEngine.Networking;

namespace com.rum {

    public class RUMPlatform:Singleton<RUMPlatform> {

        /**
         *  NotReachable                     Network is not reachable. (NONE)
         *  ReachableViaCarrierDataNetwork   Network is reachable via carrier data network. (2G/3G/4G)
         *  ReachableViaLocalAreaNetwork     Network is reachable via WiFi or cable. (WIFI)
         */
        public static string Network;

        /**
         *  Unknown         Application install mode unknown.
         *  Store           Application installed via online store.
         *  DeveloperBuild  Application installed via developer build.
         *  Adhoc           Application installed via ad hoc distribution.
         *  Enterprise      Application installed via enterprise distribution.
         *  Editor          Application running in editor.
         */
        public static string InstallMode;

        public static string SystemLanguage;
        public static string DeviceModel;
        public static string OperatingSystem;
        public static int ScreenHeight;
        public static int ScreenWidth;
        public static bool IsMobilePlatform;
        public static int SystemMemorySize;
        public static string UnityVersion;
        public static string SecureDataPath;

        public static string Manu;
        public static string SystemVersion;
        public static string Carrier;
        public static string From;

        public static bool HasInit() {

            lock (lock_obj) {

                return RUMPlatform.isInit;
            }
        }

        public FPEvent Event = new FPEvent();

        private bool _isPause;
        private bool _isFocus;
        private NetworkReachability _network;
        private IDictionary<string, object> _infoDict;

        private static bool isInit;
        private static object lock_obj = new object();

        public void Init(LocationService location) {

            this._locationService = location;
        }

        IEnumerator Start() {

            while (true) {

                yield return new WaitForSeconds(10.0f);
                this.OnTimer();
            }
        }

        void Awake() {}
        void OnEnable() {

            this._isPause = false;
            this._isFocus = false;
            this._network = Application.internetReachability;

            Application.lowMemory += OnLowMemory;
            Application.logMessageReceived += OnLogCallback;
            Application.logMessageReceivedThreaded += OnLogCallbackThreaded;

            StartCoroutine(GEO());
            StartCoroutine(FPS());

            if (!this.IsInvoking("OnInfo")) {

                this.InvokeRepeating("OnInfo", 1.0f, 0.2f);
            }

            lock (lock_obj) {

                if (RUMPlatform.isInit) {

                    return;
                }

                RUMPlatform.Network = this._network.ToString();
                RUMPlatform.SystemLanguage = Application.systemLanguage.ToString();
                RUMPlatform.DeviceModel = SystemInfo.deviceModel;
                RUMPlatform.OperatingSystem = SystemInfo.operatingSystem;
                RUMPlatform.ScreenHeight = Screen.height;
                RUMPlatform.ScreenWidth = Screen.width;
                RUMPlatform.IsMobilePlatform = Application.isMobilePlatform;
                RUMPlatform.SystemMemorySize = SystemInfo.systemMemorySize;
                RUMPlatform.UnityVersion = Application.unityVersion;
                RUMPlatform.InstallMode = Application.installMode.ToString();
                RUMPlatform.SecureDataPath = this.GetSecureDataPath();

                this._infoDict = new Dictionary<string, object>() {

                    //网络类型信息
                    { "network", RUMPlatform.Network },
                    //系统语言信息
                    { "systemLanguage", RUMPlatform.SystemLanguage },
                    //设备型号信息
                    { "deviceModel", RUMPlatform.DeviceModel },
                    //操作系统信息
                    { "operatingSystem", RUMPlatform.OperatingSystem },
                    //屏幕分辨率高度
                    { "screenHeight", RUMPlatform.ScreenHeight },
                    //屏幕分辨率宽度
                    { "screenWidth", RUMPlatform.ScreenWidth },
                    //是否移动设备
                    { "isMobile", RUMPlatform.IsMobilePlatform },
                    //系统内存(MB)
                    { "systemMemorySize", RUMPlatform.SystemMemorySize },
                    //Unity版本信息
                    { "unityVersion", RUMPlatform.UnityVersion },
                    //应用安装模式
                    { "installMode", RUMPlatform.InstallMode },
                    //支持多种复制纹理功能的情况
                    { "copyTextureSupport", SystemInfo.copyTextureSupport.ToString() },
                    //返回程序运行所在的设备类型
                    { "deviceType", SystemInfo.deviceType.ToString() },
                    //显卡的类型
                    { "graphicsDeviceType", SystemInfo.graphicsDeviceType.ToString() },
                    //GPU支持的NPOT纹理
                    { "npotSupport", SystemInfo.npotSupport.ToString() },
                    //TimeZone
                    { "timeZone", DateTime.Now.ToString("%z") }
                };

                RUMPlatform.isInit = true;
                Debug.Log("[RUMPlatform] Init Complete!");
            }
        }

        void OnDisable() {

            Application.lowMemory -= OnLowMemory;
            Application.logMessageReceived -= OnLogCallback;
            Application.logMessageReceivedThreaded -= OnLogCallbackThreaded;

            StopAllCoroutines();

            if (this.IsInvoking("OnInfo")) {

                this.CancelInvoke("OnInfo");
            }
        }

        void OnApplicationPause() {
 
            if (!this._isPause) {
                 
                this.Event.FireEvent(new EventData("app_bg", new Dictionary<string, object>()));
            } else {

                this._isFocus = true;
            }

            this._isPause = true;
        }

        void OnApplicationFocus() {

            if (this._isFocus) {
             
                this._isPause = false;
                this._isFocus = false;
            }
             
            if (this._isPause) {

                this._isFocus = true;
                this.Event.FireEvent(new EventData("app_fg", new Dictionary<string, object>()));
            }
        }

        private float _latitude = 0;
        private float _longitude = 0;
        private LocationInfo _locationInfo;
        private LocationService _locationService;

        private IEnumerator GEO() {

            yield return new WaitForSeconds(10.0f);

            if (this._locationService == null) {

                yield break;
            }

            while (true) {

                int maxWait = 20;

                if (this._locationService.isEnabledByUser) {

                    if (this._locationService.status == LocationServiceStatus.Stopped) {

                        try {

                            this._locationService.Start();
                        } catch(Exception ex) {

                            ErrorRecorderHolder.recordError(ex);
                        }

                        yield return new WaitForSeconds(1.0f);
                    }

                    while (this._locationService.status == LocationServiceStatus.Initializing) {

                        if (maxWait > 0) {

                            maxWait--;
                        } else {

                            try {

                                this._locationService.Stop();
                            } catch (Exception ex) {

                                ErrorRecorderHolder.recordError(ex);
                            }
                        }

                        yield return new WaitForSeconds(1.0f);
                    }

                    if (this._locationService.status == LocationServiceStatus.Running) {

                        try {

                            this._locationInfo = this._locationService.lastData;
                            this._locationService.Stop();
                        } catch (Exception ex) {

                            ErrorRecorderHolder.recordError(ex);
                        }
                    }
                }

                yield return new WaitForSeconds(maxWait > 0 ? maxWait * 1.0f : 1.0f);
            }
        }

        private float _lastTime;
        private int _lastFrameCount;
        private List<string> _fpsList = new List<string>(25);

        private IEnumerator FPS() {

            while (true) {

                yield return new WaitForSeconds(1.0f);

                try {

                    float fps = 0;
                    float now = Time.realtimeSinceStartup;
                    int count = Time.frameCount;

                    if (this._lastTime > 0) {

                        float timeSpan = now - this._lastTime;
                        int frameCount = count - this._lastFrameCount;

                        // fps = Mathf.RoundToInt(frameCount / timeSpan);
                        fps = frameCount / timeSpan;
                    }

                    this._lastFrameCount = count;
                    this._lastTime = now;

                    if (fps > 0.0f && this._fpsList.Count < 25) {

                        this._fpsList.Add(fps.ToString());
                    }
                } catch (Exception ex) {

                    ErrorRecorderHolder.recordError(ex);
                }
            }
        }

        private void OnLowMemory() {

            IDictionary<string, object> dict = new Dictionary<string, object>() {

                { "type", "low_memory" },
                { "system_memory", RUMPlatform.SystemMemorySize }
            };

            this.Event.FireEvent(new EventData("memory_low", dict));
        }

        private void OnTimer() {

            //netwrok_switch
            NetworkReachability network = Application.internetReachability;

            if (this._network != network) {

                this._network = network;
                RUMPlatform.Network = this._network.ToString();

                IDictionary<string, object> dict = new Dictionary<string, object>() {

                    { "nw", RUMPlatform.Network }
                };

                this.Event.FireEvent(new EventData("netwrok_switch", dict));
            }

            //fps_update
            if (this._fpsList.Count > 0) {

                List<string> fps_list = new List<string>(this._fpsList);
                this._fpsList.Clear();

                this.Event.FireEvent(new EventData("fps_update", fps_list));
            }

            //geo_update
            double distance = 0;

            try {

                if (this._locationInfo.longitude != this._longitude || this._locationInfo.latitude != this._latitude) {

                    distance = this.GetDistance(this._locationInfo.longitude, this._locationInfo.latitude, this._longitude, this._latitude);
                }
            } catch(Exception ex) {

                ErrorRecorderHolder.recordError(ex);
            }

            if (distance >= 10) {

                if (this._longitude == 0 && this._latitude == 0) {

                    distance = 0;
                }
                
                this._longitude = this._locationInfo.longitude;
                this._latitude = this._locationInfo.latitude;

                IDictionary<string, object> dict = new Dictionary<string, object>() {

                    { "latitude", this._locationInfo.latitude.ToString() }, 
                    { "longitude", this._locationInfo.longitude.ToString() },
                    { "altitude", this._locationInfo.altitude.ToString() },
                    { "horizontalAccuracy", this._locationInfo.horizontalAccuracy.ToString() },
                    { "verticalAccuracy", this._locationInfo.verticalAccuracy.ToString() },
                    { "distance", distance.ToString() },
                    { "timestamp", this._locationInfo.timestamp.ToString() }
                };

                this.Event.FireEvent(new EventData("geo_update", dict));
            }
        }

        private void OnInfo() {

            //用户定义的设备名称
            if (!this._infoDict.ContainsKey("deviceName")) {
                this._infoDict.Add("deviceName", SystemInfo.deviceName);
                return;
            }
            //设备的唯一标识符, 每一台设备都有唯一的标识符
            if (!this._infoDict.ContainsKey("deviceUniqueIdentifier")) {
                this._infoDict.Add("deviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier);
                return;
            }
            //显卡的唯一标识符ID
            if (!this._infoDict.ContainsKey("graphicsDeviceID")) {
                this._infoDict.Add("graphicsDeviceID", SystemInfo.graphicsDeviceID);
                return;
            }
            //显卡的名称
            if (!this._infoDict.ContainsKey("graphicsDeviceName")) {
                this._infoDict.Add("graphicsDeviceName", SystemInfo.graphicsDeviceName);
                return;
            }
            //显卡的供应商
            if (!this._infoDict.ContainsKey("graphicsDeviceVendor")) {
                this._infoDict.Add("graphicsDeviceVendor", SystemInfo.graphicsDeviceVendor);
                return;
            }
            //显卡供应商的唯一识别码ID
            if (!this._infoDict.ContainsKey("graphicsDeviceVendorID")) {
                this._infoDict.Add("graphicsDeviceVendorID", SystemInfo.graphicsDeviceVendorID);
                return;
            }
            //显卡的类型和版本
            if (!this._infoDict.ContainsKey("graphicsDeviceVersion")) {
                this._infoDict.Add("graphicsDeviceVersion", SystemInfo.graphicsDeviceVersion);
                return;
            }
            //显存大小(MB)
            if (!this._infoDict.ContainsKey("graphicsMemorySize")) {
                this._infoDict.Add("graphicsMemorySize", SystemInfo.graphicsMemorySize);
                return;
            }
            //是否支持多线程渲染
            if (!this._infoDict.ContainsKey("graphicsMultiThreaded")) {
                this._infoDict.Add("graphicsMultiThreaded", SystemInfo.graphicsMultiThreaded);
                return;
            }
            //显卡着色器的级别
            if (!this._infoDict.ContainsKey("graphicsShaderLevel")) {
                this._infoDict.Add("graphicsShaderLevel", SystemInfo.graphicsShaderLevel);
                return;
            }
            //支持的最大纹理大小
            if (!this._infoDict.ContainsKey("maxTextureSize")) {
                this._infoDict.Add("maxTextureSize", SystemInfo.maxTextureSize);
                return;
            }
            //当前处理器的数量
            if (!this._infoDict.ContainsKey("processorCount")) {
                this._infoDict.Add("processorCount", SystemInfo.processorCount);
                return;
            }
            //处理器的频率
            if (!this._infoDict.ContainsKey("processorFrequency")) {
                this._infoDict.Add("processorFrequency", SystemInfo.processorFrequency);
                return;
            }
            //处理器的名称
            if (!this._infoDict.ContainsKey("processorType")) {
                this._infoDict.Add("processorType", SystemInfo.processorType);
                return;
            }
            //支持渲染多少目标纹理
            if (!this._infoDict.ContainsKey("supportedRenderTargetCount")) {
                this._infoDict.Add("supportedRenderTargetCount", SystemInfo.supportedRenderTargetCount);
                return;
            }
            //是否支持2D数组纹理
            if (!this._infoDict.ContainsKey("supports2DArrayTextures")) {
                this._infoDict.Add("supports2DArrayTextures", SystemInfo.supports2DArrayTextures);
                return;
            }
            //是否支持3D（体积）纹理
            if (!this._infoDict.ContainsKey("supports3DTextures")) {
                this._infoDict.Add("supports3DTextures", SystemInfo.supports3DTextures);
                return;
            }
            //是否支持获取加速度计
            if (!this._infoDict.ContainsKey("supportsAccelerometer")) {
                this._infoDict.Add("supportsAccelerometer", SystemInfo.supportsAccelerometer);
                return;
            }
            //是否支持获取用于回放的音频设备
            if (!this._infoDict.ContainsKey("supportsAudio")) {
                this._infoDict.Add("supportsAudio", SystemInfo.supportsAudio);
                return;
            }
            //是否支持计算着色器
            if (!this._infoDict.ContainsKey("supportsComputeShaders")) {
                this._infoDict.Add("supportsComputeShaders", SystemInfo.supportsComputeShaders);
                return;
            }
            //是否支持获取陀螺仪
            if (!this._infoDict.ContainsKey("supportsGyroscope")) {
                this._infoDict.Add("supportsGyroscope", SystemInfo.supportsGyroscope);
                return;
            }
            //是否支持图形特效 always returns true
            if (!this._infoDict.ContainsKey("supportsImageEffects")) {
                this._infoDict.Add("supportsImageEffects", SystemInfo.supportsImageEffects);
                return;
            }
            //是否支持实例化GPU的Draw Call
            if (!this._infoDict.ContainsKey("supportsInstancing")) {
                this._infoDict.Add("supportsInstancing", SystemInfo.supportsInstancing);
                return;
            }
            //是否支持定位功能
            if (!this._infoDict.ContainsKey("supportsLocationService")) {
                this._infoDict.Add("supportsLocationService", SystemInfo.supportsLocationService);
                return;
            }
            //是否支持运动向量
            if (!this._infoDict.ContainsKey("supportsMotionVectors")) {
                this._infoDict.Add("supportsMotionVectors", SystemInfo.supportsMotionVectors);
                return;
            }
            //是否支持阴影深度
            if (!this._infoDict.ContainsKey("supportsRawShadowDepthSampling")) {
                this._infoDict.Add("supportsRawShadowDepthSampling", SystemInfo.supportsRawShadowDepthSampling);
                return;
            }
            //是否支持渲染纹理 always returns true
            if (!this._infoDict.ContainsKey("supportsRenderTextures")) {
                this._infoDict.Add("supportsRenderTextures", SystemInfo.supportsRenderTextures);
                return;
            }
            //是否支持立方体纹理 always returns true
            if (!this._infoDict.ContainsKey("supportsRenderToCubemap")) {
                this._infoDict.Add("supportsRenderToCubemap", SystemInfo.supportsRenderToCubemap);
                return;
            }
            //是否支持内置阴影
            if (!this._infoDict.ContainsKey("supportsShadows")) {
                this._infoDict.Add("supportsShadows", SystemInfo.supportsShadows);
                return;
            }
            //是否支持稀疏纹理
            if (!this._infoDict.ContainsKey("supportsSparseTextures")) {
                this._infoDict.Add("supportsSparseTextures", SystemInfo.supportsSparseTextures);
                return;
            }
            //是否支持模版缓存 always returns true
            if (!this._infoDict.ContainsKey("supportsStencil")) {
                this._infoDict.Add("supportsStencil", SystemInfo.supportsStencil);
                return;
            }
            //是否支持用户触摸震动反馈
            if (!this._infoDict.ContainsKey("supportsVibration")) {
                this._infoDict.Add("supportsVibration", SystemInfo.supportsVibration);
                return;
            }
            //不支持运行在当前设备的SystemInfo属性值
            if (!this._infoDict.ContainsKey("unsupportedIdentifier")) {
                this._infoDict.Add("unsupportedIdentifier", SystemInfo.unsupportedIdentifier);
                return;
            }
            //SecureDataPath
            if (!this._infoDict.ContainsKey("secureDataPath")) {
                this._infoDict.Add("secureDataPath", RUMPlatform.SecureDataPath);
                return;
            }
            //AndroidId only for android 
            if (!this._infoDict.ContainsKey("androidId")) {
                this._infoDict.Add("androidId", this.GetAndroidId());
                return;
            }
            //DeviceToken only for ios
            if (!this._infoDict.ContainsKey("deviceToken")) {
                this._infoDict.Add("deviceToken", this.GetIOSDeviceToken());
                return;
            }
            //vendorIdentifier only for ios
            if (!this._infoDict.ContainsKey("vendorIdentifier")) {
                this._infoDict.Add("vendorIdentifier", this.GetIOSVendorIdentifier());
                return;
            }

            this.Event.FireEvent(new EventData("system_info", this._infoDict));

            if (this.IsInvoking("OnInfo")) {

                this.CancelInvoke("OnInfo");
            }
        }

        private string GetSecureDataPath() {

            string secureDataPath = Application.temporaryCachePath;

            try {

                #if !UNITY_EDITOR && UNITY_IPHONE
                secureDataPath = Application.persistentDataPath;
                #elif !UNITY_EDITOR && UNITY_ANDROID
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var getFilesDir = currentActivity.Call<AndroidJavaObject>("getFilesDir")) {
                    secureDataPath = getFilesDir.Call<string>("getCanonicalPath");
                }
                #endif
            } catch (Exception ex) {

                ErrorRecorderHolder.recordError(ex);
            }

            return secureDataPath;
        }

        private string GetAndroidId() {

            string androidId = null;

            try {

                #if !UNITY_EDITOR && UNITY_ANDROID
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var contentResolver = currentActivity.Call<AndroidJavaObject> ("getContentResolver"))
                using (var secure = new AndroidJavaClass ("android.provider.Settings$Secure")) {
                    androidId = secure.CallStatic<string> ("getString", contentResolver, "android_id");
                }
                #endif
            } catch (Exception ex) {

                ErrorRecorderHolder.recordError(ex);
            }

            return androidId;
        }

        private string GetIOSDeviceToken() {

            string deviceToken = null;

            try {

                #if !UNITY_EDITOR && UNITY_IPHONE
                byte[] token = UnityEngine.iOS.NotificationServices.deviceToken;
                if (token != null) {
                    deviceToken = System.BitConverter.ToString(token).Replace("-", "");
                }
                #endif
            } catch(Exception ex) {

                ErrorRecorderHolder.recordError(ex);
            }

            return deviceToken;
        }

        private string GetIOSVendorIdentifier() {

            string vendorIdentifier = null;

            try {

                #if !UNITY_EDITOR && UNITY_IPHONE
                vendorIdentifier = UnityEngine.iOS.Device.vendorIdentifier;
                #endif
            } catch(Exception ex) {

                ErrorRecorderHolder.recordError(ex);
            }

            return vendorIdentifier;
        }

        private void OnLogCallback(string logString, string stackTrace, LogType type) {

            if (type == LogType.Assert) {

                this.OnException("crash", "main_assert", logString, stackTrace);
            }

            if (type == LogType.Exception) {

                this.OnException("error", "main_exception", logString, stackTrace);
            }
        }

        private void OnLogCallbackThreaded(string logString, string stackTrace, LogType type) {

            if (type == LogType.Exception) {

                this.OnException("error", "threaded_exception", logString, stackTrace);
            }
        }

        private void OnException(string ev, string type, string message, string stack) {

            IDictionary<string, object> dict = new Dictionary<string, object>() {

                { "ev", ev },
                { "type", type },
                { "message", message },
                { "stack", stack }
            };

            this.Event.FireEvent(new EventData("system_exception", dict));
       }

        //地球半径, 单位: 米
        private const double EARTH_RADIUS = 6378137;

        /**
         *  计算两点位置的距离, 返回两点的距离, 单位: 米
         *  该公式为GOOGLE提供, 误差小于0.2米
         */
        private double GetDistance(double lng1, double lat1, double lng2, double lat2) {

            double radLat1 = this.Rad(lat1);
            double radLng1 = this.Rad(lng1);
            double radLat2 = this.Rad(lat2);
            double radLng2 = this.Rad(lng2);
            double a = radLat1 - radLat2;
            double b = radLng1 - radLng2;

            return 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) + Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2))) * EARTH_RADIUS;
        }

        /**
         *  经纬度转化成弧度
         */
        private double Rad(double d) {

            return (double)d * Math.PI / 180d;
        }
    }
}