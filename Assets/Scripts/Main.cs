using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using GameDevWare.Serialization;
using com.rum;
using com.test;

public class Main : MonoBehaviour {

    public interface ITestCase {

        RUMClient GetClient();
        void StartTest();
        void StopTest();
    }

    private ITestCase _testCase;

    void Start() {

        //TestCase
        // this._testCase = new TestCase();

        //SingleClientSend
        this._testCase = new SingleClientSend();

        if (this._testCase != null) {

            this._testCase.StartTest();
        }

        if (!this.IsInvoking("SendHttpRequest")) {

            InvokeRepeating("SendHttpRequest", 5.0f, 1.0f);
        }
    }

    void SendHttpRequest() {

        // HttpWebRequest
        AsyncGetWithWebRequest("https://www.baidu.com");

        // UnityWebRequest
        StartCoroutine(UnityWebRequestGet("https://www.google.com"));
    }

    void Update() {}

    void OnApplicationQuit() {

        if (this.IsInvoking("SendHttpRequest")) {

            CancelInvoke("SendHttpRequest");
        }

        if (this._testCase != null) {

            this._testCase.StopTest();
        }
    }

    private DateTime _stime;

    void AsyncGetWithWebRequest(string url) {

        _stime = DateTime.Now;

        var request = (HttpWebRequest) WebRequest.Create(new Uri(url));
        request.BeginGetResponse(new AsyncCallback(ReadCallback), request);
    }

    void ReadCallback(IAsyncResult asynchronousResult) {

        HttpWebRequest request = (HttpWebRequest) asynchronousResult.AsyncState;
        HttpWebResponse response = (HttpWebResponse) request.EndGetResponse(asynchronousResult);

        using (var streamReader = new StreamReader(response.GetResponseStream())) {

            var resultString = streamReader.ReadToEnd();
            // Debug.Log(resultString);
        }

        int latency = Convert.ToInt32((DateTime.Now - _stime).TotalMilliseconds);

        if (response.StatusCode != HttpStatusCode.OK) {

            Debug.Log(response.StatusCode);
        }

        RUMClient client = _testCase.GetClient();
        if (client != null) {
            
            client.HookHttp(ref request, ref response, latency);
        }
    }

    IEnumerator UnityWebRequestGet(string url) {

        _stime = DateTime.Now;

        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if(req.isNetworkError || req.isHttpError) {

            Debug.Log(req.error);
        } else {

            // Show results as text
            // Debug.Log(req.downloadHandler.text);
 
            // Or retrieve results as binary data
            byte[] results = req.downloadHandler.data;
        }
 
        int latency = Convert.ToInt32((DateTime.Now - _stime).TotalMilliseconds);

        RUMClient client = _testCase.GetClient();
        if (client != null) {

            client.HookHttp(ref req, latency);
        }
    }
}
