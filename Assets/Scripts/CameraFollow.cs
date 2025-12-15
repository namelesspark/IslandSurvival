using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Agent를 드래그 앤 드롭
    public Vector3 offset = new Vector3(0, 0, -10); // 카메라 Z 위치 유지
    public float smoothSpeed = 5f; // 부드러운 이동 속도

    void LateUpdate()
    {
        if (target == null) return;

        // 목표 위치 계산
        Vector3 desiredPosition = target.position + offset;
        
        // 부드럽게 이동
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        transform.position = smoothedPosition;
    }
}