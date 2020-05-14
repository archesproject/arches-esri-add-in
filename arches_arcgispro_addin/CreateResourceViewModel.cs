using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Mapping.Events;
using System.Web.Script.Serialization;
using System.Net.Http;

namespace arches_arcgispro_addin
{
    internal class CreateResourceViewModel : DockPane
    {
        private const string _dockPaneID = "arches_arcgispro_addin_CreateResource";

        public static ObservableCollection<GeometryNode> _geometryNodes = new ObservableCollection<GeometryNode>();

        protected CreateResourceViewModel() {
        }

        public static void CreateNodeList()
        {
            _geometryNodes.Clear();
            foreach (var geometryNode in StaticVariables.geometryNodes)
            {
                _geometryNodes.Add(geometryNode);
            }
        }

        public ObservableCollection<GeometryNode> GeometryNodes
        {
            set
            {
                SetProperty(ref _geometryNodes, value, () => GeometryNodes);
            }

            get { return _geometryNodes; }

        }

        private GeometryNode _selectedGeometryNode = new GeometryNode("");
        public GeometryNode SelectedGeometryNode
        {
            get { return _selectedGeometryNode; }
            set
            {
                SetProperty(ref _selectedGeometryNode, value, () => SelectedGeometryNode);
                StaticVariables.archesNodeid = _selectedGeometryNode.Id;
            }
        }

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "Create a Resource";
        public string Heading
        {
            get { return _heading; }
            set
            {
                SetProperty(ref _heading, value, () => Heading);
            }
        }
    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class CreateResource_ShowButton : Button
    {
        protected override void OnClick()
        {
            CreateResourceViewModel.Show();
        }
    }
}
