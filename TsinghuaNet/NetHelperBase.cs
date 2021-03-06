﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TsinghuaNet.Models;

namespace TsinghuaNet
{
    public interface ILog : IDisposable
    {
        Task<LogResponse> LoginAsync();

        Task<LogResponse> LogoutAsync();
    }

    public interface IConnect : ILog
    {
        Task<FluxUser> GetFluxAsync();
    }

    public interface IUsereg : ILog
    {
        Task<LogResponse> LogoutAsync(IPAddress ip);
        IAsyncEnumerable<NetUser> GetUsersAsync();
        IAsyncEnumerable<NetDetail> GetDetailsAsync(NetDetailOrder order, bool descending);
    }

    internal abstract class NetHelperBase : IDisposable
    {
        protected readonly HttpClient client;

        public string Username { get; set; }

        public string Password { get; set; }

        public NetHelperBase(string username, string password, HttpClient client)
        {
            Username = username;
            Password = password;
            this.client = client ?? new HttpClient();
        }

        protected async Task<string> PostAsync(string uri)
        {
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, uri))
            using (HttpResponseMessage response = await client.SendAsync(message))
            {
                return await response.Content.ReadAsStringAsync();
            }
        }

        protected async Task<string> PostAsync(string uri, string data)
        {
            using (StringContent content = new StringContent(data ?? string.Empty, Encoding.ASCII, "application/x-www-form-urlencoded"))
            using (HttpResponseMessage response = await client.PostAsync(uri, content))
            {
                return await response.Content.ReadAsStringAsync();
            }
        }

        protected async Task<string> PostAsync(string uri, Dictionary<string, string> data)
        {
            using (HttpContent content = new FormUrlEncodedContent(data))
            using (HttpResponseMessage response = await client.PostAsync(uri, content))
            {
                return await response.Content.ReadAsStringAsync();
            }
        }

        protected async Task<byte[]> PostReturnBytesAsync(string uri, Dictionary<string, string> data)
        {
            using (HttpContent content = new FormUrlEncodedContent(data))
            using (HttpResponseMessage response = await client.PostAsync(uri, content))
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
        }

        protected Task<string> GetAsync(string uri) => client.GetStringAsync(uri);

        protected Task<byte[]> GetBytesAsync(string uri) => client.GetByteArrayAsync(uri);

        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);
        #endregion
    }
}
