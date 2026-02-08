using Match3.Unity.Views;
using UnityEditor;
using UnityEngine;

namespace Match3.Unity.Editor
{
    [CustomEditor(typeof(RenderTuningSettings))]
    public sealed class RenderTuningSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply / Refresh Now"))
                {
                    var settings = (RenderTuningSettings)target;
                    RenderTuningRuntime.ApplyNow(settings);
                }

                if (GUILayout.Button("Select Runtime Controller"))
                {
                    var controller = RenderTuningRuntime.EnsureController();
                    if (controller != null)
                        Selection.activeObject = controller.gameObject;
                }
            }
        }
    }
}

