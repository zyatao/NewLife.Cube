﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Serialization;
using NewLife.Web;

namespace NewLife.Cube.Web
{
    /// <summary>钉钉身份验证提供者</summary>
    public class DingTalkClient : OAuthClient
    {
        static DingTalkClient()
        {
            // 输出帮助日志
            XTrace.WriteLine("钉钉登录分多种方式，由Scope参数区分。");
            XTrace.WriteLine("Scope=snsapi_qrlogin, 扫码登录");
            XTrace.WriteLine("Scope=snsapi_login, 密码登录");
            XTrace.WriteLine("Scope=snsapi_auth, 钉钉内免登");
        }

        /// <summary>实例化</summary>
        public DingTalkClient()
        {
            Name = "Ding";
            Server = "https://oapi.dingtalk.com/connect/oauth2/";

            AuthUrl = "sns_authorize?appid={key}&response_type=code&scope={scope}&state={state}&redirect_uri={redirect}";
            AccessUrl = null;
            OpenIDUrl = null;
            AccessUrl = "https://oapi.dingtalk.com/sns/getuserinfo_bycode?accessKey={key}&timestamp={timestamp}&signature={signature}";
        }

        /// <summary>应用参数</summary>
        /// <param name="mi"></param>
        public override void Apply(OAuthItem mi)
        {
            base.Apply(mi);

            SetMode(Scope);
        }

        /// <summary>设置工作模式</summary>
        /// <param name="mode"></param>
        public virtual void SetMode(String mode)
        {
            switch (mode)
            {
                // 扫码登录
                case "snsapi_qrlogin":
                    Server = "https://oapi.dingtalk.com/connect/";
                    AuthUrl = "qrconnect?appid={key}&response_type=code&scope=snsapi_login&state={state}&redirect_uri={redirect}";
                    break;
                // 密码登录
                case "snsapi_login":
                    Server = "https://oapi.dingtalk.com/connect/oauth2/";
                    AuthUrl = "sns_authorize?appid={key}&response_type=code&scope=snsapi_login&state={state}&redirect_uri={redirect}";
                    break;
                // 钉钉内免登
                case "snsapi_auth":
                    Server = "https://oapi.dingtalk.com/connect/oauth2/";
                    AuthUrl = "sns_authorize?appid={key}&response_type=code&scope=snsapi_auth&state={state}&redirect_uri={redirect}";
                    break;
                default:
                    break;
            }
        }

        /// <summary>获取令牌</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public override String GetAccessToken(String code)
        {
            var url = AccessUrl;
            if (url.IsNullOrEmpty()) throw new ArgumentNullException(nameof(UserUrl), "未设置用户信息地址");

            var ts = DateTime.UtcNow.ToLong() + "";
            var sign = ts.GetBytes().SHA256(Secret.GetBytes()).ToBase64();
            url = url.Replace("{timestamp}", ts).Replace("{signature}", HttpUtility.UrlEncode(sign));

            url = GetUrl(url);

            var tmp_code = new { tmp_auth_code = code };
            WriteLog("GetUserInfo {0} {1}", url, tmp_code.ToJson());

            // 请求OpenId
            var http = new HttpClient();
            var dic = Task.Run(() => http.InvokeAsync<IDictionary<String, Object>>(HttpMethod.Post, url, tmp_code, null, "user_info")).Result;

            //var content = new StringContent(tmp_code.ToJson(), Encoding.UTF8, "application/json");
            //var response = http.PostAsync(url, content).Result;

            //var html = response.Content.ReadAsStringAsync().Result;
            //if (html.IsNullOrEmpty()) return null;

            //html = html.Trim();
            //if (Log != null && Log.Enable) WriteLog(html);

            //var dic = new JsonParser(html).Decode() as IDictionary<String, Object>;
            if (dic != null)
            {
                //dic = dic["user_info"] as IDictionary<String, Object>;
                //if (dic != null)
                //{
                NickName = dic["nick"] as String;
                OpenID = dic["openid"] as String;
                UnionID = dic["unionid"] as String;

                Items = dic.ToDictionary(e => e.Key, e => e.Value as String);
                //}
            }

            return null;
        }

        #region 服务端Api
        /// <summary>企业内部应用获取凭证，有效期7200秒</summary>
        /// <param name="appkey"></param>
        /// <param name="appsecret"></param>
        /// <returns></returns>
        public static String GetToken(String appkey, String appsecret)
        {
            var url = $"https://oapi.dingtalk.com/gettoken?appkey={appkey}&appsecret={appsecret}";

            var http = new HttpClient();
            return Task.Run(() => http.InvokeAsync<String>(HttpMethod.Get, url, null, null, "access_token")).Result;
        }

        /// <summary>企业内部应用获取用户信息</summary>
        /// <param name="access_token"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static String GetUserInfo(String access_token, String code)
        {
            var url = $"https://oapi.dingtalk.com/user/getuserinfo?access_token={access_token}&code={code}";

            var http = new HttpClient();
            return Task.Run(() => http.InvokeAsync<String>(HttpMethod.Get, url, null, null, "userid")).Result;
        }
        #endregion
    }

    //public class DingTalkServer
    //{
    //    #region 属性
    //    public String Key { get; set; }

    //    public String Secret { get; set; }
    //    #endregion

    //    #region 方法
    //    #endregion
    //}
}