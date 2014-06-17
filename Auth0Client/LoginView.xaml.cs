using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Phone.Controls;

namespace Auth0.SDK
{
    public partial class LoginView
    {
        private const string NoDetailsAvailableMessage = "No details available.";
        private string responseData = string.Empty;
        private string responseErrorDetail = string.Empty;
        private PhoneAuthenticationStatus responseStatus = PhoneAuthenticationStatus.UserCancel;

        // We need to keep this state to make sure we do the right thing even during
        // normal phone navigation actions (such as going to start screen and back).
        private bool authenticationStarted = false;
        private bool authenticationFinished = false;

        public LoginView()
        {
            InitializeComponent();

            browserControl.Navigating += BrowserControl_Navigating;
            browserControl.Navigated += BrowserControl_OnNavigated;
            browserControl.LoadCompleted += BrowserControl_LoadCompleted;
            browserControl.NavigationFailed += BrowserControl_NavigationFailed;
        }

        internal AuthenticationBroker Broker { get; set; }
        internal NavigationService NavigationService { get; set; }

        /// <summary>
        /// Handler for the browser control's load completed event.  We use this to detect when
        /// to hide the progress bar and show the browser control.
        /// </summary>
        void BrowserControl_LoadCompleted(object sender, NavigationEventArgs e)
        {
            HideProgressBar();
        }

        /// <summary>
        /// Initiates the authentication operation by pointing the browser control
        /// to the PhoneWebAuthenticationBroker.StartUri.  If the PhoneWebAuthenticationBroker
        /// isn't currently in the middle of an authentication operation, then we immediately
        /// navigate back.
        /// </summary>
        public void StartLogin()
        {
            // Make sure that there is an authentication operation in progress.
            // If not, we'll navigate back to the previous page.
            if (!Broker.AuthenticationInProgress && NavigationService != null)
            {
                NavigationService.GoBack();
            }

            if (!authenticationStarted)
            {
                authenticationStarted = true;
                authenticationFinished = false;

                // Point the browser control to the authentication start page.
                browserControl.Source = Broker.StartUri;
            }
        }


        /// <summary>
        /// Handler for the browser control's navigating event.  We use this to detect when login
        /// has completed.
        /// </summary>
        private void BrowserControl_Navigating(object sender, NavigatingEventArgs e)
        {
            ShowProgressBar();
            if (EqualsWithoutQueryString(e.Uri, Broker.EndUri))
            {
                if (e.Uri.Query.StartsWith("?error"))
                {
                    responseStatus = PhoneAuthenticationStatus.ErrorServer;
                    responseErrorDetail = NoDetailsAvailableMessage;
                    var match = Regex.Match(e.Uri.Query, @"\?error=([^&]+)&error_description=([^&]+).*", RegexOptions.None);
                    if (match.Success)
                    {
                        responseErrorDetail = string.Format("Error: {0}. Description: {1}",
                            HttpUtility.UrlDecode(match.Groups[1].Value),
                            HttpUtility.UrlDecode(match.Groups[2].Value));
                    }
                }
                else
                {
                    responseData = e.Uri.ToString();
                    responseStatus = PhoneAuthenticationStatus.Success;
                }
              
                authenticationFinished = true;

                // Navigate back now.
                this.NavigateBackWithProgress();
            }
        }

        private void BrowserControl_OnNavigated(object sender, NavigationEventArgs navigationEventArgs)
        {
            HideProgressBar();
        }

        /// <summary>
        /// Compares to URIs without taking the Query into account.
        /// </summary>
        /// <param name="uri">One of the URIs to compare.</param>
        /// <param name="otherUri">The other URI to use in the comparison.</param>
        /// <returns>True if the URIs are equal (except for the query), false otherwise.</returns>
        private bool EqualsWithoutQueryString(Uri uri, Uri otherUri)
        {
            return uri.AbsolutePath == otherUri.AbsolutePath
                            && uri.Host == otherUri.Host
                            && uri.Scheme == otherUri.Scheme;
        }

        /// <summary>
        /// Handler for the browser control's navigation failed event.  We use this to detect errors
        /// </summary>
        private void BrowserControl_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            var navEx = e.Exception as WebBrowserNavigationException;

            if (navEx != null)
            {
                // Pass along the provided error information.
                responseErrorDetail = string.Format("Error code: {0}", navEx.StatusCode);
            }
            else
            {
                // No error information available.
                responseErrorDetail = NoDetailsAvailableMessage;
            }
            responseStatus = PhoneAuthenticationStatus.ErrorHttp;

            authenticationFinished = true;
            e.Handled = true;

            // Navigate back now.
            this.NavigateBackWithProgress();
        }

        /// <summary>
        /// Displays the progress bar and navigates to the previous page.
        /// </summary>
        private void NavigateBackWithProgress()
        {
            ShowProgressBar();
            
            // If there is an active authentication operation in progress and we have
            // finished, then we need to inform the authentication broker of the results.
            // We don't want to stop the operation prematurely, such as when navigating to
            // the start screen.
            if (Broker.AuthenticationInProgress && authenticationFinished)
            {
                authenticationStarted = false;
                authenticationFinished = false;

                Broker.OnAuthenticationFinished(responseData, responseStatus, responseErrorDetail);
            }

            if (NavigationService != null)
            {
                NavigationService.GoBack();
            }
        }

        private DispatcherTimer _timer;

        /// <summary>
        /// Shows the progress bar and hides the browser control.
        /// </summary>
        private void ShowProgressBar()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }

            this.Visibility = Visibility.Collapsed;
            browserControl.Visibility = Visibility.Collapsed;
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            if (_timer == null) return;

            _timer.Stop();
            _timer = null;

            if (Broker.AuthenticationInProgress)
            {
                this.Visibility = Visibility.Visible;
                browserControl.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Hides the progress bar and shows the browser control.
        /// </summary>
        private void HideProgressBar()
        {
            if (_timer != null) return;

            _timer = new DispatcherTimer();
            _timer.Tick += TimerOnTick;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 150);
            _timer.Start();
        }

        public async Task ClearCookiesAsync()
        {
            await browserControl.ClearCookiesAsync();
        }
    }
}
