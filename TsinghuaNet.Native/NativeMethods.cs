﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace TsinghuaNet.Native
{
    public static unsafe class NativeMethods
    {
        private static readonly HttpClient Client = new HttpClient();
        private static readonly HttpClient NoProxyClient = new HttpClient(new SocketsHttpHandler() { UseProxy = false });

        static NativeMethods()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static HttpClient GetClient(int useProxy) => useProxy != 0 ? Client : NoProxyClient;

        private static IConnect GetHelper(in NetCredential cred)
        {
            string username = cred.Username == null ? null : Marshal.PtrToStringUTF8(cred.Username);
            string password = cred.Password == null ? null : Marshal.PtrToStringUTF8(cred.Password);
            return cred.State switch
            {
                NetState.Net => new NetHelper(username, password, GetClient(cred.UseProxy)),
                NetState.Auth4 => new Auth4Helper(username, password, GetClient(cred.UseProxy)),
                NetState.Auth6 => new Auth6Helper(username, password, GetClient(cred.UseProxy)),
                _ => null
            };
        }

        private static IUsereg GetUseregHelper(in NetCredential cred)
        {
            string username = cred.Username == null ? null : Marshal.PtrToStringUTF8(cred.Username);
            string password = cred.Password == null ? null : Marshal.PtrToStringUTF8(cred.Password);
            return new UseregHelper(username, password, GetClient(cred.UseProxy));
        }

        private static int WriteString(string message, IntPtr pout, int len)
        {
            if (pout != null && len > 0)
            {
                return Encoding.UTF8.GetBytes(message, new Span<byte>(pout.ToPointer(), len));
            }
            return 0;
        }

        private static string LastError;

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_last_err")]
        public static int GetLastError(IntPtr message, int len)
        {
            if (LastError != null)
            {
                return WriteString(LastError, message, len);
            }
            return 0;
        }

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_login")]
        public static int Login(in NetCredential cred)
        {
            try
            {
                IConnect helper = GetHelper(cred);
                if (helper != null)
                {
                    var task = helper.LoginAsync();
                    task.Wait();
                    var response = task.Result;
                    if (!response.Succeed)
                    {
                        LastError = response.Message;
                        return -1;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            return -1;
        }

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_logout")]
        public static int Logout(in NetCredential cred)
        {
            try
            {
                IConnect helper = GetHelper(cred);
                if (helper != null)
                {
                    var task = helper.LogoutAsync();
                    task.Wait();
                    var response = task.Result;
                    if (!response.Succeed)
                    {
                        LastError = response.Message;
                        return -1;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            return -1;
        }

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_status")]
        public static int Status(in NetCredential cred, ref NetFlux flux)
        {
            try
            {
                IConnect helper = GetHelper(cred);
                if (helper != null)
                {
                    var task = helper.GetFluxAsync();
                    task.Wait();
                    var response = task.Result;
                    if (Unsafe.AsPointer(ref flux) != null)
                    {
                        flux.Flux = response.Flux.Bytes;
                        flux.OnlineTime = (long)response.OnlineTime.TotalSeconds;
                        flux.Balance = (double)response.Balance;
                        return WriteString(response.Username, flux.Username, flux.UsernameLength);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            return -1;
        }

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_usereg_login")]
        public static int UseregLogin(in NetCredential cred)
        {
            try
            {
                IUsereg helper = GetUseregHelper(cred);
                var task = helper.LoginAsync();
                task.Wait();
                var response = task.Result;
                if (!response.Succeed)
                {
                    LastError = response.Message;
                    return -1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            return -1;
        }

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_usereg_logout")]
        public static int UseregLogout(in NetCredential cred)
        {
            try
            {
                IUsereg helper = GetUseregHelper(cred);
                var task = helper.LogoutAsync();
                task.Wait();
                var response = task.Result;
                if (!response.Succeed)
                {
                    LastError = response.Message;
                    return -1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            return -1;
        }

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_usereg_drop")]
        public static int UseregDrop(in NetCredential cred, long addr)
        {
            try
            {
                IUsereg helper = GetUseregHelper(cred);
                var task = helper.LogoutAsync(new IPAddress(addr));
                task.Wait();
                var response = task.Result;
                if (!response.Succeed)
                {
                    LastError = response.Message;
                    return -1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            return -1;
        }

        private static Models.NetUser[] NetUsers;

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_usereg_users")]
        public static int UseregUsers(in NetCredential cred)
        {
            try
            {
                IUsereg helper = GetUseregHelper(cred);
                var task = helper.GetUsersAsync().ToArrayAsync();
                NetUsers = task.GetAwaiter().GetResult();
                return NetUsers.Length;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            return -1;
        }

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_usereg_users_destory")]
        public static int UseregUsersDestory()
        {
            try
            {
                NetUsers = null;
                return 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            return -1;
        }

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_usereg_users_fetch")]
        public static int UseregUsersFetch(int index, ref NetUser user)
        {
            try
            {
                if (NetUsers == null)
                    return -1;
                if (Unsafe.AsPointer(ref user) != null)
                {
                    ref var u = ref NetUsers[index];
#pragma warning disable 0618
                    user.Address = u.Address.Address;
#pragma warning restore 0618
                    user.LoginTime = new DateTimeOffset(u.LoginTime).ToUnixTimeSeconds();
                    return WriteString(u.Client, user.Client, user.ClientLength);
                }
                return 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            return -1;
        }

        private static Models.NetDetail[] NetDetails;

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_usereg_details")]
        public static int UseregDetails(in NetCredential cred, Models.NetDetailOrder order, int descending)
        {
            try
            {
                IUsereg helper = GetUseregHelper(cred);
                var task = helper.GetDetailsAsync(order, descending != 0).ToArrayAsync();
                NetDetails = task.GetAwaiter().GetResult();
                return NetDetails.Length;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            return -1;
        }

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_usereg_details_destory")]
        public static int UseregDetailsDestory()
        {
            try
            {
                NetDetails = null;
                return 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            return -1;
        }

        [NativeCallable(CallingConvention = CallingConvention.Cdecl, EntryPoint = "tunet_usereg_details_fetch")]
        public static int UseregDetailsFetch(int index, ref NetDetail detail)
        {
            try
            {
                if (NetDetails == null)
                    return -1;
                if (Unsafe.AsPointer(ref detail) != null)
                {
                    ref var d = ref NetDetails[index];
                    detail.LoginTime = new DateTimeOffset(d.LoginTime).ToUnixTimeSeconds();
                    detail.LogoutTime = new DateTimeOffset(d.LogoutTime).ToUnixTimeSeconds();
                    detail.Flux = d.Flux.Bytes;
                }
                return 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            return -1;
        }
    }
}
