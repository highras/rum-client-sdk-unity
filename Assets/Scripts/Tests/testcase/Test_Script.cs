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
        // string hex_str = "84A3706964D202719C4FA47369676ED9204342343739444444314434333342463531424132453143313545394532333546A473616C74D3000594778399B9EDA66576656E74739189A474797065C41708C7201212088DCD0E220CA0F8FFFFFFFFFFFFFF017A00AD73797374656D5F6D656D6F7279D1055AA26576A47761726EA3656964D30005947783963394A3706964D202719C4FA3736964D300059477410D2D7BA3756964D9203262363662303138353036623834656535663934653236313435323765356232A3726964D924313537303533333737323433382D634643472D423438462D434647464136364642443144A27473D25D9DB26B";
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
