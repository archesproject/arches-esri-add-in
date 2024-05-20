using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using arches_arcgispro_addin.Behaviours;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace arches_arcgispro_addin
{
    /// <summary>
    /// Interaction logic for CreateResourceView.xaml
    /// </summary>
    public partial class CreateResourceView : UserControl
    {
        public CreateResourceView()
        {
            InitializeComponent();
        }
        public static async Task<List<GeometryNode>> GetGeometryNode()
        {
            List<GeometryNode> nodeidResponse = new List<GeometryNode>();
            try
            {
                HttpClient client = await ArchesHttpClient.GetHttpClient();
                if ((int)(DateTime.Now - StaticVariables.archesToken["timestamp"]).TotalSeconds > (int)(StaticVariables.archesToken["expires_in"] - 300)) 
                {
                    StaticVariables.archesToken = await MainDockpaneView.RefreshToken(StaticVariables.myClientid);
                }

                client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", StaticVariables.archesToken["access_token"]);
                HttpResponseMessage response = await client.GetAsync(System.IO.Path.Combine(StaticVariables.archesInstanceURL, "api/nodes/?datatype=geojson-feature-collection&perms=write_nodegroup"));

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(@responseBody);

                foreach (dynamic element in results)
                {
                    var resourceModelName = (string)element["resourcemodelname"];
                    if (resourceModelName != "Arches System Settings")
                    {
                        nodeidResponse.Add(new GeometryNode((string)element["resourcemodelname"], (string)element["name"], (string)element["nodeid"]));
                    }
                }
            }
            catch (HttpRequestException e)
            {
                System.ArgumentException argEx = new System.ArgumentException("The nodeid cannot be retrieved from the Arches server", e);
                throw argEx;
            }
            return nodeidResponse;
        }

        private async void GetNodeList_Button(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MapView.Active == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("No MapView currently active. Exiting...", "Info");
                    return;
                }
                if (StaticVariables.archesInstanceURL == "" | StaticVariables.archesInstanceURL == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Log in to Arches Server...");

                    DockPane pane = FrameworkApplication.DockPaneManager.Find("arches_arcgispro_addin_MainDockpane");
                    if (pane == null)
                        return;
                    pane.Activate();
                    return;
                }
                StaticVariables.geometryNodes = await GetGeometryNode();
                CreateResourceViewModel.CreateNodeList();
            }
            catch (Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Exception: " + ex.Message);
            }
        }

        private async void CreateUpload_Button(object sender, RoutedEventArgs e)
        {
            try
            {
                StaticVariables.selectedArchesNodeid = StaticVariables.archesNodeid;


                if (MapView.Active == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("No MapView currently active. Exiting...", "Info");
                    return;
                }
                if (StaticVariables.archesInstanceURL == "" | StaticVariables.archesInstanceURL == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Log in to Arches Server...");

                    DockPane pane = FrameworkApplication.DockPaneManager.Find("arches_arcgispro_addin_MainDockpane");
                    if (pane == null)
                        return;
                    pane.Activate();
                    return;
                }
                if (StaticVariables.archesNodeid == "" | StaticVariables.archesNodeid == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Select a Geometry Node to use...");
                    return;
                }
                if (StaticVariables.archesResourceid != "" && StaticVariables.archesResourceid != null)
                {
                    StaticVariables.archesResourceid = "";
                }

                List<string> archesGeometryCollection = await SaveResourceView.GetGeometryString();
                string archesGeometryString = String.Join(",", archesGeometryCollection);
                Dictionary<string, int> archesGeometryType = SaveResourceView.GetGeometryType(archesGeometryCollection);

                MessageBoxResult messageBoxResult = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    $"Are you sure you want to submit the selected geometry to create a new resource instance?\n\n" +
                    $"Total {archesGeometryCollection.Count} geometries will be submitted\n" +
                    $"{archesGeometryType["point"]} point(s)\n" +
                    $"{archesGeometryType["line"]} line(s)\n" +
                    $"{archesGeometryType["polygon"]} polygon(s)",
                    "Submit to Arches", MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (messageBoxResult.ToString() == "OK")
                {
                    string geometryFormat = "esrijson";
                    string submitOperation = "create";
                    var result = await SaveResourceView.SubmitToArches(null, StaticVariables.archesNodeid, archesGeometryString, geometryFormat, submitOperation);
                    StaticVariables.archesResourceid = result["resourceinstance_id"];
                    CreateResourceViewModel.GetResourceIdsCreated();
                    SaveResourceView.RefreshMapView();
                    OpenBrowserButton.IsEnabled = true;
                }
                else
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("The submission is cancelled");
                }
            }
            catch (Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Exception: " + ex.Message);
            }
        }

        private void CreateClear_Button(object sender, RoutedEventArgs e)
        {
            CreateResourceViewModel.ClearResourceIdsCreated();
            OpenBrowserButton.IsEnabled = false;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var ri = ((DataGrid)sender).SelectedItem as ResourceInstance;
            if (!string.IsNullOrEmpty(ri.Id))
            {
                DefaultBrowserBehaviour.OpenBrowser(ri.URL);
            }
        }

    }
}
