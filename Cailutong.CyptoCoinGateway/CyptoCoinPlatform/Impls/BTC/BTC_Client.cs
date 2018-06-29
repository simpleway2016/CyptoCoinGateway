using NBitcoin;
using NBXplorer;
using NBXplorer.DerivationStrategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cailutong.CyptoCoinGateway.CyptoCoinPlatform.Impls.BTC
{
    public class BTC_Client : ICyptoCoinClient
    {
        NBXplorerNetwork _Network;
        public NBXplorerNetwork Network
        {
            get
            {
                return _Network ?? (_Network = new NBXplorerNetworkProvider(ChainType.Main).GetBTC());
            }
        }

        ExplorerClient _NbxClient;
        public ExplorerClient NbxClient
        {
            get
            {
                return _NbxClient ?? (_NbxClient = new ExplorerClient(this.Network, new Uri(Config.Instance.NBXplorer.ServerUrl)));
            }
        }
        public void CreatePlatformKey(out byte[] key1, out byte[] key2)
        {
            var userExtKey = new ExtKey();
            ExtPubKey pubkey = userExtKey.Neuter();

            key1 = userExtKey.PrivateKey.ToBytes();
            key2 = pubkey.ToBytes();

            var userDerivationScheme = this.Network.DerivationStrategyFactory.CreateDirectDerivationStrategy(pubkey, new DerivationStrategyOptions()
            {
                // Use non-segwit
                Legacy = true
            });

            //set the key to NBXplorer server
            this.NbxClient.Track(userDerivationScheme);
        }

        public string GetUnusedAddress(byte[] key1, byte[] key2)
        {
            var publicKey = new ExtPubKey(key2);
            var userDerivationScheme = this.Network.DerivationStrategyFactory.CreateDirectDerivationStrategy(publicKey, new DerivationStrategyOptions()
            {
                // Use non-segwit
                Legacy = true
            });
            var info = this.NbxClient.GetUnused(userDerivationScheme , DerivationFeature.Deposit, 0, true);
            return info.ScriptPubKey.GetDestinationAddress(NBitcoin.Network.Main).ToString();
        }
    }
}
