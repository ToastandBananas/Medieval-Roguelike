using UnityEngine;

public class BowLineRenderer : MonoBehaviour
{
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] Transform stringTopTarget, stringCenterTarget, stringBottomTarget;

    bool stringShouldFollowPositions;

    void Update()
    {
        StringFollowTargetPositions();
    }

    void StringFollowTargetPositions()
    {
        if (stringShouldFollowPositions)
        {
            lineRenderer.SetPosition(0, new Vector3(stringTopTarget.localPosition.x, lineRenderer.GetPosition(0).y, stringTopTarget.localPosition.z)); // Top
            lineRenderer.SetPosition(1, new Vector3(stringCenterTarget.localPosition.x, lineRenderer.GetPosition(1).y, lineRenderer.GetPosition(1).z)); // Center
            lineRenderer.SetPosition(2, new Vector3(stringBottomTarget.localPosition.x, lineRenderer.GetPosition(2).y, stringBottomTarget.localPosition.z)); // Bottom
        }
    }

    public Transform GetStringCenterTarget()
    {
        return stringCenterTarget;
    }

    public void StringStartFollowingTargetPositions()
    {
        stringShouldFollowPositions = true;
    }

    public void StringStopFollowingTargetPositions()
    {
        stringShouldFollowPositions = false;
    }
}
