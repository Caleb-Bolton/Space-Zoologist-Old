using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    private const float deadzone = 0.01f;

    public Camera Cam
    {
        get
        {
            if(!cam)
            {
                // Try to get the main camera
                cam = Camera.main;

                // If the camera still was not found, then through a missing reference exception
                if (!cam) throw new MissingComponentException($"{nameof(CameraController)}: " +
                    $"no camera found with the 'MainCamera' tag"); 
            }
            return cam;
        }
    }
    private bool MoveEnabled => UIBlockerSettings.OperationIsAvailable("CameraMove");
    private bool ZoomEnabled => UIBlockerSettings.OperationIsAvailable("CameraZoom");
    private bool DragEnabled => UIBlockerSettings.OperationIsAvailable("CameraDrag");

    [SerializeField] float WASDSpeed = 0.5f;
    [SerializeField] float WASDAcceleration = 0.75f;
    [SerializeField] private float zoomLerpSpeed = 5f;
    [SerializeField] private float ZoomFactor = 0.8f;
    [SerializeField] bool EdgeMovement = false;
    [SerializeField] private float edgeSpeed = 5f;
    [SerializeField] private float edgeBoundary = 10f;
    [SerializeField] private float zoomHeight = 10f;
    [SerializeField] private int MapWidth = default;
    [SerializeField] private int MapHeight = default;   

    private Camera cam = default;
    private Vector3 originalPosition;
    private float targetZoom;
    private Vector3 dragOrigin;
    private Vector3 oldPos;
    private Vector2 currentVelocity;
    // True when the camera is being dragged using the center mouse button
    private bool hasMouseDrag = false;

    private CameraPositionLock positionLock = null;

    #region Monobehaviour Messages
    void Start()
    {
        targetZoom = Cam.orthographicSize;
    }

    public void UpdateBounds(object serializedPlot)
    {
        var grid = ((SerializedPlot)serializedPlot).serializedGrid;
        MapWidth = grid.width;
        MapHeight = grid.height;
        transform.position = new Vector3(MapWidth / 2, MapHeight / 2, transform.position.z);
        originalPosition = transform.position;
    }

    void Update()
    {
        if(positionLock == null)
        {
            this.HandleKeyboard();
            this.HandleMouse();
            this.HandleZoom();
            if (this.EdgeMovement) this.HandleEdgeScreen();
        }
    }
    #endregion

    #region Public Methods
    public void Lock(CameraPositionLock positionLock)
    {
        // Unlock any current position lock
        if (this.positionLock != null) Unlock();

        this.positionLock = positionLock;
        positionLock.Lock(Cam);
    }
    public void Unlock()
    {
        if(positionLock != null)
        {
            positionLock.Unlock(Cam);
            positionLock = null;
        }
    }
    #endregion

    #region Private Methods
    private void HandleZoom()
    {
        // Only receive input if controls are enabled
        float scrollData = 0;
        if (ZoomEnabled) scrollData = Input.GetAxis("Mouse ScrollWheel");

        targetZoom -= scrollData * ZoomFactor;
        targetZoom = Mathf.Clamp(targetZoom, 2.5f, zoomHeight);
        Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, targetZoom, Time.deltaTime * zoomLerpSpeed);
    }

    private void HandleKeyboard()
    {
        float xValue = currentVelocity.x;
        float yValue = currentVelocity.y;
        float zoomScalar = targetZoom/zoomHeight; //Scale the camera's speed by the zoom amount

        float accleration = WASDAcceleration * zoomScalar * Time.deltaTime;
        float maxSpeed = WASDSpeed * zoomScalar;

        // Compute once in the frame instead of multiple times
        bool moveEnabled = MoveEnabled;

        if ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) && moveEnabled) //If pressing left and not yet reached maximum negative horizontal speed, decrease by acceleration
        {
            if(xValue > -maxSpeed)
                xValue -= accleration;
        }
        else if ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) && moveEnabled) //If pressing right and not yet reached maximum horizontal speed, increase by acceleration
        {
            if(xValue < maxSpeed)
                xValue += accleration;
        }
        else if(Mathf.Abs(xValue) <= deadzone) //If within deadzone, set velocity to 0
        {
            xValue = 0;
        }
        else //Otherwise, exert drag on the camera
        {
            xValue -= accleration * Mathf.Sign(xValue);
        }

        if ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && moveEnabled) //If pressing down and not yet reached maximum negative vertical speed, decrease by acceleration
        {
            if(yValue > -maxSpeed)
                yValue -= accleration;
        }
        else if ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) && moveEnabled) //If pressing up and not yet reached maximum vertical speed, increase by acceleration
        {
            if(yValue < maxSpeed)
                yValue += accleration;
        }
        else if(Mathf.Abs(yValue) <= deadzone) //If within deadzone, set velocity to 0
        {
            yValue = 0;
        }
        else //Otherwise, exert drag on the camera
        {
            yValue -= accleration * Mathf.Sign(yValue);
        }

        Vector3 attemptedPosition = new Vector3(transform.position.x + xValue, transform.position.y + yValue, -10);

        if (!GameManager.Instance.m_tilePlacementController.godMode)
        {
            if(attemptedPosition.x < 0 || attemptedPosition.x > MapWidth)
                xValue = 0;

            if(attemptedPosition.y < 0 || attemptedPosition.y > MapHeight)
                yValue = 0;
        }
        else
        {
            if (Input.GetKey(KeyCode.Z))
                transform.position = originalPosition;
        }

        this.transform.position = new Vector3(transform.position.x + xValue, transform.position.y + yValue, -10);

        currentVelocity = new Vector2(xValue, yValue);
    }

    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(2) && DragEnabled)
        {
            dragOrigin = Input.mousePosition;
            oldPos = this.transform.position;
            hasMouseDrag = true;
        }

        if (!Input.GetMouseButton(2)) hasMouseDrag = false;

        if(hasMouseDrag)
        {
            Vector3 displacement = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.ScreenToWorldPoint(dragOrigin);
            Vector3 newPosition = new Vector3(oldPos.x + displacement.x * -1, oldPos.y + displacement.y * -1, -10);
            if (!this.IsValidLocation(newPosition))
            {
                newPosition = ClampToValidLocation(newPosition);
            }
            this.transform.position = newPosition;
        }
    }

    private void HandleEdgeScreen()
    {
        float x = 0;
        float y = 0;
        if (Input.mousePosition.x > Screen.width - edgeBoundary)
        {
            x += this.edgeSpeed * Time.deltaTime;
        }
        if (Input.mousePosition.x < 0 + edgeBoundary)
        {
            x -= this.edgeSpeed * Time.deltaTime;
        }
        if (Input.mousePosition.y > Screen.height - edgeBoundary)
        {
            y += this.edgeSpeed * Time.deltaTime;
        }
        if (Input.mousePosition.y < 0 + edgeBoundary)
        {
            y -= this.edgeSpeed * Time.deltaTime;
        }
        Vector3 newPosition = new Vector3(this.transform.position.x + x, this.transform.position.y + y, this.transform.position.z);
        if (!this.IsValidLocation(newPosition))
        {
            return;
        }
        this.transform.position = newPosition;
    }

    private bool IsValidLocation(Vector3 newPosition)
    {
        return (newPosition.x >= 0 && newPosition.y >= 0
        && newPosition.x <= MapWidth && newPosition.y <= MapHeight);
    }

    private Vector3 ClampToValidLocation(Vector3 toClamp) {
        float x = Mathf.Clamp(toClamp.x, 0, MapWidth);
        float y = Mathf.Clamp(toClamp.y, 0, MapHeight);
        return new Vector3(x, y, toClamp.z);
    }
    #endregion
}
