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

        public SaveResourceView()
        {
            InitializeComponent();
        }

        private async void GetAttribute()
        {
            await QueuedTask.Run(() =>
            {
                var selectedFeatures = MapView.Active.Map.GetSelection();
                if (selectedFeatures.Count == 1)
                {
                    var firstSelectionSet = selectedFeatures.First();
                    if (firstSelectionSet.Value.Count == 1)
                    {
                        var archesInspector = new Inspector();
                        archesInspector.Load(firstSelectionSet.Key, firstSelectionSet.Value);
                        var archesGeometry = archesInspector.Shape;
                        try
                        {
                            StaticVariables.archesResourceid = archesInspector["resourceinstanceid"].ToString();
                            StaticVariables.archesTileid = archesInspector["tileid"].ToString();
                            StaticVariables.archesNodeid = archesInspector["nodeid"].ToString();

                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                                "NodeID: " + StaticVariables.archesNodeid +
                                "\nTileID: " + StaticVariables.archesTileid +
                                "\nGeometry type: " + archesGeometry.GeometryType +
                                "\nGeometry JSON: " + archesGeometry.ToJson());

                        }
                        catch
                        {
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Make Sure to Select from a valid Arches Layer");
                        }
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Make Sure to Select ONE valid geometry");
                    }

                }
                else 
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Make Sure to Select from ONE Arches Layer");
                }


            });
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
            ArcGIS.Core.Geometry.Geometry archesGeometry;
            string archesGeometryString;
            List<string> archesGeometryCollection = new List<string>();

            var args = await QueuedTask.Run(() =>
            {
                var selectedFeatures = ArcGIS.Desktop.Mapping.MapView.Active.Map.GetSelection();
                foreach (var selectedFeature in selectedFeatures)
                {
                    foreach (var selected in selectedFeature.Value)
                    {
                        var archesInspector = new ArcGIS.Desktop.Editing.Attributes.Inspector();
                        archesInspector.Load(selectedFeature.Key, selected);
                        archesGeometry = archesInspector.Shape;
                        if (archesGeometry.SpatialReference.Wkid == 4326)
                        {
                            archesGeometryCollection.Add(archesGeometry.ToJson());
                        }
                        else {
                            var reprojectedGeometry = SRTransform(archesGeometry, archesGeometry.SpatialReference.Wkid, 4326);
                            archesGeometryCollection.Add(reprojectedGeometry.ToJson());
                        }
                    }
                }

                //JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                //archesGeometryString = jsonSerializer.Serialize(archesGeometryCollection);
                archesGeometryString = String.Join(",", archesGeometryCollection);
                return archesGeometryString;

            });

            return args;
        }

        public static async Task<Dictionary<string, string>> SubmitToArches(string tileid, string nodeid, string data, string esrijson)
        {
            Dictionary<String, String> result = new Dictionary<String, String>();

            try
            {
                var serializer = new JavaScriptSerializer();
                var stringContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("tileid", tileid),
                        new KeyValuePair<string, string>("nodeid", nodeid),
                        new KeyValuePair<string, string>(data, esrijson),
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
                throw new System.ArgumentException("The nodeid cannot be retrieved from the Arches server\n" + ex.Message, ex);
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Check for an active mapview
            try
            {
                if (MapView.Active == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("No MapView currently active. Exiting...", "Info");
                    return;
                }
                GetAttribute();

            }
            catch (Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Exception: " + ex.Message);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            StaticVariables.archesNodeid = "";
            StaticVariables.archesTileid = "";
            StaticVariables.archesResourceid = "No Resource is Registered";

            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"" +
                $"Resource ID: {StaticVariables.archesResourceid} " +
                $"\nTile ID: {StaticVariables.archesTileid} " +
                $"\nNode ID: {StaticVariables.archesNodeid} ");
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
                string archesData = "data";
                var result = await SubmitToArches(StaticVariables.archesTileid.ToString(), StaticVariables.archesNodeid, archesData, archesGeometryString);
                var message = result["results"];
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message +
                    $"\n{archesGeometryString} is submitted");
                RefreshMapView();
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
    }
}
