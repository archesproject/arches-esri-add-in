using ArcGIS.Core.Geometry;
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


namespace arches_arcgispro_addin
{
    /// <summary>
    /// Interaction logic for Dockpane1View.xaml
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
                var firstSelectionSet = selectedFeatures.First();
                var archesInspector = new Inspector();
                archesInspector.Load(firstSelectionSet.Key, firstSelectionSet.Value);
                var archesGeometry = archesInspector.Shape;
                StaticVariables.archesResourceid = archesInspector["resourceinstanceid"].ToString();
                StaticVariables.archesTileid = archesInspector["tileid"].ToString();
                //StaticVariables.archesNodeid = archesInspector["nodeid"].ToString();
                StaticVariables.archesNodeid = "8d41e4d6-a250-11e9-accd-00224800b26d";

                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    "NodeID: " + StaticVariables.archesNodeid +
                    "\nTileID: " + StaticVariables.archesTileid +
                    "\nGeometry type: " + archesGeometry.GeometryType +
                    "\nGeometry JSON: " + archesGeometry.ToJson());
            });
        }

        private ArcGIS.Core.Geometry.Geometry SRTransform(ArcGIS.Core.Geometry.Geometry inGeometry, int inSRID, int outSRID)
        {
            ArcGIS.Core.Geometry.Geometry outGeometry;
            SpatialReference inSR = SpatialReferenceBuilder.CreateSpatialReference(inSRID);
            SpatialReference outSR = SpatialReferenceBuilder.CreateSpatialReference(outSRID);
            ProjectionTransformation transformer = ProjectionTransformation.Create(inSR, outSR);
            outGeometry = GeometryEngine.Instance.ProjectEx(inGeometry, transformer);
            return outGeometry;
        }

        private async Task<string> GetGeometryString()
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
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    "Geometry Collection: " + archesGeometryString);
                return archesGeometryString;

            });

            return args;
        }

        private async Task<Dictionary<string, string>> SubmitToArches(string tileid, string nodeid, string data, string geojson)
        {
            Dictionary<String, String> result = new Dictionary<String, String>();

            try
            {
                var serializer = new JavaScriptSerializer();
                var stringContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("tileid", tileid),
                        new KeyValuePair<string, string>("nodeid", nodeid),
                        new KeyValuePair<string, string>(data, geojson),
                    });
                var response = await client.PostAsync(System.IO.Path.Combine(StaticVariables.myInstanceURL, "api/tiles/"), stringContent);
                //var response = await client.PostAsync(System.IO.Path.Combine("http://localhost:8000/api/tiles/"), stringContent);

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic responseJSON = serializer.Deserialize<dynamic>(@responseBody);
                result.Add("results", responseJSON["results"]);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
            return result;
        }

        private async void RefreshMapView()
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

                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"" +
                    $"Resource ID: {StaticVariables.archesResourceid} " +
                    $"\nTile ID: {StaticVariables.archesTileid} " +
                    $"\nNode ID: {StaticVariables.archesNodeid} ");

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
            StaticVariables.archesResourceid = "";

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
                return;
            }
            if (StaticVariables.archesResourceid == "" | StaticVariables.archesResourceid == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Register the Resource to Edit...");
                return;
            }
            string editorAddress = StaticVariables.myInstanceURL + $"resource/{StaticVariables.archesResourceid}";
            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("opening... \n" + editorAddress);
            System.Diagnostics.Process.Start(editorAddress);
        }
    }
}
