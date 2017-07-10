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
	private GameObject gameobject;
	public Transform transform;
	public bool curved = true;
	public bool antialias = true;
	public bool highquality = true;
	public float scale = 3.0f;			// size of overlay view
	public float distance = 1.25f;		// distance from surface
	public float alpha = 1.0f;          // opacity 0..1
	public float zoom_step = 0.02f;

	public Vector4 uvOffset = new Vector4(0, 0, 1, 1);
	public Vector2 mouseScale = new Vector2(1, 1);
	public Vector2 curvedRange = new Vector2(1, 2);

	private bool m_zoom = false;
	private bool m_zoomWithFollowCursor = false;
	private Vector3 m_positionNormal;
	private Quaternion m_rotationNormal;
	private Vector3 m_positionZoomed;
	private Quaternion m_rotationZoomed;

	private float m_positionAnimationStart = 0;

	// Keyboard and Mouse
	private float m_lastShowClickStart = 0;


	public VROverlayInputMethod inputMethod = VROverlayInputMethod.None;

	static public VR_Overlay instance { get; private set; }

	private string key;
	private int update_count = 0;
	

	private ulong handle = OpenVR.k_ulOverlayHandleInvalid;

	public void Create(string overlay_key, string overlay_name)
	{
		gameobject = new GameObject("ModeVR Overlay");
		transform = gameobject.transform;

		key = overlay_key;
		var overlay = OpenVR.Overlay;
		if (overlay != null)
		{
			var error = overlay.CreateOverlay(key, overlay_name /*gameObject.name*/, ref handle);
			if (error != EVROverlayError.None)
			{
				Debug.Log(overlay.GetOverlayErrorNameFromEnum(error));
				//enabled = false;
				return;
			}
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
			}

			handle = OpenVR.k_ulOverlayHandleInvalid;
		}
		Editor.DestroyImmediate(gameobject);
		VR_Overlay.instance = null;
	}

	public void UpdateOverlay()
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
			overlay.SetOverlayWidthInMeters(handle, scale);
			overlay.SetOverlayAutoCurveDistanceRangeInMeters(handle, curvedRange.x, curvedRange.y);

			var textureBounds = new VRTextureBounds_t();
			textureBounds.uMin = (0 + uvOffset.x) * uvOffset.z;
			textureBounds.vMin = (1 + uvOffset.y) * uvOffset.w;
			textureBounds.uMax = (1 + uvOffset.x) * uvOffset.z;
			textureBounds.vMax = (0 + uvOffset.y) * uvOffset.w;

			// Account for textures being upside-down in Unity.
			textureBounds.vMin = 1.0f - textureBounds.vMin;
			textureBounds.vMax = 1.0f - textureBounds.vMax;

			overlay.SetOverlayTextureBounds(handle, ref textureBounds);
						

			var vecMouseScale = new HmdVector2_t();
			vecMouseScale.v0 = mouseScale.x;
			vecMouseScale.v1 = mouseScale.y;
			overlay.SetOverlayMouseScale(handle, ref vecMouseScale);



			UpdateTransform();

			overlay.SetOverlayInputMethod(handle, inputMethod);

			if (curved || antialias)
				highquality = true;

			if (highquality)
			{
				overlay.SetHighQualityOverlay(handle);
				overlay.SetOverlayFlag(handle, VROverlayFlags.Curved, curved);
				overlay.SetOverlayFlag(handle, VROverlayFlags.RGSS4X, antialias);
			}
			else if (overlay.GetHighQualityOverlay() == handle)
			{
				overlay.SetHighQualityOverlay(OpenVR.k_ulOverlayHandleInvalid);
			}
		}
		else
		{
			overlay.HideOverlay(handle);
		}
	
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

			if (transform.position != positionDestination)
				transform.position = positionDestination;

			if (transform.rotation != rotationDestination)
				transform.rotation =  rotationDestination;

		}

	


	SteamVR_Utils.RigidTransform rigid_transform = new SteamVR_Utils.RigidTransform(transform);
	//rigid_transform.pos.z += distance;
	var t = rigid_transform.ToHmdMatrix34();
	overlay.SetOverlayTransformAbsolute(handle, ETrackingUniverseOrigin.TrackingUniverseStanding, ref t);
		update_count++;
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
		m_positionNormal = vr_head.position + vr_head.rotation * new Vector3(0, 0, distance);
		m_rotationNormal = vr_head.rotation;
	}

}

