using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Marionette
{
    [CustomEditor(typeof(MarionetteSmrPartProxy))]
    [CanEditMultipleObjects]
    public class MarionetteSmrPartProxyEditor : Editor
    {
        SerializedProperty smr;
        SerializedProperty rootBoneName;
        SerializedProperty bones;

        void OnEnable()
        {
            smr = serializedObject.FindProperty("smr");
            rootBoneName = serializedObject.FindProperty("rootBoneName");
            bones = serializedObject.FindProperty("bones");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(smr);
            EditorGUILayout.PropertyField(rootBoneName);
            EditorGUILayout.PropertyField(bones);
            serializedObject.ApplyModifiedProperties();
            MonoBehaviour monoBehav = (MonoBehaviour)target;
            monoBehav.GetComponent<MarionetteSmrPartProxy>().RecalculateHashes();
        }
    }
}
