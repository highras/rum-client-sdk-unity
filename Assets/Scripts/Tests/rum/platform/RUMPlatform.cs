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

    public class RUMPlatform : Singleton<RUMPlatform> {

        public const string PLATFORM_EVENT = "platform_event";

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
        private bool _isBackground;
        private NetworkReachability _network;

        private static bool isInit;
        private static object lock_obj = new object();

        public void Init(LocationService location) {
            this._locationService = location;
        }

        IEnumerator Start() {
            yield return new WaitForSeconds(10.0f);

            while (true) {
                this.OnTimer();
                yield return new WaitForSeconds(10.0f);
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
                RUMPlatform.isInit = true;
                Debug.Log("[RUMPlatform] Init Complete!");
            }

            StartCoroutine(GEO());
            StartCoroutine(FPS());
            StartCoroutine(INFO());
        }

        void OnDisable() {
            Application.lowMemory -= OnLowMemory;
            Application.logMessageReceived -= OnLogCallback;
            Application.logMessageReceivedThreaded -= OnLogCallbackThreaded;
            StopAllCoroutines();
        }

        void OnApplicationPause() {
            if (!this._isPause) {
                if (!this._isBackground) {
                    IDictionary<string, object> dict = new Dictionary<string, object>() {
                        {
                            "ev", "bg"
                        }
                    };
                    this._isBackground = true;
                    this.Event.FireEvent(new EventData(PLATFORM_EVENT, dict));
                }
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

                if (this._isBackground) {
                    IDictionary<string, object> dict = new Dictionary<string, object>() {
                        {
                            "ev", "fg"
                        }
                    };
                    this._isBackground = false;
                    this.Event.FireEvent(new EventData(PLATFORM_EVENT, dict));
                }
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
                if (this._locationService.isEnabledByUser) {
                    if (this._locationService.status == LocationServiceStatus.Running) {
                        try {
                            this._locationInfo = this._locationService.lastData;
                        } catch (Exception ex) {
                            ErrorRecorderHolder.recordError(ex);
                        }
                    }
                }

                yield return new WaitForSeconds(10.0f);
            }
        }

        private double _lastFpsTime;
        private int _lastFrameCount;

        private IEnumerator FPS() {
            while (true) {
                yield return new WaitForSeconds(1.0f);

                try {
                    double fps = 0.0f;
                    double now = Time.realtimeSinceStartup;
                    int count = Time.frameCount;

                    if (this._lastFpsTime > 0) {
                        double timeSpan = now - this._lastFpsTime;
                        int frameCount = count - this._lastFrameCount;
                        // fps = Mathf.RoundToInt(frameCount / timeSpan);
                        fps = frameCount / timeSpan;
                    }

                    this._lastFrameCount = count;
                    this._lastFpsTime = now;
                    this.ReportFPS(fps, now);
                } catch (Exception ex) {
                    ErrorRecorderHolder.recordError(ex);
                }
            }
        }

        private int _fpsCount = 0;
        private double _fpsTotal = 0.0f;
        private double _fpsAverage = 0.0f;
        private List<object> _fpsList = new List<object>(100);

        private void ReportFPS(double fps, double now) {
            if (Double.IsNaN(this._fpsAverage)) {
                return;
            }

            if (Double.IsNaN(fps)) {
                return;
            }

            if (++this._fpsCount == 1) {
                if (this._fpsTotal < 1.0f) {
                    this._fpsTotal = fps;
                } else {
                    this._fpsTotal = (this._fpsAverage + fps) / 2;
                }

                return;
            }

            this._fpsTotal += fps;
            this._fpsAverage = this._fpsTotal / this._fpsCount;

            if (this._fpsCount < 30) {
                return;
            }

            if ((this._fpsCount % 60 == 0) || Math.Abs(this._fpsAverage - fps) >= 3.0f) {
                IDictionary<string, object> dict = new Dictionary<string, object>() {
                    { "rts", Convert.ToString(now) },
                    { "avg", Convert.ToString(this._fpsAverage) },
                    { "fps", Convert.ToString(fps) }
                };

                if (this._fpsList.Count < this._fpsList.Capacity) {
                    this._fpsList.Add(dict);
                }
            }
        }

        private void OnLowMemory() {
            bool needFire = RUMDuplicate.Instance.Check("low_memory", 60);

            if (needFire) {
                IDictionary<string, object> dict = new Dictionary<string, object>() {
                    { "ev", "warn" },
                    { "type", "unity_low_memory" },
                    { "system_memory", SystemInfo.systemMemorySize }
                };
                this.Event.FireEvent(new EventData(PLATFORM_EVENT, dict));
            }
        }

        private void OnTimer() {
            //netwrok_switch
            NetworkReachability network = Application.internetReachability;

            if (this._network != network) {
                this._network = network;
                RUMPlatform.Network = this._network.ToString();
                IDictionary<string, object> dict = new Dictionary<string, object>() {
                    { "ev", "nwswitch"},
                    { "nw", RUMPlatform.Network }
                };
                this.Event.FireEvent(new EventData(PLATFORM_EVENT, dict));
            }

            //fps_update
            bool needFire = RUMDuplicate.Instance.Check("fps_update", 60);

            if ((needFire && this._fpsList.Count > 0) || this._fpsList.Count == this._fpsList.Capacity) {
                List<object> fps_list = new List<object>(this._fpsList);
                IDictionary<string, object> dict = new Dictionary<string, object>() {
                    { "ev", "info" },
                    { "type", "unity_fps_info" },
                    { "fps_info", fps_list }
                };
                this._fpsList.Clear();
                this.Event.FireEvent(new EventData(PLATFORM_EVENT, dict));
            }

            //geo_update
            double distance = 0;
            needFire = RUMDuplicate.Instance.Check("geo_update", 60);

            try {
                if (this._locationInfo.longitude != this._longitude || this._locationInfo.latitude != this._latitude) {
                    distance = this.GetDistance(this._locationInfo.longitude, this._locationInfo.latitude, this._longitude, this._latitude);
                }
            } catch (Exception ex) {
                ErrorRecorderHolder.recordError(ex);
            }

            if ((needFire && distance >= 10) || distance >= 100) {
                if (this._longitude == 0 && this._latitude == 0) {
                    distance = 0;
                }

                this._longitude = this._locationInfo.longitude;
                this._latitude = this._locationInfo.latitude;
                IDictionary<string, object> geo_info = new Dictionary<string, object>() {
                    { "latitude", this._locationInfo.latitude.ToString() },
                    { "longitude", this._locationInfo.longitude.ToString() },
                    { "altitude", this._locationInfo.altitude.ToString() },
                    { "horizontalAccuracy", this._locationInfo.horizontalAccuracy.ToString() },
                    { "verticalAccuracy", this._locationInfo.verticalAccuracy.ToString() },
                    { "distance", distance.ToString() },
                    { "timestamp", this._locationInfo.timestamp.ToString() }
                };
                IDictionary<string, object> dict = new Dictionary<string, object>() {
                    { "ev", "info"},
                    { "type", "unity_geo_info" },
                    { "geo_info", geo_info }
                };
                this.Event.FireEvent(new EventData(PLATFORM_EVENT, dict));
            }
        }

        private IEnumerator INFO() {
            IDictionary<string, object> info = new Dictionary<string, object>();

            while (true) {
                yield return new WaitForSeconds(0.1f);

                try {
                    if (this.OnInfo(info)) {
                        yield break;
                    }
                } catch (Exception ex) {
                    ErrorRecorderHolder.recordError(ex);
                }
            }
        }

        private bool OnInfo(IDictionary<string, object> info) {
            //网络类型信息
            if (!info.ContainsKey("network")) {
                info.Add("network", Application.internetReachability.ToString());
                return false;
            }

            //系统语言信息
            if (!info.ContainsKey("systemLanguage")) {
                info.Add("systemLanguage", RUMPlatform.SystemLanguage);
                return false;
            }

            //设备型号信息
            if (!info.ContainsKey("deviceModel")) {
                info.Add("deviceModel", RUMPlatform.DeviceModel);
                return false;
            }

            //操作系统信息
            if (!info.ContainsKey("operatingSystem")) {
                info.Add("operatingSystem", RUMPlatform.OperatingSystem);
                return false;
            }

            //屏幕分辨率高度
            if (!info.ContainsKey("screenHeight")) {
                info.Add("screenHeight", RUMPlatform.ScreenHeight);
                return false;
            }

            //屏幕分辨率宽度
            if (!info.ContainsKey("screenWidth")) {
                info.Add("screenWidth", RUMPlatform.ScreenWidth);
                return false;
            }

            //是否移动设备
            if (!info.ContainsKey("isMobile")) {
                info.Add("isMobile", RUMPlatform.IsMobilePlatform);
                return false;
            }

            //系统内存(MB)
            if (!info.ContainsKey("systemMemorySize")) {
                info.Add("systemMemorySize", SystemInfo.systemMemorySize);
                return false;
            }

            //Unity版本信息
            if (!info.ContainsKey("unityVersion")) {
                info.Add("unityVersion", RUMPlatform.UnityVersion);
                return false;
            }

            //应用安装模式
            if (!info.ContainsKey("installMode")) {
                info.Add("installMode", RUMPlatform.InstallMode);
                return false;
            }

            //支持多种复制纹理功能的情况
            if (!info.ContainsKey("copyTextureSupport")) {
                info.Add("copyTextureSupport", SystemInfo.copyTextureSupport.ToString());
                return false;
            }

            //返回程序运行所在的设备类型
            if (!info.ContainsKey("deviceType")) {
                info.Add("deviceType", SystemInfo.deviceType.ToString());
                return false;
            }

            //显卡的类型
            if (!info.ContainsKey("graphicsDeviceType")) {
                info.Add("graphicsDeviceType", SystemInfo.graphicsDeviceType.ToString());
                return false;
            }

            //GPU支持的NPOT纹理
            if (!info.ContainsKey("npotSupport")) {
                info.Add("npotSupport", SystemInfo.npotSupport.ToString());
                return false;
            }

            //TimeZone
            if (!info.ContainsKey("timeZone")) {
                info.Add("timeZone", DateTime.Now.ToString("%z"));
                return false;
            }

            //用户定义的设备名称
            if (!info.ContainsKey("deviceName")) {
                info.Add("deviceName", SystemInfo.deviceName);
                return false;
            }

            //设备的唯一标识符, 每一台设备都有唯一的标识符
            if (!info.ContainsKey("deviceUniqueIdentifier")) {
                info.Add("deviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier);
                return false;
            }

            //显卡的唯一标识符ID
            if (!info.ContainsKey("graphicsDeviceID")) {
                info.Add("graphicsDeviceID", SystemInfo.graphicsDeviceID);
                return false;
            }

            //显卡的名称
            if (!info.ContainsKey("graphicsDeviceName")) {
                info.Add("graphicsDeviceName", SystemInfo.graphicsDeviceName);
                return false;
            }

            //显卡的供应商
            if (!info.ContainsKey("graphicsDeviceVendor")) {
                info.Add("graphicsDeviceVendor", SystemInfo.graphicsDeviceVendor);
                return false;
            }

            //显卡供应商的唯一识别码ID
            if (!info.ContainsKey("graphicsDeviceVendorID")) {
                info.Add("graphicsDeviceVendorID", SystemInfo.graphicsDeviceVendorID);
                return false;
            }

            //显卡的类型和版本
            if (!info.ContainsKey("graphicsDeviceVersion")) {
                info.Add("graphicsDeviceVersion", SystemInfo.graphicsDeviceVersion);
                return false;
            }

            //显存大小(MB)
            if (!info.ContainsKey("graphicsMemorySize")) {
                info.Add("graphicsMemorySize", SystemInfo.graphicsMemorySize);
                return false;
            }

            //是否支持多线程渲染
            if (!info.ContainsKey("graphicsMultiThreaded")) {
                info.Add("graphicsMultiThreaded", SystemInfo.graphicsMultiThreaded);
                return false;
            }

            //显卡着色器的级别
            if (!info.ContainsKey("graphicsShaderLevel")) {
                info.Add("graphicsShaderLevel", SystemInfo.graphicsShaderLevel);
                return false;
            }

            //支持的最大纹理大小
            if (!info.ContainsKey("maxTextureSize")) {
                info.Add("maxTextureSize", SystemInfo.maxTextureSize);
                return false;
            }

            //当前处理器的数量
            if (!info.ContainsKey("processorCount")) {
                info.Add("processorCount", SystemInfo.processorCount);
                return false;
            }

            //处理器的频率
            if (!info.ContainsKey("processorFrequency")) {
                info.Add("processorFrequency", SystemInfo.processorFrequency);
                return false;
            }

            //处理器的名称
            if (!info.ContainsKey("processorType")) {
                info.Add("processorType", SystemInfo.processorType);
                return false;
            }

            //支持渲染多少目标纹理
            if (!info.ContainsKey("supportedRenderTargetCount")) {
                info.Add("supportedRenderTargetCount", SystemInfo.supportedRenderTargetCount);
                return false;
            }

            //是否支持2D数组纹理
            if (!info.ContainsKey("supports2DArrayTextures")) {
                info.Add("supports2DArrayTextures", SystemInfo.supports2DArrayTextures);
                return false;
            }

            //是否支持3D（体积）纹理
            if (!info.ContainsKey("supports3DTextures")) {
                info.Add("supports3DTextures", SystemInfo.supports3DTextures);
                return false;
            }

            //是否支持获取加速度计
            if (!info.ContainsKey("supportsAccelerometer")) {
                info.Add("supportsAccelerometer", SystemInfo.supportsAccelerometer);
                return false;
            }

            //是否支持获取用于回放的音频设备
            if (!info.ContainsKey("supportsAudio")) {
                info.Add("supportsAudio", SystemInfo.supportsAudio);
                return false;
            }

            //是否支持计算着色器
            if (!info.ContainsKey("supportsComputeShaders")) {
                info.Add("supportsComputeShaders", SystemInfo.supportsComputeShaders);
                return false;
            }

            //是否支持获取陀螺仪
            if (!info.ContainsKey("supportsGyroscope")) {
                info.Add("supportsGyroscope", SystemInfo.supportsGyroscope);
                return false;
            }

            //是否支持图形特效 always returns true
            if (!info.ContainsKey("supportsImageEffects")) {
                info.Add("supportsImageEffects", SystemInfo.supportsImageEffects);
                return false;
            }

            //是否支持实例化GPU的Draw Call
            if (!info.ContainsKey("supportsInstancing")) {
                info.Add("supportsInstancing", SystemInfo.supportsInstancing);
                return false;
            }

            //是否支持定位功能
            if (!info.ContainsKey("supportsLocationService")) {
                info.Add("supportsLocationService", SystemInfo.supportsLocationService);
                return false;
            }

            //是否支持运动向量
            if (!info.ContainsKey("supportsMotionVectors")) {
                info.Add("supportsMotionVectors", SystemInfo.supportsMotionVectors);
                return false;
            }

            //是否支持阴影深度
            if (!info.ContainsKey("supportsRawShadowDepthSampling")) {
                info.Add("supportsRawShadowDepthSampling", SystemInfo.supportsRawShadowDepthSampling);
                return false;
            }

            //是否支持渲染纹理 always returns true
            if (!info.ContainsKey("supportsRenderTextures")) {
                info.Add("supportsRenderTextures", SystemInfo.supportsRenderTextures);
                return false;
            }

            //是否支持立方体纹理 always returns true
            if (!info.ContainsKey("supportsRenderToCubemap")) {
                info.Add("supportsRenderToCubemap", SystemInfo.supportsRenderToCubemap);
                return false;
            }

            //是否支持内置阴影
            if (!info.ContainsKey("supportsShadows")) {
                info.Add("supportsShadows", SystemInfo.supportsShadows);
                return false;
            }

            //是否支持稀疏纹理
            if (!info.ContainsKey("supportsSparseTextures")) {
                info.Add("supportsSparseTextures", SystemInfo.supportsSparseTextures);
                return false;
            }

            //是否支持模版缓存 always returns true
            if (!info.ContainsKey("supportsStencil")) {
                info.Add("supportsStencil", SystemInfo.supportsStencil);
                return false;
            }

            //是否支持用户触摸震动反馈
            if (!info.ContainsKey("supportsVibration")) {
                info.Add("supportsVibration", SystemInfo.supportsVibration);
                return false;
            }

            //不支持运行在当前设备的SystemInfo属性值
            if (!info.ContainsKey("unsupportedIdentifier")) {
                info.Add("unsupportedIdentifier", SystemInfo.unsupportedIdentifier);
                return false;
            }

            //SecureDataPath
            if (!info.ContainsKey("secureDataPath")) {
                info.Add("secureDataPath", RUMPlatform.SecureDataPath);
                return false;
            }

            //AndroidId only for android
            if (!info.ContainsKey("androidId")) {
                info.Add("androidId", this.GetAndroidId());
                return false;
            }

            //DeviceToken only for ios
            if (!info.ContainsKey("deviceToken")) {
                info.Add("deviceToken", this.GetIOSDeviceToken());
                return false;
            }

            //vendorIdentifier only for ios
            if (!info.ContainsKey("vendorIdentifier")) {
                info.Add("vendorIdentifier", this.GetIOSVendorIdentifier());
                return false;
            }

            IDictionary<string, object> dict = new Dictionary<string, object>() {
                { "ev", "info" },
                { "type", "unity_system_info" },
                { "system_info", info }
            };
            this.Event.FireEvent(new EventData(PLATFORM_EVENT, dict));
            return true;
        }

        private string GetSecureDataPath() {
            string secureDataPath = Application.temporaryCachePath;

            try {
                #if !UNITY_EDITOR && UNITY_IPHONE
                secureDataPath = Application.persistentDataPath;
                #elif !UNITY_EDITOR && UNITY_ANDROID

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                    using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity")) {
                        using (var getFilesDir = currentActivity.Call<AndroidJavaObject>("getFilesDir")) {
                            secureDataPath = getFilesDir.Call<string>("getCanonicalPath");
                        }
                    }
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

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                    using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity")) {
                        using (var contentResolver = currentActivity.Call<AndroidJavaObject> ("getContentResolver")) {
                            using (var secure = new AndroidJavaClass ("android.provider.Settings$Secure")) {
                                androidId = secure.CallStatic<string> ("getString", contentResolver, "android_id");
                            }
                        }
                    }
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
            } catch (Exception ex) {
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
            } catch (Exception ex) {
                ErrorRecorderHolder.recordError(ex);
            }

            return vendorIdentifier;
        }

        private void OnLogCallback(string logString, string stackTrace, LogType type) {
            if (type == LogType.Assert) {
                this.OnException("crash", "unity_main_assert", logString, stackTrace);
            }

            if (type == LogType.Exception) {
                this.OnException("error", "unity_main_exception", logString, stackTrace);
            }
        }

        private void OnLogCallbackThreaded(string logString, string stackTrace, LogType type) {
            if (type == LogType.Exception) {
                this.OnException("error", "unity_threaded_exception", logString, stackTrace);
            }
        }

        private void OnException(string ev, string type, string message, string stack) {
            bool needFire = false;

            if (!string.IsNullOrEmpty(stack)) {
                needFire = RUMDuplicate.Instance.Check(stack, 60);
            } else {
                if (!string.IsNullOrEmpty(message)) {
                    needFire = RUMDuplicate.Instance.Check(message, 60);
                }
            }

            if (needFire) {
                IDictionary<string, object> dict = new Dictionary<string, object>() {
                    { "ev", ev },
                    { "type", type },
                    { "message", message },
                    { "stack", stack }
                };
                this.Event.FireEvent(new EventData(PLATFORM_EVENT, dict));
            }
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