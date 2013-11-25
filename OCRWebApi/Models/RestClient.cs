using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace OCRWebApi.Models
{
    /// <summary>
    /// Abstracts REST calls from rest of the application.
    /// </summary>
    public static class RestClient
    {
        /// <summary>
        /// makes a GET call for given URL
        /// </summary>
        /// <param name="url">The URL for REST call</param>
        /// <returns>response data as a string</returns>
        public static JObject GetData(string url)
        {
            var request = WebRequest.Create(url);
            request.Method = "GET";
            JObject result = new JObject();

            using (var response = request.GetResponse() as HttpWebResponse)
            {
                if(response == null)
                {
                    throw new InvalidOperationException("no response returned");
                }
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseStream = response.GetResponseStream();

                    if (responseStream != null)
                    {
                        using (var responseReader = new StreamReader(responseStream))
                        {
                            result = JObject.Parse(responseReader.ReadToEnd());
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("response stream is empty");
                    }
                }
                else
                {
                    throw new HttpException("API returned error code " + response.StatusCode);
                }
            }

            return result;
        }

        public static JObject PostData(string url, string postData)
        {
            var request = WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = postData.Length;
            
            if(postData != null && postData != string.Empty)
            {
                var streamWriter = new StreamWriter(request.GetRequestStream());
                streamWriter.Write(postData);
                streamWriter.Close();
            }
            
            JObject result = new JObject();

            using (var response = request.GetResponse() as HttpWebResponse)
            {
                if (response == null)
                {
                    throw new InvalidOperationException("no response returned");
                }
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseStream = response.GetResponseStream();

                    if (responseStream != null)
                    {
                        using (var responseReader = new StreamReader(responseStream))
                        {
                            result = JObject.Parse(responseReader.ReadToEnd());
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("response stream is empty");
                    }
                }
                else
                {
                    throw new HttpException("API returned error code " + response.StatusCode);
                }
            }

            return result;
        }
    }
}