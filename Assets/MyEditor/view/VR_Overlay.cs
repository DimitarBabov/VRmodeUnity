//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Displays 2d content on a large virtual screen.
//
//=============================================================================

using UnityEngine;
using UnityEditor;
using Valve.VR;


//[ExecuteInEditMode]
public class VR_Overlay 
{
	public RenderTexture texture;
	private GameObject center;
	private GameObject r1,r2,r3,r4,r5,l1,l2,l3,l4,l5;//right and left overlay game objects
	private int numOverlays =11;
	private float angle = 12; 

	public bool curved = false;//enable for curved overlay
	public bool antialias = false;//enable for curved overlay
	public bool highquality = false;//enable for curved overlay
	public float scale = 3.0f;			// size of overlay view
	public float distance = 1.25f;		// distance from surface
	public float alpha = 1.0f;          // opacity 0..1
	public float zoom_step = 0.02f;

	public Vector4 uvOffset = new Vector4(0, 0, 1, 1);
	public Vector2 mouseScale = new Vector2(1, 1);
	public Vector2 curvedRange = new Vector2(1.5f, 1.5f);

	private bool m_zoom = false;
	private bool m_zoomWithFollowCursor = false;
	private Vector3 m_positionNormal;
	private Quaternion m_rotationNormal;
	private Vector3 m_positionZoomed;
	private Quaternion m_rotationZoomed;

	private float m_positionAnimationStart = 0;

	// Keyboard and Mouse
	private float m_lastShowClickStart = 0;

	VRTextureBounds_t textureBounds;

	public VROverlayInputMethod inputMethod = VROverlayInputMethod.None;

	static public VR_Overlay instance { get; private set; }

	private string key;

	private ulong handle = OpenVR.k_ulOverlayHandleInvalid;
	private ulong handle_r1= OpenVR.k_ulOverlayHandleInvalid;
	private ulong handle_r2 = OpenVR.k_ulOverlayHandleInvalid;
	private ulong handle_r3 = OpenVR.k_ulOverlayHandleInvalid;
	private ulong handle_r4 = OpenVR.k_ulOverlayHandleInvalid;
	private ulong handle_r5 = OpenVR.k_ulOverlayHandleInvalid;

	private ulong handle_l1 = OpenVR.k_ulOverlayHandleInvalid;
	private ulong handle_l2 = OpenVR.k_ulOverlayHandleInvalid;
	private ulong handle_l3 = OpenVR.k_ulOverlayHandleInvalid;
	private ulong handle_l4 = OpenVR.k_ulOverlayHandleInvalid;
	private ulong handle_l5 = OpenVR.k_ulOverlayHandleInvalid;
	public void Create(string overlay_key, string overlay_name)
	{
		

		center = new GameObject("Center overlay");

		r1 = new GameObject("r1 overlay"); r1.transform.parent = center.transform; 
		r2 = new GameObject("r2 overlay"); r2.transform.parent = r1.transform;
		r3 = new GameObject("r3 overlay"); r3.transform.parent = r2.transform;
		r4 = new GameObject("r4 overlay"); r4.transform.parent = r3.transform;
		r5 = new GameObject("r5 overlay"); r5.transform.parent = r4.transform;

		l1 = new GameObject("l1 overlay"); l1.transform.parent = center.transform;
		l2 = new GameObject("l2 overlay"); l2.transform.parent = l1.transform;
		l3 = new GameObject("l3 overlay"); l3.transform.parent = l2.transform;
		l4 = new GameObject("l4 overlay"); l4.transform.parent = l3.transform;
		l5 = new GameObject("l5 overlay"); l5.transform.parent = l4.transform;

		orientOverlayLocal(r5.transform, 1f);
		orientOverlayLocal(r4.transform, 1f);
		orientOverlayLocal(r3.transform, 1f);
		orientOverlayLocal(r2.transform, 1f);
		orientOverlayLocal(r1.transform, 1f);

		orientOverlayLocal(l5.transform, -1f);
		orientOverlayLocal(l4.transform, -1f);
		orientOverlayLocal(l3.transform, -1f);
		orientOverlayLocal(l2.transform, -1f);
		orientOverlayLocal(l1.transform, -1f);
		

		key = overlay_key;
		var overlay = OpenVR.Overlay;
		if (overlay != null)
		{
			var error = overlay.CreateOverlay(key, overlay_name , ref handle);

			var error_r1 = overlay.CreateOverlay(key + "r1", overlay_name + "r1", ref handle_r1);
			var error_r2 = overlay.CreateOverlay(key + "r2", overlay_name + "r2", ref handle_r2);
			var error_r3 = overlay.CreateOverlay(key + "r3", overlay_name + "r3", ref handle_r3);
			var error_r4 = overlay.CreateOverlay(key + "r4", overlay_name + "r4", ref handle_r4);
			var error_r5 = overlay.CreateOverlay(key + "r5", overlay_name + "r5", ref handle_r5);

			var error_l1 = overlay.CreateOverlay(key + "l1", overlay_name + "l1", ref handle_l1);
			var error_l2 = overlay.CreateOverlay(key + "l2", overlay_name + "l2", ref handle_l2);
			var error_l3 = overlay.CreateOverlay(key + "l3", overlay_name + "l3", ref handle_l3);
			var error_l4 = overlay.CreateOverlay(key + "l4", overlay_name + "l4", ref handle_l4);
			var error_l5 = overlay.CreateOverlay(key + "l5", overlay_name + "l5", ref handle_l5);


			if (error != EVROverlayError.None )
			{
				Debug.Log(overlay.GetOverlayErrorNameFromEnum(error));
				//enabled = false;
				return;
			}
			/*...........CHECK ALL FOR ERRORS..................*/
		}
		

		VR_Overlay.instance = this;
	}

	public void Destroy()
	{
		if (handle != OpenVR.k_ulOverlayHandleInvalid)
		{
			var overlay = OpenVR.Overlay;
			if (overlay != null)
			{
				overlay.DestroyOverlay(handle);
				overlay.DestroyOverlay(handle_r1);
				overlay.DestroyOverlay(handle_r2);
				overlay.DestroyOverlay(handle_r3);
				overlay.DestroyOverlay(handle_r4);
				overlay.DestroyOverlay(handle_r5);

				overlay.DestroyOverlay(handle_l1);
				overlay.DestroyOverlay(handle_l2);
				overlay.DestroyOverlay(handle_l3);
				overlay.DestroyOverlay(handle_l4);
				overlay.DestroyOverlay(handle_l5);
			}

			handle = OpenVR.k_ulOverlayHandleInvalid;
			handle_r1 = OpenVR.k_ulOverlayHandleInvalid;
			handle_r2 = OpenVR.k_ulOverlayHandleInvalid;
			handle_r3 = OpenVR.k_ulOverlayHandleInvalid;
			handle_r4 = OpenVR.k_ulOverlayHandleInvalid;
			handle_r5 = OpenVR.k_ulOverlayHandleInvalid;

			handle_l1 = OpenVR.k_ulOverlayHandleInvalid;
			handle_l2 = OpenVR.k_ulOverlayHandleInvalid;
			handle_l3 = OpenVR.k_ulOverlayHandleInvalid;
			handle_l4 = OpenVR.k_ulOverlayHandleInvalid;
			handle_l5 = OpenVR.k_ulOverlayHandleInvalid;
		}
		
		Editor.DestroyImmediate(center);
		Editor.DestroyImmediate(r1);
		Editor.DestroyImmediate(r2);
		Editor.DestroyImmediate(r3);
		Editor.DestroyImmediate(r4);
		Editor.DestroyImmediate(r5);

		Editor.DestroyImmediate(l1);
		Editor.DestroyImmediate(l2);
		Editor.DestroyImmediate(l3);
		Editor.DestroyImmediate(l4);
		Editor.DestroyImmediate(l5);

		VR_Overlay.instance = null;
	}

	public void Update()
	{
		var overlay = OpenVR.Overlay;

		if (overlay == null)
			return;

		updateOverlay(handle, ref textureBounds, 0, 1);

		updateOverlay(handle_r1, ref textureBounds, 1, 1);
		updateOverlay(handle_r2, ref textureBounds, 2, 1);
		updateOverlay(handle_r3, ref textureBounds, 3, 1);
		updateOverlay(handle_r4, ref textureBounds, 4, 1);
		updateOverlay(handle_r5, ref textureBounds, 5, 1);

		updateOverlay(handle_l1, ref textureBounds, 1, -1);
		updateOverlay(handle_l2, ref textureBounds, 2, -1);
		updateOverlay(handle_l3, ref textureBounds, 3, -1);
		updateOverlay(handle_l4, ref textureBounds, 4, -1);
		updateOverlay(handle_l5, ref textureBounds, 5, -1);




		UpdateTransform();
	}
	
	
	public void UpdateTransform()
	{

		var overlay = OpenVR.Overlay;

		if (overlay == null)
			return;
		
	
		if (true/*Visible*/)
		{
			

			Vector3 positionDestination;
			Quaternion rotationDestination;

			if (m_zoom)
			{
				positionDestination = m_positionZoomed;
				rotationDestination = m_rotationZoomed;
			}
			else
			{
				positionDestination = m_positionNormal;
				rotationDestination = m_rotationNormal;
			}

			if (center.transform.position != positionDestination)
				center.transform.position = positionDestination;


			if (center.transform.rotation != rotationDestination)
				center.transform.rotation =  rotationDestination;

		}

		setOverlayTransform(handle,ref center);

		setOverlayTransform(handle_r1, ref r1);
		setOverlayTransform(handle_r2, ref r2);
		setOverlayTransform(handle_r3, ref r3);
		setOverlayTransform(handle_r4, ref r4);
		setOverlayTransform(handle_r5, ref r5);

		setOverlayTransform(handle_l1, ref l1);
		setOverlayTransform(handle_l2, ref l2);
		setOverlayTransform(handle_l3, ref l3);
		setOverlayTransform(handle_l4, ref l4);
		setOverlayTransform(handle_l5, ref l5);

	}
	
	private void orientOverlayLocal(Transform overlay_transform,  float sign)
	{
		float overlay_length = scale / numOverlays;		
		overlay_transform.localPosition = new Vector3(sign * overlay_length, 0, 0);
		overlay_transform.RotateAround(new Vector3(sign * overlay_length / 2f, 0, 0), new Vector3(0, 1, 0), sign * angle);
	}

	private void updateOverlay(ulong handle, ref VRTextureBounds_t textureBounds, int shift, float sign)
	{
		
		var overlay = OpenVR.Overlay;

		if (overlay == null)
			return;

		if (texture != null)
		{
			var error = overlay.ShowOverlay(handle);

			if (error == EVROverlayError.InvalidHandle || error == EVROverlayError.UnknownOverlay)
			{
				if (overlay.FindOverlay(key, ref handle) != EVROverlayError.None)
					return;
			}


			var tex = new Texture_t();
			tex.handle = texture.GetNativeTexturePtr();
			//tex.eType = SteamVR.instance.textureType;
			tex.eColorSpace = EColorSpace.Auto;

			overlay.SetOverlayTexture(handle, ref tex);
			overlay.SetOverlayAlpha(handle, alpha);
			overlay.SetOverlayWidthInMeters(handle, scale / numOverlays);
			//overlay.SetOverlayAutoCurveDistanceRangeInMeters(handle, curvedRange.x, curvedRange.y);

			textureBounds = new VRTextureBounds_t();
			textureBounds.uMin = (0 + uvOffset.x) * uvOffset.z + 0.5f +shift*sign /numOverlays -1f/(2f * numOverlays);
			textureBounds.vMin = (1 + uvOffset.y) * uvOffset.w; 
			textureBounds.uMax = (0 + uvOffset.x) * uvOffset.z + 0.5f + shift * sign / numOverlays + 1f / (2f * numOverlays);
			textureBounds.vMax = (0 + uvOffset.y) * uvOffset.w;
			// Account for textures being upside-down in Unity.
			textureBounds.vMin = 1.0f - textureBounds.vMin;
			textureBounds.vMax = 1.0f - textureBounds.vMax;
			overlay.SetOverlayTextureBounds(handle, ref textureBounds);
			overlay.SetOverlayInputMethod(handle, inputMethod);
		}else
		{
			overlay.HideOverlay(handle);
		}

	}

	private void setOverlayTransform(ulong handle, ref GameObject overlay_object)
	{
		var overlay = OpenVR.Overlay;
		SteamVR_Utils.RigidTransform rigid_transform = new SteamVR_Utils.RigidTransform(overlay_object.transform);
		var t = rigid_transform.ToHmdMatrix34();
		overlay.SetOverlayTransformAbsolute(handle, ETrackingUniverseOrigin.TrackingUniverseStanding, ref t);
	}

	public void ZoomIn(Transform vr_cam_head)
	{
		m_zoom = true;
		distance -= zoom_step;
		m_positionZoomed = vr_cam_head.position + vr_cam_head.rotation * new Vector3(0, 0, distance);
		m_rotationZoomed = vr_cam_head.rotation;
	}

	public void ZoomOut(Transform vr_cam_head)
	{
		m_zoom = true;
		distance += zoom_step;
		m_positionZoomed = vr_cam_head.position + vr_cam_head.rotation * new Vector3(0, 0, distance);
		m_rotationZoomed = vr_cam_head.rotation;
	}

	public void reposition(Transform vr_head)
	{
		m_zoom = false;
		m_positionNormal = vr_head.position + vr_head.rotation * new Vector3(0, -0.375f, distance);
		m_rotationNormal = vr_head.rotation;
	}

}

