using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Cailutong.CyptoCoinGateway
{
    public class Helper
    {
        static Helper()
        {
            //实现https post
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
        }
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受     
        }
        /// <summary>  
        /// DateTime时间格式转换为Unix时间戳格式  
        /// </summary>  
        /// <param name="time"> DateTime时间格式</param>  
        /// <returns>Unix时间戳格式</returns>  
        public static int ConvertDateTimeInt(System.DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            return (int)(time - startTime).TotalSeconds;
        }
        /// <summary>
        /// Unix_timeStamp to DateTime
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public static DateTime UnixToDateTime(int timeStamp)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }
        public static string PostQueryString(string url, IDictionary<string, object> query, int timeout)
        {
            StringBuilder str = new StringBuilder();
            foreach (var item in query)
            {
                if (str.Length > 0)
                    str.Append('&');
                str.Append(item.Key);
                str.Append("=");
                str.Append(item.Value);
            }
            return PostQueryString(url, str.ToString(), timeout);
        }
        public static string PostQueryString(string url, IDictionary<string, string> query, int timeout)
        {
            StringBuilder str = new StringBuilder();
            foreach (var item in query)
            {
                if (str.Length > 0)
                    str.Append('&');
                str.Append(item.Key);
                str.Append("=");
                str.Append(item.Value);
            }
            return PostQueryString(url, str.ToString(), timeout);
        }
        public static string PostQueryString(string url, string query, int timeout)
        {
            return PostQueryString(url, null, query, timeout);
        }

        public static string PostJson(string url, object jsonObj, int timeout)
        {
            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = "POST";
                request.ContinueTimeout = timeout;
                request.ContentType = "application/json; charset=utf-8";

                byte[] data = System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj));
                request.ContentLength = data.Length;

                var task = request.GetRequestStreamAsync();
                task.Wait();
                var requestStream = task.Result;
                requestStream.Write(data, 0, data.Length);
                requestStream.Flush();

                var taskResponse = request.GetResponseAsync();
                taskResponse.Wait();
                var responseStream = taskResponse.Result.GetResponseStream();

                var contentType = taskResponse.Result.ContentType;//Content-Type: text/html; charset=GBK
                var match = System.Text.RegularExpressions.Regex.Match(contentType, @"charset\=([\w|\-]+)");
                var charsetCode = Encoding.UTF8;
                if (match != null && !string.IsNullOrEmpty(match.Value))
                {
                    string charset = match.Groups[1].Value;
                    try
                    {
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        charsetCode = Encoding.GetEncoding(charset);
                    }
                    catch
                    {

                    }
                }

                StreamReader sr = new StreamReader(responseStream, charsetCode);
                var result = sr.ReadToEnd().Trim();
                responseStream.Dispose();
                requestStream.Dispose();

                return result;
            }
            catch (Exception ex)
            {
                handleWebException(ex);
                return null;
            }
        }

        public static string PostQueryString(string url, IDictionary<string, string> headers, string query, int timeout)
        {
            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = "POST";
                request.ContinueTimeout = timeout;
                request.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
                if (headers != null)
                {
                    foreach (var item in headers)
                    {
                        request.Headers[item.Key] = item.Value;
                    }
                }
                byte[] data = System.Text.Encoding.UTF8.GetBytes(query);
                request.ContentLength = data.Length;

                var task = request.GetRequestStreamAsync();
                task.Wait();
                var requestStream = task.Result;
                requestStream.Write(data, 0, data.Length);
                requestStream.Flush();

                var taskResponse = request.GetResponseAsync();
                taskResponse.Wait();
                var responseStream = taskResponse.Result.GetResponseStream();

                var contentType = taskResponse.Result.ContentType;//Content-Type: text/html; charset=GBK
                var match = System.Text.RegularExpressions.Regex.Match(contentType, @"charset\=([\w|\-]+)");
                var charsetCode = Encoding.UTF8;
                if (match != null && !string.IsNullOrEmpty(match.Value))
                {
                    string charset = match.Groups[1].Value;
                    try
                    {
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        charsetCode = Encoding.GetEncoding(charset);
                    }
                    catch
                    {

                    }
                }

                StreamReader sr = new StreamReader(responseStream, charsetCode);
                var result = sr.ReadToEnd().Trim();
                responseStream.Dispose();
                requestStream.Dispose();

                return result;
            }
            catch (Exception ex)
            {
                handleWebException(ex);
                return null;
            }
        }

        static void handleWebException(Exception ex)
        {
            var err = ex;
            while (err.InnerException != null && !(err is WebException))
            {
                err = err.InnerException;
            }
            if (!(err is WebException))
                throw err;
            if ((err as WebException).Response == null)
                throw err;

            var res = (HttpWebResponse)(err as WebException).Response;
            var contentType = res.ContentType;//Content-Type: text/html; charset=GBK
            var match = System.Text.RegularExpressions.Regex.Match(contentType, @"charset\=([\w|\-]+)");
            var charsetCode = Encoding.UTF8;
            if (match != null && !string.IsNullOrEmpty(match.Value))
            {
                string charset = match.Groups[1].Value;
                try
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    charsetCode = Encoding.GetEncoding(charset);
                }
                catch
                {

                }
            }
            StreamReader sr = new StreamReader(res.GetResponseStream(), charsetCode);
            var strResult = sr.ReadToEnd().Trim();
            if (strResult.Length == 0)
                throw ex;
            throw new Exception($"http错误信息:{err.Message}\r\n服务器输出内容:{strResult}");
        }

        public static string GetQueryString(string url, int timeout)
        {
            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = "GET";
                request.ContinueTimeout = timeout;

                var taskResponse = request.GetResponseAsync();
                taskResponse.Wait();
                var responseStream = taskResponse.Result.GetResponseStream();

                var contentType = taskResponse.Result.ContentType;//Content-Type: text/html; charset=GBK
                var match = System.Text.RegularExpressions.Regex.Match(contentType, @"charset\=([\w|\-]+)");
                var charsetCode = Encoding.UTF8;
                if (match != null && !string.IsNullOrEmpty(match.Value))
                {
                    string charset = match.Groups[1].Value;
                    try
                    {
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        charsetCode = Encoding.GetEncoding(charset);
                    }
                    catch
                    {

                    }
                }

                StreamReader sr = new StreamReader(responseStream, charsetCode);
                var result = sr.ReadToEnd().Trim();
                responseStream.Dispose();

                return result;
            }
            catch (Exception ex)
            {
                handleWebException(ex);
                return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fileContent"></param>
        /// <param name="formName"></param>
        /// <param name="fileName"></param>
        /// <param name="contentType">如：image/jpeg</param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static string HttpUploadFile(string url, byte[] fileContent, string formName, string fileName, string contentType, Dictionary<string, string> others)
        {
            try
            {
                string result = string.Empty;
                string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
                byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
                wr.ContentType = "multipart/form-data; boundary=" + boundary;
                wr.Method = "POST";
                wr.KeepAlive = true;
                wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

                Stream rs = wr.GetRequestStream();

                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                foreach (string key in others.Keys)
                {
                    rs.Write(boundarybytes, 0, boundarybytes.Length);
                    string formitem = string.Format(formdataTemplate, key, others[key]);
                    byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                    rs.Write(formitembytes, 0, formitembytes.Length);
                }
                rs.Write(boundarybytes, 0, boundarybytes.Length);

                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                string header = string.Format(headerTemplate, formName, fileName, contentType);
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                rs.Write(headerbytes, 0, headerbytes.Length);

                rs.Write(fileContent, 0, fileContent.Length);

                byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                rs.Write(trailer, 0, trailer.Length);
                rs.Close();

                WebResponse wresp = null;
                try
                {
                    wresp = wr.GetResponse();
                    Stream stream2 = wresp.GetResponseStream();
                    StreamReader reader2 = new StreamReader(stream2);

                    result = reader2.ReadToEnd();
                }
                catch (Exception ex)
                {
                    if (wresp != null)
                    {
                        wresp.Close();
                        wresp = null;
                    }
                }
                finally
                {
                    wr = null;
                }

                return result;
            }
            catch (Exception ex)
            {
                handleWebException(ex);
                return null;
            }
        }
        public static string Sign(SortedDictionary<string, object> data, string secret)
        {
            StringBuilder str = new StringBuilder();
            foreach (var item in data)
            {
                if (item.Value == null || item.Key == "sign" || item.Value.ToString().Length == 0)
                    continue;

                if (str.Length > 0)
                    str.Append('&');
                str.Append(item.Key);
                str.Append('=');
                str.Append(item.Value);
            }
            str.Append($"&secret={secret}");
            using (MD5 md5Hash = MD5.Create())
            {
                var bs = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(str.ToString()));
                var sb = new StringBuilder();
                foreach (byte b in bs)
                {
                    sb.Append(b.ToString("x2"));
                }
                //所有字符转为小写
                return sb.ToString().ToLower();
            }
        }

        /// <summary>
        /// 对返回结果进行签名
        /// </summary>
        /// <param name="result"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static SortedDictionary<string, object> SignResult(object result,string secret)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            SortedDictionary<string, object> dict = Newtonsoft.Json.JsonConvert.DeserializeObject<SortedDictionary<string, object>>(json);
            dict["sign"] = Sign(dict, secret);
            return dict;
        }



    }
}
