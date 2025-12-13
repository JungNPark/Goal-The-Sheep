using UnityEngine;
using UnityEngine.InputSystem;

public class WasdMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("최대 이동 속도 (m/s)")]
    public float moveSpeed = 5.0f;
    [Tooltip("중력 가속도")]
    public float gravity = -9.81f; 
    // --- [2] 가속/감속을 위한 변수 추가 ---
    [Header("Speed Transition Settings")]
    [Tooltip("가속/감속에 걸리는 시간 (초)")]
    public float transitionDuration = 1.0f; 
    // [1] 컴포넌트 참조 및 변수 설정 (기존)
    private CharacterController characterController;
    private Vector3 currentMovementInput; // Input System으로부터 받은 이동 입력 벡터 (x, z)
    private Vector3 movementVelocity;     // CharacterController에 적용할 속도 벡터 (x, y, z)
    
    
    
    private float currentHorizontalSpeed = 0f; // 현재 오브젝트의 실제 수평 속도
    private float targetSpeed = 0f;            // 목표 속도 (0 또는 moveSpeed)
    private float initialSpeed = 0f;           // 가속/감속 시작 시점의 속도
    private float transitionTimer = 0f;        // 가속/감속 진행 시간 (0에서 transitionDuration까지)
    
    // 입력이 없을 때 감속 중에도 마지막으로 이동했던 방향을 유지하기 위한 벡터
    private Vector3 lastInputDirection = Vector3.forward; 

    // [3] 초기화 (기존)
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    // [4] LogX 그래프 모양의 Ease-Out 가속 함수
    // t가 0에서 1로 변할 때, 초반에 빠르게 증가하고 후반에 느리게 목표에 도달합니다.
    private float EaseOutLog(float t)
    {
        // 표준 Ease-Out Cubic 함수 (1 - (1 - t)^3)를 사용하며,
        // 이는 사용자가 요청한 'LogX 모양'의 가속 특성을 잘 나타냅니다.
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    // [5] 매 프레임 업데이트 (수정)
    private void Update()
    {
        //땅에 있을 경우 중력 초기화
        if (characterController.isGrounded && movementVelocity.y < 0)
        {
            movementVelocity.y = -2f; 
        }
        else
        {
            movementVelocity.y += gravity * Time.deltaTime;
        }

        // B. 속도 변화 및 방향 업데이트
        
        // 1. 새로운 목표 속도 결정
        float inputMagnitude = currentMovementInput.sqrMagnitude > 0.01f ? 1f : 0f;
        float newTargetSpeed = inputMagnitude * moveSpeed;

        // 2. 속도 변화 감지 및 전환 시작
        if (newTargetSpeed != targetSpeed)
        {
            initialSpeed = currentHorizontalSpeed; // 현재 속도를 시작 속도로 설정
            targetSpeed = newTargetSpeed;         // 새로운 목표 속도 설정
            transitionTimer = 0f;                 // 타이머 리셋
        }

        // 3. 타이머 업데이트
        transitionTimer += Time.deltaTime;
        // t는 0에서 1까지의 정규화된 진행 시간입니다.
        float t = Mathf.Clamp01(transitionTimer / transitionDuration);
        
        // 4. 가속/감속 계산
        if (targetSpeed > initialSpeed)
        {
            // 가속 (targetSpeed = moveSpeed): y = logX 모양 (Ease-Out)
            float easedT = EaseOutLog(t);
            currentHorizontalSpeed = Mathf.Lerp(initialSpeed, targetSpeed, easedT);
        }
        else // targetSpeed <= initialSpeed (주로 0으로 감속)
        {
            // 감속 (targetSpeed = 0): y = -x 모양 (Linear)
            currentHorizontalSpeed = Mathf.Lerp(initialSpeed, 0f, t);
        }

        // 5. 방향 유지 및 이동 벡터 생성
        Vector3 inputDir = new Vector3(currentMovementInput.x, 0, currentMovementInput.z);
        
        // 입력이 있을 때만 마지막 이동 방향을 업데이트합니다.
        if (inputDir.sqrMagnitude > 0.01f)
        {
            // 월드 좌표계 기준의 방향을 유지하기 위해 `transform`을 사용하지 않고 입력 벡터를 정규화합니다.
            lastInputDirection = inputDir.normalized;
        }
        
        // 마지막 방향과 현재 계산된 속도를 사용하여 이동 벡터를 계산합니다.
        // lastInputDirection을 월드 좌표계에 맞게 변환합니다.
        Vector3 worldMoveDirection = transform.forward * lastInputDirection.z + transform.right * lastInputDirection.x;
        
        // 수평 이동 벡터
        Vector3 horizontalMove = worldMoveDirection.normalized * currentHorizontalSpeed;
        
        // 수직 속도를 포함하여 최종 이동 벡터 완성
        Vector3 finalMove = horizontalMove;
        finalMove.y = movementVelocity.y;
        
        // 6. CharacterController를 사용하여 이동 적용
        characterController.Move(finalMove * Time.deltaTime);
    }
    public void OnMove(InputValue value)
    {
        Debug.Log("Check1");
        // WASD 입력 값을 갱신
        Vector2 inputVector = value.Get<Vector2>();
        currentMovementInput.x = inputVector.x;
        currentMovementInput.z = inputVector.y;
    }
}
