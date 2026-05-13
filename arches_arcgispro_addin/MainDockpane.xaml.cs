using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Http;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace arches_arcgispro_addin
{
    /// <summary>
    /// HttpClient Singleton across the addin. Handles proxy detection
    /// </summary>
    public static class ArchesHttpClient
    {
        private static HttpClient _client;

        public static async Task<HttpClient> GetHttpClient()
        {
            if (_client == null)
            {
                _client = new HttpClient();
            }

            try
            {
                //test for proxy
                HttpResponseMessage response = await _client.GetAsync(StaticVariables.archesInstanceURL);
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex)
            {
                //annoyingly 407 triggers an exception rather than letting me check the reponse code.
                if (ex.InnerException.Message.Contains("407"))
                {
                    try
                    {
                        var proxy = new HttpClientHandler
                        {
                            UseProxy = true,
                            Proxy = null, // use system proxy
                            DefaultProxyCredentials = CredentialCache.DefaultNetworkCredentials
                        };

                        //need to rebuild the static HttpClient using the handler
                        _client = new HttpClient(proxy);
                        HttpResponseMessage response = await _client.GetAsync(StaticVariables.archesInstanceURL);
                        response.EnsureSuccessStatusCode();

                    }
                    catch (Exception innerEx)
                    {
                        throw new System.ArgumentException("Unable to connect to Arches instance", innerEx);
                    }
                }
                else
                {
                    throw new System.ArgumentException("Unable to connect to Arches instance", ex);
                }
            }
            return _client;
        }
    }

    public class GeometryNode
    {
        public string Name { get; set; }
        public string Id { get; set; }

        public string Model { get; set; }
        public GeometryNode(string inId)
        {
            Id = inId;
        }
        public GeometryNode(string inName, string inId)
        {
            Name = inName;
            Id = inId;
        }
        public GeometryNode(string inModel, string inName, string inId)
        {
            Model = inModel;
            Name = String.Format("{0} - {1}", inModel, inName);
            Id = inId;
        }
    }
    public static class StaticVariables
    {
        public static Dictionary<string, dynamic> archesToken;
        public static string myClientid;
        public static string archesInstanceURL;
        public static string archesTileid;
        public static string archesNodeid;
        public static string selectedArchesNodeid;
        public static string archesResourceid = "No Resource is Selected";
        public static ArcGIS.Core.Geometry.Geometry archesGeometry;
        public static List<GeometryNode> geometryNodes = new List<GeometryNode>();
    };

    /// <summary>
    /// Interaction logic for MainDockpaneView.xaml
    /// </summary>
    public partial class MainDockpaneView : UserControl
    {
        public static async Task<Dictionary<string, dynamic>> RefreshToken(string clientid)
        {
            return await OAuthHelper.RefreshTokenAsync();
        }

        private void ShowSetupPanel()
        {
            SetupPanel.Visibility = Visibility.Visible;
            ConnectionPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowConnectionPanel()
        {
            SetupPanel.Visibility = Visibility.Collapsed;
            ConnectionPanel.Visibility = Visibility.Visible;
            InstanceURLDisplay.Text = StaticVariables.archesInstanceURL;
        }

        private async void SaveConfig_Button(object sender, RoutedEventArgs e)
        {
            string instanceUrl = SetupInstanceURL.Text?.Trim();
            string clientId = SetupClientId.Text?.Trim();

            if (string.IsNullOrEmpty(instanceUrl) || string.IsNullOrEmpty(clientId))
            {
                SetupErrorMessage.Text = "Both Instance URL and Client ID are required.";
                SetupErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            SaveConfigButton.IsEnabled = false;
            string originalButtonText = SaveConfigButton.Content as string;
            SaveConfigButton.Content = "Validating...";
            SetupErrorMessage.Visibility = Visibility.Collapsed;

            try
            {
                var (ok, error) = await OAuthHelper.ValidateConfigAsync(instanceUrl, clientId);
                if (!ok)
                {
                    SetupErrorMessage.Text = error;
                    SetupErrorMessage.Visibility = Visibility.Visible;
                    return;
                }

                OAuthHelper.SaveConfig(instanceUrl, clientId);
                ShowConnectionPanel();
            }
            catch (Exception ex)
            {
                SetupErrorMessage.Text = "Failed to save configuration: " + ex.Message;
                SetupErrorMessage.Visibility = Visibility.Visible;
            }
            finally
            {
                SaveConfigButton.Content = originalButtonText;
                SaveConfigButton.IsEnabled = true;
            }
        }

        private async void MainSignIn_Button(object sender, RoutedEventArgs e)
        {
            try
            {
                SigningInMessage.Visibility = Visibility.Visible;
                FailMessage.Visibility = Visibility.Hidden;
                SucceedMessage.Visibility = Visibility.Hidden;

                StaticVariables.archesToken = await OAuthHelper.AuthorizeAsync();
                FrameworkApplication.State.Activate("token_state");

                SigningInMessage.Visibility = Visibility.Hidden;
                FailMessage.Visibility = Visibility.Hidden;
                SucceedMessage.Text = $"Connected to {StaticVariables.archesInstanceURL}";
                SucceedMessage.Visibility = Visibility.Visible;

                StaticVariables.geometryNodes = await CreateResourceView.GetGeometryNode();
                CreateResourceViewModel.CreateNodeList();
            }
            catch (OperationCanceledException)
            {
                SigningInMessage.Visibility = Visibility.Hidden;
                FailMessage.Text = "Sign-in timed out or was cancelled.";
                FailMessage.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                SigningInMessage.Visibility = Visibility.Hidden;
                FailMessage.Text = "Sign-in failed: " + ex.Message;
                FailMessage.Visibility = Visibility.Visible;
            }
        }

        private void MainSignOut_Button(object sender, RoutedEventArgs e)
        {
            OAuthHelper.ClearStoredTokens();
            StaticVariables.archesToken = null;

            FrameworkApplication.State.Deactivate("token_state");
            FailMessage.Text = "Not connected";
            FailMessage.Visibility = Visibility.Visible;
            SucceedMessage.Visibility = Visibility.Hidden;
        }

        protected override async void OnInitialized(EventArgs e)
        {
            InitializeComponent();
            base.OnInitialized(e);

            bool configValid = OAuthHelper.LoadConfig();

            if (!configValid)
            {
                ShowSetupPanel();
                return;
            }

            ShowConnectionPanel();

            try
            {
                bool refreshed = await OAuthHelper.TrySilentRefreshAsync();
                if (refreshed)
                {
                    FrameworkApplication.State.Activate("token_state");
                    FailMessage.Visibility = Visibility.Hidden;
                    SucceedMessage.Text = $"Connected to {StaticVariables.archesInstanceURL}";
                    SucceedMessage.Visibility = Visibility.Visible;

                    StaticVariables.geometryNodes = await CreateResourceView.GetGeometryNode();
                    CreateResourceViewModel.CreateNodeList();
                }
            }
            catch
            {
                // Silent refresh failed, user will need to click Sign In
            }
        }

        private void MainOpenCreate_Button(object sender, RoutedEventArgs e)
        {
            if (StaticVariables.archesInstanceURL == "" | StaticVariables.archesInstanceURL == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Log in to Arches Server...");
                return;
            }

            DockPane pane = FrameworkApplication.DockPaneManager.Find("arches_arcgispro_addin_CreateResource");
            if (pane == null)
                return;
            pane.Activate();
        }

        private void MainOpenEdit_Button(object sender, RoutedEventArgs e)
        {
            if (StaticVariables.archesInstanceURL == "" | StaticVariables.archesInstanceURL == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Log in to Arches Server...");
                return;
            }

            DockPane pane = FrameworkApplication.DockPaneManager.Find("arches_arcgispro_addin_SaveResource");
            if (pane == null)
                return;
            pane.Activate();
        }
    }
}
