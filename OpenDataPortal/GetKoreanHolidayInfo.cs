﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using System.ComponentModel;
using System.Net.Http;
using System.Xml;


namespace OpenDataPortal
{

    public sealed class GetKoreanHolidayInfo : CodeActivity
    {
        //서비스키 정보 
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> ServiceKey { get; set; }

        // 년도 yyyy 포맷 
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> Year { get; set; }

        // 년도 yyyy 포맷 
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> Month { get; set; }

        // 결과 날자, 휴일이름 의 Dictionary 타입 
        [Category("Output")]
        [RequiredArgument]
        public OutArgument<Dictionary<string, string>> Holidays { get; set; }

        //호출 결과 정보 
        [Category("Output")]
        [RequiredArgument]
        public OutArgument<int> ResultCode { get; set; }

        private HttpClient client;

        // 사용할 공공데이터 END_POINT URL 설정
        private string END_POINT = "http://apis.data.go.kr/B090041/openapi/service/SpcdeInfoService/getRestDeInfo";

        // 작업 결과 값을 반환할 경우 CodeActivity<TResult>에서 파생되고
        // Execute 메서드에서 값을 반환합니다.
        protected override void Execute(CodeActivityContext context)
        {
            client = new HttpClient();

            // 사용자가 입력한 context 받아와서 적용
            string serviceKey = context.GetValue(this.ServiceKey);
            string year = context.GetValue(this.Year);
            string month = context.GetValue(this.Month);
            Dictionary<string, string> holidayDict = new Dictionary<string, string>();
            int resultCode = 0;

            try
            {
                if (client != null)
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/xml");
                    client.DefaultRequestHeaders.Add("User-Agent", "HttpClient/.NET");
                    // Get 전송을 위한 Target URL 설정
                    var targetUrl = string.Format("{0}?serviceKey={1}&solYear={2}&solMonth={3}",
                        END_POINT, serviceKey, year, month);
                    // END_POINT 와 parameter 합친 URL 출력해보기
                    // System.Console.WriteLine(targetUrl);

                    // Response 결과
                    var response = client.GetAsync(targetUrl).Result;

                    // 성공햇을경우
                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        //Console.WriteLine(content);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(content);
                        XmlElement root = doc.DocumentElement;

                        {
                            var nodeList = root.GetElementsByTagName("resultCode");
                            if (nodeList.Count == 1)
                            {
                                var node = nodeList[0];
                                resultCode = Int32.Parse(node.InnerText);
                            }
                            else
                            {
                                resultCode = -1;
                            }
                            if (resultCode == 0)
                            {
                                // XML파일에서 item Node를 받아옴
                                nodeList = root.GetElementsByTagName("item");
                                for (int idx = 0; idx < nodeList.Count; idx++)
                                {
                                    var node = nodeList[idx];
                                    string locdate = string.Empty, dateName = string.Empty, isHoliday = string.Empty;
                                    // XML item 노드 갯수만큼 반복
                                    for (int n = 0; n < node.ChildNodes.Count; n++)
                                    {
                                        var data = node.ChildNodes[n];
                                        if (data.Name == "locdate")
                                        {
                                            locdate = data.InnerText.Trim();
                                        }
                                        else if (data.Name == "dateName")
                                        {
                                            dateName = data.InnerText.Trim();
                                        }
                                        else if (data.Name == "isHoliday")
                                        {
                                            isHoliday = data.InnerText.Trim();
                                        }
                                    }
                                    if (isHoliday.ToUpper() == "Y")
                                    {
                                        holidayDict.Add(locdate, dateName);
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        resultCode = (int)(response.StatusCode);

                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                resultCode = -1;
            }

            // 결과 값 저장
            context.SetValue(this.ResultCode, resultCode);
            context.SetValue(this.Holidays, holidayDict);
        }
    }
}
