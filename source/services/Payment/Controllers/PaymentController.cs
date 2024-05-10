using System;
using System.Framework;
using System.Framework.Data;
using System.Framework.Logging;
using System.Framework.Payment;
using System.Framework.Sales;
using System.Framework.Web;
using System.Linq;
using EmptyProject.Services.Payment.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmptyProject.Services.Payment.Controllers {
    [Route("api/[controller]")]
    [EnableCors("AllPassOrigins")]
    public class PaymentController : ApiController<ServiceUser, Model, Culture> {
        [HttpPost("Auth")]
        public JsonResponse Auth(int paymentId, PaymentType type, string provider = null) {
            try {
                var authData = GetParameter<AuthData>();
                var service = PaymentService.GetService(type, provider);
                if (service.TransactionTypes.Contains(PaymentTransactionType.Auth))
                    return Json(ResponseStatus.InternalServerError, null, "此金流服務無法授權");
                var payment = Model.DataContext.Payment.Find(paymentId);
                var record = service.Auth(payment, authData);
                Model.DataContext.SaveChanges();
                return Json(ResponseStatus.OK, record);
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e.Message, "系統忙碌中，請稍後再試");
            }
        }

        [HttpPost("Void")]
        public JsonResponse Void(int paymentId) {
            try {
                var payment = Model.DataContext.Payment.Include("Acquier").SingleOrDefault(e => e.Id == paymentId);
                var paymentType = payment.Type;
                var provider = payment.Acquier.ShortCode;

                var service = PaymentService.GetService(paymentType, provider);
                if (service.TransactionTypes.Contains(PaymentTransactionType.Void))
                    return Json(ResponseStatus.InternalServerError, null, "此金流服務不支援廢止");
                var record = service.Void(payment);
                Model.DataContext.SaveChanges();
                return Json(ResponseStatus.OK, record);
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e.Message, "系統忙碌中，請稍後再試");
            }
        }

        [HttpPost("Capture")]
        public JsonResponse Capture(int paymentId) {
            try {
                var payment = Model.DataContext.Payment.Include("Acquier").SingleOrDefault(e => e.Id == paymentId);
                var paymentType = payment.Type;
                var provider = payment.Acquier.ShortCode;

                var service = PaymentService.GetService(paymentType, provider);
                if (service.TransactionTypes.Contains(PaymentTransactionType.Capture))
                    return Json(ResponseStatus.InternalServerError, null, "此金流服務不支援請款");
                var record = service.Capture(payment);
                Model.DataContext.SaveChanges();
                return Json(ResponseStatus.OK, record);
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e.Message, "系統忙碌中，請稍後再試");
            }
        }

        [HttpPost("Refund")]
        public JsonResponse Refund(int paymentId) {
            try {
                var authData = GetParameter<AuthData>();
                var payment = Model.DataContext.Payment.Include("Acquier").SingleOrDefault(e => e.Id == paymentId);
                var paymentType = payment.Type;
                var provider = payment.Acquier.ShortCode;

                var service = PaymentService.GetService(paymentType, provider);
                if (service.TransactionTypes.Contains(PaymentTransactionType.Refund))
                    return Json(ResponseStatus.InternalServerError, null, "此金流服務不支援退款");
                var record = service.Refund(payment, authData);
                Model.DataContext.SaveChanges();
                return Json(ResponseStatus.OK, record);
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e.Message, "系統忙碌中，請稍後再試");
            }
        }

        [HttpPost("Continue")]
        public JsonResponse Continue(int recordId) {
            try {
                var record = Model.DataContext.PaymentRecord.Include("Payment.Acquier").SingleOrDefault(e => e.Id == recordId);
                var paymentType = record.Payment.Type;
                var provider = record.Payment.Acquier.ShortCode;

                var service = PaymentService.GetService(paymentType, provider);
                var parameters = new Parameters();
                parameters.Add("recordId", recordId);
                service.Continue(record, parameters);
                Model.DataContext.SaveChanges();

                return Json(ResponseStatus.OK, record);
            } catch (Exception e) {
                Logger.LogError(e, e.Message);
                return Json(ResponseStatus.InternalServerError, e.Message, "系統忙碌中，請稍後再試");
            }
        }
    }
}