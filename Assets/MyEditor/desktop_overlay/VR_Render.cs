//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Handles rendering of all  VR_Cameras
//
//=============================================================================

using UnityEngine;
using System.Collections;
using Valve.VR;

public class  VR_Render : MonoBehaviour
{
	public bool pauseGameWhenDashboardIsVisible = true;
	public bool lockPhysicsUpdateRateToRenderFrequency = true;

	public SteamVR_ExternalCamera externalCamera;
	public string externalCameraConfigPath = "externalcamera.cfg";

	public ETrackingUniverseOrigin trackingSpace = ETrackingUniverseOrigin.TrackingUniverseStanding;

	static public EVREye eye { get; private set; }

	static private  VR_Render _instance;
	static public  VR_Render instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = GameObject.FindObjectOfType< VR_Render>();

				if (_instance == null)
					_instance = new GameObject("[SteamVR]").AddComponent< VR_Render>();
			}
			return _instance;
		}
	}

	void OnDestroy()
	{
		_instance = null;
	}

	static private bool isQuitting;
	void OnApplicationQuit()
	{
		isQuitting = true;
		SteamVR.SafeDispose();
	}

	static public void Add(VR_Camera vrcam)
	{
		if (!isQuitting)
			instance.AddInternal(vrcam);
	}

	static public void Remove(VR_Camera vrcam)
	{
		if (!isQuitting && _instance != null)
			instance.RemoveInternal(vrcam);
	}

	static public  VR_Camera Top()
	{
		if (!isQuitting)
			return instance.TopInternal();

		return null;
	}

	private  VR_Camera[] cameras = new  VR_Camera[0];

	void AddInternal( VR_Camera vrcam)
	{
		var camera = vrcam.GetComponent<Camera>();
		var length = cameras.Length;
		var sorted = new  VR_Camera[length + 1];
		int insert = 0;
		for (int i = 0; i < length; i++)
		{
			var c = cameras[i].GetComponent<Camera>();
			if (i == insert && c.depth > camera.depth)
				sorted[insert++] = vrcam;

			sorted[insert++] = cameras[i];
		}
		if (insert == length)
			sorted[insert] = vrcam;

		cameras = sorted;
	}

	void RemoveInternal( VR_Camera vrcam)
	{
		var length = cameras.Length;
		int count = 0;
		for (int i = 0; i < length; i++)
		{
			var c = cameras[i];
			if (c == vrcam)
				++count;
		}
		if (count == 0)
			return;

		var sorted = new  VR_Camera[length - count];
		int insert = 0;
		for (int i = 0; i < length; i++)
		{
			var c = cameras[i];
			if (c != vrcam)
				sorted[insert++] = c;
		}

		cameras = sorted;
	}

	 VR_Camera TopInternal()
	{
		if (cameras.Length > 0)
			return cameras[cameras.Length - 1];

		return null;
	}

	public TrackedDevicePose_t[] poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
	public TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[0];

	static private bool _pauseRendering;
	static public bool pauseRendering
	{
		get { return _pauseRendering; }
		set
		{
			_pauseRendering = value;

			var compositor = OpenVR.Compositor;
			if (compositor != null)
				compositor.SuspendRendering(value);
		}
	}

	private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

	private IEnumerator RenderLoop()
	{
		while (Application.isPlaying)
		{
			yield return waitForEndOfFrame;

			if (pauseRendering)
				continue;

			var compositor = OpenVR.Compositor;
			if (compositor != null)
			{
				if (!compositor.CanRenderScene())
					continue;

				compositor.SetTrackingSpace(trackingSpace);
			}

			var overlay = VR_Overlay.instance;
			if (overlay != null)
				overlay.UpdateOverlay();

			
		}
	}

	

	float sceneResolutionScale = 1.0f, timeScale = 1.0f;

	private void OnInputFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			if (pauseGameWhenDashboardIsVisible)
			{
				Time.timeScale = timeScale;
			}

			 VR_Camera.sceneResolutionScale = sceneResolutionScale;
		}
		else
		{
			if (pauseGameWhenDashboardIsVisible)
			{
				timeScale = Time.timeScale;
				Time.timeScale = 0.0f;
			}

			sceneResolutionScale =  VR_Camera.sceneResolutionScale;
			 VR_Camera.sceneResolutionScale = 0.5f;
		}
	}

	void OnQuit(VREvent_t vrEvent)
	{
#if UNITY_EDITOR
		foreach (System.Reflection.Assembly a in System.AppDomain.CurrentDomain.GetAssemblies())
		{
			var t = a.GetType("UnityEditor.EditorApplication");
			if (t != null)
			{
				t.GetProperty("isPlaying").SetValue(null, false, null);
				break;
			}
		}
#else
		Application.Quit();
#endif
	}

	

	void OnEnable()
	{
		StartCoroutine("RenderLoop");
		SteamVR_Events.InputFocus.Listen(OnInputFocus);
		SteamVR_Events.System(EVREventType.VREvent_Quit).Listen(OnQuit);

		var vr = SteamVR.instance;
		if (vr == null)
		{
			enabled = false;
			return;
		}
		
	}

	void OnDisable()
	{
		StopAllCoroutines();
		SteamVR_Events.InputFocus.Remove(OnInputFocus);
		SteamVR_Events.System(EVREventType.VREvent_Quit).Remove(OnQuit);
		
	}

	void Awake()
	{
	}

#if !(UNITY_5_6)
	private SteamVR_UpdatePoses poseUpdater;
#endif

	void Update()
	{
#if !(UNITY_5_6)
		if (poseUpdater == null)
		{
			var go = new GameObject("poseUpdater");
			go.transform.parent = transform;
			poseUpdater = go.AddComponent<SteamVR_UpdatePoses>();
		}
#endif
		

		// Dispatch any OpenVR events.
		var system = OpenVR.System;
		if (system != null)
		{
			var vrEvent = new VREvent_t();
			var size = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VREvent_t));
			for (int i = 0; i < 64; i++)
			{
				if (!system.PollNextEvent(ref vrEvent, size))
					break;

				switch ((EVREventType)vrEvent.eventType)
				{
					case EVREventType.VREvent_InputFocusCaptured: // another app has taken focus (likely dashboard)
						if (vrEvent.data.process.oldPid == 0)
						{
							SteamVR_Events.InputFocus.Send(false);
						}
						break;
					case EVREventType.VREvent_InputFocusReleased: // that app has released input focus
						if (vrEvent.data.process.pid == 0)
						{
							SteamVR_Events.InputFocus.Send(true);
						}
						break;
					case EVREventType.VREvent_ShowRenderModels:
						SteamVR_Events.HideRenderModels.Send(false);
						break;
					case EVREventType.VREvent_HideRenderModels:
						SteamVR_Events.HideRenderModels.Send(true);
						break;
					default:
						SteamVR_Events.System((EVREventType)vrEvent.eventType).Send(vrEvent);
						break;
				}
			}
		}

		// Ensure various settings to minimize latency.
		Application.targetFrameRate = -1;
		Application.runInBackground = true; // don't require companion window focus
		QualitySettings.maxQueuedFrames = -1;
		QualitySettings.vSyncCount = 0; // this applies to the companion window

		if (lockPhysicsUpdateRateToRenderFrequency && Time.timeScale > 0.0f)
		{
			var vr = SteamVR.instance;
			if (vr != null)
			{
				var timing = new Compositor_FrameTiming();
				timing.m_nSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Compositor_FrameTiming));
				vr.compositor.GetFrameTiming(ref timing, 0);

				Time.fixedDeltaTime = Time.timeScale / vr.hmd_DisplayFrequency;
			}
		}
	}
}

