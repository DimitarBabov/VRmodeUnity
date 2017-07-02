﻿//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Displays 2d content on a large virtual screen.
//
//=============================================================================

using UnityEngine;
using Valve.VR;

public class VR_OverLayEditor 
{
	public Texture texture;
	public bool curved = true;
	public bool antialias = true;
	public bool highquality = true;
	public float scale = 3.0f;			// size of overlay view
	public float distance = 1.25f;		// distance from surface
	public float alpha = 1.0f;			// opacity 0..1

	public Vector4 uvOffset = new Vector4(0, 0, 1, 1);
	public Vector2 mouseScale = new Vector2(1, 1);
	public Vector2 curvedRange = new Vector2(1, 2);

	public VROverlayInputMethod inputMethod = VROverlayInputMethod.None;

	static public VR_OverLayEditor instance { get; private set; }

	static public string key { get { return "unity:" + Application.companyName + "." + Application.productName; } }

	private ulong handle = OpenVR.k_ulOverlayHandleInvalid;

	void Enable()
	{
		var overlay = OpenVR.Overlay;
		if (overlay != null)
		{
			var error = overlay.CreateOverlay(key, "VR Overlay", ref handle);
			if (error != EVROverlayError.None)
			{
				Debug.Log(overlay.GetOverlayErrorNameFromEnum(error));
				
				return;
			}
		}

		VR_OverLayEditor.instance = this;
	}

	void Disable()
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

		VR_OverLayEditor.instance = null;
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
			tex.eType = SteamVR.instance.textureType;
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

			
			var offset = new SteamVR_Utils.RigidTransform();
			offset.pos.z += distance;

			var t = offset.ToHmdMatrix34();
			overlay.SetOverlayTransformAbsolute(handle, VR_Render.instance.trackingSpace, ref t);
			

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

	
}
