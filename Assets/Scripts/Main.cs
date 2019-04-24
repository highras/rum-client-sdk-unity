using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.rum;

public class Main : MonoBehaviour
{
    private RUMClient _client;

    // Start is called before the first frame update
    void Start() {
    
       this._client = new RUMClient(
            41000013,
            "c23e9d90-bada-440d-8316-44790f615ec1",
            null,
            null,
            true
        );

        this._client.GetEvent().AddListener("error", (evd) => {

            Debug.Log("error: " + evd.GetException().Message);
        });

        this._client.GetEvent().AddListener("close", (evd) => {

            Debug.Log("closed!");
        });

        this._client.GetEvent().AddListener("ready", (evd) => {

            Debug.Log("ready!");

            this.SendCustomEvent();
        });

        this._client.Connect("52.83.220.166:13609", false);
    }

    void SendCustomEvent() {

        IDictionary<string, object> attrs = new Dictionary<string, object>();
        attrs.Add("debug", "this is a custom event");

        this._client.CustomEvent("MY_EVENT", attrs);
    }

    // Update is called once per frame
    void Update() {
        
    }

    void OnApplicationQuit() {

        if (this._client != null) {

            this._client.Destroy();
        }
    }
}
