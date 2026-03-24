# arches-esri-add-in

An ArcGIS Pro add-in that provides geometry editing capabilities for Arches, and provides a quick access workflow for editing Arches records. This is used in conjunction with Arches spatial views which provide a data source for layers in ArcGIS Pro.

## System Requirements
- ArcGIS Pro 3.3+
- Arches 7.x deployment with OAuth2 enabled (Django OAuth Toolkit)

## Installation

### 1. Configure the Arches server

Register an OAuth application on your Arches instance at `/o/applications/`:

- **Name:** ArcGIS Pro Add-in (or whatever you prefer)
- **Client type:** Public
- **Authorization grant type:** Authorization code
- **Redirect URI:** `http://127.0.0.1:53821/callback/`

Copy the **Client ID** — you'll need it in the next step.

### 2. Configure the add-in

Edit `arches_config.json` in the add-in package with your deployment details:

```json
{
  "instance_url": "https://your-arches-server.com/",
  "client_id": "your-client-id-from-step-1"
}
```

Optional settings:

| Setting | Default | Description |
|---------|---------|-------------|
| `callback_port` | `53821` | Local port for the OAuth redirect listener |
| `auth_timeout_seconds` | `180` | How long to wait for the user to complete browser sign-in |

### 3. Install the add-in

1. Download or build the `.esriAddinX` file
2. Double-click the file to install, or follow the [ESRI instructions for managing add-ins](https://pro.arcgis.com/en/pro-app/latest/get-started/manage-add-ins.htm)
3. Open ArcGIS Pro — the **Arches Project** tab will appear in the ribbon

### 4. Sign in

1. Click **Arches Connection** in the ribbon
2. Click **Sign In** — your browser will open to the Arches login page
3. Log in to Arches — the browser will redirect back and you can close the tab
4. ArcGIS Pro will show "Connected" and enable the Create/Edit Resource tools

On subsequent launches, the add-in will attempt to silently reconnect using a stored refresh token.

### Adding Arches spatial layers into ArcGIS Pro

To add spatial data into ArcGIS Pro from Arches, you will firstly need to configure one or more spatial views to provide you with a data source.

Instructions on creating these spatial views can be found in the official Arches documentation [here](https://arches.readthedocs.io/en/stable/administering/spatial-views/).

Then in ArcGIS Pro, create a database connection to the Arches database using the credentials found [here](https://arches.readthedocs.io/en/stable/administering/spatial-views/#using-the-spatial-views). Note that if you, or your IT team have configured different credentials, you will need to use those instead.

You should then be able to use the ArcGIS Pro Catalog to open the connection and add the data sources to the map.

If you do not have direct database access, then it will be necessary for your IT or GIS team to provide a service that connects to that data source.

## Development

### Requirements
- Visual Studio 2022 Community (or higher) with the .NET desktop development workload
- [ArcGIS Pro SDK for .NET](https://pro.arcgis.com/en/pro-app/sdk/)
- ArcGIS Pro 3.3+ installed locally

##### Installation Guide
https://github.com/Esri/arcgis-pro-sdk/wiki/ProGuide-Installation-and-Upgrade

### Building
1. Open `arches_arcgispro_addin.csproj` in Visual Studio
2. Build the solution — the `.esriAddinX` package is generated automatically in the output directory
