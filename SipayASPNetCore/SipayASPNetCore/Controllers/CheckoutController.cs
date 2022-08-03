using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SipayASPNetCore.Extensions;
using SipayASPNetCore.Models;
using SipayASPNetCore.Requests;
using SipayASPNetCore.Responses;
using SipayASPNetCore.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SipayASPNetCore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ILogger<CheckoutController> _logger;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CheckoutController(ILogger<CheckoutController> logger, IConfiguration config,
        IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Index(IFormCollection form)
        {
            var paymentForm = GetPaymentInfo(form);

            var recurring = GetRecurringPaymentInfo(form);

            Settings settings = new Settings();

            settings.AppID = _config["SIPAY:AppID"];
            settings.AppSecret = _config["SIPAY:AppSecret"];
            settings.MerchantKey = _config["SIPAY:MerchantKey"];
            settings.BaseUrl = _config["SIPAY:BaseUrl"];

            Item product = new Item();
            product.Description = "";
            product.Name = "Test Product";
            product.Quantity = 1;
            product.Price = paymentForm.Amount;

          if (paymentForm.Is3D == PaymentType.WhiteLabel3D || paymentForm.Is3D == PaymentType.WhiteLabel2DOr3D)
            {
                //// 3D

                Sipay3DPaymentRequest paymentRequest = new Sipay3DPaymentRequest(settings, paymentForm.SelectedPosData);

                paymentRequest.CCNo = paymentForm.CreditCardNumber.Replace(" ", "");
                paymentRequest.CCHolderName = paymentForm.CreditCardName;
                paymentRequest.CCV = paymentForm.CreditCardCvv2;
                paymentRequest.ExpiryYear = paymentForm.CreditCardExpireYear.ToString();
                paymentRequest.ExpiryMonth = paymentForm.CreditCardExpireMonth.ToString();
                paymentRequest.InvoiceDescription = "";
                paymentRequest.InvoiceId = paymentForm.OrderId;

                string baseUrl = _httpContextAccessor.HttpContext.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value;
                paymentRequest.ReturnUrl = baseUrl + "/Checkout/SuccessUrl";
                paymentRequest.CancelUrl = baseUrl + "/Checkout/CancelUrl";

                paymentRequest.Items.Add(product);

                if (recurring.Item1)
                {
                    paymentRequest.IsRecurringPayment = true;
                    paymentRequest.RecurringPaymentNumber = recurring.Item2;
                    paymentRequest.RecurringPaymentCycle = recurring.Item3;
                    paymentRequest.RecurringPaymentInterval = recurring.Item4;
                    paymentRequest.RecurringWebhookKey = "yakala.co";

                }

                string requestForm = paymentRequest.GenerateFormHtmlToRedirect(_config["SIPAY:BaseUrl"] + "/api/pay3d");

                var bytes = Encoding.UTF8.GetBytes(requestForm);
                await HttpContext.Response.Body.WriteAsync(bytes, 0, bytes.Length);

                //return View("Request3DSipay", requestForm);
            }

            return View();
        }



        [NonAction]
        public (bool, int, string, int) GetRecurringPaymentInfo(IFormCollection form)
        {
            bool is_recurring_payment = false;
            int recurring_payment_number = 0;
            string recurring_payment_cycle = "";
            int recurring_payment_interval = 0;

            if (!String.IsNullOrEmpty(form["is_recurring_payment"]))
            {
                is_recurring_payment = form["is_recurring_payment"] == "on";
                //Boolean.TryParse(form["is_recurring_payment"], out is_recurring_payment);
            }

            if (!String.IsNullOrEmpty(form["recurring_payment_number"]))
            {
                Int32.TryParse(form["recurring_payment_number"], out recurring_payment_number);
            }

            if (!String.IsNullOrEmpty(form["recurring_payment_cycle"]))
            {
                recurring_payment_cycle = form["recurring_payment_cycle"];
            }

            if (!String.IsNullOrEmpty(form["recurring_payment_interval"]))
            {
                Int32.TryParse(form["recurring_payment_interval"], out recurring_payment_interval);
            }

            return (is_recurring_payment, recurring_payment_number, recurring_payment_cycle, recurring_payment_interval);
        }



        [NonAction]
        public PaymentModel GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new PaymentModel();

            paymentInfo.CreditCardType = form["CreditCardType"];
            paymentInfo.CreditCardName = form["CardholderName"];

            if (!String.IsNullOrEmpty(form["card-number"]))
            {
                paymentInfo.CreditCardNumber = form["card-number"];
            }
            if (!String.IsNullOrEmpty(form["ExpireMonth"]))
            {
                paymentInfo.CreditCardExpireMonth = int.Parse(form["ExpireMonth"]);
            }
            if (!String.IsNullOrEmpty(form["ExpireYear"]))
            {
                paymentInfo.CreditCardExpireYear = int.Parse(form["ExpireYear"]);
            }

            if (!String.IsNullOrEmpty(form["Amount"]))
            {
                paymentInfo.Amount = decimal.Parse(form["Amount"]);
            }

            if (!String.IsNullOrEmpty(form["OrderId"]))
            {
                paymentInfo.OrderId = form["OrderId"];
            }
            paymentInfo.CreditCardCvv2 = form["CardCode"];

            if (!String.IsNullOrEmpty(form["SelectedPosData"]))
            {
                var posData = form["SelectedPosData"];

                paymentInfo.SelectedPosData = JsonConvert.DeserializeObject<PosData>(form["SelectedPosData"]);
            }

            if (!String.IsNullOrEmpty(form["Is3D"]))
            {
                paymentInfo.Is3D = (PaymentType)(Int32.TryParse(form["Is3D"], out int is3D) ? is3D : 0);

            }

            return paymentInfo;
        }



        public ActionResult CheckBinCode(string binCode, decimal amount, bool isRecurring)
        {
            if (binCode.Length >= 6)
            {
                Settings settings = new Settings();

                settings.AppID = _config["SIPAY:AppID"];
                settings.AppSecret = _config["SIPAY:AppSecret"];
                settings.MerchantKey = _config["SIPAY:MerchantKey"];
                settings.BaseUrl = _config["SIPAY:BaseUrl"];

                SipayGetPosRequest posRequest = new SipayGetPosRequest();

                posRequest.CreditCardNo = binCode;
                posRequest.Amount = amount;
                posRequest.CurrencyCode = "TRY";
                //posRequest.CurrencyCode = "EUR";

                posRequest.IsRecurring = isRecurring;

                SipayGetPosResponse posResponse = SipayPaymentService.GetPos(posRequest, settings, GetAuthorizationToken(settings).Data.token);

                //GEÇİCİ

                for (int i = 0; i < posResponse.Data.Count; i++)
                {
                    posResponse.Data[i].amount_to_be_paid = posResponse.Data[i].amount_to_be_paid + (i * 0.1M);
                }

                return Ok(new { posResponse = posResponse, is_3d = GetAuthorizationToken(settings).Data.is_3d });
            }

            return Ok();
        }


        [NonAction]
        public SipayTokenResponse GetAuthorizationToken(Settings settings)
        {
            if (HttpContext.Session.Get<SipayTokenResponse>("token") == default)
            {
                SipayTokenResponse tokenResponse = SipayPaymentService.CreateToken(settings);

                HttpContext.Session.Set<SipayTokenResponse>("token", tokenResponse);
            }

            return HttpContext.Session.Get<SipayTokenResponse>("token");
        }

    }
}
