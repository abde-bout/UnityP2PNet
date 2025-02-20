using UnityEditor;
using UnityEngine;

namespace UnityP2PNet
{
    [CustomEditor(typeof(NetSettingsData))]
    public class NetSettingsDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);

            if (GUILayout.Button("Refresh"))
            {
                (target as NetSettingsData).Refresh();
            }
        }
    }
}