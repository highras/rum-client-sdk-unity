# fpnn rum sdk unity #

#### 依赖 ####
* [fpnn.unitypackage](https://github.com/highras/fpnn-sdk-unity)
* [Json&MsgPack.unitypackage](https://github.com/deniszykov/msgpack-unity3d)

#### IPV6 ####
* `SOCKET`链接支持`IPV6`接口
* 兼容`DNS64/NAT64`网络环境

#### 其他 ####
* 在`Unity`主线程中初始化`RUMRegistration.Register(new LocationService())`
* 若`RUMRegistration`已初始化,`RUMClient`可在任意线程中构造和使用(线程安全)
* 异步函数均由子线程呼叫,不要在其中使用仅UI线程的函数,不要阻塞异步函数
* 数据类型支持[Json](https://www.json.org/)标准, 但非整型需转换为字符串类型
* 用户ID与`RUMClient`实例绑定,如果切换用户ID请使用新的`RUMClient`实例重新建立连接
* 位置信息需要`Input.location`处于`Running`状态, 参考:[LocationService.Start](https://docs.unity3d.com/ScriptReference/LocationService.Start.html)

#### Events ####
* `event`:
    * `ready`: 连接可用
    * `config`: 获取事件配置 
    * `close`: 连接关闭

#### 一个例子 ####
```c#
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.rum;

//UnityMainThread
RUMRegistration.Register(new LocationService());

//AnyThread
RUMClient client = new RUMClient(
    41000013,
    "c23e9d90-bada-440d-8316-44790f615ec1",
    "uid:xxxxxx-xxxxx-xxxx",
    "appv:1.0.0",
    true
);

client.GetEvent().AddListener("close", (evd) => {
    Debug.Log("closed!");
});
client.GetEvent().AddListener("ready", (evd) => {
    Debug.Log("ready!");
});

client.Connect("52.83.220.166:13609");

int intValue = 666666;
long longValue = 999999999;

IDictionary<string, object> attrs = new Dictionary<string, object>() {
    { "bool": true },
    { "string": "str" },
    { "int": intValue },
    { "long": longValue }
};

client.SetUid("xxxxxx-xxxxx-xxxx");
client.CustomEvent("Event_Name", attrs);

//Destroy
// client.Destroy();
// client = null;
```

#### 接口说明 ####
* [API-SDK接口](README-API.md)