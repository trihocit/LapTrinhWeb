﻿using DALTWeb.Helpers;
using DALTWeb.ViewModels;

namespace DALTWeb.Services
{
	public class VNPayService : IVnPayService
	{
		private readonly IConfiguration _config;

		public VNPayService(IConfiguration config) 
		{
			_config = config;
		}
		public string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model)
		{
			var tick = DateTime.Now.Ticks.ToString();
			var vnpay = new VnPayLibrary();
			vnpay.AddRequestData("vnp_Version", _config["VnPay:Version"]);
			vnpay.AddRequestData("vnp_Command", _config["VnPay:Command"]);
			vnpay.AddRequestData("vnp_TmnCode", _config["VnPay:TmnCode"]);
			vnpay.AddRequestData("vnp_Amount", (model.Amount).ToString());
			vnpay.AddRequestData("vnp_CreateDate", model.CreateDate.ToString("yyyyMMddHHmmss"));
			vnpay.AddRequestData("vnp_CurrCode", _config["VnPay:CurrCode"]);
			vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
			vnpay.AddRequestData("vnp_Locale", _config["VnPay:Locale"]);
			vnpay.AddRequestData("vnp_OrderInfo", model.OrderId.ToString());
			vnpay.AddRequestData("vnp_OrderType", "other");
			vnpay.AddRequestData("vnp_ReturnUrl", _config["VnPay:PaymentBackReturnUrl"]);
			vnpay.AddRequestData("vnp_TxnRef", tick);

			var paymentUrl = vnpay.CreateRequestUrl(_config["Vnpay:BaseUrl"], _config["Vnpay:HashSecret"]);
			return paymentUrl;
		}

		public VnPaymentResponseModel PaymentExecute(IQueryCollection collections)
		{
			var vnpay = new VnPayLibrary();
			foreach(var(key, value) in collections)
			{
				if(!string.IsNullOrEmpty(key)&&key.StartsWith("vnp_"))
				{
					vnpay.AddResponseData(key, value.ToString());
				}
			}

			var vnp_orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
			var vnp_TransactionId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
			var vnp_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
			var vnp_ResponCode = vnpay.GetResponseData("vnp_ResponseCode");
			var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

			bool checkSignnature = vnpay.ValidateSignature(vnp_SecureHash, _config["Vnpay:HashSecret"]);
			if(!checkSignnature)
			{
				return new VnPaymentResponseModel
				{
					Success = false
				};
			}

			return new VnPaymentResponseModel {
				Success = true, 
				PaymentMethod = "VnPay",
				OrderDescription = vnp_OrderInfo,
				OrderId = vnp_orderId.ToString(),
				TransactionId = vnp_TransactionId.ToString(),
				Token = vnp_SecureHash,
				VnPayResponseCode = vnp_ResponCode
			};
		}
	}
}
