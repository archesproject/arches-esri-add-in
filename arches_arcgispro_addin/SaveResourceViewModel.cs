using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;

namespace arches_arcgispro_addin
{
    public class AttributeValue
    {
        public string Field { get; set; }
        public string Value { get; set; }
        public AttributeValue(string inField, string inValue)
        {
            Field = inField;
            Value = inValue;
        }
    }

    internal class SaveResourceViewModel : DockPane
    {
        private const string _dockPaneID = "arches_arcgispro_addin_SaveResource";

        private string _resourceIdEdited;
        private ICommand _buttonClick;
        private ICommand _buttonClick2;
        private static readonly object _lockCollections = new object();
        public static ObservableCollection<AttributeValue> _attributeValues = new ObservableCollection<AttributeValue>();

        public ObservableCollection<AttributeValue> AttributeValues
        {
            set
            {
                SetProperty(ref _attributeValues, value, () => AttributeValues);
            }
            get { return _attributeValues; }
        }

        public async void GetAttributeValues()
        {
            lock (_lockCollections) ;
            await QueuedTask.Run(() =>
            {
                var selectedFeatures = MapView.Active.Map.GetSelection();
                var firstSelectionSet = selectedFeatures.First();
                var archesInspector = new Inspector();
                archesInspector.Load(firstSelectionSet.Key, firstSelectionSet.Value);
                _attributeValues.Clear();
                try
                {
                    foreach (var attribute in archesInspector) {
                        AttributeValue newAttribute = new AttributeValue(attribute.FieldAlias, attribute.CurrentValue.ToString());
                        _attributeValues.Add(newAttribute);
                    }
                }
                catch (Exception ex)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ex.Message);
                }
            });
        }

        public static void ClearAttributeValues()
        {
            lock (_lockCollections) ;
            _attributeValues.Clear();
        }

        public string ResourceIdEdited
        {
            get { return _resourceIdEdited; }
            set
            {
                SetProperty(ref _resourceIdEdited, value, () => ResourceIdEdited);
            }
        }

        protected SaveResourceViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(_attributeValues, _lockCollections);
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
                            ResourceIdEdited = StaticVariables.archesResourceid;
                        }
                        catch (Exception ex)
                        {
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Make Sure to Select a Geometry from a valid Arches Layer");
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ex.Message);
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
        public ICommand ButtonClick
        {
            get
            {
                return _buttonClick ?? (_buttonClick = new RelayCommand(() =>
                {
                    try
                    {
                        if (MapView.Active == null)
                        {
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("No MapView currently active. Exiting...", "Info");
                            return;
                        }
                        GetAttribute();
                        GetAttributeValues();
                    }
                    catch (Exception ex)
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Exception: " + ex.Message);
                    }
                }, true));
            }
        }

        public ICommand ButtonClick2
        {
            get
            {
                return _buttonClick2 ?? (_buttonClick2 = new RelayCommand(() =>
                {
                    try
                    {
                        StaticVariables.archesNodeid = "";
                        StaticVariables.archesTileid = "";
                        StaticVariables.archesResourceid = "No Resource is Selected";
                        ResourceIdEdited = StaticVariables.archesResourceid;
                        ClearAttributeValues();
                    }
                    catch (Exception ex)
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Exception: " + ex.Message);
                    }
                }, true));
            }
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "Edit Resources";
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
