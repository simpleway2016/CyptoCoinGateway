using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Cailutong.CyptoCoinGateway.CyptoCoinPlatform.Impls.BTC;

namespace Cailutong.CyptoCoinGateway.Controllers
{
    [Produces("application/json")]
    public class AccountController : Controller
    {

        /// <summary>
        /// 生成一个新的钱包
        /// </summary>
        /// <returns></returns>
        DBModels.Wallet CreateWallet()
        {
            string account = null;
            using (var db = new MainDB())
            {
                //generate account number
                while (account == null)
                {
                    account = new Random().Next(10000000, 99999999).ToString();
                    if (db.Wallet.Any(m => m.Account == account))
                    {
                        //account exists,generate account number again
                        account = null;
                    }
                }

                db.BeginTransaction();

                try
                {
                    var wallet = new DBModels.Wallet()
                    {
                        Account = account,
                        Secret = Guid.NewGuid().ToString("N")
                    };
                    db.Insert(wallet);

                    #region 创建BTC平台密钥
                    BTC_Client btc_client = new BTC_Client();
                    byte[] key1, key2;
                    btc_client.CreatePlatformKey(out key1, out key2);

                    var walletInfo = new DBModels.WalletCyptoCoinInfo()
                    {
                        Key1 = key1,
                        Key2 = key2,
                        WalletId = wallet.id,
                        Currency = "BTC",
                    };
                    db.Insert(walletInfo); 
                    #endregion


                    db.CommitTransaction();

                    return wallet;
                }
                catch
                {
                    db.RollbackTransaction();
                    throw;
                }
            }
        }
    }
}