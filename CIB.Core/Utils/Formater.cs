using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CIB.Core.Utils
{
    public static class Formater
    {
        public static string PhoneNumber(string phone)
        {
            var validateNumber = String.Empty;
            if (phone[..3] == "234")
            {
                validateNumber = phone;
            }
            else if (phone[..4] == "+234")
            {
                validateNumber = phone.Trim().Substring(1);
            }
            else if (phone.Length == 11)
            {
                validateNumber = "234" + phone.Trim().Substring(1);
            }
            else
            {
                validateNumber = phone;
            }
            return validateNumber;
        }
        public static string SerializeData(object postdata, string contentType)
        {
            var resp = "";
            switch (contentType)
            {
                case "application/json":
                resp = JsonConvert.SerializeObject(postdata);
                break;
                case "application/x-www-form-urlencoded":
                var jo = (JObject)postdata;
                resp = string.Join("&", jo.Properties().Select(property => property.Name + "=" + property.Value.ToString()).ToArray());
                break;
                default:
                break;
            }
            return resp;
        }
        public static string GetContentType(string path)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }
        public static string JsonType(object data)
        {
            return JsonConvert.SerializeObject(data);
        }
    }
}