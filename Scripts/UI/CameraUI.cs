using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraUI : MonoBehaviour
{
    [Tooltip("Movement speed in units per second.")]
    public float speed = 5f;
    [Tooltip("Multiplier while holding Shift.")]
    public float sprintMultiplier = 2f;
    [Tooltip("Enable smoothing.")]
    public bool smooth = true;
    [Tooltip("Smoothing time in seconds.")]
    public float smoothTime = 0.08f;

    [Header("Zoom")]
    [Tooltip("Zoom speed multiplier for mouse wheel.")]
    public float zoomSpeed = 40f;
    [Tooltip("Minimum zoom (orthographic size or FOV).")]
    public float minZoom = 2f;
    [Tooltip("Maximum zoom (orthographic size or FOV).")]
    public float maxZoom = 60f;
    [Tooltip("Enable smoothing for zoom.")]
    public bool zoomSmooth = true;
    [Tooltip("Smoothing time for zoom in seconds.")]
    public float zoomSmoothTime = 0.08f;

    Camera cam;
    float zoomVelocity;

    Vector3 velocity;

    void Update()
    {
        // Use the new Input System: keyboard (WASD / arrows) and optional gamepad left stick
        Vector2 input = Vector2.zero;

        // Gamepad input (left stick)
        if (Gamepad.current != null)
        {
            input += Gamepad.current.leftStick.ReadValue();
        }

        // Keyboard input
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
        }

        // Clamp diagonal inputs to unit length
        if (input.sqrMagnitude > 1f) input = input.normalized;

        bool sprint = false;
        if (kb != null)
            sprint = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        else if (Gamepad.current != null)
            sprint = Gamepad.current.rightShoulder.isPressed || Gamepad.current.leftShoulder.isPressed;

        float currentSpeed = speed * (sprint ? sprintMultiplier : 1f);

        Vector3 delta = new Vector3(input.x, input.y, 0f) * currentSpeed * Time.deltaTime;
        Vector3 target = transform.position + delta;

        if (smooth)
            transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
        else
            transform.position = target;

        MouseWheelZoom();
        // Mouse-wheel zoom (new Input System)
    }

    private void MouseWheelZoom()
    {
        // If mouse over ui element, ignore zoom
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        // If mouse not in game window, ignore zoom
        if (!Application.isFocused)
            return;
            
        float scrollY = 0f;
        if (Mouse.current != null)
        {
            // Mouse.current.scroll is a Vector2 (x, y) — we use y for vertical wheel
            scrollY = Mouse.current.scroll.ReadValue().y;
        }

        if (Mathf.Abs(scrollY) > 0.0001f)
        {
            if (cam == null) cam = GetComponent<Camera>();

            if (cam != null)
            {
                if (cam.orthographic)
                {
                    float targetZoom = cam.orthographicSize - scrollY * zoomSpeed * Time.deltaTime;
                    targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
                    if (zoomSmooth)
                        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref zoomVelocity, zoomSmoothTime);
                    else
                        cam.orthographicSize = targetZoom;
                }
                else
                {
                    float targetFov = cam.fieldOfView - scrollY * zoomSpeed * Time.deltaTime;
                    targetFov = Mathf.Clamp(targetFov, minZoom, maxZoom);
                    if (zoomSmooth)
                        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFov, ref zoomVelocity, zoomSmoothTime);
                    else
                        cam.fieldOfView = targetFov;
                }
            }
        
        }
    }
}