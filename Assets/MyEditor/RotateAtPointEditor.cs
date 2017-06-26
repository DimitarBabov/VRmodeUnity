// Name this script "RotateAtPointEditor"
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RotateAtPoint))]
[CanEditMultipleObjects]
public class RotateAtPointEditor : Editor
{
	public void OnSceneGUI()
	{
		RotateAtPoint t = (target as RotateAtPoint);

		EditorGUI.BeginChangeCheck();
		Quaternion rot = Handles.RotationHandle(t.rot, Vector3.zero);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(target, "Rotated RotateAt Point");
			t.rot = rot;
			t.Update();
		}
	}
}
#endif