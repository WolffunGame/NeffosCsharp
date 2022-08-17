using System;
using System.Collections.Generic;
using System.Text;

namespace NeffosCSharp
{
    public class Options
    {
        public Dictionary<string, string> Headers { get; set; }
        public string[] Protocols { get; set; }
        public int ReconnectionAttempts { get; set; }

        private const string URLParamAsHeaderPrefix = "X-Websocket-Header-";
        public string ParseHeadersAsUrlParameters(string url)
        {
            if (Headers == null || Headers.Count == 0)
                return url;
            var stringBuilder = new StringBuilder(url);
            foreach (var header in Headers)
            {
                //if headers has property of key
                if (string.IsNullOrEmpty(header.Key))
                    continue;
                //encode URI component header key
                var key = Uri.EscapeDataString($"{header.Key}{URLParamAsHeaderPrefix}");
                //encode URI component header value
                var value = Uri.EscapeDataString(header.Value);

                var part = $"{key}={value}";
                //if url already contains query string
                if (url.Contains("?"))
                {
                    if (!url.Contains("#"))
                    {
                        var urlParts = url.Split('#');
                        //split url by # and get the first part of the split then append it to the stringBuilder
                        stringBuilder.Append(urlParts[0]);
                        stringBuilder.Append("?");
                        stringBuilder.Append(part);
                        stringBuilder.Append("#");
                        //split url by # and get the second part of the split then append it to the stringBuilder
                        stringBuilder.Append(urlParts[1]);
                    }
                    else
                    {
                        stringBuilder.Append(url);
                        stringBuilder.Append("?");
                        stringBuilder.Append(part);
                    }
                }
                else
                {
                    var urlParts = url.Split('?');
                    stringBuilder.Append(urlParts[0]);
                    stringBuilder.Append("?");
                    stringBuilder.Append(part);
                    stringBuilder.Append("&");
                    stringBuilder.Append(urlParts[1]);
                }
            }
            return stringBuilder.ToString();
        }
        
     }
    
}