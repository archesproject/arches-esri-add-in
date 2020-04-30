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
using ArcGIS.Desktop.Mapping.Events;

namespace arches_arcgispro_addin
{
    internal class SaveResourceViewModel : DockPane
    {
        private const string _dockPaneID = "arches_arcgispro_addin_SaveResource";

        private FeatureLayer _selectedFeatureLayer;

        /// <summary>
        /// used to lock collections for use by multiple threads
        /// </summary>
        private readonly object _lockCollections = new object();
        /// <summary>
        /// UI lists, read-only collections, and properties
        /// </summary>
        private readonly ObservableCollection<FeatureLayer> _featureLayers = new ObservableCollection<FeatureLayer>();
        private readonly ReadOnlyObservableCollection<FeatureLayer> _readOnlyFeatureLayers;

        protected SaveResourceViewModel()
        {
            _readOnlyFeatureLayers = new ReadOnlyObservableCollection<FeatureLayer>(_featureLayers);
            BindingOperations.EnableCollectionSynchronization(_readOnlyFeatureLayers, _lockCollections);

            ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);
        }

        /// <summary>
        /// Called when the pane is first created to give it the opportunity to initialize itself asynchronously.
        /// </summary>
        /// <returns>
        /// A task that represents the work queued to execute in the ThreadPool.
        /// </returns>
        protected override Task InitializeAsync()
        {
            GetFeatureLayers();
            return base.InitializeAsync();
        }

        /// <summary>
        /// List of the current active map's feature layers
        /// </summary>
        public ReadOnlyObservableCollection<FeatureLayer> FeatureLayers
        {
            get { return _readOnlyFeatureLayers; }
        }

        /// <summary>
        /// The selected feature layer
        /// </summary>
        public FeatureLayer SelectedFeatureLayer
        {
            get { return _selectedFeatureLayer; }
            set
            {
                SetProperty(ref _selectedFeatureLayer, value, () => SelectedFeatureLayer);
            }
        }

        /// <summary>
        /// The active map view changed therefore we refresh the feature layer drop-down
        /// </summary>
        /// <param name="args"></param>
        private void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
        {
            if (args.IncomingView == null) return;
            GetFeatureLayers();
        }

        /// <summary>
        /// This method is called to use the current active mapview and retrieve all 
        /// feature layers that are part of the map layers in the current map view.
        /// </summary>
        private void GetFeatureLayers()
        {
            //Get the active map view.
            var mapView = MapView.Active;
            if (mapView == null) return;
            var featureLayers = mapView.Map.Layers.OfType<FeatureLayer>();
            lock (_lockCollections)
            {
                _featureLayers.Clear();
                foreach (var featureLayer in featureLayers) _featureLayers.Add(featureLayer);
            }
            NotifyPropertyChanged(() => FeatureLayers);
        }

        public string ResourceIDEdited
        {
            get { return StaticVariables.archesResourceid; }
            set
            {
                SetProperty(ref StaticVariables.archesResourceid, value, () => ResourceIDEdited);
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
        private string _heading = "Save Resources";
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
    internal class SaveResource_ShowButton : Button
    {
        protected override void OnClick()
        {
            SaveResourceViewModel.Show();
        }
    }
}
