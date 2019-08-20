# fpnn rum sdk unity #

#### 依赖 ####
* [fpnn.unitypackage](https://github.com/highras/fpnn-sdk-unity)
* [Json&MsgPack.unitypackage](https://github.com/deniszykov/msgpack-unity3d)

#### IPV6 ####
* `SOCKET`链接支持`IPV6`接口
* 兼容`DNS64/NAT64`网络环境

#### 其他 ####
* `RUMClient`可在任意线程中构造实例和调用函数, 前提是`RUMPlatform`已被初始化
* `RUMPlatform`初始化, 在`Unity`主线程中调用`RUMPlatform.Instance.InitSelfListener();`
* 异步函数均由子线程呼叫, 不要在其中调用仅`Unity`主线程中可执行的函数, 不要阻塞事件和回调函数
* 用户ID与RUMClient实例绑定, 如果切换用户ID请使用新的RUMClient实例重新建立连接
* HTTP HOOK: 半自动非侵入方式, 不会抓取请求内容, 参考`./Scripts/Main.cs`

#### 一个例子 ####
```c#
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.rum;

/**
 * 在UnityMainThread中初始化RUMPlatform
 */
//RUMPlatform.Instance.InitSelfListener();

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

client.Connect("52.83.220.166:13609", false, false);
    
client.SetUid("xxxxxx-xxxxx-xxxx");
client.CustomEvent("info", new Dictionary<string, object>());

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
* `Constructor(int pid, string token, string uid, string appv, bool debug)`: 构造RUMClient
    * `pid`: **(int)** 应用ID, RUM项目控制台获取
    * `token`: **(string)** 应用Token, RUM项目控制台获取
    * `uid`: **(string)** 应用开放用户ID 
    * `appv`: **(string)** 应用版本号
    * `debug`: **(bool)** 是否开启调试日志

* `Destroy()`: 断开链接并销毁 

* `Connect(string endpoint, bool clearRumId, bool clearEvents)`: 连接服务器
    * `endpoint`: **(string)** RUMAgent接入地址, 由RUM项目控制台获取
    * `clearRumId`: **(bool)** 是否清理本地RumId缓存
    * `clearEvents`: **(bool)** 是否清理本地事件缓存

* `GetSession()`: **(long)** 会话 ID, 设备唯一, 可用于服务端事件关联

* `GetRumId()`: **(string)** RUM ID, 唯一, 可用于服务端事件关联

* `SetUid(string value)`: 设置用户ID
    * `value`: **(string)** 用户ID

* `CustomEvent(string ev, IDictionary<string, object> attrs)`: 上报自定义事件 
    * `ev`: **(string)** 自定义事件名称
    * `attrs`: **(IDictionary[string,object])** 自定义事件内容

* `HttpEvent(string url, string method, int status, long reqsize, long respsize, int latency, IDictionary<string, object> attrs)`: 上报Http事件 
    * `url`: **(string)** 请求地址
    * `method`: **(string)** 请求类型`POST` `GET`...
    * `status`: **(int)** 响应状态`200` `404` `500`...
    * `reqsize`: **(long)** 上传内容长度(B)
    * `respsize`: **(long)** 下载内容长度(B)
    * `latency`: **(int)** 请求耗时(ms)
    * `attrs`: **(IDictionary[string,object])** 自定义内容

* `HookHttp(HttpWebRequest req, HttpWebResponse res, int latency)`: 抓取以`System.Net.HttpWebRequest`方式发起的Http请求
    * `req`: **(HttpWebRequest)** `HttpWebRequest` 请求对象
    * `res`: **(HttpWebResponse)** `HttpWebResponse` 响应对象
    * `latency`: **(int)** 请求耗时(ms)

* `HookHttp(UnityWebRequest req, int latency)`: 抓取以`UnityEngine.Networking.UnityWebRequest`方式发起的Http请求
    * `req`: **(UnityWebRequest)** `UnityWebRequest` 请求对象
    * `latency`: **(int)** 请求耗时(ms)
