using System.Linq;
using UnityEngine;
using UnityEditor;

namespace DDSSequencer.Editor.Pipeline
{

using CompressFormat = TextureConverter.CompressFormat;
using CompressQuality = TextureConverter.DDSCompressQuality;

public sealed class SeqnenceConverterWindow : EditorWindow
{
    static string StoredSrcDirectory;
    [SerializeField] string _srcDirectoryPath;

    static string StoredOutDirectory;
    [SerializeField] string _outDirectoryPath;
    [SerializeField] string _srcDirectoryInfo;

    const string DefaultNVTTPath = "C:\\Program Files\\NVIDIA Corporation\\NVIDIA Texture Tools Exporter\\nvtt_export.exe";
    [SerializeField] string _nvttPath;

    [SerializeField] CompressFormat _format = CompressFormat.BC7;
    [SerializeField] CompressQuality _quality = CompressQuality.Normal;
    [SerializeField] bool _yFlip = true;
    [SerializeField] bool _useCuda = true;
    [SerializeField] bool _mips = false;
    [SerializeField] bool _deleteTmp = true;

    [SerializeField] int _fps = 30;

    GUIStyle _boldLabel;

    [MenuItem("Window/DDS Sequencer/Sequence Converter")]
    static void Open()
    {
        var window = GetWindow<SeqnenceConverterWindow>("Sequence Converter");
        window.minSize = new Vector2(480, 440);
    }

    public void OnGUI()
    {
        EditorGUILayout.Space(5);
        DrawPathSettings();

        EditorGUILayout.Space(20);
        DrawExportSettings();

        EditorGUILayout.Space(20);
        DrawProcessButton();

        EditorGUILayout.Space(5);
        DrawProceedingInfo();

        if(GUI.changed)
        {
            CheckDirectoryPath();
        }
    }

    void DrawPathSettings()
    {
        EditorGUILayout.LabelField("Path Settings", _boldLabel);

        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Source Sequence Directory" + _srcDirectoryInfo);

        using (new EditorGUILayout.HorizontalScope())
        {
            _srcDirectoryPath = EditorGUILayout.TextField(_srcDirectoryPath);

            if(GUILayout.Button("Select", GUILayout.Width(80)))
            {
                _srcDirectoryPath = EditorUtility.OpenFolderPanel("Open",  Application.dataPath, string.Empty);
                CheckDirectoryPath();
            }
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Save Directory");

        using (new EditorGUILayout.HorizontalScope())
        {
            _outDirectoryPath = EditorGUILayout.TextField(_outDirectoryPath);

            if(GUILayout.Button("Select", GUILayout.Width(80)))
            {
                _outDirectoryPath = EditorUtility.OpenFolderPanel("Open",  Application.dataPath, string.Empty);
            }
        }

        EditorGUI.indentLevel--;
    }

    void DrawExportSettings()
    {
        EditorGUILayout.LabelField("Export Settings", _boldLabel);

        EditorGUI.indentLevel++;

        if(!System.IO.File.Exists(DefaultNVTTPath))
        {
            EditorGUILayout.LabelField("Application Path");
            using (new EditorGUILayout.HorizontalScope())
            {
                _nvttPath = EditorGUILayout.TextField(_nvttPath);

                if(GUILayout.Button("Select", GUILayout.Width(80)))
                {
                    _nvttPath = EditorUtility.OpenFilePanel("Open",  Application.dataPath, string.Empty);
                }
            }
        }
        else
        {
            _nvttPath = DefaultNVTTPath;
        }

        EditorGUILayout.Space(2);
        _format = (CompressFormat)EditorGUILayout.EnumPopup("Compress Format", _format, GUILayout.Width(250));
        _quality = (CompressQuality)EditorGUILayout.EnumPopup("Compress Quality", _quality, GUILayout.Width(250));

        using(new EditorGUI.DisabledGroupScope((int)_quality >= 2))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Use CUDA", GUILayout.Width(134));
                _useCuda = EditorGUILayout.Toggle(_useCuda);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Flip Vertically", GUILayout.Width(134));
            _yFlip = EditorGUILayout.Toggle(_yFlip);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Ganarate Mips", GUILayout.Width(134));
            _mips = EditorGUILayout.Toggle(_mips);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Delete Temp Files", GUILayout.Width(134));
            _deleteTmp = EditorGUILayout.Toggle(_deleteTmp);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Frame Rate", GUILayout.Width(134));
            _fps = EditorGUILayout.IntField(_fps, GUILayout.Width(45));
        }

        EditorGUI.indentLevel--;
    }

    void DrawProcessButton()
    {
        if(GUILayout.Button("Proceed"))
        {

#if UNITY_EDITOR_WIN
            if(!System.IO.File.Exists(_nvttPath))
            {
                Debug.LogError("Error : NVIDIA Texture Tools Exporter is Not Found");
                return;
            }
#endif

            TextureConverter.Process(_srcDirectoryPath, _outDirectoryPath, _nvttPath, new TextureConverter.ExportSetting
            {
                format = _format,
                quality = _quality,
                useCuda = _useCuda,
                mips = _mips,
                yFlip = _yFlip,
                deleteTmp = _deleteTmp,
                fps = _fps
            });
        }

        EditorGUILayout.Space(2);
        using(new EditorGUI.DisabledGroupScope(!TextureConverter.IsProceeding))
        {
            if(GUILayout.Button("Stop"))
            {
                TextureConverter.StopProcess();
            }
        }
    }

    void DrawProceedingInfo()
    {
        var rect = GUILayoutUtility.GetRect(position.width, System.IO.File.Exists(DefaultNVTTPath)? 80 : 38);
        rect.height += position.size.y - 460;

        using (new GUI.GroupScope(rect, GUI.skin.box))
        {
            rect.x = 5;
            rect.y = 0;
            rect.height = EditorGUIUtility.singleLineHeight * 1.5f;

            if(TextureConverter.IsOptProcess)
            {
                GUI.Label(rect, $"2/2 Proceeding Asset Optimize.... {TextureConverter.CompleteCount} / {TextureConverter.Count}");
            }
            else if(TextureConverter.IsDdsProcess)
            {
                GUI.Label(rect, $"1/2 Proceeding dds Convert on NVIDIA Texture Tools Exporter.... ");
            }
        }
    }

    public void OnEnable()
    {
        _boldLabel = new GUIStyle()
        {
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState(){textColor = Color.grey * 1.5f}
        };
        
        _srcDirectoryPath = StoredSrcDirectory ?? _srcDirectoryPath;
        _outDirectoryPath = StoredOutDirectory ?? _outDirectoryPath;

        CheckDirectoryPath();
    } 

    public void OnDisable()
    {
        StoredSrcDirectory = _srcDirectoryPath;
        StoredOutDirectory = _outDirectoryPath;

        TextureConverter.StopProcess();
    }

    void CheckDirectoryPath()
    {
        if(!System.IO.Directory.Exists(_srcDirectoryPath))
        {
            _srcDirectoryInfo = "";
        }
        else
        {
            var files = System.IO.Directory.GetFiles(_srcDirectoryPath)
                                            .Where(x => TextureConverter.IsImageFileExtension(x)).ToArray();

            _srcDirectoryInfo = "      " + files.Length.ToString() + " Files";
        }
    }
}

}