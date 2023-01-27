using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor (typeof(Vision))]
public class FieldOfViewEditor : Editor
{
    Vector3 yOffset = new Vector3(0, 0.15f, 0);

    void OnSceneGUI()
    {
        Vision fov = (Vision)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(fov.transform.position, Vector3.up, Vector3.forward, 360, fov.viewRadius);
        Vector3 viewAngleA = fov.DirectionFromAngle(-fov.viewAngle / 2, false);
        Vector3 viewAngleB = fov.DirectionFromAngle(fov.viewAngle / 2, false);

        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngleA * fov.viewRadius);
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngleB * fov.viewRadius);

        Handles.color = Color.red;
        if (fov.visibleUnits.Count > 0)
        {
            foreach (KeyValuePair<Unit, Transform> keyValuePair in fov.visibleUnits)
            {
                Handles.DrawLine(fov.transform.position, keyValuePair.Value.position + yOffset);
            }
        }
    }
}
