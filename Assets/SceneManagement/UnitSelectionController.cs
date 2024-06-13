using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static Bounds GetViewportBounds(Camera camera, Vector3 screenPosition1, Vector3 screenPosition2)
    {
        Vector3 v1 = Camera.main.ScreenToViewportPoint(screenPosition1);
        Vector3 v2 = Camera.main.ScreenToViewportPoint(screenPosition2);
        Vector3 min = Vector3.Min(v1, v2);
        Vector3 max = Vector3.Max(v1, v2);
        min.z = camera.nearClipPlane;
        max.z = camera.farClipPlane;

        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    public static Rect GetScreenRect(Vector3 screenPosition1, Vector3 screenPosition2)
    {
        // Move origin from bottom left to top left
        screenPosition1.y = Screen.height - screenPosition1.y;
        screenPosition2.y = Screen.height - screenPosition2.y;
        // Calculate corners
        Vector3 topLeft = Vector3.Min(screenPosition1, screenPosition2);
        Vector3 bottomRight = Vector3.Max(screenPosition1, screenPosition2);
        // Create Rect
        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    public static void DrawScreenRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    public static void DrawScreenRectBorder(Rect rect, float thickness, Color color)
    {
        // Top
        Utils.DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
        // Left
        Utils.DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
        // Right
        Utils.DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        // Bottom
        Utils.DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
    }
}


public class UnitSelectionController : MonoBehaviour
{
    private PoliceOfficer throwingOfficer = null;
    public GameObject flashbangPrefab;

    private bool flipFormation;
    public float selectionPlaneHeight;
    Plane plane = new Plane(Vector3.up, 0.1f);
    private LineRenderer lineRenderer;

    public GameObject positionIndicatorPrefab;
    public GameObject vehiclePositionIndicatorPrefab;
    private List<GameObject> _positionIndicators = new List<GameObject>();

    private Vector3 _startLinePosition;

    public LayerMask policeLayer;
    private Vector3 _startMousePosition;
    private Vector3 lastMousePosition;
    private Vector3 endWorldPosition;
    private bool _isSelecting;

    public float minRowDistance;
    public float minVehicleRowDistance;

    private List<PoliceOfficer> _selectedUnits = new List<PoliceOfficer>();
    private List<PoliceOfficer> _previouslySelectedUnits = new List<PoliceOfficer>();

    // Add a list for selected vehicles
    private List<CarControl> _selectedVehicles = new List<CarControl>();

    void Start()
    {
        // Initialize LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();

        // Set some properties of the LineRenderer
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.yellow;

        // Set the position count to 2 since we're drawing a line
        lineRenderer.positionCount = 2;

        flipFormation = false;
    }

    void UpdateLine(Vector3 startPos, Vector3 endPos)
    {
        // Convert the screen points to world points
        var startWorldPosition = Camera.main.ScreenToWorldPoint(startPos);
        var endWorldPosition = Camera.main.ScreenToWorldPoint(endPos);

        /*        // Adjust the Z coordinate of the positions
                startWorldPosition.z = -5; // change this value as needed
                endWorldPosition.z = -5; // change this value as needed*/

        // Set the positions of the LineRenderer to the start and end positions
        lineRenderer.SetPosition(0, startWorldPosition);
        lineRenderer.SetPosition(1, endWorldPosition);
    }

    void ClearLine()
    {
        // Set the position count to 0 to hide the line
        lineRenderer.positionCount = 0;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && Input.GetMouseButton(1))
        {
            flipFormation = true;
        }

        if (Input.GetKeyDown(KeyCode.B)) // Flashbang code
        {
            if (_selectedUnits.Count == 1)
            {
                // If only one officer is selected, choose that officer
                throwingOfficer = _selectedUnits[0];
            }
            else if (_selectedUnits.Count > 1)
            {
                // If multiple officers are selected, find the nearest to the mouse cursor
                throwingOfficer = FindNearestOfficerToCursor(_selectedUnits);
            }
        }

        if (throwingOfficer != null && _selectedUnits.Count > 1)
        {
            throwingOfficer = FindNearestOfficerToCursor(_selectedUnits);
        }

        // If we press the left mouse button, save mouse location and begin selection
        if (Input.GetMouseButtonDown(0) && throwingOfficer == null)
        {
            _isSelecting = true;
            _startMousePosition = Input.mousePosition;

            // Reset the outline width of previously selected units
            foreach (var unit in _previouslySelectedUnits)
            {
                unit.GetComponent<JumpFloodOutlineRenderer>().outlineColor = Color.white;
                unit.GetComponent<JumpFloodOutlineRenderer>().outlinePixelWidth = 0;
            }
            _previouslySelectedUnits.Clear();

            // Clear selected units list
            _selectedUnits.Clear();
            _selectedVehicles.Clear(); // Clear selected vehicles list
        }
        // If we let go of the left mouse button, end selection
        else if (Input.GetMouseButtonUp(0) && throwingOfficer == null)
        {
            _isSelecting = false;

            // Add the newly selected units to the previouslySelectedUnits list
            _previouslySelectedUnits.AddRange(_selectedUnits);
            foreach (var unit in _previouslySelectedUnits)
            {
                unit.GetComponent<JumpFloodOutlineRenderer>().outlineColor = Color.yellow;
                unit.GetComponent<JumpFloodOutlineRenderer>().outlinePixelWidth = 2;
            }
        }

        // Instantiate and throw flashbang on mouse click if an officer is selected for throwing
        if (throwingOfficer != null && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Instantiate the flashbang and set its trajectory
                GameObject flashbang = Instantiate(flashbangPrefab, throwingOfficer.transform.position, Quaternion.identity);
                ThrowFlashbang(flashbang, hit.point);    // Add logic to apply force or set the direction for the flashbang
                throwingOfficer = null; // Reset the throwing officer
            }
        }

        // Update selection logic to include vehicles
        if (_isSelecting)
        {
            SelectUnits<PoliceOfficer>(_selectedUnits);
            SelectUnits<CarControl>(_selectedVehicles);
        }

        // If we press the right mouse button and we have selected units, save mouse location as the start position of the line
        if (Input.GetMouseButtonDown(1) && (_selectedUnits.Count > 0 || _selectedVehicles.Count > 0))
        {
            float distance;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out distance))
            {
                _startLinePosition = ray.GetPoint(distance);
            }

            // Also add the initial point to the LineRenderer
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, _startLinePosition);
            lineRenderer.SetPosition(1, _startLinePosition);
        }
        // If we are holding the right mouse button and we have selected units, update the end position of the line and draw position indicators
        else if (Input.GetMouseButton(1) && (_selectedUnits.Count > 0 || _selectedVehicles.Count > 0))
        {
            if (Input.mousePosition != lastMousePosition)
            {
                foreach (var indicator in _positionIndicators)
                {
                    Destroy(indicator);
                }
                _positionIndicators.Clear();
                DrawLineAndPositionIndicators();
            }
        }
        else if (Input.GetMouseButtonUp(1))
        {
            if (_selectedUnits.Count > 0)
            {
                MoveUnitsToPositions(_selectedUnits, _positionIndicators);
            }

            if (_selectedVehicles.Count > 0)
            {
                MoveVehiclesToPositions(_selectedVehicles, _positionIndicators);
            }

            ClearAfterCommandIssued();
        }
    }

    // Method to clear selections and indicators after commands are issued
    private void ClearAfterCommandIssued()
    {
        // Clear position indicators
        foreach (var indicator in _positionIndicators)
        {
            Destroy(indicator);
        }
        _positionIndicators.Clear();

        // Clear the line renderer
        ClearLine();

        // Reset flipFormation if needed
        flipFormation = false;

        // You can also clear the selected units list if you want to deselect them after moving
        _selectedUnits.Clear();
        _selectedVehicles.Clear();

        // Reset selection effects on previously selected units
        foreach (var unit in _previouslySelectedUnits)
        {
            unit.GetComponent<JumpFloodOutlineRenderer>().outlineColor = Color.white;
            unit.GetComponent<JumpFloodOutlineRenderer>().outlinePixelWidth = 0;
        }
        _previouslySelectedUnits.Clear();
    }


    // Method to move Police Officers to positions
    private void MoveUnitsToPositions(List<PoliceOfficer> units, List<GameObject> indicators)
    {
        // Create a copy of the indicators list so we can modify it
        List<GameObject> availableIndicators = new List<GameObject>(indicators);

        foreach (var unit in units)
        {
            GameObject closestIndicator = null;
            float closestDistance = float.MaxValue;

            // Find the closest position indicator to the current unit
            foreach (var indicator in availableIndicators)
            {
                float distance = Vector3.Distance(unit.transform.position, indicator.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndicator = indicator;
                }
            }

            // Command the unit to move to the closest indicator's position and remove the indicator from availableIndicators
            if (closestIndicator != null)
            {
                unit.MoveToPosition(closestIndicator.transform.position, closestIndicator.transform.rotation);
                availableIndicators.Remove(closestIndicator);
            }
        }
    }


    // Generic method to select units
    private void SelectUnits<T>(List<T> selectedList) where T : MonoBehaviour
    {
        foreach (var selectableObject in FindObjectsOfType<T>())
        {
            var gameObject = selectableObject.gameObject;
            if (IsWithinSelectionBounds(gameObject))
            {
                if (!selectedList.Contains(selectableObject))
                {
                    selectedList.Add(selectableObject);
                    // Apply selection effects (e.g., outline) here
                    selectableObject.GetComponent<JumpFloodOutlineRenderer>().outlineColor = Color.yellow;
                    selectableObject.GetComponent<JumpFloodOutlineRenderer>().outlinePixelWidth = 2;
                }
            }
            else
            {
                if (selectedList.Contains(selectableObject))
                {
                    selectedList.Remove(selectableObject);
                    // Remove selection effects here
                    selectableObject.GetComponent<JumpFloodOutlineRenderer>().outlineColor = Color.white;
                    selectableObject.GetComponent<JumpFloodOutlineRenderer>().outlinePixelWidth = 0;
                }
            }
        }
    }

    private void MoveVehiclesToPositions(List<CarControl> vehicles, List<GameObject> indicators)
    {
        foreach (var vehicle in vehicles)
        {
            float closestDistance = float.MaxValue;
            Vector3 closestPosition = Vector3.zero;

            // Find the closest position indicator to the current vehicle
            foreach (var indicator in indicators)
            {
                float distance = Vector3.Distance(vehicle.transform.position, indicator.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPosition = indicator.transform.position;
                }
            }

            // Set the vehicle's goal to the closest indicator's position
            if (closestDistance != float.MaxValue)
            {
                // Assuming your CarControl script has a method to set the goal or destination
                vehicle.SetDestination(closestPosition);
            }
        }
    }

    private bool IsWithinSelectionBounds(GameObject gameObject)
    {
        if (!_isSelecting)
            return false;

        var camera = Camera.main;
        var viewportBounds = Utils.GetViewportBounds(camera, _startMousePosition, Input.mousePosition);

        return viewportBounds.Contains(camera.WorldToViewportPoint(gameObject.transform.position));
    }

    // Draw a box to visualize the selection
    private void OnGUI()
    {
        if (_isSelecting)
        {
            // Create a rect from both mouse positions
            var rect = Utils.GetScreenRect(_startMousePosition, Input.mousePosition);
            Utils.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
            Utils.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f));
        }
    }

    private void DrawLineAndPositionIndicators()
    {
        // Get current line end position
        var endLinePosition = Input.mousePosition;

        // Convert the screen points to world points
        var startWorldPosition = _startLinePosition;
        float distancetoplane;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out distancetoplane))
        {
            endWorldPosition = ray.GetPoint(distancetoplane);
        }

        // Calculate direction and distance between the start and end points
        var direction = (endWorldPosition - startWorldPosition).normalized;
        var distance = Vector3.Distance(startWorldPosition, endWorldPosition);

        // Draw unit lines and indicators
        int unitRows = DrawPositionIndicators(_selectedUnits.Count, minRowDistance, startWorldPosition, direction, distance, positionIndicatorPrefab);

        // If both units and vehicles are selected, start vehicle rows behind the last unit row
        Vector3 vehicleStartPos = startWorldPosition;
        if (_selectedUnits.Count > 0 && _selectedVehicles.Count > 0)
        {
            vehicleStartPos += Rotate90CW(direction) * (minRowDistance * 1.1f) * unitRows;
            vehicleStartPos += Rotate90CW(direction) * (minVehicleRowDistance * 1.1f);
        }

        // Draw vehicle lines and indicators
        DrawPositionIndicators(_selectedVehicles.Count, minVehicleRowDistance, vehicleStartPos, direction, distance, vehiclePositionIndicatorPrefab);
    }

    // Method to draw position indicators and return the number of rows created
    private int DrawPositionIndicators(int count, float rowDistance, Vector3 startPosition, Vector3 direction, float distance, GameObject prefab)
    {
        // Determine the maximum number of units/vehicles that can fit in a row
        var maxPerRow = Mathf.FloorToInt(distance / rowDistance);
        if (maxPerRow < 1)
        {
            maxPerRow = 1;
        }

        // Determine how many rows are needed
        var rows = Mathf.CeilToInt((float)count / maxPerRow);
        Vector3 rowdir = Rotate90CW(direction);
        if (flipFormation)
        {
            rowdir = -rowdir;
        }

        // Calculate perpendicular direction for the indicators' rotation
        Vector3 perpendicularDirection = Vector3.Cross(direction, Vector3.up).normalized;
        Quaternion indicatorRotation = Quaternion.LookRotation(perpendicularDirection);

        // Draw the lines and instantiate the position indicators
        for (int i = 0; i < rows; i++)
        {
            // Calculate the start position of the current row
            var rowStartWorldPosition = startPosition + rowdir * (rowDistance * 1.1f) * i;

            // Draw the line for the current row
            Debug.DrawLine(rowStartWorldPosition, rowStartWorldPosition + direction * distance, Color.yellow);
            if (i == 0)
            {
                lineRenderer.SetPosition(1, endWorldPosition);
            }

            // Determine how many units/vehicles will be in the current row
            var inRow = Mathf.Min(maxPerRow, count - i * maxPerRow);
            var unitDistance = rowDistance;



            // Calculate the distance between units/vehicles in the current row
            if (inRow == 1)
            {
                rowStartWorldPosition += direction * 0.5f * distance;
            }
            else
            {
                unitDistance = inRow > 1 ? distance / (inRow - 1) : 0;
            }

            // Instantiate the position indicators for the current row
            for (int j = 0; j < inRow; j++)
            {
                var position = rowStartWorldPosition + direction * unitDistance * j;
                var indicator = Instantiate(prefab, position, indicatorRotation);
                _positionIndicators.Add(indicator);
            }
        }

        return rows;
    }

    // clockwise
    Vector3 Rotate90CW(Vector3 aDir)
    {
        return new Vector3(aDir.z, 0, -aDir.x);
    }

    // counter clockwise
    Vector3 Rotate90CCW(Vector3 aDir)
    {
        return new Vector3(-aDir.z, 0, aDir.x);
    }

    private PoliceOfficer FindNearestOfficerToCursor(List<PoliceOfficer> officers)
    {
        PoliceOfficer nearestOfficer = null;
        float minDistance = float.MaxValue;
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        foreach (var officer in officers)
        {
            float distance = Vector3.Distance(mousePosition, officer.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestOfficer = officer;
            }
        }

        return nearestOfficer;
    }

    void ThrowFlashbang(GameObject flashbang, Vector3 targetPosition)
    {
        Rigidbody rb = flashbang.GetComponent<Rigidbody>();
        if (rb == null) return;


        float gravity = Physics.gravity.magnitude;
        Vector3 direction = targetPosition - flashbang.transform.position;
        float heightDifference = direction.y;
        direction.y = 0;
        float distance = direction.magnitude;
        direction.y = distance;
        distance += heightDifference;
        float velocity = Mathf.Sqrt(distance * gravity);
        float angle = Mathf.Asin(heightDifference / distance) * 0.5f;
        Vector3 velocityVector = Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, Vector3.Cross(Vector3.up, direction)) * direction.normalized * velocity;

        rb.AddForce(velocityVector * rb.mass, ForceMode.Impulse);
        Vector3 randomdir = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5));
        rb.AddTorque(randomdir);

    }

}