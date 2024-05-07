# Project Configuration
In order for this project to be functional in editor and on device there is some initial setup that needs to be done.

## Application Configuration
To run the project and use the platform services we need to create an application on the [Meta Quest Developer Center](https://developer.oculus.com/).

To run on device you will need a Quest application, and to run in editor you will need a Rift application. The following sections will describe the configuration required for the application to run.

### Data Use Checkup
To use the features from the Platform we need to request which kind of data is required for the application. This can be found in the _Data Use Checkup_ section of the application.

![data use checkup](./Media/dashboard/datausecheckup.png "Data use Checkup")

And configure the required Data Usage:
* **User Id**: Avatars, Oculus Username
* **User Profile**: Avatars, Oculus Username
* **Avatars**: Avatars

Once completed you will need to submit the request, click the submit button at the bottom.
![data use checkup submit](./Media/dashboard/datausecheckup_submit.png "Data use Checkup submit")

### Set the Application ID
We then need to the set the application ID in our project in Unity.

The identifier (__App ID__) can be found in the _API_ section.

![Application API](./Media/dashboard/dashboard_api.png "Application API")

Then it needs to be placed in the [Assets/Resources/OculusPlatformSettings.asset](../Assets/Resources/OculusPlatformSettings.asset)

![Oculus Platform Settings Menu](./Media/editor/oculusplatformsettings_menu.png "Oculus Platform Settings Menu")

![Oculus Platform Settings](./Media/editor/oculusplatformsettings.png "Oculus Platform Settings")

## Photon Configuration

To get the sample working, you will need to configure Photon with your own account and applications. The Photon base plan is free.
- Visit [photonengine.com](https://www.photonengine.com) and [create an account](https://doc.photonengine.com/en-us/realtime/current/getting-started/obtain-your-app-id)
- From your Photon dashboard, click "Create A New App"
    - We will create 2 apps, "Fusion" and "Voice"
- First fill out the form making sure to set type to "Photon Fusion". Then click Create.
- Second fill out the form making sure to set type to "Photon Voice". Then click Create.

Your new app will now show on your Photon dashboard. Click the App ID to reveal the full string and copy the value for each app.

Open your unity project and paste your Fusion App ID and Voice App ID in [Assets/Photon/Resources/PhotonAppSettings](../Assets/Photon/Fusion/Resources/PhotonAppSettings.asset).

![Photon App Settings](./Media/editor/photonappsettings.png "Photon App Settings")


The Photon Realtime transport should now work. You can check the dashboard in your Photon account to verify there is network traffic.

## Headset permissions
When you first launch the application a permission popup will ask to share point cloud, you must say yes if you want to use colocation.
If you answered no, from this application or another application using shared point cloud data, you can go on device to
**Settings > Privacy > Device Permissions > Share Point Cloud Data**. It must be enabled.

The application should also have Spatial Data permission enabled, this can be verified on device here: **Settings -> Apps -> Permissions -> Spatial Data**
