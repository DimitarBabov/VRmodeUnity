using UnityEngine;

using Battlehub.RTCommon;
using UnityEngine.UI;

namespace Battlehub.RTGizmos
{
    //[ExecuteInEditMode]
    public class Demo : MonoBehaviour
    {
        [SerializeField]
        private Camera SceneCamera;

        [SerializeField]
        private GameObject[] Objects;

        [SerializeField]
        private Text Text;

        private void Awake()
        {
            if(SceneCamera == null)
            {
                SceneCamera = Camera.main;
            }

            if (GLRenderer.Instance == null)
            {
                GameObject glRenderer = new GameObject();
                glRenderer.name = "GLRenderer";
                glRenderer.AddComponent<GLRenderer>();
            }

            if (SceneCamera != null)
            {
                if (!SceneCamera.GetComponent<GLCamera>())
                {
                    SceneCamera.gameObject.AddComponent<GLCamera>();
                }
            }

            SpriteGizmoManager.OnlyExposedToEditorObjects = false;
            RuntimeEditorApplication.IsOpened = true;

            Arrange();   
        }

        private void Arrange()
        {
            float angle = 0;
            float deltaAngle = 360 / Mathf.Max(Objects.Length, 1);
            float radius = 10;
            for(int i = 0; i < Objects.Length; ++i)
            {
                Vector3 position = Quaternion.AngleAxis(angle, Vector3.up) * (Vector3.forward * radius);
                angle += deltaAngle;

                position.y = Objects[i].transform.position.y;

                Objects[i].transform.position = position;
            }
        }

        private Quaternion m_targetRotation = Quaternion.identity;
        private float m_targetAngle = 0;
        private int m_index = 0;
        private void Update()
        {
            float deltaAngle = 360 / Mathf.Max(Objects.Length, 1);
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                m_targetAngle -= deltaAngle;
                m_targetRotation = Quaternion.Euler(0, m_targetAngle, 0);
                m_index--;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                m_targetAngle += deltaAngle;
                m_targetRotation = Quaternion.Euler(0, m_targetAngle, 0);
                m_index++;
            }

            if(m_index < 0)
            {
                m_index = Objects.Length - 1;
            }
            else if(m_index >= Objects.Length)
            {
                m_index = 0;
            }

            BaseGizmo gizmo = Objects[m_index].GetComponentInChildren<BaseGizmo>();
            Text.text = gizmo.GetType().Name;
        }

        private void FixedUpdate()
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, m_targetRotation, Time.deltaTime * 5);
        }
    }

}
