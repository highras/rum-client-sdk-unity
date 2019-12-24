using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using com.fpnn;
using com.rum;

public class Unit_RUMPlatform {

    [SetUp]
    public void SetUp() {
#if RUM_ENABLE_LOCATION_SERVICE
        RUMRegistration.Register(null);
#else
        RUMRegistration.Register();
#endif
    }

    [TearDown]
    public void TearDown() {}

    [Test]
    public void Platform_HasInit() {
        Assert.IsTrue(RUMPlatform.HasInit());
    }

    [Test]
    public void Platform_Network() {
        Assert.IsNotNull(RUMPlatform.Network);
    }

    [Test]
    public void Platform_SystemLanguage() {
        Assert.IsNotNull(RUMPlatform.SystemLanguage);
    }

    [Test]
    public void Platform_DeviceModel() {
        Assert.IsNotNull(RUMPlatform.DeviceModel);
    }

    [Test]
    public void Platform_OperatingSystem() {
        Assert.IsNotNull(RUMPlatform.OperatingSystem);
    }

    [Test]
    public void Platform_ScreenHeight() {
        Assert.IsNotNull(RUMPlatform.ScreenHeight);
    }

    [Test]
    public void Platform_ScreenWidth() {
        Assert.IsNotNull(RUMPlatform.ScreenWidth);
    }

    [Test]
    public void Platform_IsMobilePlatform() {
        Assert.IsNotNull(RUMPlatform.IsMobilePlatform);
    }

    [Test]
    public void Platform_SystemMemorySize() {
        Assert.IsNotNull(RUMPlatform.SystemMemorySize);
    }

    [Test]
    public void Platform_UnityVersion() {
        Assert.IsNotNull(RUMPlatform.UnityVersion);
    }

    [Test]
    public void Platform_SecureDataPath() {
        Assert.IsNotNull(RUMPlatform.SecureDataPath);
    }
}
