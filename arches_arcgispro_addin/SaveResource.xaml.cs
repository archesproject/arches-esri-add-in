﻿using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Models.Utilities;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using ArcGIS.Desktop.Framework.Contracts;
using System.Net.Http.Headers;
using ArcGIS.Desktop.Framework;

namespace arches_arcgispro_addin
{
    /// <summary>
    /// Interaction logic for SaveResourceView.xaml
    /// </summary>
    public partial class SaveResourceView : UserControl
    {
        static readonly HttpClient client = new HttpClient();
        public static Boolean GeometryBeReplaced;

        public SaveResourceView()
        {
            InitializeComponent();
        }

        private static ArcGIS.Core.Geometry.Geometry SRTransform(ArcGIS.Core.Geometry.Geometry inGeometry, int inSRID, int outSRID)
        {
            ArcGIS.Core.Geometry.Geometry outGeometry;
            SpatialReference inSR = SpatialReferenceBuilder.CreateSpatialReference(inSRID);
            SpatialReference outSR = SpatialReferenceBuilder.CreateSpatialReference(outSRID);
            ProjectionTransformation transformer = ProjectionTransformation.Create(inSR, outSR);
            outGeometry = GeometryEngine.Instance.ProjectEx(inGeometry, transformer);
            return outGeometry;
        }

        public static async Task<string> GetGeometryString()
        {
            ArcGIS.Core.Geometry.Geometry selectedGeometry;
            string selectedGeometryString;
            List<string> selectedGeometryCollection = new List<string>();

            var args = await QueuedTask.Run(() =>
            {
                var selectedFeatures = ArcGIS.Desktop.Mapping.MapView.Active.Map.GetSelection();

                foreach (var selectedFeature in selectedFeatures)
                {
                    foreach (var selected in selectedFeature.Value)
                    {
                        var selectedInspector = new ArcGIS.Desktop.Editing.Attributes.Inspector();
                        selectedInspector.Load(selectedFeature.Key, selected);
                        selectedGeometry = selectedInspector.Shape;
                        if (selectedGeometry.SpatialReference.Wkid == 4326)
                        {
                            selectedGeometryCollection.Add(selectedGeometry.ToJson());
                        }
                        else {
                            var reprojectedGeometry = SRTransform(selectedGeometry, selectedGeometry.SpatialReference.Wkid, 4326);
                            selectedGeometryCollection.Add(reprojectedGeometry.ToJson());
                        }
                    }
                }
                selectedGeometryString = String.Join(",", selectedGeometryCollection);
                return selectedGeometryString;

            });

            return args;
        }

        public static async Task<Dictionary<string, string>> SubmitToArches(string tileid, string nodeid, string esrijson, string geometryFormat, string submitOperation)
        {
            Dictionary<String, String> result = new Dictionary<String, String>();
            try
            {
                var serializer = new JavaScriptSerializer();
                var stringContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("tileid", tileid),
                        new KeyValuePair<string, string>("nodeid", nodeid),
                        new KeyValuePair<string, string>("data", esrijson),
                        new KeyValuePair<string, string>("format", geometryFormat),
                        new KeyValuePair<string, string>("operation", submitOperation),
                    });

                client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", StaticVariables.myToken["access_token"]);
                var response = await client.PostAsync(System.IO.Path.Combine(StaticVariables.myInstanceURL, "api/tiles/"), stringContent);

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic responseJSON = serializer.Deserialize<dynamic>(@responseBody);

                if (responseJSON.ContainsKey("nodegroup_id")) { result.Add("nodegroup_id", responseJSON["nodegroup_id"]); }
                if (responseJSON.ContainsKey("resourceinstance_id")) { result.Add("resourceinstance_id", responseJSON["resourceinstance_id"]); }
                if (responseJSON.ContainsKey("tileid")) { result.Add("tileid", responseJSON["tileid"]); }
            }
            catch (HttpRequestException ex)
            {
                throw new System.ArgumentException(ex.Message, ex);
            }
            return result;
        }

        public static async void RefreshMapView()
        {
            await QueuedTask.Run(() =>
            {
                MapView.Active.Redraw(true);
            });
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
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
                if (StaticVariables.archesResourceid == "" | StaticVariables.archesResourceid == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Register the Resource to Edit...");
                    return;
                }

                string archesGeometryString = await GetGeometryString();
                string geometryFormat = "esrijson";
                string submitOperation = (GeometryBeReplaced) ? "replace" : "append";

                MessageBoxResult messageBoxResult = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                        $"Are you sure you want to {submitOperation.ToUpper()}?\n{archesGeometryString}",
                        "Submit to Arches",
                        MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (messageBoxResult.ToString() == "OK") 
                {
                    var result = await SubmitToArches(StaticVariables.archesTileid.ToString(), StaticVariables.archesNodeid, archesGeometryString, geometryFormat, submitOperation);
                    //var message = result["results"];
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"\n{archesGeometryString} is submitted");
                    RefreshMapView();
                }
                else
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Submission is Cancelled");
                }
            }
            catch (Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Exception: " + ex.Message);
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (StaticVariables.myInstanceURL == "" | StaticVariables.myInstanceURL == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Log in to Arches Server...");

                DockPane pane = FrameworkApplication.DockPaneManager.Find("arches_arcgispro_addin_MainDockpane");
                if (pane == null)
                    return;
                pane.Activate();
                return;
            }
            if (StaticVariables.archesResourceid == "" | StaticVariables.archesResourceid == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Register the Resource to Edit...");
                return;
            }
            string editorAddress = StaticVariables.myInstanceURL + $"resource/{StaticVariables.archesResourceid}";
            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("opening... \n" + editorAddress);
            UI.ChromePaneViewModel.OpenChromePane(editorAddress);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GeometryBeReplaced = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GeometryBeReplaced = false;
        }
    }
}
