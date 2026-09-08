using System.Collections;
using UnityEngine;

public class ProceduralLegStepper : MonoBehaviour
{
    [Header("Move Target")]
    [SerializeField] private Transform leftFootTarget;
    [SerializeField] private Transform rightFootTarget;

    [Header("Sole Point")]
    [SerializeField] private Transform leftSolePoint;
    [SerializeField] private Transform rightSolePoint;
    
    [Header("Step")]
    [SerializeField] private float stepDistance = 0.5f;
    [SerializeField] private float stepDuration = 0.25f;
    [SerializeField] private float stepHeight = 0.3f;

    // 둘 다 스텝이 필요할 때 직전에 움직이지 않은 발을 우선한다.
    // false로 시작하므로 첫 스텝은 왼발이 된다.
    private bool lastMovedLeft;
    private bool isStepping;

    private Vector3 savedLeftFootPosition;
    private Vector3 savedRightFootPosition;

    // Foot Target은 발목, Sole Point는 발바닥이므로 둘 사이 간격을 보존한다.
    private Vector3 leftTargetFromSoleLocal;
    private Vector3 rightTargetFromSoleLocal;

    private void Awake()
    {
        if (leftFootTarget == null || rightFootTarget == null ||
            leftSolePoint == null || rightSolePoint == null)
        {
            Debug.LogError("Foot Target과 Sole Point를 모두 할당해야 합니다.", this);
            enabled = false;
            return;
        }

        savedLeftFootPosition = leftFootTarget.position;
        savedRightFootPosition = rightFootTarget.position;

        leftTargetFromSoleLocal = transform.InverseTransformVector(
            leftFootTarget.position - leftSolePoint.position);
        rightTargetFromSoleLocal = transform.InverseTransformVector(
            rightFootTarget.position - rightSolePoint.position);
    }

    private void Update()
    {
        leftFootTarget.position = savedLeftFootPosition;
        rightFootTarget.position = savedRightFootPosition;
        
        if (isStepping)
            return;

        Vector3 desiredLeftFootPosition = leftSolePoint.position +
            transform.TransformVector(leftTargetFromSoleLocal);
        Vector3 desiredRightFootPosition = rightSolePoint.position +
            transform.TransformVector(rightTargetFromSoleLocal);

        Vector3 leftOffset = savedLeftFootPosition - desiredLeftFootPosition;
        Vector3 rightOffset = savedRightFootPosition - desiredRightFootPosition;

        leftOffset.y = 0f;
        rightOffset.y = 0f;

        float leftDistance = leftOffset.magnitude;
        float rightDistance = rightOffset.magnitude;

        bool leftNeedsStep = leftDistance > stepDistance;
        bool rightNeedsStep = rightDistance > stepDistance;

        if (!leftNeedsStep && !rightNeedsStep)
            return;

        bool moveLeft;

        if (leftNeedsStep && rightNeedsStep)
        {
            moveLeft = !lastMovedLeft;
        }
        else
        {
            moveLeft = leftNeedsStep;
        }

        Vector3 destination = moveLeft
            ? desiredLeftFootPosition
            : desiredRightFootPosition;

        StartCoroutine(MoveFoot(moveLeft, destination));
    }

    private IEnumerator MoveFoot(bool moveLeft, Vector3 endPosition)
    {
        isStepping = true;

        Vector3 startPosition = moveLeft
            ? savedLeftFootPosition
            : savedRightFootPosition;

        float elapsed = 0f;

        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / stepDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = Vector3.Lerp(startPosition, endPosition, smoothT);
            position.y += Mathf.Sin(t * Mathf.PI) * stepHeight;

            if (moveLeft)
            {
                savedLeftFootPosition = position;
                leftFootTarget.position = position;
            }
            else
            {
                savedRightFootPosition = position;
                rightFootTarget.position = position;
            }

            yield return null;
        }

        if (moveLeft)
        {
            savedLeftFootPosition = endPosition;
            leftFootTarget.position = endPosition;
        }
        else
        {
            savedRightFootPosition = endPosition;
            rightFootTarget.position = endPosition;
        }

        lastMovedLeft = moveLeft;
        isStepping = false;
    }
}
