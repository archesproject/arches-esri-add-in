using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Http;
using System.Web.Script.Serialization;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework;
using System.Collections.ObjectModel;
using ArcGIS.Core.Data.UtilityNetwork.Trace;

namespace arches_arcgispro_addin
{
    /// <summary>
    /// Interaction logic for MainDockpaneView.xaml
    /// </summary>

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
        //public static string myUsername;
        //public static string myPassword;
        public static string archesTileid;
        public static string archesNodeid;
        public static string archesResourceid = "No Resource is Selected";
        public static ArcGIS.Core.Geometry.Geometry archesGeometry;
        public static List<GeometryNode> geometryNodes = new List<GeometryNode>();
    };
    public partial class MainDockpaneView : UserControl
    {
        // HttpClient is intended to be instantiated once per application, rather than per-use. See Remarks.
        static readonly HttpClient client = new HttpClient();

        private async Task GetInstances()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(System.IO.Path.Combine(StaticVariables.archesInstanceURL, "search/resources"));

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                var serializer = new JavaScriptSerializer();
                dynamic responseJSON = serializer.Deserialize<dynamic>(@responseBody);
                JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                dynamic results = responseJSON["results"]["hits"]["hits"];
                int count = 0;
                string names = "";
                foreach (dynamic element in results)
                {
                    count++;
                    string displayname = element["_source"]["displayname"];
                    names += $"{count}. {displayname} \n";
                }
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"{count} Instances:\n{names}");
            }
            catch (Exception ex)
            {
                throw new System.ArgumentException("Address is wrong", ex);
            }
        }

        private async Task<string> GetClientId()
        {
            string clientid = "";

            try
            {
                var serializer = new JavaScriptSerializer();
                var stringContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("username", Username.Text),
                        new KeyValuePair<string, string>("password", Password.Password),
                    });
                var response = await client.PostAsync(System.IO.Path.Combine(StaticVariables.archesInstanceURL, "auth/get_client_id"), stringContent);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic responseJSON = serializer.Deserialize<dynamic>(@responseBody);
                dynamic results = responseJSON;
                clientid = results["clientid"];
            }
            catch (Exception ex)
            {
                throw new System.ArgumentException("Failed to get a client ID", ex);
            }
            return clientid;
        }

        private async Task<Dictionary<string, dynamic>> GetToken(string clientid)
        {

            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();

            try
            {
                var serializer = new JavaScriptSerializer();
                var stringContent = new FormUrlEncodedContent(new[]
                    {
                            new KeyValuePair<string, string>("username", Username.Text),
                            new KeyValuePair<string, string>("password", Password.Password),
                            new KeyValuePair<string, string>("client_id", clientid),
                            new KeyValuePair<string, string>("grant_type", "password"),
                        });
                var response = await client.PostAsync(System.IO.Path.Combine(StaticVariables.archesInstanceURL, "o/token/"), stringContent);

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic responseJSON = serializer.Deserialize<dynamic>(@responseBody);
                dynamic results = responseJSON;
                result.Add("access_token", results["access_token"]);
                result.Add("refresh_token", results["refresh_token"]);
                result.Add("expires_in", results["expires_in"]);
                result.Add("token_type", results["token_type"]);
                result.Add("scope", results["scope"]);
                result.Add("timestamp", DateTime.Now);
            }
            catch (Exception ex)
            {
                throw new System.ArgumentException($"Failed to get the Token: {ex.Message}", ex);
            }
            return result;
        }

        public static async Task<Dictionary<string, dynamic>> RefreshToken(string clientid)
        {

            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();

            try
            {
                var serializer = new JavaScriptSerializer();
                var stringContent = new FormUrlEncodedContent(new[]
                    {
                            new KeyValuePair<string, string>("refresh_token", StaticVariables.archesToken["refresh_token"]),
                            new KeyValuePair<string, string>("client_id", clientid),
                            new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        });
                var response = await client.PostAsync(System.IO.Path.Combine(StaticVariables.archesInstanceURL, "o/token/"), stringContent);

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic responseJSON = serializer.Deserialize<dynamic>(@responseBody);
                dynamic results = responseJSON;
                result.Add("access_token", results["access_token"]);
                result.Add("refresh_token", results["refresh_token"]);
                result.Add("expires_in", results["expires_in"]);
                result.Add("token_type", results["token_type"]);
                result.Add("scope", results["scope"]);
                result.Add("timestamp", DateTime.Now);
            }
            catch (Exception ex)
            {
                throw new System.ArgumentException($"Failed to refresh the Token: {ex.Message}", ex);
            }
            return result;
        }

        private async Task<Dictionary<string, string>> GetResource(string resourceid, string token)
        {
            StaticVariables.archesInstanceURL = InstanceURL.Text;

            Dictionary<String, String> result = new Dictionary<String, String>();

            try
            {
                var serializer = new JavaScriptSerializer();
                string header = "Bearer " + token;
                try
                {
                    client.DefaultRequestHeaders.Add("Authorization", header);
                }
                catch (System.FormatException e)
                {
                    Console.WriteLine("Message :{0} ", e.Message);
                }
                var response = await client.GetAsync(StaticVariables.archesInstanceURL + $"resources/{resourceid}?format=json");
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(StaticVariables.archesInstanceURL + $"resources/{resourceid}?format=json");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic responseJSON = serializer.Deserialize<dynamic>(@responseBody);
                result.Add("resourceid", responseJSON["resourceinstanceid"]);
                result.Add("graphid", responseJSON["graph_id"]);
                result.Add("displayname", responseJSON["displayname"]);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Message :{0} ", e.Message);
            }
            return result;
        }


        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try {
                StaticVariables.archesInstanceURL = InstanceURL.Text;
                StaticVariables.myClientid = await GetClientId();
                StaticVariables.archesToken = await GetToken(StaticVariables.myClientid);
                FrameworkApplication.State.Activate("token_state");
                //CreateResourceButton.IsEnabled = true;
                //EditResourceButton.IsEnabled = true;
                FailMessage.Visibility = Visibility.Hidden;
                SucceedMessage.Visibility = Visibility.Visible;
                //ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Successfully Logged in to {StaticVariables.archesInstanceURL}");

                StaticVariables.geometryNodes = await CreateResourceView.GetGeometryNode();
                CreateResourceViewModel.CreateNodeList();
            }
            catch (Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ex.Message + "\nCheck the Instance URL and/or the Credentials");
            }
        }

        public MainDockpaneView()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            InstanceURL.Text = "";
            Username.Text = "";
            Password.Password = "";

            FrameworkApplication.State.Deactivate("token_state");
            FailMessage.Visibility = Visibility.Visible;
            SucceedMessage.Visibility = Visibility.Hidden;
            //CreateResourceButton.IsEnabled = false;
            //EditResourceButton.IsEnabled = false;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
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

        private void Button_Click_3(object sender, RoutedEventArgs e)
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
