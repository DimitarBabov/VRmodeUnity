using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;

using System.Linq;
using System.Text;


public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

//[ExecuteInEditMode]
public class MYWMListener 
{
	public IntPtr interactionWindow;
	IntPtr hMainWindow;
	IntPtr oldWndProcPtr;
	IntPtr newWndProcPtr;
	WndProcDelegate newWndProc;
	bool isFrameGeneratorRunning = false;

	[DllImport("user32.dll")]
	static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

	[DllImport("user32.dll")]
	static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	private static extern System.IntPtr GetActiveWindow();


	void Start()
	{
		

		if (isFrameGeneratorRunning)
			return;

		hMainWindow = GetActiveWindow();
		newWndProc = new WndProcDelegate(wndProc);
		newWndProcPtr = Marshal.GetFunctionPointerForDelegate(newWndProc);
		oldWndProcPtr = SetWindowLongPtr(hMainWindow, -4, newWndProcPtr);
		isFrameGeneratorRunning = true;

		HackStart();
	}

	
	IntPtr wndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		Debug.Log(msg);
		return CallWindowProc(oldWndProcPtr, hWnd, msg, wParam, lParam);
	}
	
	void OnDisable()
	{
		HackStop();
		Debug.Log("Uninstall Hook");
		if (!isFrameGeneratorRunning) return;
		SetWindowLongPtr(hMainWindow, -4, oldWndProcPtr);
		hMainWindow = IntPtr.Zero;
		oldWndProcPtr = IntPtr.Zero;
		newWndProcPtr = IntPtr.Zero;
		newWndProc = null;
		isFrameGeneratorRunning = false;
	}

	
	static MYWMListener CreateInstance()
	{
		return new MYWMListener();
	}

	private System.Diagnostics.Process m_process = null;

	

	public void HackStart()
	{
		HackStop();

		string exePath = "Assets\\MyEditor\\Hack\\VrDesktopMirrorWorkaround.exe";
		if (System.IO.File.Exists(exePath))
		{
			m_process = new System.Diagnostics.Process();
			m_process.StartInfo.FileName = exePath;
			m_process.StartInfo.CreateNoWindow = true;
			m_process.StartInfo.UseShellExecute = true;
			m_process.StartInfo.Arguments = hMainWindow.ToString();
			m_process.Start();
		}
		else
		{
			Debug.Log("VR Desktop Mirror Hack exe not found: " + exePath);
		}
	}

	public void HackStop()
	{


		if (m_process != null)
		{
			if (m_process.HasExited == false)
			{
				m_process.Kill();
			}
		}
		m_process = null;
	}

	public static MYWMListener instance;

	[MenuItem("VR Mode/wnd_proc test %a", false)]
	static void EditorVR()
	{
		if (instance == null)
		{
			instance = CreateInstance();
			instance.Start();
			return;
		}else
		{
			instance.OnDisable();
			instance = null;
		}
	}
}
