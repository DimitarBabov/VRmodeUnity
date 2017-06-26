// Name this script "RotateAtPoint"
using UnityEngine;
[ExecuteInEditMode]
public class RotateAtPoint : MonoBehaviour
{
	public Quaternion rot = Quaternion.identity;
	public void Update()
	{
		transform.rotation = rot;
	}

	private void OnDrawGizmos()
	{

	}
}