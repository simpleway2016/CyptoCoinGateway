using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Filters;
using Cailutong.CyptoCoinGateway.CyptoCoinPlatform.Impls.BTC;
using Cailutong.CyptoCoinGateway.Models;

namespace Cailutong.CyptoCoinGateway.Controllers
{
    [Produces("application/json")]
    public class ApiController : Controller
    {
        public object Test()
        {
            return new
            {
                Name = "hello",
                Other = 3
            };
        }

        

        /// <summary>
        /// 为新交易生成付款地址
        /// </summary>
        /// <param name="db"></param>
        /// <param name="account"></param>
        /// <param name="currency"></param>
        /// <param name="client"></param>
        /// <param name="tran"></param>
        void transactionGetCyptoCoinAddress(MainDB db ,string account,string currency,CyptoCoinPlatform.ICyptoCoinClient client, DBModels.Transaction tran)
        {
            var time = DateTime.Now.AddDays(-1);
            //获取一天前生成的交易，但是一直没有支付，可以认为地址已经不被使用，应该是不会有人在用
            var oldtran = db.Transaction.FirstOrDefault(m => m.Status == DBModels.Transaction_StatusEnum.WaitForPay && m.Currency == currency && m.CreateTime < time);
            string cyptoAddress = null; 
            if(oldtran  != null)
            {
                //
                cyptoAddress = oldtran.CyptoCoinAddress;
                //把这笔交易作废
                oldtran.Status = DBModels.Transaction_StatusEnum.Invalided;
                db.Update(oldtran);
            }

            if(cyptoAddress == null)
            {
                var walletKeyInfo = (from m in db.Wallet
                                     join b in db.WalletCyptoCoinInfo on m.id equals b.WalletId
                                     where m.Account == account
                                     select b).FirstOrDefault();

                //获取一个付款地址
                cyptoAddress = client.GetUnusedAddress(walletKeyInfo.Key1 , walletKeyInfo.Key2);
            }
            tran.Currency = currency;
            tran.CyptoCoinAddress = cyptoAddress;
            tran.CreateTime = DateTime.Now;
            tran.Status = DBModels.Transaction_StatusEnum.WaitForPay;
            
        }

        /// <summary>
        /// 查询支付状态
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        public object Query([FromBody] NullableSortedDict<string, object> postData)
        {
            try
            {
                using (var db = new MainDB())
                {
                    string outTradeNo = postData.GetValue<string>("outTradeNo");
                    string sign = postData.GetValue<string>("sign");

                    var tran = db.Transaction.FirstOrDefault(m => m.OutTradeNo == outTradeNo);
                    if (tran == null)
                    {
                        throw new Exception("无效的outTradeNo");
                    }

                    var wallet = db.Wallet.FirstOrDefault(m => m.id == tran.WalletId);

                    var signResult = Helper.Sign(postData, wallet.Secret);
                    if (signResult != sign)
                        throw new Exception("签名校验失败");

                    return Helper.SignResult(new
                    {
                        outTradeNo = outTradeNo,
                        payedAmount = tran.PayedAmount,
                        status = (int)tran.Status,
                        cyptoCoinTrans = Newtonsoft.Json.JsonConvert.SerializeObject( (from m in db.CyptoCoinTran
                                          where m.TransactionId == tran.id
                                          select new {
                                              cyptoCoinTransId = m.CyptoCoinTransId,
                                              confirmations = m.Confirmations,
                                              payedAmount = m.PayedAmount,
                                              payTime = m.PayTime // 接收到款项的时间
                                          }).ToArray())
                    }, wallet.Secret);
                }
            }
            catch(Exception ex)
            {
                return new
                {
                    status = "error",
                    errMsg = ex.Message
                };
            }
        }

        /// <summary>
        /// 发起btc支付交易
        /// </summary>
        /// <returns></returns>
        public object Pay([FromBody] NullableSortedDict<string, object> postData)
        {
            using (var db = new MainDB())
            using (Way.Lib.CLog log = new Way.Lib.CLog("Pay"))
            {
                try
                {
                    double? amount = postData.GetValue<double?>("amount");
                    string account = postData.GetValue<string>("account");
                    string sign = postData.GetValue<string>("sign");
                    string outTradeNo = postData.GetValue<string>("outTradeNo");
                    string notifyUrl = postData.GetValue<string>("notifyUrl");
                    string currency = postData.GetValue<string>("currency");

                    if (amount <= 0)
                        throw new Exception("金额无效");

                    var wallet = db.Wallet.FirstOrDefault(m => m.Account == account);
                    if (wallet == null)
                        throw new Exception($"账户“{account}”不存在");
                    
                    log.LogJson(postData);
                    currency = currency.ToUpper();

                    var signResult = Helper.Sign(postData, wallet.Secret);
                    if (signResult != sign)
                        throw new Exception("签名校验失败");

                    var newTran = new DBModels.Transaction {
                        Amount = amount,
                        NotifyUrl = notifyUrl,
                        OutTradeNo = outTradeNo
                    };

                    //根据币种创建ICyptoCoinClient
                    var client = Activator.CreateInstance( typeof(ApiController).Assembly.GetType($"Cailutong.CyptoCoinGateway.CyptoCoinPlatform.Impls.{currency}.{currency}_Client")) as CyptoCoinPlatform.ICyptoCoinClient;
                    if (client == null)
                        throw new Exception("不支持" + currency);

                    //填充交易里面的付款地址
                    transactionGetCyptoCoinAddress(db, account, currency, client, newTran);
                    newTran.WalletId = wallet.id;
                    db.Insert(newTran);

                    return Helper.SignResult(new
                    {
                        status = "success",
                        outTradeNo = outTradeNo,
                        targetAddress = newTran.CyptoCoinAddress
                    },wallet.Secret);
                }
                catch (Exception ex)
                {
                    log.Log(ex.ToString());
                    return new
                    {
                        status = "error",
                        errMsg = ex.Message
                    };
                }
            }
        }

#if DEBUG
        public object PayTest()
        {
            NullableSortedDict<string, object> postDict = new NullableSortedDict<string, object>();
            postDict["account"] = "34509003";
            postDict["outTradeNo"] = Guid.NewGuid().ToString("N");
            postDict["amount"] = 0.0001;
            postDict["notifyUrl"] = $"http://{Request.Host}/api/callbacktest";
            postDict["currency"] = "BTC";
            postDict["sign"] = Helper.Sign(postDict , "810de1dffdb7409383370f2bc509f7a3");

            return Pay(postDict );
        }

     
        public object callbacktest([FromBody] NullableSortedDict<string, object> data)
        {
            //string outTradeNo,int status,double? payedAmount

            return new {
                status = "success"
            };
        }
#endif
    }
}
