#define VDM_SteamVR

using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Xml;

public class VdmDesktop : MonoBehaviour
{
    [HideInInspector]
    public int Screen = 0;
    [HideInInspector]
    public int ScreenIndex = 0;

    [DllImport("user32.dll")]
    static extern void mouse_event(int dwFlags, int dx, int dy,
                      int dwData, int dwExtraInfo);

    [Flags]
    public enum MouseEventFlags
    {
        LEFTDOWN = 0x00000002,
        LEFTUP = 0x00000004,
        MIDDLEDOWN = 0x00000020,
        MIDDLEUP = 0x00000040,
        MOVE = 0x00000001,
        ABSOLUTE = 0x00008000,
        RIGHTDOWN = 0x00000008,
        RIGHTUP = 0x00000010
    }
    
    private VdmDesktopManager m_manager;
    //private LineRenderer m_line;
    private Renderer m_renderer;
    private MeshCollider m_collider;

    private bool m_zoom = false;
    private bool m_zoomWithFollowCursor = false;

    private Vector3 m_positionNormal;
    private Quaternion m_rotationNormal;
    private Vector3 m_positionZoomed;
    private Quaternion m_rotationZoomed;
    
    private float m_positionAnimationStart = 0;
    
    // Keyboard and Mouse
    private float m_lastShowClickStart = 0;

    void Start()
    {
        m_manager = transform.parent.GetComponent<VdmDesktopManager>();
       // m_line = GetComponent<LineRenderer>();
        m_renderer = GetComponent<Renderer>();
        m_collider = GetComponent<MeshCollider>();

        m_manager.Connect(this);

        Hide();
    }

    public void Update()
    {
        bool skip = false;
        if (Visible() == false)
            skip = true;
        
        if(skip == false)    
        {   
            float step = 0;
            if (Time.time - m_positionAnimationStart > 1)
                step = 1;
            else
                step = Time.time - m_positionAnimationStart;

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
                transform.position = Vector3.Lerp(transform.position, positionDestination, step);

            if (transform.rotation != rotationDestination)
                transform.rotation = Quaternion.Lerp(transform.rotation, rotationDestination, step);
            
        }
        
    }
    
    void OnEnable()
    {
    }

    void OnDisable()
    {
        m_manager.Disconnect(this);
    }

    public void HideLine()
    {
      //  m_line.enabled = false;
    }

    public void Hide()
    {
        m_renderer.enabled = false;
        m_collider.enabled = false;
    }

    public void Show()
    {
        m_renderer.enabled = true;
        m_collider.enabled = true;
      
    }

    public bool Visible()
    {
        return (m_renderer.enabled);
    }

    public void CheckKeyboardAndMouse()
    {
		

		if (Input.GetKeyDown(m_manager.KeyboardShow))
        {
            VdmDesktopManager.ActionInThisFrame = true;

            m_lastShowClickStart = Time.time;

            if (Visible() == false)
            {
                Show();
                m_lastShowClickStart -= 10; // Avoid quick show/close
            }
        }
         
        if (Input.GetKey(m_manager.KeyboardShow))
        {
            VdmDesktopManager.ActionInThisFrame = true;

            m_manager.KeyboardDistance += Input.GetAxisRaw("Mouse ScrollWheel");
            m_manager.KeyboardDistance = Mathf.Clamp(m_manager.KeyboardDistance, 0.2f, 100);

            m_positionNormal = Camera.main.transform.position + Camera.main.transform.rotation * new Vector3(0, 0, m_manager.KeyboardDistance);
            m_positionNormal += m_manager.MultiMonitorPositionOffset * ScreenIndex;
            m_rotationNormal = Camera.main.transform.rotation;            
        }

        if (Input.GetKeyUp(m_manager.KeyboardShow))
        {
            VdmDesktopManager.ActionInThisFrame = true;

            if (Time.time - m_lastShowClickStart < 0.5f)
            {
                m_lastShowClickStart = 0;

                Hide();
            }
            
        }

        if (m_manager.KeyboardZoom != KeyCode.None)
        {
            if (Input.GetKeyDown(m_manager.KeyboardZoom))
            {
                if (m_zoom == false)
                {
                    m_zoomWithFollowCursor = true;
                    ZoomIn();
                }
                else
                    ZoomOut();
            }

            if( (m_zoom) && (m_zoomWithFollowCursor) )
            {
                VdmDesktopManager.ActionInThisFrame = true;

                m_manager.KeyboardZoomDistance += Input.GetAxisRaw("Mouse ScrollWheel");
                m_manager.KeyboardZoomDistance = Mathf.Clamp(m_manager.KeyboardZoomDistance, 0.2f, 100);

                // Cursor position in world space
                Vector3 cursorPos = m_manager.GetCursorPos();
                cursorPos.x = cursorPos.x / m_manager.GetScreenWidth(Screen);
                cursorPos.y = cursorPos.y / m_manager.GetScreenHeight(Screen);
                cursorPos.y = 1 - cursorPos.y;
                cursorPos.x = cursorPos.x - 0.5f;
                cursorPos.y = cursorPos.y - 0.5f;
                cursorPos = transform.TransformPoint(cursorPos);
                
                Vector3 deltaCursor = transform.position - cursorPos;
                
                m_positionZoomed = Camera.main.transform.position + Camera.main.transform.rotation * new Vector3(0, 0, m_manager.KeyboardZoomDistance);
                m_positionZoomed += m_manager.MultiMonitorPositionOffset * ScreenIndex;
                m_rotationZoomed = Camera.main.transform.rotation;

                m_positionZoomed += deltaCursor;
            }
            
        }
    }


    public void ZoomIn()
    {
        m_positionAnimationStart = Time.time;
        m_zoom = true;
    }

    public void ZoomOut()
    {
        m_positionAnimationStart = Time.time;
        
        m_zoom = false;
    }
    
    public void ReInit(Texture2D tex, int width, int height)
    {
        GetComponent<Renderer>().material.mainTexture = tex;
        GetComponent<Renderer>().material.mainTexture.filterMode = m_manager.TextureFilterMode;
        GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(1, -1));

        float sx = width;
        float sy = height;
        sx = sx * m_manager.ScreenScaleFactor;
        sy = sy * m_manager.ScreenScaleFactor;
        transform.localScale = new Vector3(sx, sy, 1);
        
    }
    
}