using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using GameDevWare.Serialization;
using com.fpnn;
using com.rum;

public class Integration_RUMFile {

    [SetUp]
    public void SetUp() {
        RUMRegistration.Register(null);
    }

    [TearDown]
    public void TearDown() {}

    [UnityTest]
    public IEnumerator File_RumLog_Save_Load() {
        RUMFile file = new RUMFile(1000, false);
        file.SaveStorage(new byte[100]);
        RUMFile.Result save_res = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result load_res = file.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(100, ((byte[])load_res.content).Length);
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save_Load_Load() {
        RUMFile file = new RUMFile(1001, false);
        file.SaveStorage(new byte[100]);
        RUMFile.Result save_res = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result load_res_1 = file.LoadRumLog();
        RUMFile.Result load_res_2 = file.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res_1.success);
        Assert.IsFalse(load_res_2.success);
        Assert.AreEqual(100, ((byte[])load_res_1.content).Length);
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_Save0_Load() {
        RUMFile file = new RUMFile(1002, false);
        file.SaveStorage(new byte[100]);
        RUMFile.Result save_res_1 = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result save_res_2 = file.SaveRumLog(0, new byte[200]);
        RUMFile.Result load_res = file.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(200, ((byte[])load_res.content).Length);
    }

    [UnityTest]
    public IEnumerator File_RumLog_aSave0_bSave0_Load() {
        RUMFile file_a = new RUMFile(1003, false);
        RUMFile file_b = new RUMFile(1003, false);
        file_a.SaveStorage(new byte[100]);
        RUMFile.Result save_res_1 = file_a.SaveRumLog(0, new byte[100]);
        RUMFile.Result save_res_2 = file_b.SaveRumLog(0, new byte[200]);
        RUMFile.Result load_res = file_a.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        // foreach (byte b in (byte[])load_res.content) {
        //     Debug.Log(b.ToString("X2") + " ");
        // }
        file_a.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(200, ((byte[])load_res.content).Length);
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_Save1_Load() {
        RUMFile file = new RUMFile(1004, false);
        file.SaveStorage(new byte[100]);
        RUMFile.Result save_res_1 = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result save_res_2 = file.SaveRumLog(1, new byte[200]);
        RUMFile.Result load_res = file.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(100, ((byte[])load_res.content).Length);
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_Save1_Load_Load() {
        RUMFile file = new RUMFile(1005, false);
        file.SaveStorage(new byte[100]);
        RUMFile.Result save_res_1 = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result save_res_2 = file.SaveRumLog(1, new byte[200]);
        RUMFile.Result load_res_1 = file.LoadRumLog();
        RUMFile.Result load_res_2 = file.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res_1.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(100, ((byte[])load_res_1.content).Length);
        Assert.AreEqual(200, ((byte[])load_res_2.content).Length);
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_Savex_Save0_Load_Load() {
        RUMFile file = new RUMFile(1006, false);
        file.SaveStorage(new byte[100]);
        RUMFile.Result save_res_1 = file.SaveRumLog(0, new byte[100]);
        yield return new WaitForSeconds(1.1f);
        RUMFile.Result save_res_2 = file.SaveRumLog(1, new byte[200]);
        yield return new WaitForSeconds(1.1f);
        file.SaveRumLog(2, new byte[110]);
        yield return new WaitForSeconds(1.1f);
        file.SaveRumLog(3, new byte[120]);
        yield return new WaitForSeconds(1.1f);
        file.SaveRumLog(0, new byte[300]);
        RUMFile.Result load_res_1 = file.LoadRumLog();
        RUMFile.Result load_res_2 = file.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res_1.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(200, ((byte[])load_res_1.content).Length);
        Assert.AreEqual(110, ((byte[])load_res_2.content).Length);
    }

    [UnityTest]
    public IEnumerator File_RumLog_Load_Save_Load() {
        RUMFile file = new RUMFile(1007, false);
        file.SaveStorage(new byte[100]);
        RUMFile.Result load_res_1 = file.LoadRumLog();
        RUMFile.Result save_res = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result load_res_2 = file.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsFalse(load_res_1.success);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res_2.success);
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_Load_Save0_Load() {
        RUMFile file = new RUMFile(1008, false);
        file.SaveStorage(new byte[100]);
        RUMFile.Result save_res_1 = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result load_res_1 = file.LoadRumLog();
        RUMFile.Result save_res_2 = file.SaveRumLog(0, new byte[200]);
        RUMFile.Result load_res_2 = file.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(load_res_1.success);
        Assert.AreEqual(100, ((byte[])load_res_1.content).Length);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res_2.success);
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_aLoad_Save0_bLoad() {
        RUMFile file_a = new RUMFile(1009, false);
        RUMFile file_b = new RUMFile(1009, false);
        file_a.SaveStorage(new byte[100]);
        RUMFile.Result save_res_1 = file_a.SaveRumLog(0, new byte[100]);
        RUMFile.Result load_res_1 = file_a.LoadRumLog();
        RUMFile.Result save_res_2 = file_a.SaveRumLog(0, new byte[200]);
        RUMFile.Result load_res_2 = file_b.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        file_a.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(load_res_1.success);
        Assert.AreEqual(100, ((byte[])load_res_1.content).Length);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(200, ((byte[])load_res_2.content).Length);
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_Load_Save1_Load() {
        RUMFile file = new RUMFile(1010, false);
        file.SaveStorage(new byte[100]);
        RUMFile.Result save_res_1 = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result load_res_1 = file.LoadRumLog();
        RUMFile.Result save_res_2 = file.SaveRumLog(1, new byte[200]);
        RUMFile.Result load_res_2 = file.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(load_res_1.success);
        Assert.AreEqual(100, ((byte[])load_res_1.content).Length);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(200, ((byte[])load_res_2.content).Length);
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save19_Load() {
        RUMFile file = new RUMFile(1011, false);
        file.SaveStorage(new byte[100]);
        RUMFile.Result save_res = file.SaveRumLog(19, new byte[100]);
        RUMFile.Result load_res = file.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(100, ((byte[])load_res.content).Length);
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save20_Load() {
        RUMFile file = new RUMFile(1012, false);
        file.SaveStorage(new byte[100]);
        RUMFile.Result save_res = file.SaveRumLog(20, new byte[100]);
        RUMFile.Result load_res = file.LoadRumLog();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res.success);
    }

    [UnityTest]
    public IEnumerator File_Storage_Save_Load() {
        RUMFile file = new RUMFile(1013, false);
        RUMFile.Result save_res = file.SaveStorage(new byte[100]);
        RUMFile.Result load_res = file.LoadStorage();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(100, ((byte[])load_res.content).Length);
    }

    [UnityTest]
    public IEnumerator File_Storage_Save_Load_Load() {
        RUMFile file = new RUMFile(1014, false);
        RUMFile.Result save_res = file.SaveStorage(new byte[100]);
        RUMFile.Result load_res_1 = file.LoadStorage();
        RUMFile.Result load_res_2 = file.LoadStorage();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res_1.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(((byte[])load_res_1.content).Length, ((byte[])load_res_2.content).Length);
    }

    [UnityTest]
    public IEnumerator File_Storage_Save_Save_Load() {
        RUMFile file = new RUMFile(1015, false);
        RUMFile.Result save_res_1 = file.SaveStorage(new byte[100]);
        RUMFile.Result save_res_2 = file.SaveStorage(new byte[200]);
        RUMFile.Result load_res = file.LoadStorage();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(200, ((byte[])load_res.content).Length);
    }

    [UnityTest]
    public IEnumerator File_Storage_Save_Save_Load_Load() {
        RUMFile file = new RUMFile(1016, false);
        RUMFile.Result save_res_1 = file.SaveStorage(new byte[100]);
        RUMFile.Result save_res_2 = file.SaveStorage(new byte[200]);
        RUMFile.Result load_res_1 = file.LoadStorage();
        RUMFile.Result load_res_2 = file.LoadStorage();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res_1.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(((byte[])load_res_1.content).Length, ((byte[])load_res_2.content).Length);
    }

    [UnityTest]
    public IEnumerator File_Storage_Load_Save_Load() {
        RUMFile file = new RUMFile(1017, false);
        RUMFile.Result load_res_1 = file.LoadStorage();
        RUMFile.Result save_res = file.SaveStorage(new byte[100]);
        RUMFile.Result load_res_2 = file.LoadStorage();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsFalse(load_res_1.success);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(100, ((byte[])load_res_2.content).Length);
    }

    [UnityTest]
    public IEnumerator File_Storage_Save_Load_Save_Load() {
        RUMFile file = new RUMFile(1018, false);
        RUMFile.Result save_res_1 = file.SaveStorage(new byte[100]);
        RUMFile.Result load_res_1 = file.LoadStorage();
        RUMFile.Result save_res_2 = file.SaveStorage(new byte[200]);
        RUMFile.Result load_res_2 = file.LoadStorage();
        yield return new WaitForSeconds(0.5f);
        file.ClearAllFile();
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(load_res_1.success);
        Assert.AreEqual(100, ((byte[])load_res_1.content).Length);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(200, ((byte[])load_res_2.content).Length);
    }
}