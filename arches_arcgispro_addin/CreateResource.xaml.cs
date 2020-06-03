﻿using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


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
        static readonly HttpClient client = new HttpClient();
        private async Task<List<GeometryNode>> GetGeometryNode()
        {
            List<GeometryNode> nodeidResponse = new List<GeometryNode>();
            try
            {
                if ((DateTime.Now - StaticVariables.myToken["timestamp"]).TotalSeconds > (StaticVariables.myToken["expires_in"] - 300)) 
                {
                    StaticVariables.myToken = await MainDockpaneView.RefreshToken(StaticVariables.myClientid);
                }

                client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", StaticVariables.myToken["access_token"]);
                HttpResponseMessage response = await client.GetAsync(System.IO.Path.Combine(StaticVariables.myInstanceURL, "api/nodes/?datatype=geojson-feature-collection&perms=write_nodegroup"));

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                var serializer = new JavaScriptSerializer();
                dynamic results = serializer.Deserialize<dynamic>(@responseBody);

                foreach (dynamic element in results)
                {
                    nodeidResponse.Add(new GeometryNode(element["resourcemodelname"], element["name"], element["nodeid"]));
                }
            }
            catch (HttpRequestException e)
            {
                System.ArgumentException argEx = new System.ArgumentException("The nodeid cannot be retrieved from the Arches server", e);
                throw argEx;
            }
            return nodeidResponse;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MapView.Active == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("No MapView currently active. Exiting...", "Info");
                    return;
                }
                if (StaticVariables.myInstanceURL == "" | StaticVariables.myInstanceURL == null)
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

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MapView.Active == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("No MapView currently active. Exiting...", "Info");
                    return;
                }
                if (StaticVariables.myInstanceURL == "" | StaticVariables.myInstanceURL == null)
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
                
                string archesGeometryString = await SaveResourceView.GetGeometryString();

                MessageBoxResult messageBoxResult = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    $"Are you sure you want to submit the selected geometry to create a new resource instance?" +
                    $"\n\n{archesGeometryString}",
                    "Submit to Arches", MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (messageBoxResult.ToString() == "OK")
                {
                    string geometryFormat = "esrijson";
                    string submitOperation = "create";
                    var result = await SaveResourceView.SubmitToArches(null, StaticVariables.archesNodeid, archesGeometryString, geometryFormat, submitOperation);
                    StaticVariables.archesResourceid = result["resourceinstance_id"];
                    CreateResourceViewModel.GetResourceIdsCreated();
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"A resource id: \n{StaticVariables.archesResourceid} is assigned");
                    SaveResourceView.RefreshMapView();
                    OpenChromiumButton.IsEnabled = true;
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

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            CreateResourceViewModel.ClearResourceIdsCreated();
            OpenChromiumButton.IsEnabled = false;
        }
    }
}
