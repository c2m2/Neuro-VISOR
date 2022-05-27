# Neuro-VISOR Multi-Headsets Supported (Beta)
**For a more compreshensive documentation about the software itself, please refer to this [link](https://github.com/c2m2/Neuro-VISOR).**
## Branch Description
The original VR framework used for Neuro-VISOR is implemented using the [Oculus SDK](https://developer.oculus.com/downloads/), this approach allows naive support for developing software for Oculus devices using Unity Engine, however, this means the software would not run on devices of other VR vendors. Fortunately, the Unity Engine has a unified plug-in framework named [XR](https://docs.unity3d.com/Manual/XRPluginArchitecture.html) that enables direct integrations for multiple platforms. Again, this branch is only an attempt to replace the original VR framework with the new XR framework, meaning there are bugs and missing features compared to the original version. It is only an experimental transition.
![OculusSDKToXR](https://user-images.githubusercontent.com/60633000/170388567-3cf6da60-9613-4b3e-9b31-c058f665588e.png)

## How to Use It
1. Clone/download the repository locally and open the local directory with Unity
2. Connect your VR headset to your computer, and make sure the connection is set up properly as different VR vendors have their ways of connecting the headset to the pc.
3. Once the Unity Editor loads in the project, make sure the corresponding XR plug-in is installed for the desired VR vendor in the package. If your VR headset is not on the default list of the setting or it is not presented in the package manager. (Do consider using the OpenVR Desktop plug-in, OpenVR is the API for SteamVR runtime that supports the Valve Index, HTC Vive, Oculus Rift, Windows Mixed Reality headsets, and others.)
![Whiteboard](https://user-images.githubusercontent.com/60633000/170595928-1dce2150-bf78-4c3c-9ae4-4004dbd80a5a.png)
4. Hit the play button in the Unity editor and you are good to go.

## Features/Limitations
* The current XR adaptation is not perfect which results in limited functionalities compared to [the original release](https://github.com/c2m2/Neuro-VISOR/releases/tag/v2.0.0) in which this work is modified from.
* The current branch preserves much of the desktop control as the original version but is limited in its VR control mechanism. The user would be able to initialize the 3D neuron to the simulation space and perform grabbing interaction. Much of the advanced interactions such as applying a voltage to the 3D neuron and placing a plot, clamp, and synapse on the neuron would need to perform using the desktop mode. (For specific details on each of the feature work in the greater details, please refer to this [link](https://github.com/c2m2/Neuro-VISOR).

## Reference
**This [file](https://github.com/c2m2/Neuro-VISOR/files/8783083/URPReport.pdf) contains more information on how I attempt to apply the Unity XR Plug-in framework to the software.**
1. Oculus Developers. Map Controllers. url:https://developer.oculus.com/documentation/unity/unity-ovrinput/.
2. Unity Technologies. Unity XR Input. url:https://docs.unity3d.com/2019.4/Documentation/Manual/xr_input.html.
3. Unity Technologies. XR Interaction Toolkit Manual 1.0.0-pre.8. url:https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@1.0/manual/index.html.
4. Unity Technologies. XR Interaction Toolkit Scripting API 1.0.0-pre.8. url: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@1.0/api/UnityEngine.XR.Interaction.Toolkit.html.
