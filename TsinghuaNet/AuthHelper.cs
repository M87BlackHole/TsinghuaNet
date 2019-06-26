﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TsinghuaNet
{
    /// <summary>
    /// Exposes methods to login, logout and get flux from https://auth4.tsinghua.edu.cn/ or https://auth6.tsinghua.edu.cn/
    /// </summary>
    public abstract class AuthHelper : NetHelperBase, IConnect
    {
        private const string LogUri = "https://auth{0}.tsinghua.edu.cn/cgi-bin/srun_portal";
        private const string FluxUri = "https://auth{0}.tsinghua.edu.cn/rad_user_info.php";
        private const string ChallengeUri = "https://auth{0}.tsinghua.edu.cn/cgi-bin/get_challenge?username={{0}}&double_stack=1&ip&callback=callback";
        private static readonly int[] AcIds = new int[] { 1, 25, 33, 35, 37 };
        private readonly int version;
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthHelper"/> class.
        /// </summary>
        /// <param name="username">The username to login.</param>
        /// <param name="password">The password to login.</param>
        /// <param name="client">A user-specified instance of <see cref="HttpClient"/>.</param>
        /// <param name="version">4 for auth4 and 6 for auth6</param>
        internal AuthHelper(string username, string password, HttpClient client, int version)
            : base(username, password, client)
        {
            this.version = version;
        }

        private async Task<LogResponse> LogAsync(Func<int, Task<Dictionary<string, string>>> f)
        {
            LogResponse response = default;
            string uri = string.Format(LogUri, version);
            foreach (int ac_id in AcIds)
            {
                response = LogResponse.ParseFromAuth(await PostAsync(uri, await f(ac_id)));
                if (response.Succeed)
                    break;
            }
            return response;
        }
        /// <summary>
        /// Login to the network.
        /// </summary>
        /// <returns>The response of the website.</returns>
        public Task<LogResponse> LoginAsync() => LogAsync(GetLoginDataAsync);

        /// <summary>
        /// Logout from the network.
        /// </summary>
        /// <returns>The response of the website.</returns>
        public Task<LogResponse> LogoutAsync() => LogAsync(GetLogoutDataAsync);

        /// <summary>
        /// Get information of the user online.
        /// </summary>
        /// <returns>An instance of <see cref="FluxUser"/> class of the current user.</returns>
        public async Task<FluxUser> GetFluxAsync() => FluxUser.Parse(await PostAsync(FluxUri));

        /// <summary>
        /// Get "challenge" to encode the password.
        /// </summary>
        /// <returns>The content of the website.</returns>
        private async Task<string> GetChallengeAsync()
        {
            string result = await GetAsync(string.Format(ChallengeUri, Username));
            JObject json = JObject.Parse(result.Substring(9, result.Length - 10));
            return (string)json["challenge"];
        }

        private const string LoginInfoJson = "{{\"username\": \"{0}\", \"password\": \"{1}\", \"ip\": \"\", \"acid\": \"{2}\", \"enc_ver\": \"srun_bx1\"}}";
        private const string ChkSumData = "{0}{1}{0}{2}{0}{4}{0}{0}200{0}1{0}{3}";
        /// <summary>
        /// Get login data with username, password and "challenge".
        /// </summary>
        /// <returns>A dictionary contains the data.</returns>
        private async Task<Dictionary<string, string>> GetLoginDataAsync(int ac_id)
        {
            string token = await GetChallengeAsync();
            string passwordMD5 = CryptographyHelper.GetHMACMD5(token);
            string info = "{SRBX1}" + CryptographyHelper.Base64Encode(CryptographyHelper.XEncode(string.Format(LoginInfoJson, Username, Password, ac_id), token));
            return new Dictionary<string, string>
            {
                ["action"] = "login",
                ["ac_id"] = ac_id.ToString(),
                ["double_stack"] = "1",
                ["n"] = "200",
                ["type"] = "1",
                ["username"] = Username,
                ["password"] = "{MD5}" + passwordMD5,
                ["info"] = info,
                ["chksum"] = CryptographyHelper.GetSHA1(string.Format(ChkSumData, token, Username, passwordMD5, info, ac_id)),
                ["callback"] = "callback"
            };
        }

        private const string LogoutInfoJson = "{{\"username\": \"{0}\", \"ip\": \"\", \"acid\": \"{1}\", \"enc_ver\": \"srun_bx1\"}}";
        private const string LogoutChkSumData = "{0}{1}{0}{3}{0}{0}200{0}1{0}{2}";
        private async Task<Dictionary<string, string>> GetLogoutDataAsync(int ac_id)
        {
            string token = await GetChallengeAsync();
            string info = "{SRBX1}" + CryptographyHelper.Base64Encode(CryptographyHelper.XEncode(string.Format(LogoutInfoJson, Username, ac_id), token));
            return new Dictionary<string, string>
            {
                ["action"] = "logout",
                ["ac_id"] = ac_id.ToString(),
                ["double_stack"] = "1",
                ["n"] = "200",
                ["type"] = "1",
                ["username"] = Username,
                ["info"] = info,
                ["chksum"] = CryptographyHelper.GetSHA1(string.Format(LogoutChkSumData, token, Username, info, ac_id)),
                ["callback"] = "callback"
            };
        }
    }
    /// <summary>
    /// Exposes methods to login, logout and get flux from https://auth4.tsinghua.edu.cn/.
    /// </summary>
    public class Auth4Helper : AuthHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Auth4Helper"/> class.
        /// </summary>
        /// <param name="username">The username to login.</param>
        /// <param name="password">The password to login.</param>
        /// <param name="client">A user-specified instance of <see cref="HttpClient"/>.</param>
        public Auth4Helper(string username, string password, HttpClient client)
            : base(username, password, client, 4)
        { }
    }
    /// <summary>
    /// Exposes methods to login, logout and get flux from https://auth6.tsinghua.edu.cn/.
    /// </summary>
    public class Auth6Helper : AuthHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Auth6Helper"/> class.
        /// </summary>
        /// <param name="username">The username to login.</param>
        /// <param name="password">The password to login.</param>
        /// <param name="client">A user-specified instance of <see cref="HttpClient"/>.</param>
        public Auth6Helper(string username, string password, HttpClient client)
            : base(username, password, client, 6)
        { }
    }
}
