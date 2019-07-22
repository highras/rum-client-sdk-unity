using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using GameDevWare.Serialization;

using com.fpnn;
using com.rum;

using UnityEngine;

namespace com.test {

    public class SingleClientSend : Main.ITestCase {

        private int send_qps = 50;
        private int trace_interval = 10;

        private RUMClient _client;

        /**
         *  单客户端实例发送QPS脚本
         */
        public SingleClientSend() {}

        public void StartTest() {

            this._client = new RUMClient(
                41000006,
                "7e592712-01ea-4250-bf39-e51e00c004e9",
                null,
                null,
                // true
                false 
            );

            SingleClientSend self = this;

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

            this._client.Connect("rum-us-frontgate.funplus.com:13609", false, false);

            this.StartThread();
        }

        public void StopTest() {

            this.StopThread();

            if (this._client != null) {

                this._client.Destroy();
            }
        }

        private Thread _thread;
        private bool _sendAble;

        private void StartThread() {

            if (!this._sendAble) {

                this._sendAble = true;

                this._thread = new Thread(new ThreadStart(SendCustomEvent));
                this._thread.Start();
            }
        }

        private void StopThread() {

            this._sendAble = false;

            this._sendCount = 0;
            this._traceTimestamp = 0;
        }

        private void SendCustomEvent() {

            SingleClientSend self = this;

            while(this._sendAble) {

                try {

                    IDictionary<string, object> attrs = new Dictionary<string, object>();
                    attrs.Add("custom_debug", "test text");

                    this._client.CustomEvent("info", attrs);

                    this.SendInc();
                    Thread.Sleep((int) Math.Ceiling((1000f / this.send_qps)));
                }catch(Exception ex) {

                    Debug.Log(ex);
                }
            }
        }

        private int _sendCount;
        private long _traceTimestamp;
        private System.Object inc_locker = new System.Object();

        private void SendInc() {

            lock(inc_locker) {

                this._sendCount++;

                if (this._traceTimestamp <= 0) {

                    this._traceTimestamp = com.fpnn.ThreadPool.Instance.GetMilliTimestamp();
                }

                int interval = (int)((com.fpnn.ThreadPool.Instance.GetMilliTimestamp() - this._traceTimestamp) / 1000);

                if (interval >= this.trace_interval) {

                    Debug.Log(
                        com.fpnn.ThreadPool.Instance.GetMilliTimestamp()
                        + ", trace interval: " + interval
                        + ", send count: " + this._sendCount 
                        + ", send qps: " + (int)(this._sendCount / interval)
                        );

                    this._sendCount = 0;
                    this._traceTimestamp = com.fpnn.ThreadPool.Instance.GetMilliTimestamp();
                }
            }
        }   
    }
}