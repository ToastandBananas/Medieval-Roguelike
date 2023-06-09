using UnityEditor;

[CustomEditor(typeof(MeshOption))]
public class MeshOptionEditor : Editor
{
    MeshOption mo;

    void OnSceneGUI()
    {
        mo = target as MeshOption;

        // If someone glicks the GUI cylinder button, show the next Mesh Option
        if (Handles.Button(mo.transform.position - mo.transform.forward * mo.size, mo.transform.rotation, mo.size * 2, mo.size * 1.7f, Handles.CylinderHandleCap))
            mo.NextOption();
    }
}
