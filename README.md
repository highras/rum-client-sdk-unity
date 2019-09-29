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
* 数据类型仅支持标准[ Json ](https://www.json.org/),非整型需要转换为字符串类型
* 用户ID与`RUMClient`实例绑定,如果切换用户ID请使用新的`RUMClient`实例重新建立连接
* 位置信息需要`Input.location`处于`Running`状态, 参考:[ LocationService.Start ](https://docs.unity3d.com/ScriptReference/LocationService.Start.html)
* HTTP HOOK: 半自动非侵入方式,不会抓取请求内容

#### 一个例子 ####
```c#
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.rum;

// UnityMainThread
RUMRegistration.Register(new LocationService());

// AnyThread
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
client.CustomEvent("info", attrs);

// Destroy
// client.Destroy();
// client = null;
```

#### Events ####
* `event`:
    * `ready`: 连接可用
    * `config`: 获取事件配置 
    * `close`: 连接关闭

#### API ####
* `RUMRegistration::Register(LocationService location)`: 在`Unity`主线程中注册RUM服务
    * `location`: **(LocationService)** 定位服务, 不启用可传`null`

* `Constructor(int pid, string secret, string uid, string appv, bool debug)`: 构造RUMClient
    * `pid`: **(int)** 应用ID, RUM项目控制台获取
    * `secret`: **(string)** 应用SecretKey, RUM项目控制台获取
    * `uid`: **(string)** 应用开放用户ID 
    * `appv`: **(string)** 应用版本号
    * `debug`: **(bool)** 是否开启调试日志

* `Constructor(int pid, string secret, string uid, string appv, bool debug, bool clearRumId, bool clearEvents)`: 构造RUMClient
    * `pid`: **(int)** 应用ID, RUM项目控制台获取
    * `secret`: **(string)** 应用SecretKey, RUM项目控制台获取
    * `uid`: **(string)** 应用开放用户ID 
    * `appv`: **(string)** 应用版本号
    * `debug`: **(bool)** 是否开启调试日志
    * `clearRumId`: **(bool)** 是否清理本地RumId缓存
    * `clearEvents`: **(bool)** 是否清理本地事件缓存

* `Destroy()`: 断开链接并销毁 

* `Connect(string endpoint)`: 连接服务器
    * `endpoint`: **(string)** RUMAgent接入地址, 由RUM项目控制台获取

* `GetSession()`: **(long)** 会话 ID, 设备唯一, 可用于服务端事件关联

* `GetRumId()`: **(string)** RUM ID, 唯一, 可用于服务端事件关联

* `SetUid(string value)`: 设置用户ID, 与`RUMClient`实例绑定
    * `value`: **(string)** 用户ID

* `CustomEvent(string ev, IDictionary<string, object> attrs)`: 上报自定义事件 
    * `ev`: **(string)** 自定义事件名称
    * `attrs`: **(IDictionary[string,object])** 自定义事件内容, 值类型`object`仅支持`bool` `int` `long` `string`

* `HttpEvent(string url, string method, int status, long reqsize, long respsize, int latency, IDictionary<string, object> attrs)`: 上报Http事件 
    * `url`: **(string)** 请求地址
    * `method`: **(string)** 请求类型`POST` `GET`...
    * `status`: **(int)** 响应状态`200` `404` `500`...
    * `reqsize`: **(long)** 上传内容长度(B)
    * `respsize`: **(long)** 下载内容长度(B)
    * `latency`: **(int)** 请求耗时(ms)
    * `attrs`: **(IDictionary[string,object])** 自定义内容, 值类型`object`仅支持`bool` `int` `long` `string`

* `HookHttp(HttpWebRequest req, HttpWebResponse res, int latency)`: 抓取以`System.Net.HttpWebRequest`方式发起的Http请求
    * `req`: **(HttpWebRequest)** `HttpWebRequest` 请求对象
    * `res`: **(HttpWebResponse)** `HttpWebResponse` 响应对象
    * `latency`: **(int)** 请求耗时(ms)

* `HookHttp(UnityWebRequest req, int latency)`: 抓取以`UnityEngine.Networking.UnityWebRequest`方式发起的Http请求
    * `req`: **(UnityWebRequest)** `UnityWebRequest` 请求对象
    * `latency`: **(int)** 请求耗时(ms)
