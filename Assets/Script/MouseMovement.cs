using UnityEngine;
using UnityEngine.InputSystem;

public class MouseMovement : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float accelerationFactor = 5f;
    private float _currentSpeed = 0f;
    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        float zDistance = _mainCamera.WorldToScreenPoint(transform.position).z;
        Vector3 targetPos = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, zDistance));

        float distanceToTarget = Vector3.Distance(transform.position, targetPos);
        float acceleration = distanceToTarget * accelerationFactor;

        _currentSpeed += acceleration * Time.deltaTime;
        _currentSpeed = Mathf.Min(_currentSpeed, maxSpeed);

        transform.position = Vector3.MoveTowards(transform.position, targetPos, _currentSpeed * Time.deltaTime);

        if (transform.position == targetPos)
            _currentSpeed = 0f;
    }
}
