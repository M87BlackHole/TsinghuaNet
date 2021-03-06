﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TsinghuaNet.Models;

namespace TsinghuaNet
{
    internal abstract class AuthHelper : NetHelperBase, IConnect
    {
        private const string LogUri = "https://auth{0}.tsinghua.edu.cn/cgi-bin/srun_portal";
        private const string FluxUri = "https://auth{0}.tsinghua.edu.cn/rad_user_info.php";
        private const string ChallengeUri = "https://auth{0}.tsinghua.edu.cn/cgi-bin/get_challenge?username={1}&double_stack=1&ip&callback=callback";
        private static readonly int[] AcIds = new int[] { 1, 25, 33, 35, 37 };
        private readonly int version;

        public AuthHelper(string username, string password, HttpClient client, int version)
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
                response = LogResponse.ParseFromAuth(await PostReturnBytesAsync(uri, await f(ac_id)));
                if (response.Succeed)
                    break;
            }
            if (response.Succeed)
                return response;
            else
            {
                int ac_id = await GetAcId();
                if (ac_id > 0)
                    return LogResponse.ParseFromAuth(await PostReturnBytesAsync(uri, await f(ac_id)));
                else
                    return response;
            }
        }

        public Task<LogResponse> LoginAsync() => LogAsync(GetLoginDataAsync);

        public Task<LogResponse> LogoutAsync() => LogAsync(GetLogoutDataAsync);

        private static readonly Regex AcIdRegex = new Regex(@"/index_([0-9]+)\.html");
        private async Task<int> GetAcId()
        {
            var response = await client.GetAsync("http://net.tsinghua.edu.cn/");
            // It may not be redirected.
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.TemporaryRedirect)
            {
                var match = AcIdRegex.Match((response.StatusCode == HttpStatusCode.TemporaryRedirect ? response.Headers.Location : response.RequestMessage.RequestUri).LocalPath);
                if (match.Success)
                {
                    return int.Parse(match.Groups[1].Value);
                }
            }
            return -1;
        }

        public async Task<FluxUser> GetFluxAsync() => FluxUser.Parse(await PostAsync(string.Format(FluxUri, version)), 2);

        private async Task<string> GetChallengeAsync()
        {
            byte[] result = await GetBytesAsync(string.Format(ChallengeUri, version, Username));
            JsonDocument json = JsonDocument.Parse(result.AsMemory().Slice(9, result.Length - 10));
            return json.RootElement.GetProperty("challenge").GetString();
        }

        private const string LoginInfoJson = "{{\"username\": \"{0}\", \"password\": \"{1}\", \"ip\": \"\", \"acid\": \"{2}\", \"enc_ver\": \"srun_bx1\"}}";
        private const string ChkSumData = "{0}{1}{0}{2}{0}{4}{0}{0}200{0}1{0}{3}";
        private async Task<Dictionary<string, string>> GetLoginDataAsync(int ac_id)
        {
            string token = await GetChallengeAsync();
            string passwordMD5 = CryptographyHelper.GetHMACMD5(token);
            string info = "{SRBX1}" + CryptographyHelper.Base64Encode(CryptographyHelper.XXTeaEncrypt(string.Format(LoginInfoJson, Username, Password, ac_id), token));
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
            string info = "{SRBX1}" + CryptographyHelper.Base64Encode(CryptographyHelper.XXTeaEncrypt(string.Format(LogoutInfoJson, Username, ac_id), token));
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

    internal class Auth4Helper : AuthHelper
    {
        public Auth4Helper(string username, string password, HttpClient client)
            : base(username, password, client, 4)
        { }
    }

    internal class Auth6Helper : AuthHelper
    {
        public Auth6Helper(string username, string password, HttpClient client)
            : base(username, password, client, 6)
        { }
    }
}
