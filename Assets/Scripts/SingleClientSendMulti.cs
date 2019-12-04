using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using GameDevWare.Serialization;

using com.fpnn;
using com.rum;

using UnityEngine;

public class SingleClientSendMulti : Main.ITestCase {

    private class SendLocker {

        public int Status = 0;
    }

    private int send_qps = 200;
    private int trace_interval = 5;
    private int batch_count = 2;

    private SendLocker send_locker = new SendLocker();

    private RUMClient _client;
    public RUMClient GetClient() {
        return this._client;
    }

    /**
     *  单客户端实例发送QPS脚本
     */
    public SingleClientSendMulti() {}

    public void StartTest() {
        SingleClientSendMulti self = this;
        this._client = new RUMClient(1000002, "594968dd-500c-45f6-9341-6fa5138643d6", null, null, true);
        this._client.GetEvent().AddListener("close", (evd) => {
            Debug.Log("test closed!");
        });
        this._client.GetEvent().AddListener("ready", (evd) => {
            Debug.Log("test ready!");
        });
        this._client.GetEvent().AddListener("config", (evd) => {
            Debug.Log("test config!");
        });
        this._client.GetEvent().AddListener("error", (evd) => {
            Debug.Log(evd.GetException());
        });
        this._client.SetUid("uid:11111111111");
        this._client.Connect("rum-nx-front.ilivedata.com:13609");
        this.StartThread();
    }

    public void StopTest() {
        this.StopThread();

        if (this._client != null) {
            this._client.Destroy();
        }
    }

    private List<Thread> _threads;

    private void StartThread() {
        lock (send_locker) {
            if (send_locker.Status != 0) {
                return;
            }

            send_locker.Status = 1;

            this._threads = new List<Thread>();

            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(new ThreadStart(SendCustomEvent));
                // t.IsBackground = true;
                t.Start();

                this._threads.Add(t);
            }
        }
    }

    private void StopThread() {
        lock (send_locker) {
            send_locker.Status = 0;
            this._sendCount = 0;
            this._traceTimestamp = 0;
        }
    }

    private void SendCustomEvent() {
        SingleClientSendMulti self = this;

        try {
            while (true) {
                lock (send_locker)
                {
                    if (send_locker.Status == 0)
                    {
                        return;
                    }

                    //batch_count = (send_qps < 10) ? send_qps : batch_count;
                }

                    for (int i = 0; i < this.batch_count; i++) {
                        IDictionary<string, object> attrs = new Dictionary<string, object>();
                        attrs.Add("custom_debug", "{\"json\":\"text\"}");
                        attrs.Add("custom_Test", "ts: " + FPManager.Instance.GetMilliTimestamp());
                        attrs.Add("custom_data", "dsdadsd asdasdad asde aedsfdsfewfesfdsfdsewfewf esfds fdsffef ef dsfdsfdsfe wefes fs");
                        this._client.CustomEvent("Custom_Test", new Dictionary<string, object>(attrs));
                            // this._client.CustomEvent("Debug_Test", new Dictionary<string, object>(attrs));
                        // this._client.CustomEvent("Info_Test", new Dictionary<string, object>(attrs));

                    lock (send_locker)
                    {
                        this.SendInc();
                    }
                }

                if (this.send_qps > 0) {
                    Thread.Sleep((int) Math.Ceiling((1000f / this.send_qps) * this.batch_count));
                } else {
                    Thread.Sleep(1000);
                }
            }
        } catch (Exception ex) {
            Debug.Log(ex);
        }
    }

    private int _sendCount;
    private long _traceTimestamp;

    private void SendInc() {
        this._sendCount++;

        if (this._traceTimestamp <= 0) {
            this._traceTimestamp = com.fpnn.FPManager.Instance.GetMilliTimestamp();
        }

        int interval = (int)((com.fpnn.FPManager.Instance.GetMilliTimestamp() - this._traceTimestamp) / 1000);

        if (interval >= this.trace_interval) {
            Debug.Log(
                com.fpnn.FPManager.Instance.GetMilliTimestamp()
                + ", trace interval: " + interval
                + ", send count: " + this._sendCount
                + ", send qps: " + (int)(this._sendCount / interval)
            );
            this._sendCount = 0;
            this._traceTimestamp = com.fpnn.FPManager.Instance.GetMilliTimestamp();
        }
    }
}