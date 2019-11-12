# fpnn rum sdk unity #

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