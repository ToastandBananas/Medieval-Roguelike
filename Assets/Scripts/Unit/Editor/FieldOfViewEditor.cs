using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using InteractableObjects;

namespace UnitSystem
{
    [CustomEditor(typeof(Vision))]
    public class FieldOfViewEditor : Editor
    {
        void OnSceneGUI()
        {
            Vision fov = (Vision)target;
            Handles.color = Color.white;
            Handles.DrawWireArc(fov.transform.position, Vector3.up, Vector3.forward, 360, fov.ViewRadius());
            Vector3 viewAngleA = fov.DirectionFromAngle(-fov.ViewAngle() / 2, false);
            Vector3 viewAngleB = fov.DirectionFromAngle(fov.ViewAngle() / 2, false);

            Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngleA * fov.ViewRadius());
            Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngleB * fov.ViewRadius());

            Handles.color = Color.red;
            if (fov.knownUnits != null && fov.knownUnits.Count > 0)
            {
                foreach (KeyValuePair<Unit, int> visibleUnit in fov.knownUnits)
                {
                    // Handles.DrawLine(fov.transform.position, visibleUnit.Key.transform.position + fov.YOffset());
                }
            }

            if (fov.looseItemsInViewRadius != null && fov.looseItemsInViewRadius.Length > 0)
            {
                for (int i = 0; i < fov.looseItemsInViewRadius.Length; i++)
                {
                    if (fov.looseItemsInViewRadius[i].CompareTag("Loose Item") == false)
                        continue;

                    fov.looseItemsInViewRadius[i].TryGetComponent(out LooseItem looseItem);
                    if (looseItem != null)
                        Handles.DrawLine(fov.transform.position, looseItem.MeshCollider.bounds.center);
                }
            }
        }
    }
}
