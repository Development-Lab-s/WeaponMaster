using UnityEngine;

public class ProceduralLegStepper : MonoBehaviour
{
    [Header("Move Target")]
    [SerializeField] private Transform leftFootTarget;
    [SerializeField] private Transform rightFootTarget;

    [Header("Sole Point")]
    [SerializeField]private Transform leftSolePoint;
    [SerializeField]private Transform rightSolePoint;
    
    [Header("Step")]
    [SerializeField]private float stepDistance = 0.5f;
    [SerializeField] private float stepInterval = 0.25f;
    
    private bool nextMoveLeft = true;
    private float nextStepTime;
    
    private Vector3 savedLeftFootPosition;
    private Vector3 savedRightFootPosition;

    private void Awake()
    {
        savedLeftFootPosition = leftFootTarget.position;
        savedRightFootPosition = rightFootTarget.position;
    }

    private void Update()
    {
        leftFootTarget.position = savedLeftFootPosition;
        rightFootTarget.position = savedRightFootPosition;
        
        Vector3 leftOffset = savedLeftFootPosition - leftSolePoint.position;
        Vector3 rightOffset = savedRightFootPosition - rightSolePoint.position;

        leftOffset.y = 0f;
        rightOffset.y = 0f;

        float leftDistance = leftOffset.magnitude;
        float rightDistance = rightOffset.magnitude;

        if (Time.time < nextStepTime)
            return;
        
        if (nextMoveLeft && leftDistance > stepDistance)
        {
            savedLeftFootPosition = leftSolePoint.position;
            nextMoveLeft = false;

            nextStepTime = Time.time + stepInterval;
        }
        else if (!nextMoveLeft && rightDistance > stepDistance)
        {
            savedRightFootPosition = rightSolePoint.position;
            nextMoveLeft = true;

            nextStepTime = Time.time + stepInterval;
        }
    }
}
