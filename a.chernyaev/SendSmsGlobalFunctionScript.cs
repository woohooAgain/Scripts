using System;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
// имя класса "Script" нужно оставить
public static class Script
{

    // имя метода "Main" нужно оставить
    public static Dictionary<string, object> Main(int Phone, string StringPhone, string Text)
    {
        var apiId = "8B3E5CA0-ACBE-D1D0-E204-6607D2F593AF";
        //var url = $"https://sms.ru/sms/send";
        var url = $"https://sms.ru/sms/send?api_id={apiId}&to={StringPhone}&msg={Text}&json=1";


        var webRequest = (HttpWebRequest)WebRequest.Create(url);
        webRequest.ContentType = "application/json; charset=UTF-8";
        webRequest.Method = "POST";
        
        //var data = new Request
        //{
        //    api_id = apiId,
        //    to = StringPhone,
        //    msg = Text
        //};
        //var json = JsonConvert.SerializeObject(data);
        //var postBytes = Encoding.UTF8.GetBytes(json);
//
        //using (var webRequestStream = webRequest.GetRequestStream())
        //{
        //    webRequestStream.Write(postBytes, 0, postBytes.Length);
        //    webRequestStream.Close();
        //}

        var response = webRequest.GetResponse();
        var responseStream = response.GetResponseStream();
        SmsResult smsResult;
        string responseString;
        var serializer = new JsonSerializer();
        var sr = new StreamReader(responseStream);
        
            var jsonTextReader = new JsonTextReader(sr);
            
                smsResult = serializer.Deserialize<SmsResult>(jsonTextReader);
            
            responseString = sr.ReadToEnd();
		
        var success = smsResult.status == "OK";

        var result = new Dictionary<string, object>();
        result.Add("Success", success);
        result.Add("Response", responseString);
        return result;
    
    }

    public class Request
    {
        public string api_id { get; set; }
        public string to { get; set; }
        public string msg { get; set; }
    }

    public class SmsResult
    {
        public string status { get; set; }
        public int status_code { get; set; }
        public Dictionary<string, PhoneResult>  sms { get; set; }
        public decimal balance { get; set; }
    }

    public class PhoneResult
    {
        public string status { get; set; }
        public int status_code { get; set; }
        public string sms_id { get; set; }
    }
}