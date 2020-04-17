﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace arches_arcgispro_addin
{
    internal class Login : Button
    {
        protected override void OnClick()
        {
            string stringURI = ArcGIS.Desktop.Core.Project.Current.URI;
            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Project path:  {stringURI}", "Project Info");
        }
    }
}