//#if UNITY_EDITOR  
using System;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
//using UnityEditor.Experimental.EditorVR.Helpers;
using System.Reflection;
using UnityEngine.VR;
#if ENABLE_STEAMVR_INPUT
using Valve.VR;
#endif
using UnityEditor;
using UnityObject = UnityEngine.Object;


[InitializeOnLoad]
sealed class VRView : EditorWindow
{	

	
	EditorWindow[] m_EditorWindows;
	static VRView s_ActiveView;
	//OpenVR stuff
	TrackedDevicePose_t[] renderPoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
	TrackedDevicePose_t[] gamePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
	VRTextureBounds_t[] textureBounds;
	Texture_t VR_textureLeftEye, VR_textureRightEye;	

	CVRSystem hmd;


	GameObject LeftEye;
	GameObject RightEye;
	RenderTexture renderTextureLeftEye;
	RenderTexture renderTextureRightEye;
	Camera leftEyeCam;
	Camera rightEyeCam;

	Transform m_CameraRig;

	bool m_HMDReady;
	bool m_UseCustomPreviewCamera;

	public static Transform cameraRig
	{
		get
		{
			if (s_ActiveView)
				return s_ActiveView.m_CameraRig;

			return null;
		}
	}

	

	public static VRView activeView
	{
		get { return s_ActiveView; }
	}	

	public static event Action<EditorWindow> beforeOnGUI;
	
	public static event Action<bool> hmdStatusChange;

	public Rect guiRect { get; private set; }

	static VRView GetWindow()
	{
		return GetWindow<VRView>(true);
	}


	// Life cycle management across playmode switches is an odd beast indeed, and there is a need to reliably relaunch
	// EditorVR after we switch back out of playmode (assuming the view was visible before a playmode switch). So,
	// we watch until playmode is done and then relaunch.  
	static void ReopenOnExitPlaymode()
	{/*
		bool launch = EditorPrefs.GetBool(k_LaunchOnExitPlaymode, false);
		if (!launch || !EditorApplication.isPlaying)
		{
			EditorPrefs.DeleteKey(k_LaunchOnExitPlaymode);
			EditorApplication.update -= ReopenOnExitPlaymode;
			if (launch)
				GetWindow<VRView>();
		}
		*/
	}

	public void OnEnable()
	{
		EditorApplication.playmodeStateChanged += OnPlaymodeStateChanged;

		Assert.IsNull(s_ActiveView, "Only one EditorVR should be active");

	
		LeftEye = EditorUtility.CreateGameObjectWithHideFlags("VRCameraLeftEye", HideFlags.HideAndDontSave, typeof(Camera));
		leftEyeCam = LeftEye.GetComponent<Camera>();
		leftEyeCam.cameraType = CameraType.VR;
		leftEyeCam.nearClipPlane = 0.01f;
		leftEyeCam.farClipPlane = 1000f;
		leftEyeCam.transform.position = Vector3.zero;
		leftEyeCam.transform.rotation = Quaternion.identity;
		

		RightEye = EditorUtility.CreateGameObjectWithHideFlags("VRCameraRightEye", HideFlags.HideAndDontSave, typeof(Camera));
		rightEyeCam = RightEye.GetComponent<Camera>();
		rightEyeCam.cameraType = CameraType.VR;
		rightEyeCam.nearClipPlane = 0.01f;
		rightEyeCam.farClipPlane = 1000f;

		rightEyeCam.transform.position = Vector3.zero;
		rightEyeCam.transform.rotation = Quaternion.identity;
		
				

		//currentCamera.cameraType = CameraType.VR;
		//Create left eye texture
		renderTextureLeftEye = new RenderTexture(1520, 1680, 24);
		renderTextureLeftEye.format = RenderTextureFormat.ARGB32;
		renderTextureLeftEye.antiAliasing = 2;
		renderTextureLeftEye.Create();

		leftEyeCam.stereoTargetEye = StereoTargetEyeMask.Left;
		leftEyeCam.targetTexture = renderTextureLeftEye;
		leftEyeCam.Render();

		//Create right eye texture
		renderTextureRightEye = new RenderTexture(1520, 1680, 24);
		renderTextureRightEye.format = RenderTextureFormat.ARGB32;
		renderTextureRightEye.antiAliasing = 2;
		renderTextureRightEye.Create();


		rightEyeCam.stereoTargetEye = StereoTargetEyeMask.Right;
		rightEyeCam.targetTexture = renderTextureRightEye;
		rightEyeCam.Render();

		//Initialize VR
		VR_init();		
		
	}

	public void OnDisable()
	{		

		EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;

		//VRSettings.enabled = false;
		OpenVR.Shutdown();

		

		SetOtherViewsEnabled(true);

		if (m_CameraRig)
			DestroyImmediate(m_CameraRig.gameObject, true);

	}

	void UpdateCameraTransform()
	{
		
	}

	

	void OnGUI()
	{
		

		
	}

	

	private void OnPlaymodeStateChanged()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
		{
			
			Close();
		}
	}

	private void Update()
	{

		// If code is compiling, then we need to clean up the window resources before classes get re-initialized
		if (EditorApplication.isCompiling)
		{
			Close();
			return;
		}

	

		// Our camera is disabled, so it doesn't get automatically updated to HMD values until it renders
		UpdateCameraTransform();

		VR_render();

		UpdateHMDStatus();

		SetSceneViewsAutoRepaint(false);
	}

	void UpdateHMDStatus()
	{
		if (hmdStatusChange != null)
		{
			var ready = GetIsUserPresent();
			if (m_HMDReady != ready)
			{
				m_HMDReady = ready;
				hmdStatusChange(ready);
			}
		}
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

	void SetGameViewsAutoRepaint(bool enabled)
	{
		var asm = Assembly.GetAssembly(typeof(UnityEditor.EditorWindow));
		var type = asm.GetType("UnityEditor.GameView");
		SetAutoRepaintOnSceneChanged(type, enabled);
	}

	void SetSceneViewsAutoRepaint(bool enabled)
	{
		SetAutoRepaintOnSceneChanged(typeof(SceneView), enabled);
	}

	void SetOtherViewsEnabled(bool enabled)
	{
		SetGameViewsAutoRepaint(enabled);
		SetSceneViewsAutoRepaint(enabled);
	}

	void SetAutoRepaintOnSceneChanged(Type viewType, bool enabled)
	{
		if (m_EditorWindows == null)
			m_EditorWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();

		var windowCount = m_EditorWindows.Length;
		var mouseOverWindow = EditorWindow.mouseOverWindow;
		for (int i = 0; i < windowCount; i++)
		{
			var window = m_EditorWindows[i];
			if (window.GetType() == viewType)
				window.autoRepaintOnSceneChange = enabled || (window == mouseOverWindow);
		}


	}



	public void VR_init()
	{
		renderPoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
		gamePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

		var error = EVRInitError.None;
		hmd = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
		//Debug.Log("Connected to " + hmd_TrackingSystemName + ":" + hmd_SerialNumber);

		var compositor = OpenVR.Compositor;
		var overlay = OpenVR.Overlay;

		// Setup render values
		uint w = 0, h = 0;
		hmd.GetRecommendedRenderTargetSize(ref w, ref h);
		float sceneWidth = (float)w;
		float sceneHeight = (float)h;

		float l_left = 0.0f, l_right = 0.0f, l_top = 0.0f, l_bottom = 0.0f;
		hmd.GetProjectionRaw(EVREye.Eye_Left, ref l_left, ref l_right, ref l_top, ref l_bottom);
		

		float r_left = 0.0f, r_right = 0.0f, r_top = 0.0f, r_bottom = 0.0f;
		hmd.GetProjectionRaw(EVREye.Eye_Right, ref r_left, ref r_right, ref r_top, ref r_bottom);

		Vector2 tanHalfFov = new Vector2(
			Mathf.Max(-l_left, l_right, -r_left, r_right),
			Mathf.Max(-l_top, l_bottom, -r_top, r_bottom));

		textureBounds = new VRTextureBounds_t[2];

		textureBounds[0].uMin = 0.5f + 0.5f * l_left / tanHalfFov.x;
		textureBounds[0].uMax = 0.5f + 0.5f * l_right / tanHalfFov.x;
		textureBounds[0].vMin = 0.5f - 0.5f * l_bottom / tanHalfFov.y;
		textureBounds[0].vMax = 0.5f - 0.5f * l_top / tanHalfFov.y;

		textureBounds[1].uMin = 0.5f + 0.5f * r_left / tanHalfFov.x;
		textureBounds[1].uMax = 0.5f + 0.5f * r_right / tanHalfFov.x;
		textureBounds[1].vMin = 0.5f - 0.5f * r_bottom / tanHalfFov.y;
		textureBounds[1].vMax = 0.5f - 0.5f * r_top / tanHalfFov.y;
		
		// Account for textures being upside-down in Unity.This gave really hard time to figure it out
		textureBounds[0].vMin = 1.0f - textureBounds[0].vMin;
		textureBounds[0].vMax = 1.0f - textureBounds[0].vMax;
		textureBounds[1].vMin = 1.0f - textureBounds[1].vMin;
		textureBounds[1].vMax = 1.0f - textureBounds[1].vMax;

		// Grow the recommended size to account for the overlapping fov
		sceneWidth = sceneWidth / Mathf.Max(textureBounds[0].uMax - textureBounds[0].uMin, textureBounds[1].uMax - textureBounds[1].uMin);
		sceneHeight = sceneHeight / Mathf.Max(textureBounds[0].vMax - textureBounds[0].vMin, textureBounds[1].vMax - textureBounds[1].vMin);

		float aspect = tanHalfFov.x / tanHalfFov.y;
		float fieldOfView = 2.0f * Mathf.Atan(tanHalfFov.y) * Mathf.Rad2Deg;
		leftEyeCam.aspect = aspect;
		rightEyeCam.aspect = aspect;
		leftEyeCam.fieldOfView = fieldOfView;
		rightEyeCam.fieldOfView = fieldOfView;
		
		
	}

	public void VR_render()
	{

		leftEyeCam.Render();
		rightEyeCam.Render();

		VR_textureLeftEye = new Texture_t();
		VR_textureRightEye = new Texture_t();

		VR_textureLeftEye.handle = renderTextureLeftEye.GetNativeTexturePtr();
		VR_textureRightEye.handle = renderTextureRightEye.GetNativeTexturePtr();

		//texture.eType = SteamVR.instance.textureType;
		VR_textureLeftEye.eColorSpace = EColorSpace.Auto;
		VR_textureRightEye.eColorSpace = EColorSpace.Auto;


		if (!SteamVR.active && !SteamVR.usingNativeSupport)
		{

			if (OpenVR.Compositor.CanRenderScene())
			{

			
				OpenVR.Compositor.Submit(EVREye.Eye_Left, ref VR_textureLeftEye, ref textureBounds[0], EVRSubmitFlags.Submit_Default);
				OpenVR.Compositor.Submit(EVREye.Eye_Right, ref VR_textureRightEye, ref textureBounds[1], EVRSubmitFlags.Submit_Default);
				OpenVR.Compositor.WaitGetPoses(renderPoseArray, gamePoseArray);

				SteamVR_Utils.RigidTransform pose_head =new SteamVR_Utils.RigidTransform(renderPoseArray[0].mDeviceToAbsoluteTracking);
				SteamVR_Utils.RigidTransform pose_left_to_head =new SteamVR_Utils.RigidTransform(hmd.GetEyeToHeadTransform(EVREye.Eye_Left));
				SteamVR_Utils.RigidTransform pose_right_to_head = new SteamVR_Utils.RigidTransform(hmd.GetEyeToHeadTransform(EVREye.Eye_Right));
				
				leftEyeCam.transform.localPosition = pose_head.TransformPoint(pose_left_to_head.pos);
				leftEyeCam.transform.localRotation = pose_head.rot * pose_left_to_head.rot;

				rightEyeCam.transform.localPosition = pose_head.TransformPoint(pose_right_to_head.pos);
				rightEyeCam.transform.localRotation = pose_head.rot * pose_right_to_head.rot;
			}
		}
	}

	[MenuItem("Window/EditorVR %e", false)]
	static void ShowEditorVR()
	{
		// Using a utility window improves performance by saving from the overhead of DockArea.OnGUI()
		EditorWindow.GetWindow<VRView>(true, "EditorVR", true);
	}

	[MenuItem("Window/EditorVR %e", true)]
	static bool ShouldShowEditorVR()
	{
		return PlayerSettings.virtualRealitySupported;
	}
}

