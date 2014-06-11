// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Apache License (https://github.com/WindowsAzure/azure-mobile-services/blob/master/LICENSE.txt)
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Auth0.SDK
{
    /// <summary>
    /// Third-party provider authentication control for the Windows Phone platform.
    /// </summary>
    public partial class LoginPage : PhoneApplicationPage
    {
        private string responseData = string.Empty;
        private string responseErrorDetail = string.Empty;
        private PhoneAuthenticationStatus responseStatus = PhoneAuthenticationStatus.UserCancel;

        // We need to keep this state to make sure we do the right thing even during
        // normal phone navigation actions (such as going to start screen and back).
        private bool authenticationStarted = false;

        /// <summary>
        /// The AuthenticationBroker associated with the current Login action.
        /// </summary>
        internal AuthenticationBroker Broker { get; set; }

        /// <summary>
        /// Initiatlizes the page by hooking up some event handlers.
        /// </summary>
        public LoginPage()
        {
            InitializeComponent();

            BackKeyPress += LoginPage_BackKeyPress;
        }

        /// <summary>
        /// Initiates the authentication operation by pointing the browser control
        /// to the PhoneWebAuthenticationBroker.StartUri.  If the PhoneWebAuthenticationBroker
        /// isn't currently in the middle of an authentication operation, then we immediately
        /// navigate back.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Make sure that there is an authentication operation in progress.
            // If not, we'll navigate back to the previous page.
            if (!Broker.AuthenticationInProgress)
            {
                this.NavigationService.GoBack();
            }

            if (!authenticationStarted)
            {
                authenticationStarted = true;

                // Point the browser control to the authentication start page.
                LoginView.Broker = Broker;
                LoginView.NavigationService = NavigationService;
                LoginView.StartLogin();
            }
        }

        /// <summary>
        /// Updates the PhoneWebAuthenticationBroker on the state of the authentication
        /// operation.  If we navigated back by pressing the back key, then the operation
        /// will be canceled.  If the browser control successfully completed the operation,
        /// signaled by its navigating to the PhoneWebAuthenticationBroker.EndUri, then we
        /// pass the results on.
        /// </summary>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            // If there is an active authentication operation in progress and we have
            // finished, then we need to inform the authentication broker of the results.
            // We don't want to stop the operation prematurely, such as when navigating to
            // the start screen.
            if (Broker.AuthenticationInProgress)
            {
                authenticationStarted = false;
                Broker.OnAuthenticationFinished(responseData, responseStatus, responseErrorDetail);
            }
        }

        /// <summary>
        /// Handler for the page's back key events.  We use this to determine whether navigations
        /// away from this page are benign (such as going to the start screen) or actually meant
        /// to cancel the operation.
        /// </summary>
        void LoginPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            responseData = "";
            responseStatus = PhoneAuthenticationStatus.UserCancel;

            authenticationStarted = false;
        }

        public async Task ClearCookiesAsync()
        {
            await LoginView.ClearCookiesAsync();
        }
    }
}