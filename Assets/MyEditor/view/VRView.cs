#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.VR;
#if ENABLE_STEAMVR_INPUT
using Valve.VR;
#endif
using UnityEditor;


sealed class VRView 
{
	const string k_ShowDeviceView = "VRView.ShowDeviceView";
	const string k_UseCustomPreviewCamera = "VRView.UseCustomPreviewCamera";
	const string k_LaunchOnExitPlaymode = "VRView.LaunchOnExitPlaymode";
	
	//OpenVR stuff
	CVRSystem hmd;
	VR_Overlay overlay;
	
	VR_CameraRig vr_cam;
	VR_DesktopManager desktop;
	Texture scene_hole_texture;
	
	bool m_HMDReady;
	
	public static bool viewDisabled;

	
	public static event Action<bool> hmdStatusChange;

	public Rect guiRect { get; private set; }

	static VRView CreateInstance()
	{
		return new VRView();
	}


	/*
	static VRView GetWindow()
	{
		return GetWindow<VRView>(true);
	}


	/*
	static void ReopenOnExitPlaymode()
	{
		bool launch = EditorPrefs.GetBool(k_LaunchOnExitPlaymode, false);
		if (!launch || !EditorApplication.isPlaying)
		{
			EditorPrefs.DeleteKey(k_LaunchOnExitPlaymode);
			EditorApplication.update -= ReopenOnExitPlaymode;
			if (launch)
				GetWindow<VRView>();
		}
		
	}*/



	public void OnEnable()
	{
		viewDisabled = false;

		EditorApplication.playmodeStateChanged += OnPlaymodeStateChanged;
		EditorApplication.update += Update;


		//Initialize VR
		var error = EVRInitError.None;

		hmd = OpenVR.System;

		if (hmd == null)
			hmd = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);

		//OVERLAYS
		if (overlay == null)
		{
			overlay = new VR_Overlay();
			overlay.Create("Desktop", "Desktop Overlay");
		}
		
		//VR camera
		if (vr_cam == null)
		{
			vr_cam = new VR_CameraRig();
			vr_cam.Create();
			vr_cam.SetTexture();
		}

		if (desktop == null)
		{
			//VR_desktop
			desktop = new VR_DesktopManager();
			desktop.Start();			
		}
		//temporary texture assignment........................................................................
		overlay.texture = desktop.main_texture;

	}

	public void OnDisable()
	{
		viewDisabled = true;

		EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;
		EditorApplication.update -= Update;

		
		if (vr_cam !=null)
		{
			vr_cam.Destroy();
			vr_cam = null;
		}



		if (overlay != null)
		{
			overlay.Destroy();
			overlay = null;
		}

		

		OpenVR.Shutdown();
	}

	

	

	void OnGUI()
	{
		
		
		//Will render texture on window...

	}

	

	private void OnPlaymodeStateChanged()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
		{			
			EditorPrefs.SetBool(k_LaunchOnExitPlaymode, true);
			//Close();
		}

		OnDisable();
	}
	
	private void Update()
	{

		// If code is compiling, then we need to clean up the window resources before classes get re-initialized
		if (EditorApplication.isCompiling)
		{
			//Close();
			return;
		}

		VR_render();

	}

	

	static bool GetIsUserPresent()
	{
#if ENABLE_OVR_INPUT
			if (VRSettings.loadedDeviceName == "Oculus")
				return OVRPlugin.userPresent;
#endif
#if ENABLE_STEAMVR_INPUT
			if (VRSettings.loadedDeviceName == "OpenVR")
				return OpenVR.System.GetTrackedDeviceActivityLevel(0) == EDeviceActivityLevel.k_EDeviceActivityLevel_UserInteraction;
#endif
		return true;
	}
	
	public void VR_init()
	{

		
				
		//scene_hole_texture = FindObjectOfType<texture_sample>().sampleTexture;

		//Graphics.ConvertTexture(temp_tex, scene_hole_texture);
		
		//Graphics.CopyTexture(scene_hole_texture, 0,0, 0,0,100,100, overlay.texture,0,0,100,100);
		
		
	}

	public void VR_render()
	{
		if (Application.isPlaying)
			return;

		desktop.Update();
		vr_cam.Render();
		overlay.UpdateOverlay();
		


	}

	public static VRView instance;

	[MenuItem("VR Mode/Enable or Disam Edit VR %e", false)]
	static void EditorVR()
	{
		viewDisabled = !viewDisabled;
		// Using a utility window improves performance by saving from the overhead of DockArea.OnGUI()
		if (instance == null)
		{
			instance = CreateInstance();
			instance.OnEnable();
			return;
		}

		if(viewDisabled)
		{
			instance.OnDisable();			
		}else
		{
			instance.OnEnable();
		}


		
		//EditorWindow.GetWindow<VRView>(true, "VR Mode", true);
	}

	[MenuItem("VR Mode/Enable Edit VR %e", true)]
	static bool ShouldShowEditorVR()
	{
		return PlayerSettings.virtualRealitySupported;
	}

	[MenuItem("VR Mode/Reposition VR camera %q", false)]
	static void CloseEditorVR()
	{
		Debug.Log("VR camera repositioned...");
		//VR_Overlay.instance.reposition();
	}
}
#endif