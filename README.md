# arches-esri-add-in

An ArcGIS Pro add-in that provides geometry editing capabilities for Arches, and provides a quick access workflow for editing Arches records. This is used in conjunction with Arches spatial views which provide a data source for layers in ArcGIS Pro 

## System Requirements

- ArcGIS Pro v3.x
- Arches v6.0+


## How to install and use the Arches-Esri addin

### Download and install Arches-Esri add-in

1. Select the branch that corresponds with the version of the plugin you would like to install.
2. Download the .esriAddinX file from `arches_arcgispro_addin/dist` folder
3. Once downloaded, double click the add in to install or follow the instructions found [here](https://pro.arcgis.com/en/pro-app/latest/get-started/manage-add-ins.htm)
4. Follow instructions to install and configure koop on your Arches server.

### Adding Arches spatial layers into ArcGIS Pro

To add spatial data into ArcGIS Pro from Arches, you will firstly need to configure one or more spatial views to provide you with a data source.

Instructions on creating these spatial views can be found in the official Arches documentation [here](https://arches.readthedocs.io/en/stable/administering/spatial-views/).

Then in ArcGIS Pro, create a database connection to the Arches database using the credentials found [here](https://arches.readthedocs.io/en/stable/administering/spatial-views/#using-the-spatial-views).  Note that if you, or your IT team have configured different credentials, you will need to use those instead.

You should then be able to use the ArcGIS Pro Catalog to open the connection and add the data sources to the map.

If you do not have direct database access, then it will be necessary for your IT or GIS team to provide a service that connects to that data source.

## What you need to develop the Arches-ArcGIS Pro addin

### Visual Studio
https://visualstudio.microsoft.com/

### ESRI ArcGIS Pro SDK
https://pro.arcgis.com/en/pro-app/sdk/

##### Installation Guide
https://github.com/Esri/arcgis-pro-sdk/wiki/ProGuide-Installation-and-Upgrade
