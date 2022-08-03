using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System;
using System.Collections.Generic;
using SipayASPNetCore.Requests;
using SipayASPNetCore.Models;
using SipayASPNetCore.Responses;
using SipayASPNetCore.Helpers;
using Newtonsoft.Json;

namespace SipayASPNetCore.Services
{
    public class SipayPaymentService
    {

        private static readonly HttpClient _httpClient;
        static SipayPaymentService()
        {
#if !NETSTANDARD
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#endif

            _httpClient = new HttpClient();
        }


        //TAKSİT BİLGİSİ ALMA
        //taksit listesini tarım kartları için vade aralığını ve ödeme sıklığını sağlamaktan sorumludur.
        public static SipayGetPosResponse GetPos(SipayGetPosRequest request, Settings settings, string token)
        {

            request.MerchantKey = settings.MerchantKey;

            var header = new Dictionary<string, string>();
            header.Add("Authorization", "Bearer " + token);

            SipayGetPosResponse response = PostDataAsync<SipayGetPosResponse, SipayGetPosRequest>(settings.BaseUrl + "/api/getpos", request, header);

            return response;
        }




        public static Response PostDataAsync<Response, Request>(string endPoint, Request dto, Dictionary<string, string> headers = null)
        {

            HttpRequestMessage requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(endPoint),
                Content = JsonBuilder.ToJsonString<Request>(dto)
            };

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }
            }

            var httpResponse = _httpClient.SendAsync(requestMessage).Result;

            // GEÇİCİ
            var t = httpResponse.Content.ReadAsStringAsync().Result;

            if (!httpResponse.IsSuccessStatusCode)
            {
                return default;
            }


            return JsonConvert.DeserializeObject<Response>(httpResponse.Content.ReadAsStringAsync().Result);
        }

        //TOKEN ALMA: Api iş yerini doğrulamak için diğer apilerle kullanılacak bir token oluşturulur
        //Üye iş yeri için ayarlanan ödeme entegrasyon seçeneği de döndürülür
        //yanıt anahtarı is_3D dir: 0 whiteLabel 2D,2: 1 whitelabel 2D veya 3D,3: whitelabel 3D,4: Markalı ödeme çözümü
        public static SipayTokenResponse CreateToken(Settings settings)
        {
            SipayTokenRequest tokenRequest = new SipayTokenRequest();
            tokenRequest.AppID = settings.AppID;
            tokenRequest.AppSecret = settings.AppSecret;

            SipayTokenResponse response = PostDataAsync<SipayTokenResponse, SipayTokenRequest>(settings.BaseUrl + "/api/token", tokenRequest);
            return response;
        }


    }

}
