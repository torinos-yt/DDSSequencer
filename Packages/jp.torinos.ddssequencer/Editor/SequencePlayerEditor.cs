using UnityEngine;
using UnityEditor;
using DDSSequencer.Runtime;

namespace DDSSequencer.Editor
{

[CustomEditor(typeof(SequencePlayer))]
sealed class SequencePlayerEditor : UnityEditor.Editor
{
    SerializedProperty _seqDirectory;

    SerializedProperty _targetRenderer;
    SerializedProperty _targetProperty;

    SerializedProperty _targetTexture;

    SerializedProperty _playMode;
    SerializedProperty _playOnAwake;
    SerializedProperty _loop;
    SerializedProperty _speed;

    SerializedProperty _cacheFrame;
    SequencePlayer _player;

    void OnEnable()
    {
        _seqDirectory = serializedObject.FindProperty("_sequencePath");

        _targetRenderer = serializedObject.FindProperty("_targetRenderer");
        _targetProperty = serializedObject.FindProperty("_targetProperty");

        _targetTexture = serializedObject.FindProperty("_targetTexture");

        _playMode = serializedObject.FindProperty("_playMode");
        _playOnAwake = serializedObject.FindProperty("_playOnAwake");
        _loop = serializedObject.FindProperty("_loop");
        _speed = serializedObject.FindProperty("_speed");

        _cacheFrame = serializedObject.FindProperty("_allFrameCache");

        _player = target as SequencePlayer;
        _player.OpenSequenceFromDirectory(_seqDirectory.stringValue);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        // Sequence Directory
        using(new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.DelayedTextField(_seqDirectory);

            if(GUILayout.Button("Select", GUILayout.Width(60)))
                _seqDirectory.stringValue = EditorUtility.OpenFolderPanel("Open", Application.streamingAssetsPath, string.Empty);
        }

        if(EditorGUI.EndChangeCheck())
            _player.OpenSequenceFromDirectory(_seqDirectory.stringValue);

        // Sequence info
        if(_player.IsValid)
        {
            EditorGUILayout.LabelField($"Frame Size : {_player.FrameInfo.size}");
            EditorGUILayout.LabelField($"Frame Rate : {_player.FrameInfo.fps}");
            EditorGUILayout.LabelField($"Frame Format : {_player.FrameInfo.format}");
            EditorGUILayout.LabelField($"Frame Count : {_player.FrameCount}");
            EditorGUILayout.LabelField($"Duration : {_player.Duration} sec");
        }

        EditorGUILayout.Space(10);
        
        // Target texture
        EditorGUILayout.PropertyField(_targetTexture);

        // Target renderer
        EditorGUILayout.PropertyField(_targetRenderer);

        EditorGUI.indentLevel++;

        if (_targetRenderer.hasMultipleDifferentValues)
        {
            EditorGUILayout.PropertyField(_targetProperty, new GUIContent("Target Property"));
        }
        else if (_targetRenderer.objectReferenceValue != null)
        {
            MaterialPropertySelector.DropdownList(_targetRenderer, _targetProperty);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        EditorGUILayout.PropertyField(_playOnAwake, new GUIContent("Play On Awake"));
        EditorGUILayout.PropertyField(_loop, new GUIContent("Loop"));
        EditorGUILayout.PropertyField(_cacheFrame, new GUIContent("Caching All Frame"));
        EditorGUILayout.Space(5);

        EditorGUILayout.PropertyField(_playMode, new GUIContent("Play Mode"));

        if(_playMode.intValue == 0)
            _speed.floatValue = EditorGUILayout.Slider("Play Speed", _speed.floatValue, -5f, 5f);

        serializedObject.ApplyModifiedProperties();
    }
}

}