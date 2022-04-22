// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
  public static class HttpHelpers
  {
    static readonly string _baseAddressFmt = "http://localhost:{0}";
    static readonly string _baseAddressSslFmt = "https://localhost:{0}";
    private static readonly Uri _baseAddress = new Uri("http://localhost:8080");
    private static readonly Uri _baseAddressSsl = new Uri("https://localhost:8081");

    public static async Task<(HttpStatusCode statusCode, string content)> GetAsync(string url, int port)
    {
      using (HttpClient httpClient = new HttpClient())
      {
        httpClient.BaseAddress = new Uri(string.Format(_baseAddressFmt, port));

        HttpResponseMessage response = await httpClient.GetAsync(url);
        string content = await response.Content.ReadAsStringAsync();

        return (response.StatusCode, content);
      }
    }

    public static async Task<(HttpStatusCode statusCode, string content)> GetSslAsync(string url, int httpsPort)
    {
      var handler = new HttpClientHandler
      {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
      };

      using (HttpClient httpClient = new HttpClient(handler))
      {
        httpClient.BaseAddress = new Uri(string.Format(_baseAddressSslFmt, httpsPort)); ;

        HttpResponseMessage response = await httpClient.GetAsync(url);
        string content = await response.Content.ReadAsStringAsync();

        return (response.StatusCode, content);
      }
    }

    public static async Task<(HttpStatusCode statusCode, string content)> PostAsync(string url, int port)
    {
      using (HttpClient httpClient = new HttpClient())
      {
        httpClient.BaseAddress = new Uri(string.Format(_baseAddressFmt, port));

        HttpResponseMessage response = await httpClient.PostAsync(url, null);
        string responseContent = await response.Content.ReadAsStringAsync();

        return (response.StatusCode, responseContent);
      }
    }

    public static async Task<(HttpStatusCode statusCode, string content)> PostJsonAsync<T>(string url, T data, int port)
    {
      using (HttpClient httpClient = new HttpClient())
      {
        httpClient.BaseAddress = new Uri(string.Format(_baseAddressFmt, port));

        string json = SerializationHelpers.SerializeJson(data);
        using StringContent requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.PostAsync(url, requestContent);
        string responseContent = await response.Content.ReadAsStringAsync();

        return (response.StatusCode, responseContent);
      }
    }

    public static async Task<(HttpStatusCode statusCode, string content)> PostXmlAsync<T>(string url, T data, int port)
    {
      using (HttpClient httpClient = new HttpClient())
      {
        httpClient.BaseAddress = new Uri(string.Format(_baseAddressFmt, port));

        string xml = SerializationHelpers.SerializeXml(data);
        using StringContent requestContent = new StringContent(xml, Encoding.UTF8, "text/xml");

        HttpResponseMessage response = await httpClient.PostAsync(url, requestContent);
        string responseContent = await response.Content.ReadAsStringAsync();

        return (response.StatusCode, responseContent);
      }
    }
  }
}
