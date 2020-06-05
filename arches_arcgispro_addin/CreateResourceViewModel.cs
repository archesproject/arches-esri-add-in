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
using System.Windows.Input;

namespace arches_arcgispro_addin
{
    public class ResourceInstance
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Model { get; set; }
        public ResourceInstance(string inId)
        {
            Name = "";
            Id = inId;
            Model = "";
        }
        public ResourceInstance(string inName, string inId, string inModel)
        {
            Name = inName;
            Id = inId;
            Model = inModel;
        }
    }

    internal class CreateResourceViewModel : DockPane
    {
        private const string _dockPaneID = "arches_arcgispro_addin_CreateResource";

        public static ObservableCollection<GeometryNode> _geometryNodes = new ObservableCollection<GeometryNode>();
        public static ObservableCollection<ResourceInstance> _resourceIdsCreated = new ObservableCollection<ResourceInstance>();
        private static readonly object _lockCollections = new object();
        private static int counter_created = 1;

        private bool _nodeSelected = false;
        public bool NodeSelected
        {
            get { return _nodeSelected; }
            set
            {
                SetProperty(ref _nodeSelected, value, () => NodeSelected);
            }
        }

        protected CreateResourceViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(_resourceIdsCreated, _lockCollections);
        }

        public ObservableCollection<ResourceInstance> ResourceIdsCreated
        {
            set
            {
                SetProperty(ref _resourceIdsCreated, value, () => ResourceIdsCreated);
            }
            get { return _resourceIdsCreated; }
        }

        private ResourceInstance _selectedResourceId = new ResourceInstance("");

        public ResourceInstance SelectedResourceId
        {
            get { return _selectedResourceId; }
            set
            {
                SetProperty(ref _selectedResourceId, value, () => SelectedResourceId);
            }
        }

        public static void GetResourceIdsCreated()
        {
            lock(_lockCollections);
            ResourceInstance newResourceId = new ResourceInstance("ArcGIS-" + counter_created, StaticVariables.archesResourceid, "");
            counter_created += 1;
            _resourceIdsCreated.Add(newResourceId);
        }

        public static void ClearResourceIdsCreated()
        {
            lock(_lockCollections);
            _resourceIdsCreated.Clear();
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
                if (_selectedGeometryNode == null)
                {
                    NodeSelected = false;
                }
                else
                {
                    StaticVariables.archesNodeid = _selectedGeometryNode.Id;
                    NodeSelected = true;
                }
            }
        }

        private ICommand _buttonClick2;
        public ICommand ButtonClick2
        {
            get
            {
                return _buttonClick2 ?? (_buttonClick2 = new RelayCommand(() =>
                {
                    if (StaticVariables.archesInstanceURL == "" | StaticVariables.archesInstanceURL == null)
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
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Submit a Geometry to Arches to Create a Resource...");
                        return;
                    }
                    if (SelectedResourceId == null)
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Choose a Resource ID from the Dropdown");
                        return;
                    }
                    if (SelectedResourceId.Id == "")
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please, Choose a Resource ID from the Dropdown");
                        return;
                    }
                    string editorAddress = StaticVariables.archesInstanceURL + $"resource/{SelectedResourceId.Id}";
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("opening... \n" + editorAddress);
                    UI.ChromePaneViewModel.OpenChromePane(editorAddress);

                }, true));
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
        private string _heading = "Create Resources";
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
