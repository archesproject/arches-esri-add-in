using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework.Threading.Tasks;
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
    /// Interaction logic for CreateResourceView.xaml
    /// </summary>
    public partial class CreateResourceView : UserControl
    {
        public CreateResourceView()
        {
            InitializeComponent();
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
                string archesData = "data";
                var result = await SaveResourceView.SubmitToArches("", StaticVariables.archesNodeid, archesData, archesGeometryString);
                StaticVariables.archesResourceid = result["results"];
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"A resource id: \n{StaticVariables.archesResourceid} is assigned");
                SaveResourceView.RefreshMapView();
            }
            catch (Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Exception: " + ex.Message);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (StaticVariables.myInstanceURL == "" | StaticVariables.myInstanceURL == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Log in to Arches Server...");
                return;
            }
            if (StaticVariables.archesResourceid == "" | StaticVariables.archesResourceid == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Submit a Geometry to Arches to Create a Resource...");
                return;
            }
            string editorAddress = StaticVariables.myInstanceURL + $"resource/{StaticVariables.archesResourceid}";
            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("opening... \n" + editorAddress);
            System.Diagnostics.Process.Start(editorAddress);

        }
    }
}
