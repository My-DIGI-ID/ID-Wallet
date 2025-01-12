﻿using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Events;
using IDWallet.Models;
using IDWallet.Models.AusweisSDK;
using IDWallet.Resources;
using IDWallet.Services;
using IDWallet.Views.BaseId.PopUps;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Proof.PopUps;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class BaseIdViewModel : CustomViewModel
    {
        private readonly SDKMessageService _sdkService = App.Container.Resolve<SDKMessageService>();
        private readonly ConnectService _connectService = App.Container.Resolve<ConnectService>();
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly ICredentialService _credentialService = App.Container.Resolve<ICredentialService>();
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly IMessageService _messageService = App.Container.Resolve<IMessageService>();
        private readonly ICustomWalletRecordService _walletRecordService =
                            App.Container.Resolve<ICustomWalletRecordService>();

        private int _progress;
        private int _carouselPosition;
        private bool _progressBarIsVisible;
        private bool _isStartEnabled;
        private bool _isInfoVisible;
        private int _idPinLength;
        private string _idPinHeaderLabel;
        private string _idPinBoldLabel;
        private string _idPinBodyLabel;
        private bool _idPinLinkIsVisible;
        private bool _forgotPINLinkIsVisible;
        private bool _moreInformationLinkIsVisible;
        private string _newPIN = null;
        private bool _isActivityIndicatorVisible;
        private readonly ReadyToScanPopUp _scanPopUp;
        private int _scanProcessCounter;
        private bool _pinPadIsVisible;
        private bool _idPinBoldIsVisible;
        private string _baseIdConnection;
        private bool _hasAcceptedAccess = false;

        private BaseIdProcessType _baseIdProcessType;
        public bool ViewModelWasResetted { get; set; }
        public INavigation Navigation { get; set; }
        private SdkMessageType _activeMessageType;

        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public int CarouselPosition
        {
            get => _carouselPosition;
            set => SetProperty(ref _carouselPosition, value);
        }

        public bool ProgressBarIsVisible
        {
            get => _progressBarIsVisible;
            set => SetProperty(ref _progressBarIsVisible, value);
        }

        public Func<IList<char>, bool> IdPinValidator { get; }

        public bool IsStartEnabled
        {
            get => _isStartEnabled;
            set => SetProperty(ref _isStartEnabled, value);
        }

        public bool IsInfoVisible
        {
            get => _isInfoVisible;
            set => SetProperty(ref _isInfoVisible, value);
        }

        public int IdPinLength
        {
            get => _idPinLength;
            set => SetProperty(ref _idPinLength, value);
        }

        public string IdPinHeaderLabel
        {
            get => _idPinHeaderLabel;
            set => SetProperty(ref _idPinHeaderLabel, value);
        }

        public string IdPinBoldLabel
        {
            get => _idPinBoldLabel;
            set => SetProperty(ref _idPinBoldLabel, value);
        }

        public bool IdPinBoldIsVisible
        {
            get => _idPinBoldIsVisible;
            set => SetProperty(ref _idPinBoldIsVisible, value);
        }

        public string IdPinBodyLabel
        {
            get => _idPinBodyLabel;
            set => SetProperty(ref _idPinBodyLabel, value);
        }

        public bool IdPinLinkIsVisible
        {
            get => _idPinLinkIsVisible;
            set => SetProperty(ref _idPinLinkIsVisible, value);
        }

        public bool MoreInformationLinkIsVisible
        {
            get => _moreInformationLinkIsVisible;
            set => SetProperty(ref _moreInformationLinkIsVisible, value);
        }

        public bool ForgotPINLinkIsVisible
        {
            get => _forgotPINLinkIsVisible;
            set => SetProperty(ref _forgotPINLinkIsVisible, value);
        }

        public bool IsActivityIndicatorVisible
        {
            get => _isActivityIndicatorVisible;
            set => SetProperty(ref _isActivityIndicatorVisible, value);
        }

        public bool PinPadIsVisible
        {
            get => _pinPadIsVisible;
            set => SetProperty(ref _pinPadIsVisible, value);
        }

        public int ScanProcessCounter
        {
            get => _scanProcessCounter;
            set => SetProperty(ref _scanProcessCounter, value);
        }

        private bool _alreadySubscribed;

        private Command _idPinErrorCommand;
        public Command IdPinErrorCommand =>
            _idPinErrorCommand ??= new Command(async () => { await IdPinErrorTask(); });

        private Command _idPinSuccessCommand;
        public Command IdPinSuccessCommand =>
            _idPinSuccessCommand ??= new Command(IdPinSuccessTask);

        private Command _changeDigitsTappedCommand;
        public Command ChangeDigitsTappedCommand =>
            _changeDigitsTappedCommand ??= new Command(ChangeDigitsTapped);

        private Command _sixDigitsTappedCommand;
        public Command SixDigitsTappedCommand =>
            _sixDigitsTappedCommand ??= new Command(UseRegularPIN);

        private Command _fiveDigitsTappedCommand;
        public Command FiveDigitsTappedCommand =>
            _fiveDigitsTappedCommand ??= new Command(UseTransportPIN);

        private Command _forgotPINTappedCommand;
        public Command ForgotPINTappedCommand =>
            _forgotPINTappedCommand ??= new Command(async () => { await ForgotPINTapped(); });

        private Command _moreInformationTappedCommand;
        public Command MoreInformationTappedCommand =>
            _moreInformationTappedCommand ??= new Command(MoreInformationTapped);

        public BaseIdViewModel()
        {
            ViewModelWasResetted = false;
            IsActivityIndicatorVisible = false;
            _activeMessageType = SdkMessageType.UNKNOWN_COMMAND;
            Progress = 0;
            ProgressBarIsVisible = true;
            IdPinLength = 6;
            _baseIdProcessType = BaseIdProcessType.None;
            _scanPopUp = new ReadyToScanPopUp(this);
            ScanProcessCounter = 0;

            IsStartEnabled = true;
            IdPinBoldIsVisible = true;
            PinPadIsVisible = false;
            IdPinLinkIsVisible = false;
            ForgotPINLinkIsVisible = false;
            MoreInformationLinkIsVisible = false;

            if (WalletParams.AusweisHost.Equals("demo.gessine.bundesdruckerei.de/ssi"))
            {
                IsInfoVisible = true;
            }
            else
            {
                IsInfoVisible = false;
            }

            IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Default_Header_Label;
            IdPinBoldLabel = Lang.BaseIDPage_PINScreen_Selection_Bold_Text;
            IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Selection_Body_Text;

            IdPinValidator = arg => { return ValidateIdPin(arg); };

            _alreadySubscribed = false;
        }

        public void Subscribe()
        {
            if (!_alreadySubscribed)
            {
                _alreadySubscribed = true;
                _sdkService.StartBaseIdFlow();
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.AccessRights, Access_Rights);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterPIN, Enter_PIN);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterNewPIN, Enter_New_PIN);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterCAN, Enter_CAN);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterPUK, Enter_PUK);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.Auth, Auth);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.ChangePIN, Change_PIN);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.InsertCard, Insert_Card);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.Reader, Reader);
                MessagingCenter.Subscribe<ServiceMessageEventService, string>(this, WalletEvents.BaseIdCredentialOffer, BaseIdCredentialOffer);
                MessagingCenter.Subscribe<ServiceMessageEventService, string>(this, WalletEvents.BaseIdCredentialIssue, BaseIdCredentialIssue);
            }
        }

        public void Unsubscribe()
        {
            _alreadySubscribed = false;

            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.AccessRights);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterPIN);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterNewPIN);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterCAN);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterPUK);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.Auth);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.ChangePIN);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.InsertCard);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.Reader);
            MessagingCenter.Unsubscribe<ServiceMessageEventService, string>(this, WalletEvents.BaseIdCredentialOffer);
            MessagingCenter.Unsubscribe<ServiceMessageEventService, string>(this, WalletEvents.BaseIdCredentialIssue);
        }

        private void Auth(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (sdkMessage.Result != null && sdkMessage.Result.Description != "The process has been cancelled.")
                {
                    string redirectUrl = sdkMessage.Url;

                    System.Net.Http.HttpResponseMessage result = await _sdkService.SdkHttpClient.GetAsync(redirectUrl);

                    if (result.IsSuccessStatusCode)
                    {
                        try
                        {
                            string resultString = await result.Content.ReadAsStringAsync();
                            SdkInvitation ausweisSdkInvitation = JObject.Parse(resultString).ToObject<SdkInvitation>();

                            Agent.Models.CustomConnectionInvitationMessage connectionInvitationMessage = _connectService.ReadInvitationUrl(ausweisSdkInvitation.InvitationUrl);

                            ConnectionRecord baseIdConnection = await _connectService.AcceptInvitationAsync(connectionInvitationMessage);
                            App.BaseIdConnectionId = _baseIdConnection = baseIdConnection.Id;

                            if (!string.IsNullOrEmpty(ausweisSdkInvitation.RevocationPassphrase))
                            {
                                baseIdConnection.SetTag(WalletParams.KeyRevocationPassphrase, ausweisSdkInvitation.RevocationPassphrase);
                                IAgentContext agentContext = await _agentProvider.GetContextAsync();
                                await _walletRecordService.UpdateAsync(agentContext.Wallet, baseIdConnection);
                            }

                            MessagingCenter.Send(this, WalletEvents.ReloadConnections);
                        }
                        catch (Exception)
                        {
                            _sdkService.SendCancel();
                            _sdkService.StartBaseIdFlow();

                            if (sdkMessage.Result != null && !string.IsNullOrEmpty(sdkMessage.Result.Message) && sdkMessage.Result.Message.Equals("The authenticity of your ID card could not be verified. Please make sure that you are using a genuine ID card. Please note that test applications require the use of a test ID card."))
                            {
                                BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                                        Lang.PopUp_BaseID_Auth_Error_Title,
                                        Lang.PopUp_BaseID_Auth_Error_Card_Text,
                                        Lang.PopUp_BaseID_Auth_Error_Button
                                        );
                                await popUp.ShowPopUp();
                                try
                                {
                                    await Navigation.PopAsync();
                                }
                                catch (Exception)
                                { }
                            }
                            else
                            {
                                BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                                        Lang.PopUp_BaseID_Auth_Error_Title,
                                        Lang.PopUp_BaseID_Auth_Message_Error_Text,
                                        Lang.PopUp_BaseID_Auth_Error_Button
                                        );
                                await popUp.ShowPopUp();
                                try
                                {
                                    await Navigation.PopAsync();
                                }
                                catch (Exception)
                                { }
                            }
                        }
                    }
                    else
                    {
                        _sdkService.SendCancel();
                        _sdkService.StartBaseIdFlow();
                        if (sdkMessage.Result != null && !string.IsNullOrEmpty(sdkMessage.Result.Message) && sdkMessage.Result.Message.Equals("The authenticity of your ID card could not be verified. Please make sure that you are using a genuine ID card. Please note that test applications require the use of a test ID card."))
                        {
                            BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                                    Lang.PopUp_BaseID_Auth_Error_Title,
                                    Lang.PopUp_BaseID_Auth_Error_Card_Text,
                                    Lang.PopUp_BaseID_Auth_Error_Button
                                    );
                            await popUp.ShowPopUp();
                            try
                            {
                                await Navigation.PopAsync();
                            }
                            catch (Exception)
                            { }
                        }
                        else
                        {
                            BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                                    Lang.PopUp_BaseID_Auth_Error_Title,
                                    Lang.PopUp_BaseID_Auth_Message_Error_Text,
                                    Lang.PopUp_BaseID_Auth_Error_Button
                                    );
                            await popUp.ShowPopUp();
                            try
                            {
                                await Navigation.PopAsync();
                            }
                            catch (Exception)
                            { }
                        }
                    }
                }
                else if (_baseIdProcessType == BaseIdProcessType.TransportPIN)
                {
                    _sdkService.SendRunChangePIN();
                }
            });
        }

        private void Access_Rights(SDKMessageService obj, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                _activeMessageType = SdkMessageType.ACCESS_RIGHTS;
                if (!_hasAcceptedAccess)
                {
                    AccessRightsPopUp accessRightsPopUp = new AccessRightsPopUp(sdkMessage.Chat.Effective);
                    PopUpResult accessRightsResult = await accessRightsPopUp.ShowPopUp();

                    if (accessRightsResult == PopUpResult.Accepted)
                    {
                        _hasAcceptedAccess = true;
                        _sdkService.SendAccept();
                        IsActivityIndicatorVisible = true;
                    }
                    else
                    {
                        _sdkService.SendCancel();
                        _sdkService.StartBaseIdFlow();
                        _baseIdProcessType = BaseIdProcessType.None;
                        await Navigation.PopAsync();
                    }
                }
                else
                {
                    _sdkService.SendAccept();
                }
            });
        }

        private void Insert_Card(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            if (ScanProcessCounter < 2)
            {
                ScanProcessCounter += 1;
            }

            Device.BeginInvokeOnMainThread(async () =>
            {
                if (_activeMessageType == SdkMessageType.ACCESS_RIGHTS)
                {
                    IsActivityIndicatorVisible = false;
                    CarouselPosition = 2;
                    Progress = 2;
                }
                else
                {
                    _scanPopUp.ShowPopUp();
                    _scanPopUp.IsOpen = true;
                }
            });
        }

        private void Reader(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (_scanPopUp.IsOpen)
                {
                    try
                    {
                        _scanPopUp.IsOpen = false;
                        _scanPopUp.CancelScan();
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            });
        }

        private void Enter_PIN(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                if (CarouselPosition != 3)
                {
                    CarouselPosition = 3;
                }

                switch (_baseIdProcessType)
                {
                    case BaseIdProcessType.Authentication:
                        await HandleAuthenticationEnterPin(sdkMessage);
                        _activeMessageType = SdkMessageType.ENTER_PIN;
                        UseRegularPIN();
                        break;
                    case BaseIdProcessType.ChangePIN:
                        _activeMessageType = SdkMessageType.ENTER_PIN;
                        await HandleChangePinEnterPin(sdkMessage);
                        break;
                    case BaseIdProcessType.TransportPIN:
                        if (_activeMessageType == SdkMessageType.ENTER_CAN)
                        {
                            _activeMessageType = SdkMessageType.ENTER_PIN;
                            UseTransportPinNoCancel();
                        }
                        else
                        {
                            _activeMessageType = SdkMessageType.ENTER_PIN;
                        }
                        await HandleTransportPinEnterPin(sdkMessage);
                        break;
                    default:
                        break;
                }
            });
        }

        private void Enter_CAN(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                if (CarouselPosition != 3)
                {
                    CarouselPosition = 3;
                }
                if (_activeMessageType == SdkMessageType.ENTER_CAN)
                {
                    BaseIdBasicPopUp canPopUp = new BaseIdBasicPopUp(
                        Lang.PopUp_BaseID_Wrong_CAN_Title,
                        Lang.PopUp_BaseID_Wrong_CAN_Text,
                        Lang.PopUp_BaseID_Wrong_CAN_Button);
                    await canPopUp.ShowPopUp();
                }
                else
                {
                    UseRegularPIN();
                    EnterCANPopUp canPopUp = new EnterCANPopUp(Lang.PopUp_BaseID_Enter_CAN_Text_1);

                    if (_baseIdProcessType == BaseIdProcessType.TransportPIN)
                    {
                        canPopUp = new EnterCANPopUp(Lang.PopUp_BaseID_Enter_CAN_Text_4);
                    }

                    await canPopUp.ShowPopUp();

                    IdPinLength = 6;
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_CAN_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_CAN_Body_Label;
                    IdPinLinkIsVisible = false;
                    ForgotPINLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                }
                _activeMessageType = SdkMessageType.ENTER_CAN;
            });
        }

        private void Enter_PUK(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                if (CarouselPosition != 3)
                {
                    CarouselPosition = 3;
                }
                if (!sdkMessage.Reader.Card.Inoperative)
                {
                    if (_activeMessageType == SdkMessageType.ENTER_PUK)
                    {
                        BaseIdBasicPopUp pukPopUp = new BaseIdBasicPopUp(
                            Lang.PopUp_BaseID_Wrong_PUK_Title,
                            Lang.PopUp_BaseID_Wrong_PUK_Text,
                            Lang.PopUp_BaseID_Wrong_PUK_Button
                            );
                        await pukPopUp.ShowPopUp();
                    }
                    else if (_activeMessageType == SdkMessageType.ENTER_PIN)
                    {
                        IdPinLength = 10;
                        IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_PUK_Header_Label;
                        IdPinBodyLabel = Lang.BaseIDPage_PINScreen_PUK_Body_Label;
                        IdPinLinkIsVisible = false;
                        ForgotPINLinkIsVisible = false;
                        MoreInformationLinkIsVisible = true;

                        EnterPUKPopUp pukPopUp = new EnterPUKPopUp();
                        await pukPopUp.ShowPopUp();
                    }

                    _activeMessageType = SdkMessageType.ENTER_PUK;
                }
                else
                {
                    BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                        Lang.PopUp_BaseID_Inoperative_PUK_Title,
                        Lang.PopUp_BaseID_Inoperative_PUK_Text,
                        Lang.PopUp_BaseID_Inoperative_PUK_Button);
                    await popUp.ShowPopUp();

                    await Navigation.PopAsync();
                }
            });
        }

        private void Enter_New_PIN(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                _activeMessageType = SdkMessageType.ENTER_NEW_PIN;

                IdPinLength = 6;
                IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_New_Header_Label;
                IdPinBodyLabel = Lang.BaseIDPage_PINScreen_New_Body_Label;
                IdPinLinkIsVisible = false;
                ForgotPINLinkIsVisible = false;
                MoreInformationLinkIsVisible = false;
            });
        }

        private void Change_PIN(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (sdkMessage.Success)
                {
                    _baseIdProcessType = BaseIdProcessType.None;
                    IsActivityIndicatorVisible = false;
                    ProgressBarIsVisible = false;
                    CarouselPosition = 6;
                }
            });
        }

        private void BaseIdCredentialIssue(ServiceMessageEventService arg1, string arg2)
        {
            IsActivityIndicatorVisible = false;
            GoToNext();
        }

        private void BaseIdCredentialOffer(ServiceMessageEventService arg1, string credentialRecordId)
        {
            IsActivityIndicatorVisible = false;
            GoToNext(credentialRecordId);
        }

        public void GoToNext(string credentialRecordId = "")
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IAgentContext agentContext = await _agentProvider.GetContextAsync();
                switch (CarouselPosition)
                {
                    case 0:
                        if (Progress == 0)
                        {
                            CarouselPosition = 1;
                            Progress = 1;
                            IsStartEnabled = false;
                        }
                        else
                        {
                            MessagingCenter.Send(this, WalletEvents.ReloadCredentials);
                            MessagingCenter.Send(this, WalletEvents.ReloadHistory);
                            _sdkService.StartBaseIdFlow();

                            try
                            {
                                ConnectionRecord baseIdconnectionRecord = await _connectionService.GetAsync(agentContext, _baseIdConnection);
                                string lockPin = baseIdconnectionRecord.GetTag(WalletParams.KeyRevocationPassphrase);
                                if (!string.IsNullOrEmpty(lockPin))
                                {
                                    LockPINPopUp lockPopUp = new LockPINPopUp(lockPin);
                                    await lockPopUp.ShowPopUp();
                                }
                            }
                            catch (Exception)
                            {
                                // ignore.
                            }


                            CarouselPosition = 5;
                            Progress = 4;
                        }
                        break;
                    case 1:
                        IsInfoVisible = false;
                        if (await ShowPinPrompt())
                        {
                            IsActivityIndicatorVisible = true;

                            await _sdkService.SendRunAuth();
                            _baseIdProcessType = BaseIdProcessType.Authentication;
                        }
                        break;
                    case 2:
                        CarouselPosition = 3;
                        break;
                    case 3:
                        CarouselPosition = 4;
                        IsActivityIndicatorVisible = true;
                        break;
                    case 4:
                        CarouselPosition = 0;
                        Progress = 3;

                        CredentialRecord credentialRecord = await _credentialService.GetAsync(agentContext, credentialRecordId);
                        ConnectionRecord connectionRecord = await _connectionService.GetAsync(agentContext, App.BaseIdConnectionId);
                        BaseIdOfferPopUp offerPopUp = new BaseIdOfferPopUp(new BaseIdOfferMessage(connectionRecord, credentialRecord));
                        PopUpResult popUpResult = await offerPopUp.ShowPopUp();

                        if (popUpResult != PopUpResult.Accepted)
                        {
                            await Navigation.PopAsync();
                        }
                        else
                        {
                            try
                            {
                                (CredentialRequestMessage request, CredentialRecord record) = await _credentialService.CreateRequestAsync(agentContext, credentialRecordId);
                                await _messageService.SendAsync(agentContext, request, connectionRecord);
                                IsActivityIndicatorVisible = true;
                            }
                            catch (Exception)
                            {
                                credentialRecord =
                                await _walletRecordService.GetAsync<CredentialRecord>(agentContext.Wallet,
                                    credentialRecordId, true);
                                credentialRecord.SetTag("AutoError", "true");
                                await _walletRecordService.UpdateAsync(agentContext.Wallet, credentialRecord);

                                BasicPopUp alertPopUp = new BasicPopUp(
                                    Lang.PopUp_Credential_Error_Title,
                                    Lang.PopUp_Credential_Error_Message,
                                    Lang.PopUp_Credential_Error_Button);
                                await alertPopUp.ShowPopUp();
                            }
                        }
                        break;
                    default:
                        await Navigation.PopAsync();
                        break;
                }
            });
        }

        private async Task<bool> ShowPinPrompt()
        {
            ProofRequest proofRequest = new ProofRequest();
            ProofViewModel viewModel = new ProofViewModel(proofRequest, "");

            ProofAuthenticationPopUp authPopUp = new ProofAuthenticationPopUp(new AuthViewModel(viewModel))
            {
                AlwaysDisplay = true
            };
#pragma warning disable CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
            authPopUp.ShowPopUp(); // No await.
#pragma warning restore CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.

            while (!viewModel.AuthSuccess)
            {
                if (viewModel.AuthError)
                {
                    return false;
                }
                await Task.Delay(100);
            }
            authPopUp.OnAuthCanceled(authPopUp, null);

            if (!viewModel.AuthError && viewModel.AuthSuccess)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ValidateIdPin(IList<char> arg)
        {
            string enteredDigits = string.Concat(arg.TakeWhile(char.IsNumber));

            if (_activeMessageType == SdkMessageType.ENTER_NEW_PIN)
            {
                if (string.IsNullOrEmpty(_newPIN))
                {
                    _newPIN = enteredDigits;
                    return true;
                }
                else if (_newPIN == enteredDigits)
                {
                    IsActivityIndicatorVisible = true;
                    _sdkService.SendSetNewPIN(enteredDigits);
                    return true;
                }
                else
                {
                    _newPIN = null;
                    return false;
                }
            }
            else
            {
                if (_activeMessageType == SdkMessageType.ENTER_CAN)
                {
                    IsActivityIndicatorVisible = true;
                    _sdkService.SendSetCAN(enteredDigits);
                }
                else if (_activeMessageType == SdkMessageType.ENTER_PUK)
                {
                    IsActivityIndicatorVisible = true;
                    _sdkService.SendSetPUK(enteredDigits);
                }
                else
                {
                    IsActivityIndicatorVisible = true;
                    _sdkService.SendSetPIN(enteredDigits);

                    if (_baseIdProcessType == BaseIdProcessType.Authentication)
                    {
                        GoToNext();
                    }
                }

                return true;
            }
        }

        private async Task IdPinErrorTask()
        {
            if (_activeMessageType == SdkMessageType.ENTER_NEW_PIN)
            {
                IdPinLength = 6;
                IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_New_Header_Label;
                IdPinBodyLabel = Lang.BaseIDPage_PINScreen_New_Body_Label;
                IdPinLinkIsVisible = false;
                ForgotPINLinkIsVisible = false;
                MoreInformationLinkIsVisible = false;
            }
        }

        private void IdPinSuccessTask()
        {
            if (_activeMessageType == SdkMessageType.ENTER_NEW_PIN)
            {
                IdPinLength = 6;
                IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Confirm_Header_Label;
                IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Confirm_Body_Label;
                IdPinLinkIsVisible = false;
                ForgotPINLinkIsVisible = false;
                MoreInformationLinkIsVisible = false;
            }
        }

        private void ChangeDigitsTapped()
        {
            IsActivityIndicatorVisible = true;
            if (_baseIdProcessType == BaseIdProcessType.Authentication)
            {
                UseTransportPIN();
            }
            else if (_baseIdProcessType == BaseIdProcessType.TransportPIN)
            {
                GoToStart();
            }
            else if (_baseIdProcessType == BaseIdProcessType.None)
            {
                GoToStart();
            }
        }

        private void UseTransportPIN()
        {
            IsActivityIndicatorVisible = true;
            ProgressBarIsVisible = false;
            IdPinBoldIsVisible = false;
            PinPadIsVisible = true;

            switch (_activeMessageType)
            {
                case SdkMessageType.ENTER_PIN:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Transport_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Transport_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = false;
                    IdPinLength = 5;
                    break;
                case SdkMessageType.ENTER_CAN:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_CAN_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_CAN_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    IdPinLength = 6;
                    break;
                case SdkMessageType.ENTER_PUK:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_PUK_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_PUK_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    IdPinLength = 10;
                    break;
            }

            _baseIdProcessType = BaseIdProcessType.TransportPIN;
            _activeMessageType = SdkMessageType.UNKNOWN_COMMAND;

            _sdkService.SendCancel();
            _sdkService.StartBaseIdFlow();
        }

        private void UseTransportPinNoCancel()
        {
            ProgressBarIsVisible = false;
            IdPinBoldIsVisible = false;
            PinPadIsVisible = true;

            switch (_activeMessageType)
            {
                case SdkMessageType.ENTER_PIN:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Transport_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Transport_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = false;
                    IdPinLength = 5;
                    break;
                case SdkMessageType.ENTER_CAN:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_CAN_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_CAN_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    IdPinLength = 6;
                    break;
                case SdkMessageType.ENTER_PUK:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_PUK_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_PUK_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    IdPinLength = 10;
                    break;
            }

            _baseIdProcessType = BaseIdProcessType.TransportPIN;
            _activeMessageType = SdkMessageType.UNKNOWN_COMMAND;
        }

        private void UseRegularPIN()
        {
            IdPinBoldIsVisible = false;
            PinPadIsVisible = true;

            switch (_activeMessageType)
            {
                case SdkMessageType.ENTER_PIN:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Default_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Default_Body_Label;
                    ForgotPINLinkIsVisible = true;
                    IdPinLinkIsVisible = true;
                    MoreInformationLinkIsVisible = false;
                    IdPinLength = 6;
                    break;
                case SdkMessageType.ENTER_CAN:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_CAN_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_CAN_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    IdPinLength = 6;
                    break;
                case SdkMessageType.ENTER_PUK:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_PUK_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_PUK_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    IdPinLength = 10;
                    break;
            }
        }

        private async Task ForgotPINTapped()
        {
            BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                Lang.PopUp_BaseID_Forgot_My_PIN_Title,
                Lang.PopUp_BaseID_Forgot_My_PIN_Text,
                Lang.PopUp_BaseID_Forgot_My_PIN_Button
                );
            await popUp.ShowPopUp();
        }

        private void MoreInformationTapped()
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (_activeMessageType == SdkMessageType.ENTER_CAN)
                {
                    CANInfoPopUp canPopUp = new CANInfoPopUp();
                    await canPopUp.ShowPopUp();
                }
                else if (_activeMessageType == SdkMessageType.ENTER_PUK)
                {
                    BaseIdBasicPopUp pupPopUp = new BaseIdBasicPopUp(
                        Lang.PopUp_BaseID_PUK_Info_Title,
                        Lang.PopUp_BaseID_PUK_Info_Text,
                        Lang.PopUp_BaseID_PUK_Info_Button
                        );
                    await pupPopUp.ShowPopUp();
                }
            });
        }

        public void CancelCurrentProcess()
        {
            _sdkService.SendCancel();
            _sdkService.StartBaseIdFlow();
            _baseIdProcessType = BaseIdProcessType.None;
        }

        public void GoToStart()
        {
            if (WalletParams.AusweisHost.Equals("demo.gessine.bundesdruckerei.de/ssi"))
            {
                IsInfoVisible = true;
            }
            else
            {
                IsInfoVisible = false;
            }

            App.PopUpIsOpen = false;
            _activeMessageType = SdkMessageType.UNKNOWN_COMMAND;
            _baseIdProcessType = BaseIdProcessType.None;
            _sdkService.SendCancel();
            _sdkService.StartBaseIdFlow();
            _baseIdConnection = "";
            _hasAcceptedAccess = false;
            IsActivityIndicatorVisible = false;
            CarouselPosition = 0;
            Progress = 0;
            ProgressBarIsVisible = true;
            IdPinLength = 6;
            IsStartEnabled = true;
            IdPinBoldIsVisible = true;
            PinPadIsVisible = false;
            IdPinLinkIsVisible = false;
            ForgotPINLinkIsVisible = false;
            MoreInformationLinkIsVisible = false;
            ScanProcessCounter = 0;

            IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Default_Header_Label;
            IdPinBoldLabel = Lang.BaseIDPage_PINScreen_Selection_Bold_Text;
            IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Selection_Body_Text;

            ViewModelWasResetted = true;
        }

        private async Task HandleAuthenticationEnterPin(SdkMessage sdkMessage)
        {
            switch (sdkMessage.Reader.Card.RetryCounter)
            {
                case 3:
                    if (_activeMessageType == SdkMessageType.ENTER_PUK)
                    {
                        BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                            Lang.PopUp_BaseID_PUK_Success_Title,
                            Lang.PopUp_BaseID_PUK_Success_Text,
                            Lang.PopUp_BaseID_PUK_Success_Button);
                        await popUp.ShowPopUp();

                        _sdkService.SendCancel();
                        _sdkService.StartBaseIdFlow();
                        await Navigation.PopAsync();
                    }
                    break;
                case 2:
                    if (_activeMessageType == SdkMessageType.ENTER_PIN)
                    {
                        WrongPINPopUp wrongPinPopUp2 = new WrongPINPopUp(sdkMessage.Reader.Card.RetryCounter, Lang.PopUp_BaseID_Wrong_PIN_Pre_Text);
                        await wrongPinPopUp2.ShowPopUp();
                    }
                    break;
                case 1:
                    if (_activeMessageType == SdkMessageType.ENTER_CAN)
                    {
                        ProgressBarIsVisible = true;
                        IdPinLinkIsVisible = true;
                        ForgotPINLinkIsVisible = true;
                        MoreInformationLinkIsVisible = false;
                        IdPinLength = 6;
                        IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Default_Header_Label;
                        IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Default_Body_Label;
                    }
                    break;
                default:
                    break;
            }
        }

        private async Task HandleChangePinEnterPin(SdkMessage sdkMessage)
        {
            switch (sdkMessage.Reader.Card.RetryCounter)
            {
                case 3:
                    if (_activeMessageType == SdkMessageType.ENTER_PUK)
                    {
                        BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                            Lang.PopUp_BaseID_PUK_Success_Title,
                            Lang.PopUp_BaseID_PUK_Success_Text,
                            Lang.PopUp_BaseID_PUK_Success_Button);
                        await popUp.ShowPopUp();

                        _sdkService.SendCancel();
                        _sdkService.StartBaseIdFlow();
                        await Navigation.PopAsync();
                    }
                    break;
                case 2:
                    if (_activeMessageType == SdkMessageType.ENTER_PIN)
                    {
                        WrongPINPopUp wrongPinPopUp2 = new WrongPINPopUp(sdkMessage.Reader.Card.RetryCounter, Lang.PopUp_BaseID_Wrong_PIN_Pre_Text);
                        await wrongPinPopUp2.ShowPopUp();
                    }
                    break;
                case 1:
                    if (_activeMessageType == SdkMessageType.ENTER_CAN)
                    {
                        ProgressBarIsVisible = false;
                        IdPinLinkIsVisible = false;
                        ForgotPINLinkIsVisible = false;
                        MoreInformationLinkIsVisible = false;
                        IdPinLength = 6;
                        IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_ChangePIN_Header_Label;
                        IdPinBodyLabel = Lang.BaseIDPage_PINScreen_ChangePIN_Body_Label;
                    }
                    break;
                default:
                    break;
            }
        }

        private async Task HandleTransportPinEnterPin(SdkMessage sdkMessage)
        {
            switch (sdkMessage.Reader.Card.RetryCounter)
            {
                case 3:
                    if (_activeMessageType == SdkMessageType.ENTER_PUK)
                    {
                        BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                            Lang.PopUp_BaseID_PUK_Success_Title,
                            Lang.PopUp_BaseID_PUK_Success_Text,
                            Lang.PopUp_BaseID_PUK_Success_Button);
                        await popUp.ShowPopUp();

                        _sdkService.SendCancel();
                        _sdkService.StartBaseIdFlow();
                        await Navigation.PopAsync();
                    }
                    break;
                case 2:
                    if (_activeMessageType == SdkMessageType.ENTER_PIN)
                    {
                        WrongPINPopUp wrongPinPopUp2 = new WrongPINPopUp(sdkMessage.Reader.Card.RetryCounter, Lang.PopUp_BaseID_Wrong_PIN_Pre_Text_2);
                        await wrongPinPopUp2.ShowPopUp();
                    }
                    break;
                case 1:
                    if (_activeMessageType == SdkMessageType.ENTER_CAN)
                    {
                        ProgressBarIsVisible = false;
                        IdPinLinkIsVisible = false;
                        ForgotPINLinkIsVisible = false;
                        MoreInformationLinkIsVisible = false;
                        IdPinLength = 5;
                        IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Transport_Header_Label;
                        IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Transport_Body_Label;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
