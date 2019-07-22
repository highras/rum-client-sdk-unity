using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using GameDevWare.Serialization;
using UnityEngine;

using com.fpnn;
using com.rum;

namespace com.test {

    public class TestCase : Main.ITestCase {

        private RUMClient _client;

        public void StartTest() {

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
                self._client.SetUid("uid:11111111111");
            });

            this._client.GetEvent().AddListener("config", (evd) => {

                Debug.Log("TestCase config!");
            });

            // client.Connect("52.83.220.166:13609", false, false);
            this._client.Connect("rum-us-frontgate.funplus.com:13609", false, false);
        }

        public void StopTest() {

            if (this._client != null) {

                this._client.Destroy();
            }
        }

        public void SendCustomEvent() {

            IDictionary<string, object> attrs = new Dictionary<string, object>();
            attrs.Add("custom_debug", "test text");

            this._client.CustomEvent("info", attrs);
        }

        public void SendDisableEvent() {

            IDictionary<string, object> attrs = new Dictionary<string, object>();
            attrs.Add("custom_debug", "test text");

            this._client.CustomEvent("MY_EVENT", attrs);
        }
    }
}