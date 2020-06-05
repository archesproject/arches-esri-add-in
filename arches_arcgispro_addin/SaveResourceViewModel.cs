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
        private ICommand _buttonClick1;
        private static readonly object _lockCollections = new object();
        public static ObservableCollection<AttributeValue> _attributeValues = new ObservableCollection<AttributeValue>();

        private bool _registered = false;
        private string _registeredVisibility = "Hidden";
        private string _unregisteredVisibility = "Visible";
        private string _message = "";
        private string _messageBoxVisibility = "Hidden";

        public bool Registered
        {
            get { return _registered; }
            set
            {
                SetProperty(ref _registered, value, () => Registered);
            }
        }

        public string RegisteredVisibility
        {
            get { return _registeredVisibility; }
            set
            {
                SetProperty(ref _registeredVisibility, value, () => RegisteredVisibility);
            }
        }

        public string UnregisteredVisibility
        {
            get { return _unregisteredVisibility; }
            set
            {
                SetProperty(ref _unregisteredVisibility, value, () => UnregisteredVisibility);
            }
        }

        public string Message
        {
            get { return _message; }
            set
            {
                SetProperty(ref _message, value, () => Message);
                if (_message != "") {
                    MessageBoxVisibility = "Visible";
                }
                else
                {
                    MessageBoxVisibility = "Hidden";
                }
            }
        }
        public string MessageBoxVisibility
        {
            get { return _messageBoxVisibility; }
            set
            {
                SetProperty(ref _messageBoxVisibility, value, () => MessageBoxVisibility);
            }
        }

        public ObservableCollection<AttributeValue> AttributeValues
        {
            set
            {
                SetProperty(ref _attributeValues, value, () => AttributeValues);
            }
            get { return _attributeValues; }
        }

        public void ClearAttribute()
        {
            StaticVariables.archesNodeid = "";
            StaticVariables.archesTileid = "";
            StaticVariables.archesResourceid = "No Resource is Selected";
            ResourceIdEdited = StaticVariables.archesResourceid;
            Registered = false;
            Message = "";
        }

        public void ClearAttributeValues()
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
                        _attributeValues.Clear();
                        try
                        {
                            foreach (var attribute in archesInspector)
                            {
                                AttributeValue newAttribute = new AttributeValue(attribute.FieldAlias, attribute.CurrentValue.ToString());
                                _attributeValues.Add(newAttribute);

                                StaticVariables.archesResourceid = archesInspector["resourceinstanceid"].ToString();
                                StaticVariables.archesTileid = archesInspector["tileid"].ToString();
                                StaticVariables.archesNodeid = archesInspector["nodeid"].ToString();
                                ResourceIdEdited = StaticVariables.archesResourceid;
                                Registered = true;
                                RegisteredVisibility = "Visible";
                                UnregisteredVisibility = "Hidden";
                                MessageBoxVisibility = "Hidden";
                            }
                        }
                        catch (Exception ex)
                        {
                            ClearAttribute();
                            ClearAttributeValues();
                            Message = $"This feature may not exist on \n{StaticVariables.myInstanceURL}\n{ex.Message}";
                        }
                    }
                    else
                    {
                        ClearAttribute();
                        ClearAttributeValues();
                        Message = "Make Sure to Select ONE valid geometry";
                    }
                }
                else
                {
                    ClearAttribute();
                    ClearAttributeValues();
                    Message = "Make Sure to Select from ONE Arches Layer";
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
                            Message = "No MapView currently active.";
                            return;
                        }
                        GetAttribute();
                    }
                    catch (Exception ex)
                    {
                        Message = "Exception: " + ex.Message;
                    }
                }, true));
            }
        }

        public ICommand ButtonClick1
        {
            get
            {
                return _buttonClick1 ?? (_buttonClick1 = new RelayCommand(() =>
                {
                    try
                    {
                        ClearAttribute();
                        ClearAttributeValues();
                        RegisteredVisibility = "Hidden";
                        UnregisteredVisibility = "Visible";
                    }
                    catch (Exception ex)
                    {
                        Message = "Exception: " + ex.Message;
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
