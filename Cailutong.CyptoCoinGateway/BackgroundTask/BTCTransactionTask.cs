using NBitcoin;
using NBXplorer;
using NBXplorer.DerivationStrategy;
using NBXplorer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cailutong.CyptoCoinGateway.BackgroundTask
{
    public class BTCTransactionTask
    {
        public static void Run()
        {
            var network = new NBXplorerNetworkProvider(ChainType.Main).GetBTC();
            ExplorerClient client = new ExplorerClient(network , new Uri(Config.Instance.NBXplorer.ServerUrl));
            Dictionary<int, GetTransactionsResponse> lastResponseDict = new Dictionary<int, GetTransactionsResponse>();                     

            using (Way.Lib.CLog log = new Way.Lib.CLog("runForTransactions"))
            {
                while (true)
                {
                    try
                    {
                        //程序刚启动，把所有交易拿出来检验一遍
                        checkAllTransactionForInit(network, client, log);
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Threading.Thread.Sleep(3000);
                        using (Way.Lib.CLog logErr = new Way.Lib.CLog("handleAllTransactionForInit error", false))
                        {
                            logErr.Log(ex.ToString());
                        }
                    }
                }

                while (true)
                {
                    try
                    {
                        var events = client.CreateNotificationSession();
                        events.ListenAllDerivationSchemes();
                        log.Log($"init");
                        while (true)
                        {
                            log.Log($"waiting event...");

                            var txEvent = events.NextEvent();
                            log.Log($"received event,type:{txEvent?.GetType().FullName}");
                            if (txEvent is NBXplorer.Models.NewTransactionEvent)
                            {
                                using (var db = new MainDB())
                                {
                                    var tranEvent = txEvent as NBXplorer.Models.NewTransactionEvent;
                                    checkTransaction(db, tranEvent.TransactionData.Transaction, tranEvent.TransactionData.Confirmations, log);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Threading.Thread.Sleep(2000);
                        using (Way.Lib.CLog logErr = new Way.Lib.CLog("runForTransactions error", false))
                        {
                            logErr.Log(ex.ToString());
                        }
                    }
                }
            }
        }

        static void checkAllTransactionForInit(NBXplorerNetwork network, ExplorerClient client ,Way.Lib.CLog log)
        {
            using (var db = new MainDB())
            {
                var allWalletInfos = db.WalletCyptoCoinInfo.Where(m => m.Currency == "BTC").ToArray();
                foreach (var walletinfo in allWalletInfos)
                {
                    var publicKey = new ExtPubKey(walletinfo.Key2);
                    var userDerivationScheme = network.DerivationStrategyFactory.CreateDirectDerivationStrategy(publicKey, new DerivationStrategyOptions()
                    {
                        // Use non-segwit
                        Legacy = true
                    });
                   var lastResponse = client.GetTransactions(userDerivationScheme,null);

                    List<TransactionInformation> allTrans = new List<TransactionInformation>();
                    allTrans.AddRange(lastResponse.UnconfirmedTransactions.Transactions);
                    //为了防止程序关闭期间，有transaction发生，所以ConfirmedTransactions也要分析
                    allTrans.AddRange(lastResponse.ConfirmedTransactions.Transactions);

                    foreach (var payedTranInfo in allTrans)
                    {
                        var payedTranid = payedTranInfo.TransactionId.ToString();
                        if (payedTranInfo.Confirmations == 0)
                            log.Log($"received unconfirmed transaction：{payedTranid}");
                        else
                            log.Log($"received confirmed({payedTranInfo.Confirmations})");

                        checkTransaction(db, payedTranInfo.Transaction, payedTranInfo.Confirmations, log);
                    }
                }
            }
        }

        static void checkTransaction(MainDB db, NBitcoin.Transaction cyptoCoinTransaction,int confirmations,Way.Lib.CLog log)
        {
            var payedTranid = cyptoCoinTransaction.GetHash().ToString();
            log.Log($"transactionid：{payedTranid}");

            foreach (var txOut in cyptoCoinTransaction.Outputs)
            {
                var addr = txOut.ScriptPubKey.GetDestinationAddress(NBitcoin.Network.Main).ToString();
                log.Log($"txOut：{addr} value:{txOut.Value}");

                //如果支付地址和钱包交易里的地址一致，那么，进行处理
                var transaction = db.Transaction.FirstOrDefault(m => m.Status != DBModels.Transaction_StatusEnum.Invalided && m.CyptoCoinAddress == addr);
                if (transaction != null)
                {
                    var cyptoCoinTranItem = db.CyptoCoinTran.FirstOrDefault(m => m.TransactionId == transaction.id && m.CyptoCoinTransId == payedTranid);
                    if (cyptoCoinTranItem == null)
                    {
                        log.Log($"belong to Transaction {transaction.id}，increase Transaction's PayedAmount");

                        var amount = Convert.ToDouble(txOut.Value.ToDecimal(NBitcoin.MoneyUnit.BTC));
                        transaction.PayedAmount += amount;
                        if (transaction.PayedAmount >= transaction.Amount)
                        {
                            transaction.Status = DBModels.Transaction_StatusEnum.AllPayed;
                        }
                        else
                        {
                            transaction.Status = DBModels.Transaction_StatusEnum.PartialPayed;
                        }
                        db.Update(transaction);

                        var secret = db.Wallet.Where(m => m.id == transaction.WalletId).Select(m => m.Secret).FirstOrDefault();

                        //发送通知
                        if (!string.IsNullOrEmpty(transaction.NotifyUrl))
                        {
                            Task.Run(() => NotifyTask.SentNotify( transaction, secret));
                        }

                        cyptoCoinTranItem = new DBModels.CyptoCoinTran()
                        {
                            CyptoCoinTransId = payedTranid,
                            TransactionId = transaction.id,
                            PayedAmount = amount,
                            PayTime = DateTime.Now
                        };
                        db.Insert(cyptoCoinTranItem);

                        
                    }
                    else
                    {
                        cyptoCoinTranItem.Confirmations = confirmations;
                        db.Update(cyptoCoinTranItem);


                        //发送通知
                        if (!string.IsNullOrEmpty(transaction.NotifyUrl) && confirmations <= 6)
                        {
                            var secret = db.Wallet.Where(m => m.id == transaction.WalletId).Select(m => m.Secret).FirstOrDefault();
                            Task.Run(() => NotifyTask.SentNotify(transaction, secret));
                        }
                    }
                }
            }
        }
    }
}
