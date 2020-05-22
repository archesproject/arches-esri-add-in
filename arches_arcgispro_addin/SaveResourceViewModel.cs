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

        protected SaveResourceViewModel()
        {
        }

        /// <summary>
        /// Called when the pane is first created to give it the opportunity to initialize itself asynchronously.
        /// </summary>
        /// <returns>
        /// A task that represents the work queued to execute in the ThreadPool.
        /// </returns>
        protected override Task InitializeAsync()
        {
            return base.InitializeAsync();
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
