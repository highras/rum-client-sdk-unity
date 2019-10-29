using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using GameDevWare.Serialization;
using com.fpnn;
using com.rum;

public class Test_Debug {

    [SetUp]
    public void SetUp() {}

    [TearDown]
    public void TearDown() {}

    [UnityTest]
    public IEnumerator Debug_HexString() {
        // string hex_str = "84A3706964D202719C4FA47369676ED9203546354342434635364330354245313732444130423042463534393436433335A473616C74D3000595501E4DD39EA66576656E74739189A474797065C41008BB2C120B08E6D20D2205B104BB0600AD73797374656D5F6D656D6F7279D1071CA26576A47761726EA3656964D3000595501E4D104DA3706964D202719C4FA3736964D30005954F6D0D665BA3756964D9206262303164343064326230613765333035363936613764646365343338353037A3726964D924313537303839303631383938382D634141432D423246342D313130434238413146314635A27473D25DABE46F";
        // int len = hex_str.Length / 2;
        // byte[] bytes = new byte[len];
        // string hex;
        // int j = 0;
        // for (int i = 0; i < bytes.Length; i++) {
        //     hex = new String(new Char[] { hex_str[j], hex_str[j + 1] });
        //     bytes[i] = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
        //     j = j + 2;
        // }
        // Debug.Log("len:" + bytes.Length + ", str10: " + System.Text.Encoding.UTF8.GetString(bytes));
        // string path = "/Users/di.zhao/Desktop/data.bytes";
        // using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write)) {
        //     fs.Write(bytes, 0, bytes.Length);
        // }
        // Debug.Log("check the bytes file: " + path);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Debug_WriteFile() {
        // byte[] bytes = new byte[0];
        // string path = "/Users/di.zhao/Desktop/data.bytes";
        // using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write)) {
        //     fs.Write(bytes, 0, bytes.Length);
        // }
        // Debug.Log("check the bytes file: " + path);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Debug_MsgPack_Deserialize() {
        // IDictionary<string, object> payload = null;
        // try {
        //     using (MemoryStream inputStream = new MemoryStream(bytes)) {
        //         payload = MsgPack.Deserialize<IDictionary<string, object>>(inputStream);
        //     }
        // } catch (Exception ex) {
        //     Debug.LogError(ex);
        // }
        // Debug.Log(Json.SerializeToString(payload));
        yield return null;
    }

    [UnityTest]
    public IEnumerator Debug_MsgPack_Serialize() {
        // byte[] bytes = new byte[0];
        // IDictionary<string, object> payload = null;
        // try {
        //     using (MemoryStream outputStream = new MemoryStream()) {
        //         MsgPack.Serialize(payload, outputStream);
        //         outputStream.Seek(0, SeekOrigin.Begin);
        //         bytes = outputStream.ToArray();
        //     }
        // } catch (Exception ex) {
        //     Debug.LogError(ex);
        // }
        // Debug.Log("bytes len: " + bytes.Length);
        yield return null;
    }
}
