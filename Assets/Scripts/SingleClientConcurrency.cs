using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using GameDevWare.Serialization;

using com.fpnn;
using com.rum;

using UnityEngine;

public class SingleClientConcurrency : Main.ITestCase {

    private class ThreadLocker {

        public int Status = 0;
    }

    private const int THREAD_COUNT = 50;

    private bool trace_log = false;

    private object self_locker = new object();
    private ThreadLocker thread_locker = new ThreadLocker();

    private RUMClient _client;

    public RUMClient GetClient() {
        return this._client;
    }

    /**
     *  单客户端实例并发脚本
     */
    public SingleClientConcurrency() {}

    private DateTime _startTime;

    public void StartTest() {
        this._client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, true);
        this._client.GetEvent().AddListener("close", (evd) => {
            Debug.Log("[ CONCURRENCY ] closed!");
        });
        this._client.GetEvent().AddListener("ready", (evd) => {
            Debug.Log("[ CONCURRENCY ] ready!");
        });
        this._client.GetEvent().AddListener("config", (evd) => {
            Debug.Log("[ CONCURRENCY ] config!");
        });
        this._client.GetEvent().AddListener("error", (evd) => {
            Debug.LogError(evd.GetException());
        });
        this._client.Connect("rum-nx-front.ifunplus.cn:13609");
        this._startTime = DateTime.Now;
        this.SetUid();
        this.StartThread();
    }

    public void StopTest() {
        this.StopThread();

        if (this._client != null) {
            this._client.Destroy();
        }
    }

    private List<Thread> _threads = new List<Thread>(THREAD_COUNT);

    private void StartThread() {
        lock (thread_locker) {
            if (thread_locker.Status != 0) {
                return;
            }

            thread_locker.Status = 1;

            for (int i = THREAD_COUNT; i > 0; i--) {
                Thread t = new Thread(new ThreadStart(RandomAction));
                t.IsBackground = true;
                t.Start();
                this._threads.Add(t);
            }
        }

        if (trace_log) {
            Debug.Log("[ CONCURRENCY ] start thread, count: " + THREAD_COUNT);
        }
    }

    private void StopThread() {
        lock (thread_locker) {
            thread_locker.Status = 0;
            this._threads.Clear();
        }

        if (trace_log) {
            Debug.Log("[ CONCURRENCY ] stop thread");
        }

        this.TraceResult();
    }

    private System.Random _random = new System.Random();

    private void RandomAction() {
        try {
            while (true) {
                int action = 7;
                int sleep = 1000;

                lock (thread_locker) {
                    if (thread_locker.Status == 0) {
                        return;
                    }

                    action = this._random.Next(1, 8);
                    sleep = this._random.Next(1, 6) * 100;
                }

                switch (action) {
                    case 1:
                        this.HttpRequest();
                        break;

                    case 2:
                        this.GetSession();
                        break;

                    case 3:
                        this.GetRumId();
                        break;

                    case 4:
                        // this.SetUid();
                        break;

                    case 5:
                        this.CustomTestEvent();
                        break;

                    case 6:
                        this.InfoTestEvent();
                        break;

                    case 7:
                        this.DebugTestEvent();
                        break;
                }

                Thread.Sleep(sleep);
            }
        } catch (Exception ex) {
            Debug.LogError(ex);
        }
    }

    private int _httpCount;
    // action 1
    private void HttpRequest() {
        if (trace_log) {
            Debug.Log("[ CONCURRENCY ] HttpRequest, httpCount");
        }

        AsyncGetWithWebRequest("https://www.github.com");

        lock (self_locker) {
            this._httpCount++;
        }
    }

    private int _sessionCount;
    // action 2
    private void GetSession() {
        long session = 0;

        if (this._client != null) {
            session = this._client.GetSession();
        }

        if (trace_log) {
            Debug.Log("[ CONCURRENCY ] GetSession, sessionCount");
        }

        lock (self_locker) {
            this._sessionCount++;
        }
    }

    private int _rumidCount;
    // action 3
    private void GetRumId() {
        string rumId = null;

        if (this._client != null) {
            this._client.GetRumId();
        }

        if (trace_log) {
            Debug.Log("[ CONCURRENCY ] GetRumId, rumidCount");
        }

        lock (self_locker) {
            this._rumidCount++;
        }
    }

    private int _uidCount;
    // action 4
    private void SetUid() {
        string uid = String.Format("UserId-{0}", FPManager.Instance.GetMilliTimestamp());

        if (this._client != null) {
            this._client.SetUid(uid);
        }

        if (trace_log) {
            Debug.Log("[ CONCURRENCY ] SetUid, uidCount");
        }

        lock (self_locker) {
            this._uidCount++;
        }
    }

    private int _customCount;
    // action 5
    private void CustomTestEvent() {
        if (trace_log) {
            Debug.Log("[ CONCURRENCY ] CustomTestEvent, customCount");
        }

        if (this._client != null) {
            this._client.CustomEvent("Custom_Test", new Dictionary<string, object>(this.GetAttrs()));
        }

        lock (self_locker) {
            this._customCount++;
        }
    }

    private int _infoCount;
    // action 6
    private void InfoTestEvent() {
        if (trace_log) {
            Debug.Log("[ CONCURRENCY ] InfoTestEvent, infoCount");
        }

        if (this._client != null) {
            this._client.CustomEvent("Info_Test", new Dictionary<string, object>(this.GetAttrs()));
        }

        lock (self_locker) {
            this._infoCount++;
        }
    }

    private int _debugCount;
    // action 7
    private void DebugTestEvent() {
        if (trace_log) {
            Debug.Log("[ CONCURRENCY ] DebugTestEvent, debugCount");
        }

        if (this._client != null) {
            this._client.CustomEvent("Debug_Test", new Dictionary<string, object>(this.GetAttrs()));
        }

        lock (self_locker) {
            this._debugCount++;
        }
    }

    private IDictionary<string, object> _attrs;

    private IDictionary<string, object> GetAttrs() {
        if (this._attrs != null) {
            return this._attrs;
        }

        string[] defined_arr = { "", null, "debug text" };
        IDictionary<string, object> defined_dict = new Dictionary<string, object>() {
            { "dict_1", 0 },
            { "dict_2", -1 },
            { "dict_3", 7777777777777777 },
            { "dict_4", true },
            { "dict_5", false },
            { "dict_6", null },
            { "dict_7", "" },
            { "dict_8", "debug text" },
            { "dict_9", "中文" }
        };
        this._attrs = new Dictionary<string, object>() {
            { "custom", defined_arr },
            { "info", defined_dict },
            { "debug", null }
        };
        return this._attrs;
    }

    private DateTime _stime;

    private void AsyncGetWithWebRequest(string url) {
        this._stime = DateTime.Now;
        var request = (HttpWebRequest) WebRequest.Create(new Uri(url));
        request.BeginGetResponse(new AsyncCallback(ReadCallback), request);
    }

    private void ReadCallback(IAsyncResult asynchronousResult) {
        HttpWebRequest request = (HttpWebRequest) asynchronousResult.AsyncState;
        HttpWebResponse response = (HttpWebResponse) request.EndGetResponse(asynchronousResult);

        using (var streamReader = new StreamReader(response.GetResponseStream())) {
            var resultString = streamReader.ReadToEnd();
        }

        int latency = Convert.ToInt32((DateTime.Now - this._stime).TotalMilliseconds);

        if (this._client != null) {
            this._client.HookHttp(request, response, latency);
        }
    }

    private void TraceResult() {
        int httpCount = 0;
        int sessionCount = 0;
        int rumidCount = 0;
        int uidCount = 0;
        int customCount = 0;
        int infoCount = 0;
        int debugCount = 0;
        int costMs = Convert.ToInt32((DateTime.Now - this._startTime).TotalMilliseconds);

        lock (self_locker) {
            httpCount = this._httpCount;
            sessionCount = this._sessionCount;
            rumidCount = this._rumidCount;
            uidCount = this._uidCount;
            customCount = this._customCount;
            infoCount = this._infoCount;
            debugCount = this._debugCount;
        }

        Debug.Log(String.Format("[ CONCURRENCY ] {0}:{1}, {2}:{3}, {4}:{5}, {6}:{7}, {8}:{9}, {10}:{11}, {12}:{13}, {14}:{15}"
                , "costMs", costMs
                , "httpCount", httpCount
                , "sessionCount", sessionCount
                , "rumidCount", rumidCount
                , "uidCount", uidCount
                , "customCount", customCount
                , "infoCount", infoCount
                , "debugCount", debugCount));
    }
}