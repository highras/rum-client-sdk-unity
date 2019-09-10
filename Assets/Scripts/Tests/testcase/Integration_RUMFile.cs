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

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result load_res = file.LoadRumLog();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(100, ((byte[])load_res.content).Length);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save_Load_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result load_res_1 = file.LoadRumLog();
        RUMFile.Result load_res_2 = file.LoadRumLog();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res_1.success);
        Assert.IsFalse(load_res_2.success);
        Assert.AreEqual(100, ((byte[])load_res_1.content).Length);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_Save0_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res_1 = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result save_res_2 = file.SaveRumLog(0, new byte[200]);
        RUMFile.Result load_res = file.LoadRumLog();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(200, ((byte[])load_res.content).Length);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_RumLog_aSave0_bSave0_Load() {

        RUMFile file_a = new RUMFile(100, false);
        RUMFile file_b = new RUMFile(100, false);
        RUMFile.Result save_res_1 = file_a.SaveRumLog(0, new byte[100]);
        RUMFile.Result save_res_2 = file_b.SaveRumLog(0, new byte[200]);
        RUMFile.Result load_res = file_a.LoadRumLog();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(200, ((byte[])load_res.content).Length);

        file_a.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_Save1_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res_1 = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result save_res_2 = file.SaveRumLog(1, new byte[200]);
        RUMFile.Result load_res = file.LoadRumLog();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(100, ((byte[])load_res.content).Length);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_Save1_Load_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res_1 = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result save_res_2 = file.SaveRumLog(1, new byte[200]);
        RUMFile.Result load_res_1 = file.LoadRumLog();
        RUMFile.Result load_res_2 = file.LoadRumLog();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res_1.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(100, ((byte[])load_res_1.content).Length);
        Assert.AreEqual(200, ((byte[])load_res_2.content).Length);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_RumLog_Load_Save_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result load_res_1 = file.LoadRumLog();
        RUMFile.Result save_res = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result load_res_2 = file.LoadRumLog();

        yield return new WaitForSeconds(0.5f);
        Assert.IsFalse(load_res_1.success);
        Assert.IsTrue(save_res.success);
        Assert.IsFalse(load_res_2.success);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_Load_Save0_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res_1 = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result load_res_1 = file.LoadRumLog();
        RUMFile.Result save_res_2 = file.SaveRumLog(0, new byte[200]);
        RUMFile.Result load_res_2 = file.LoadRumLog();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(load_res_1.success);
        Assert.AreEqual(100, ((byte[])load_res_1.content).Length);
        Assert.IsTrue(save_res_2.success);
        Assert.IsFalse(load_res_2.success);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_aLoad_Save0_bLoad() {

        RUMFile file_a = new RUMFile(100, false);
        RUMFile file_b = new RUMFile(100, false);
        RUMFile.Result save_res_1 = file_a.SaveRumLog(0, new byte[100]);
        RUMFile.Result load_res_1 = file_a.LoadRumLog();
        RUMFile.Result save_res_2 = file_a.SaveRumLog(0, new byte[200]);
        RUMFile.Result load_res_2 = file_b.LoadRumLog();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(load_res_1.success);
        Assert.AreEqual(100, ((byte[])load_res_1.content).Length);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(200, ((byte[])load_res_2.content).Length);

        file_a.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save0_Load_Save1_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res_1 = file.SaveRumLog(0, new byte[100]);
        RUMFile.Result load_res_1 = file.LoadRumLog();
        RUMFile.Result save_res_2 = file.SaveRumLog(1, new byte[200]);
        RUMFile.Result load_res_2 = file.LoadRumLog();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(load_res_1.success);
        Assert.AreEqual(100, ((byte[])load_res_1.content).Length);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(200, ((byte[])load_res_2.content).Length);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save19_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res = file.SaveRumLog(19, new byte[100]);
        RUMFile.Result load_res = file.LoadRumLog();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(100, ((byte[])load_res.content).Length);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_RumLog_Save20_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res = file.SaveRumLog(20, new byte[100]);
        RUMFile.Result load_res = file.LoadRumLog();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res.success);
        Assert.IsFalse(load_res.success);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_Storage_Save_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res = file.SaveStorage(new byte[100]);
        RUMFile.Result load_res = file.LoadStorage();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(100, ((byte[])load_res.content).Length);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_Storage_Save_Load_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res = file.SaveStorage(new byte[100]);
        RUMFile.Result load_res_1 = file.LoadStorage();
        RUMFile.Result load_res_2 = file.LoadStorage();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res_1.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(((byte[])load_res_1.content).Length, ((byte[])load_res_2.content).Length);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_Storage_Save_Save_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res_1 = file.SaveStorage(new byte[100]);
        RUMFile.Result save_res_2 = file.SaveStorage(new byte[200]);
        RUMFile.Result load_res = file.LoadStorage();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res.success);
        Assert.AreEqual(200, ((byte[])load_res.content).Length);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_Storage_Save_Save_Load_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res_1 = file.SaveStorage(new byte[100]);
        RUMFile.Result save_res_2 = file.SaveStorage(new byte[200]);
        RUMFile.Result load_res_1 = file.LoadStorage();
        RUMFile.Result load_res_2 = file.LoadStorage();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res_1.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(((byte[])load_res_1.content).Length, ((byte[])load_res_2.content).Length);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_Storage_Load_Save_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result load_res_1 = file.LoadStorage();
        RUMFile.Result save_res = file.SaveStorage(new byte[100]);
        RUMFile.Result load_res_2 = file.LoadStorage();

        yield return new WaitForSeconds(0.5f);
        Assert.IsFalse(load_res_1.success);
        Assert.IsTrue(save_res.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(100, ((byte[])load_res_2.content).Length);

        file.ClearAllFile();
    }

    [UnityTest]
    public IEnumerator File_Storage_Save_Load_Save_Load() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result save_res_1 = file.SaveStorage(new byte[100]);
        RUMFile.Result load_res_1 = file.LoadStorage();
        RUMFile.Result save_res_2 = file.SaveStorage(new byte[200]);
        RUMFile.Result load_res_2 = file.LoadStorage();

        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(save_res_1.success);
        Assert.IsTrue(load_res_1.success);
        Assert.AreEqual(100, ((byte[])load_res_1.content).Length);
        Assert.IsTrue(save_res_2.success);
        Assert.IsTrue(load_res_2.success);
        Assert.AreEqual(200, ((byte[])load_res_2.content).Length);

        file.ClearAllFile();
    }
}
