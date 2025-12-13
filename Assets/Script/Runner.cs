using UnityEngine;

public class Runner : MonoBehaviour
{
    // [1] 컴포넌트 및 변수 설정
    private Rigidbody rb;

    [Header("Threat Settings")]
    [Tooltip("피해야 할 오브젝트의 태그")]
    public string threatTag = "Threat";
    [Tooltip("회피를 시작할 위협 감지 반경")]
    public float fleeRadius = 10.0f;

    [Header("Movement Settings")]
    [Tooltip("최대 도망 가속도")]
    public float acceleration = 15.0f;
    [Tooltip("최대 도망 속도")]
    public float maxSpeed = 7.0f;
    [Tooltip("브레이크 힘 (정지 시도용)")]
    public float brakeForce = 10.0f;
    
    [Header("Cliff Detection Settings")]
    [Tooltip("낭떠러지 감지 레이어 마스크")]
    public LayerMask groundLayer;
    [Tooltip("낭떠러지 감지 시 기다릴 최소 거리")]
    public float cliffWaitDistance = 2.0f;
    [Tooltip("낭떠러지 감지를 위한 레이캐스트 거리")]
    public float cliffRayDistance = 1.0f;

    private Transform nearestThreat;

    // [2] 초기화
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // 필요에 따라 회전 잠금
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // [3] 매 프레임 업데이트
    private void FixedUpdate()
    {
        // 1. 가장 가까운 위협 찾기
        FindNearestThreat();

        if (nearestThreat == null)
        {
            // 위협이 없으면 서서히 정지
            ApplyBrake();
            return;
        }

        Vector3 threatDirection = transform.position - nearestThreat.position;
        float distanceToThreat = threatDirection.magnitude;
        Vector3 detectDir;

        if (distanceToThreat <= fleeRadius)
        {
            // 2. 낭떠러지 감지
            bool isNearCliff = IsNearCliff(threatDirection.normalized);
            
            // 3. 낭떠러지 회피 로직
            if (isNearCliff)
            {
                // A. 낭떠러지 근처이고, 위협이 '아주 근처'에 오지 않았다면 대기
                if (distanceToThreat > cliffWaitDistance)
                {
                    ApplyBrake(); // 정지 상태 유지
                    Debug.Log("낭떠러지 근처 대기 중. 위협 거리: " + distanceToThreat);
                    return; 
                }
                // B. 낭떠러지 근처인데, 위협이 '아주 근처'에 왔다면 강제 도망 (떨어짐 허용)
                else
                {
                    Flee(threatDirection.normalized); 
                    Debug.Log("낭떠러지 근처! 위협이 너무 가까워져 강제 이동 시작 (낙하).");
                }
            }
            else
            {
                // 4. 일반 회피 로직 (낭떠러지가 아닐 때)
                Flee(threatDirection.normalized);
            }
        }
        else
        {
            // 위협이 감지 반경 밖에 있다면 서서히 정지
            ApplyBrake();
        }
    }
    
    // [4] 회피 힘 적용
    private void Flee(Vector3 direction)
    {
        // 도망치는 방향으로 가속도 적용
        rb.AddForce(direction * acceleration * rb.mass, ForceMode.Acceleration);
        
        // 최대 속도 제한
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
        
        // 이동 방향으로 오브젝트 회전 (Y축만)
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f);
        }
    }
    
    // [5] 정지 시도 (브레이크 적용)
    private void ApplyBrake()
    {
        // Rigidbody의 현재 속도와 반대 방향으로 브레이크 힘을 적용하여 서서히 멈춥니다.
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            rb.AddForce(-rb.linearVelocity.normalized * brakeForce * rb.mass, ForceMode.Acceleration);
        }
    }

    // [6] 낭떠러지 감지
    private bool IsNearCliff(Vector3 fleeDirection)
    {
        // 도망가려는 방향(fleeDirection)으로만 레이캐스트를 쏴서 바닥이 있는지 확인합니다.
        
        // 1. 현재 위치에서 도망 방향으로 레이캐스트
        RaycastHit hit;
        
        // 레이캐스트 시작 위치를 지면에서 살짝 위에 둡니다. (오류 방지)
        Vector3 rayStartPoint = transform.position + Vector3.up * 0.1f;

        // 2. 도망 방향으로 cliffRayDistance만큼 레이를 쌥니다.
        if (!Physics.Raycast(rayStartPoint, fleeDirection, out hit, cliffRayDistance, groundLayer))
        {
            // 레이가 땅을 맞추지 못했다면 낭떠러지로 간주
            Debug.DrawRay(rayStartPoint, fleeDirection * cliffRayDistance, Color.red, 0.5f);
            return true;
        }
        
        // 레이가 땅을 맞췄다면, 맞은 지점의 경사가 급한지 확인하는 추가 로직을 넣을 수도 있으나,
        // 여기서는 간단히 레이가 땅을 맞췄는지 여부만 확인합니다.
        Debug.DrawRay(rayStartPoint, fleeDirection * cliffRayDistance, Color.green, 0.5f);
        return false;
    }
    
    // [7] 가장 가까운 위협 찾기 (이전 코드와 동일)
    private void FindNearestThreat()
    {
        GameObject[] threats = GameObject.FindGameObjectsWithTag(threatTag);
        float closestDistance = Mathf.Infinity;
        nearestThreat = null;

        foreach (GameObject threat in threats)
        {
            float distance = Vector3.Distance(transform.position, threat.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestThreat = threat.transform;
            }
        }
    }
}
