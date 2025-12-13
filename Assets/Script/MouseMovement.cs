using UnityEngine;
using UnityEngine.InputSystem;

public class MouseMovement : MonoBehaviour
{
    // [1] 컴포넌트 참조 및 상태 변수
    private Rigidbody rb;
    private Vector3 targetPosition; // 마우스 클릭 또는 현재 포인터가 가리키는 월드 위치
    private bool isMoving = false; // 이동 중 여부
    
    [Header("Movement Settings")]
    [Tooltip("기본 최대 속도 (m/s)")]
    public float baseMaxSpeed = 10.0f;
    [Tooltip("최대 가속도 (m/s^2)")]
    public float acceleration = 25.0f;
    
    // [2] 초과 속도 (Overspeed) 관련 변수
    private const float OVERSPEED_BOOST = 1.10f; // 10% 증가
    private const float OVERSPEED_DURATION = 1.0f; // 초과 속도 돌입 조건 시간
    
    private float currentMaxSpeed; // 현재 적용되는 최대 속도 (baseMaxSpeed 또는 Overspeed)
    private bool isOverspeed = false; // 초과 속도 상태 여부
    private float maxSpeedTimer = 0f; // 최대 속도 도달 후 경과 시간 (초과 속도 상태 진입 조건용)
    
    // [3] 초기화
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentMaxSpeed = baseMaxSpeed; // 초기 최대 속도 설정
        
        // Rigidbody 설정 (물리적으로 부드러운 움직임을 위해)
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    // [4] 마우스 포인터 위치 입력 처리 (Input System)
    // 이 메서드는 마우스 포인터가 움직일 때마다 호출됩니다.
    public void OnMousePosition(InputValue value)
    {
        Vector2 screenPosition = value.Get<Vector2>();
        
        // 스크린 좌표를 월드 좌표로 변환합니다.
        // Raycast를 사용하여 정확히 마우스가 땅(Ground)의 어느 지점을 가리키는지 찾아야 합니다.
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        
        // 월드 좌표계의 Ground 레이어를 가정합니다.
        // 유니티 Layer 설정에서 Ground를 10번 레이어로 가정합니다.
        int layerMask = 1 << 10; 
        
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            // Y축을 고정하고, 마우스 포인터가 가리키는 지점을 목표 위치로 설정합니다.
            targetPosition = new Vector3(hit.point.x, transform.position.y, hit.point.z);
            isMoving = true;
        }
    }
    
    // [5] 물리 업데이트 (일정 시간 간격으로 실행)
    private void FixedUpdate()
    {
        if (!isMoving)
        {
            // 입력이 없으면 모든 물리적인 힘을 0으로 만듭니다.
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // A. 목표 위치까지의 벡터 계산
        Vector3 displacement = targetPosition - transform.position;
        float distanceToTarget = displacement.magnitude;
        Vector3 direction = displacement.normalized;

        // B. 도착 여부 확인 및 속도 감속 처리
        if (distanceToTarget < 0.1f) 
        {
            HandleArrival();
            return;
        }
        
        // C. 현재 속도 및 방향 설정
        float currentSpeed = rb.linearVelocity.magnitude;

        // D. 가속도 크기 계산 (마우스 포인터 거리에 비례)
        // 거리가 멀면 최대 가속도를 적용하고, 가까워질수록 가속도를 줄여서 부드럽게 접근합니다.
        // 가속 크기는 distanceToTarget에 비례하지만, 최대 가속도(acceleration)를 넘지 않도록 Clamp 합니다.
        float proportionalAcceleration = Mathf.Min(acceleration, distanceToTarget * 2.0f); // *2.0f는 임의의 증폭 계수
        
        // E. 이동 힘 (Force) 적용
        rb.AddForce(direction * proportionalAcceleration * rb.mass, ForceMode.Acceleration);

        // F. 최대 속도 및 초과 속도(Overspeed) 상태 관리
        
        // 1. 현재 속도 확인 및 속도 제한
        if (currentSpeed >= currentMaxSpeed)
        {
            // 속도가 최대 속도에 도달했거나 초과했을 때 타이머 시작
            maxSpeedTimer += Time.fixedDeltaTime;
        }
        else
        {
            // 속도가 최대 속도보다 낮다면 타이머 리셋
            maxSpeedTimer = 0f;
            
            // 2. 초과 속도 상태 종료 조건 확인
            if (isOverspeed && currentSpeed < baseMaxSpeed)
            {
                ExitOverspeed();
            }
        }
        
        // 3. 초과 속도 상태 진입 조건 확인
        if (!isOverspeed && maxSpeedTimer >= OVERSPEED_DURATION)
        {
            EnterOverspeed();
        }
        
        // 4. 속도 제한 적용 (Clamp)
        if (currentSpeed > currentMaxSpeed)
        {
            // 속도가 최대 속도를 초과했다면 (초과 속도 상태를 고려하여) 속도를 제한합니다.
            rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
        }
        
        // G. 오브젝트 회전 (옵션: 이동 방향을 바라보게 합니다)
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f);
        }
    }

    // [6] 초과 속도 상태 진입
    private void EnterOverspeed()
    {
        isOverspeed = true;
        currentMaxSpeed = baseMaxSpeed * OVERSPEED_BOOST;
        Debug.Log("Overspeed 상태 진입! 최대 속도: " + currentMaxSpeed);
    }

    // [7] 초과 속도 상태 종료
    private void ExitOverspeed()
    {
        isOverspeed = false;
        currentMaxSpeed = baseMaxSpeed;
        Debug.Log("Overspeed 상태 종료! 최대 속도: " + currentMaxSpeed);
    }
    
    // [8] 목표 위치 도착 처리
    private void HandleArrival()
    {
        if (isOverspeed)
        {
            // 초과 속도 상태: 선형 감속(관성 효과)
            // Rigidbody의 감속 (Drag) 효과를 이용하거나, 직접 선형 감속을 구현합니다.
            // 여기서는 코드를 간결하게 하기 위해 Drag를 일시적으로 높이는 방법을 사용합니다.
            rb.linearDamping = 5.0f; // 높은 Drag 값으로 선형 감속을 유도
            
            // 초과 속도 상태 종료
            ExitOverspeed();
            
            // 관성으로 인해 잠시 후 목표 위치에서 멀어지므로, isMoving은 유지하여 방향을 다시 잡게 합니다.
            // isMoving = false; // 주석 처리하여 관성 후 다시 목표를 따라가게 함
            Debug.Log("관성 감속 시작. 드래그: " + rb.linearDamping);
            
            // 일정 시간 후 Drag를 원상 복구 (예: 0.5초 후)
            Invoke(nameof(ResetDrag), 0.5f);
        }
        else
        {
            // 일반 상태: 바로 멈춤
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            isMoving = false;
            maxSpeedTimer = 0f;
            Debug.Log("목표 도착 및 정지.");
        }
    }
    
    // [9] Drag 값 초기화
    private void ResetDrag()
    {
        rb.linearDamping = 0.0f; // Rigidbody의 기본 Drag 값으로 원상 복구
        Debug.Log("드래그 초기화: " + rb.linearDamping);
    }
}
