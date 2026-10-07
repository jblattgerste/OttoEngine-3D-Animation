using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace OttoEngine
{
    /// <summary>
    /// Orbits the camera around the engine. Mouse: drag to rotate, scroll to zoom, double-click to reset.
    /// Touch: drag to rotate, pinch to zoom, double-tap to reset. Input that starts on the UI is ignored.
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        [Tooltip("Point the camera orbits around and looks at.")]
        public Vector3 target = new Vector3(0f, 0.2f, 0f);
        public float distance = 1.25f, minDistance = 0.45f, maxDistance = 2.6f;
        [Tooltip("Degrees around the engine; 0 looks straight at the cut face.")]
        public float yaw = 20f;
        public float pitch = 12f, minPitch = -5f, maxPitch = 75f;
        public float rotateSpeed = 0.25f, scrollZoomSpeed = 0.0015f, smoothing = 12f;
        [Tooltip("Moves the engine on screen (fractions of the screen) so the caption panel does not cover it: on wide " +
                 "screens to the right and up, on tall screens up.")]
        public Vector2 landscapeShift = new Vector2(0.12f, 0.06f), portraitShift = new Vector2(0f, 0.03f);

        private float startYaw, startPitch, startDistance, currentYaw, currentPitch, currentDistance;
        private bool dragging;
        private float lastClick = -1f;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private Camera cam;

        private void OnEnable() => EnhancedTouchSupport.Enable();
        private void OnDisable() => EnhancedTouchSupport.Disable();

        private void Start()
        {
            (startYaw, startPitch, startDistance) = (yaw, pitch, distance);
            (currentYaw, currentPitch, currentDistance) = (yaw, pitch, distance);
            Apply();
        }

        /// <summary>Back to the starting view.</summary>
        public void ResetView() => (yaw, pitch, distance) = (startYaw, startPitch, startDistance);

        private void Update()
        {
            var touches = Touch.activeTouches;
            if (touches.Count > 0) HandleTouches(touches);
            else HandleMouse();

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            var k = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            currentYaw = Mathf.Lerp(currentYaw, yaw, k);
            currentPitch = Mathf.Lerp(currentPitch, pitch, k);
            currentDistance = Mathf.Lerp(currentDistance, distance, k);
            Apply();
        }

        private void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            var position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                dragging = !OverUI(position);
                if (dragging && Time.unscaledTime - lastClick < 0.3f) ResetView();
                lastClick = Time.unscaledTime;
            }
            if (mouse.leftButton.wasReleasedThisFrame) dragging = false;
            if (dragging) Rotate(mouse.delta.ReadValue());
            var scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f && !OverUI(position)) distance *= Mathf.Exp(-Mathf.Clamp(scroll, -400f, 400f) * scrollZoomSpeed);
        }

        private void HandleTouches(UnityEngine.InputSystem.Utilities.ReadOnlyArray<Touch> touches)
        {
            if (touches.Count == 1)
            {
                var touch = touches[0];
                if (touch.phase == TouchPhase.Began)
                {
                    dragging = !OverUI(touch.screenPosition);
                    if (dragging && touch.tapCount == 2) ResetView();
                }
                if (dragging && touch.phase == TouchPhase.Moved) Rotate(touch.delta);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) dragging = false;
                return;
            }
            dragging = false;
            var a = touches[0];
            var b = touches[1];
            var now = Vector2.Distance(a.screenPosition, b.screenPosition);
            var before = Vector2.Distance(a.screenPosition - a.delta, b.screenPosition - b.delta);
            if (now > 1f && before > 1f && !OverUI((a.screenPosition + b.screenPosition) * 0.5f)) distance *= before / now;
        }

        private void Rotate(Vector2 delta)
        {
            yaw += delta.x * rotateSpeed;
            pitch -= delta.y * rotateSpeed;
        }

        private bool OverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPosition }, uiHits);
            return uiHits.Count > 0;
        }

        private void LateUpdate()
        {
            // off-axis projection: shifts the image without changing what the camera orbits around
            if (cam == null) cam = GetComponent<Camera>();
            cam.ResetProjectionMatrix();
            var shift = cam.aspect >= 1f ? landscapeShift : portraitShift;
            var projection = cam.projectionMatrix;
            projection[0, 2] = -2f * shift.x;
            projection[1, 2] = -2f * shift.y;
            cam.projectionMatrix = projection;
        }

        private void Apply()
        {
            // yaw 0 = in front of the cut face (the engine's +z side), looking at it
            var rotation = Quaternion.Euler(currentPitch, 180f + currentYaw, 0f);
            transform.SetPositionAndRotation(target - rotation * Vector3.forward * currentDistance, rotation);
        }
    }
}
