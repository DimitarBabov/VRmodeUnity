//#//if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections.Generic;
using System.Linq;
//using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
//using UnityEngine.InputNew;
using System.IO;
using UnityObject = UnityEngine.Object;

class EditingContextManager : MonoBehaviour
	{
		static EditingContextManager()
		{
			VRView.viewEnabled += OnVRViewEnabled;
			VRView.viewDisabled += OnVRViewDisabled;
		}

		static void OnVRViewEnabled()
		{
			//InitializeInputManager();
			//s_Instance = ObjectUtils.CreateGameObjectWithComponent<EditingContextManager>();
		}

		static void OnVRViewDisabled()
		{
			//ObjectUtils.Destroy(s_Instance.gameObject);
			//ObjectUtils.Destroy(s_InputManager.gameObject);
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

		

		void OnEnable()
		{
		

			//ISetEditingContextMethods.getAvailableEditingContexts = GetAvailableEditingContexts;
			//ISetEditingContextMethods.getPreviousEditingContexts = GetPreviousEditingContexts;
			//ISetEditingContextMethods.setEditingContext = SetEditingContext;
			//ISetEditingContextMethods.restorePreviousEditingContext = RestorePreviousContext;
			
			//var availableContexts = GetAvailableEditingContexts();
			//m_ContextNames = availableContexts.Select(c => c.name).ToArray();

			//SetEditingContext(defaultContext);

		
				VRView.afterOnGUI += OnVRViewGUI;
		}

		void OnDisable()
		{
			
		}

		void OnVRViewGUI(EditorWindow window)
		{
			var view = (VRView)window;
			GUILayout.BeginArea(view.guiRect);
			{
				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				{
					//DoGUI(m_ContextNames, ref m_SelectedContextIndex, () => SetEditingContext(m_AvailableContexts[m_SelectedContextIndex]));
					GUILayout.FlexibleSpace();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();
		}

		internal static void DoGUI(string[] contextNames, ref int selectedContextIndex, Action callback = null)
		{
			selectedContextIndex = EditorGUILayout.Popup(string.Empty, selectedContextIndex, contextNames);
			if (GUI.changed)
			{
				if (callback != null)
					callback();
				GUIUtility.ExitGUI();
			}
		}
		
	}

