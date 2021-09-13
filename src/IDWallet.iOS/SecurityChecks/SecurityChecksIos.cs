﻿using DeviceCheck;
using IDWallet.Interfaces;
using IDWallet.iOS.SecurityChecks;
using Foundation;
using Security;

[assembly: Xamarin.Forms.Dependency(typeof(SecurityChecksIos))]
namespace IDWallet.iOS.SecurityChecks
{
    public class SecurityChecksIos : ISecurityChecks
    {
        public async void SafetyCheck(byte[] nonce)
        {
            DCAppAttestService attestService = DCAppAttestService.SharedService;

            if (attestService.Supported)
            {
                string key = await attestService.GenerateKeyAsync();

                NSData attest = await attestService.AttestKeyAsync(key, NSData.FromArray(Sha256.sha256(nonce)));

                App.SafetyKey = key;
                App.SafetyResult = attest.GetBase64EncodedString(new NSDataBase64EncodingOptions());
            }
        }
    }
}