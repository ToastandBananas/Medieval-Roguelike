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
            Handles.DrawWireArc(fov.Unit.transform.position, Vector3.up, fov.Unit.transform.forward, 360, fov.ViewRadius);
            Vector3 viewAngleA = fov.DirectionFromAngle(-fov.ViewAngle / 2, false);
            Vector3 viewAngleB = fov.DirectionFromAngle(fov.ViewAngle / 2, false);

            Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngleA * fov.ViewRadius);
            Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngleB * fov.ViewRadius);

            Handles.color = Color.red;
            if (fov.knownUnits != null && fov.knownUnits.Count > 0)
            {
                foreach (KeyValuePair<Unit, int> visibleUnit in fov.knownUnits)
                {
                    // Handles.DrawLine(fov.transform.position, visibleUnit.Key.transform.position + fov.YOffset());
                }
            }

            if (fov.LooseItemsInViewRadius != null && fov.LooseItemsInViewRadius.Length > 0)
            {
                for (int i = 0; i < fov.LooseItemsInViewRadius.Length; i++)
                {
                    if (fov.LooseItemsInViewRadius[i].CompareTag("Loose Item") == false)
                        continue;

                    fov.LooseItemsInViewRadius[i].TryGetComponent(out Interactable_LooseItem looseItem);
                    if (looseItem != null)
                        Handles.DrawLine(fov.transform.position, looseItem.transform.TransformPoint(looseItem.MeshCollider.sharedMesh.bounds.center));
                }
            }
        }
    }
}
