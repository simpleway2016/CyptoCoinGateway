using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cailutong.CyptoCoinGateway.BackgroundTask
{
    /// <summary>
    /// 把交易状态通知到客户那里
    /// </summary>
    public class NotifyTask
    {
        public static void SentNotify(  DBModels.Transaction tran,string secret)
        {
            SortedDictionary<string, object> data = null;
            try
            {
              
                using (var db = new MainDB())
                {
                    data = Helper.SignResult(new
                    {
                        outTradeNo = tran.OutTradeNo,
                        payedAmount = tran.PayedAmount,
                        status = (int)tran.Status,
                        //取出比特币交易信息，并转成json字符串
                        cyptoCoinTrans = Newtonsoft.Json.JsonConvert.SerializeObject((from m in db.CyptoCoinTran
                                                                                      where m.TransactionId == tran.id
                                                                                      select new
                                                                                      {
                                                                                          cyptoCoinTransId = m.CyptoCoinTransId,
                                                                                          confirmations = m.Confirmations,
                                                                                          payedAmount = m.PayedAmount,
                                                                                          payTime = m.PayTime // 接收到款项的时间
                                                                                      }).ToArray())
                    }, secret);
                }
                var result = Helper.PostJson(tran.NotifyUrl, data, 8000);
               
            }
            catch (Exception ex)
            {
                using (Way.Lib.CLog log = new Way.Lib.CLog("SentNotify error", false))
                {
                    log.Log($"DBModels.Transaction.Id:{tran.id}");
                    if( data != null )
                    {
                        log.LogJson(data);
                    }
                    log.Log(ex.ToString());
                }
            }
        }
    }
}
