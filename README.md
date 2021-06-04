# arches-esri-add-in
An ESRI add-in that will interface with a Koop.js service backed by an Arches data store. This add-in requires both the installation of the ArcGIS Pro add-in and the installation and configuation of a koop.js server configured to serve data from an Arches installation. 

## System Requirements
ArcGIS Pro v2.5(?)
Arches v5.2.x(?) Deployment
Arches-Koop


## How to install and use the Arches-Esri addin

### Download and install Arches-Esri add-in
1. Select the branch that corresponds with the version of the plugin you would like to download (stable/1.0.x as of writing).
2. Download the .esriAddinX file from arches_arcgispro_addin/dist folder
3. Once downloaded, double click the add in to install or follow the instructions found here: https://pro.arcgis.com/en/pro-app/latest/get-started/manage-add-ins.htm
4. Follow instructions to install and configure koop on your Arches server.

### Installing koop on your Arches server
1. Clone the koop [repository](https://github.com/archesproject/arches-koop) on your Arches server.
2. Navigate to the root folder of the repository and run `yarn install`
3. Koop.js has an issue with the spatial transformation. Although ArcGIS Pro sends the valid spatial reference wkid, it does not pass the koop.js’ validation step. As a result the reprojection to some coordinate systems fails, including British National Grid. Here is a work-around the issue. The change will let users bypass the validation steps. (Note that this can cause an error in koop.js if a client application other than except for ArcGIS Pro is used with a Spatial Reference is indeed invalid). Make the following modifications:
    
    file: /arches-koop/node_modules/winnow/lib/normalize-query-options/spatial-reference.js
    instruction: comment out lines 19-27
    
    file: /arches-koop/node_modules/featureserver/dist/geometry/normalize-spatial-reference.js
    instruction: comment out lines 18-27
    
    file: /arches-koop/node_modules/winnow/lib/normalize-query-options/geometry-filter-spatial-reference.js
    instruction: comment out line 18

### Configuring koop on your Arches server
1. Edit the config file (arches-koop/config/development.json) based on the Arches [geojson api](https://arches.readthedocs.io/en/stable/api/#geojson)

  "url": for the Arches server
  "nodeid": geometry node
  "nodegroup": the values that you want to include as the properties, multiple nodegroupa can be added, separated by commas
   "type": geometry type (from Point, LineString, Polygon, MultiPoint, MultiLineString, MultiPolygon) you will have to check the response to confirm your selection
   "properties: to map the incoming properties for the specified nodegroups to the attribute names for ArcGIS
   
   Other parameters for the Arches geojson API can also be added, such as "use_display_values": true

2. To start the koop.js development server run `yarn start` from the root of the arches-koop application
3. Koop will run at the port 8080 by default, this can be changed in arches-koop/config/default.json if necessary.

## What you need to develop the Arches-ArcGIS Pro addin

### Visual Studio
https://visualstudio.microsoft.com/

### ESRI ArcGIS Pro SDK
https://pro.arcgis.com/en/pro-app/sdk/

##### Installation Guide
https://github.com/Esri/arcgis-pro-sdk/wiki/ProGuide-Installation-and-Upgrade

### Arches Koop Application (Local Koop Server for development)
https://github.com/archesproject/arches-koop
