using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using GameDevWare.Serialization;
using UnityEngine;

using com.fpnn;
using com.rum;

namespace com.test {

    public class TestCase : Main.ITestCase {

        private class TestLocker {

            public int Status = 0;
        }

        private RUMClient _client;

        public RUMClient GetClient() {

            lock (test_locker) {

                return this._client;
            }
        }

        public void StartTest() {

            this.StartThread();
        }

        public void StopTest() {

            this.StopThread();
            this.DestroyClinet();
        }

        private void CreateClinet() {

            lock (test_locker) {

                this._client = new RUMClient(
                    // 41000013,
                    // "c23e9d90-bada-440d-8316-44790f615ec1",
                    41000006,
                    "7e592712-01ea-4250-bf39-e51e00c004e9",
                    null,
                    null,
                    true
                );

                TestCase self = this;

                this._client.GetEvent().AddListener("close", (evd) => {

                    Debug.Log("TestCase closed!");
                });

                this._client.GetEvent().AddListener("ready", (evd) => {

                    Debug.Log("TestCase ready!");
                });

                this._client.GetEvent().AddListener("config", (evd) => {

                    Debug.Log("TestCase config!");
                });

                // client.Connect("52.83.220.166:13609", false, false);
                this._client.Connect("rum-us-frontgate.funplus.com:13609", false, false);
                this._client.SetUid("uid:11111111111");
            }
        }

        private void DestroyClinet() {

            lock (test_locker) {

                if (this._client != null) {

                    this._client.Destroy();
                    this._client = null;
                }
            }
        }

        private Thread _thread;
        private TestLocker test_locker = new TestLocker();

        private void StartThread() {

            lock (test_locker) {

                if (test_locker.Status != 0) {

                    return;
                }

                test_locker.Status = 1;

                this._thread = new Thread(new ThreadStart(TestCreateAndDestroy));
                this._thread.IsBackground = true;
                this._thread.Start();
            }
        }

        private void StopThread() {

            lock (test_locker) {

                test_locker.Status = 0;
            }
        }

        private int count;

        private void TestCreateAndDestroy() {

            TestCase self = this;

            try {

                while(true) {

                    lock (test_locker) {

                        if (test_locker.Status == 0) {

                            return;
                        } 
                    }

                    if (++count % 2 != 0) {

                        self.CreateClinet();
                        // Thread.Sleep(3 * 1000);
                    }else {

                        self.DestroyClinet();
                        // Thread.Sleep(1 * 1000);
                    }
                }
            }catch(Exception ex) {

                Debug.Log(ex);
            }
        }

        public void SendCustomEvent() {

            IDictionary<string, object> attrs = new Dictionary<string, object>();
            attrs.Add("custom_debug", "test text");

            lock (test_locker) {

                if (this._client != null) {

                    this._client.CustomEvent("info", attrs);
                }
            }
        }

        public void SendDisableEvent() {

            IDictionary<string, object> attrs = new Dictionary<string, object>();
            attrs.Add("custom_debug", "test text");

            lock (test_locker) {

                if (this._client != null) {

                    this._client.CustomEvent("MY_EVENT", attrs);
                }
            }
        }
    }
}