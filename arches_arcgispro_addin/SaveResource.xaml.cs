using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
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


namespace arches_arcgispro_addin
{
    /// <summary>
    /// Interaction logic for Dockpane1View.xaml
    /// </summary>
    public partial class SaveResourceView : UserControl
    {
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
                var archesResourceid = archesInspector["resourceinstanceid"];
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

        private async void Button_Click(object sender, RoutedEventArgs e)
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
            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("You Clicked the Submit Button in the Save Resources Dockpane");
        }
    }
}
