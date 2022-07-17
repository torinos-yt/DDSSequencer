using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace DDSSequencer.Editor.Pipeline
{

using CompressFormat = TextureConverter.CompressFormat;
using CompressQuality = TextureConverter.DDSCompressQuality;

public sealed class SeqnenceConverterWindow : EditorWindow
{
    public enum SequenceSource
    {
        Movie,
        Sequence
    }

    static string StoredSrcDirectory;
    [SerializeField] string _srcPath;

    static string StoredOutDirectory;
    [SerializeField] string _outDirectoryPath;
    [SerializeField] string _srcInfo;

    const string DefaultNVTTPath = "C:\\Program Files\\NVIDIA Corporation\\NVIDIA Texture Tools Exporter\\nvtt_export.exe";
    const string DefaultNVTTPath2 = "C:\\Program Files\\NVIDIA Corporation\\NVIDIA Texture Tools\\nvtt_export.exe";
    [SerializeField] string _nvttPath;

    [SerializeField] bool _pathToffmpeg;

    [SerializeField] SequenceSource _source = SequenceSource.Movie;

    [SerializeField] CompressFormat _format = CompressFormat.BC7;
    [SerializeField] CompressQuality _quality = CompressQuality.Normal;
    [SerializeField] bool _yFlip = true;
    [SerializeField] bool _useCuda = true;
    [SerializeField] bool _mips = false;
    [SerializeField] bool _deleteTmp = true;
    [SerializeField] bool _existffmpeg = true;

    [SerializeField] int _fps = 30;

    GUIStyle _boldLabel;

    [MenuItem("Window/DDS Sequencer/Sequence Converter")]
    static void Open()
    {
        var window = GetWindow<SeqnenceConverterWindow>("Sequence Converter");
        window.minSize = new Vector2(480, 440);

        try
        {
            var info = new System.Diagnostics.ProcessStartInfo();
            info.FileName = "ffmpeg";
            info.Arguments = "-version";
            info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

            System.Diagnostics.Process.Start(info);
            window._existffmpeg = true;
        }
        catch(System.ComponentModel.Win32Exception)
        {
            window._existffmpeg = false;
        }
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
        EditorGUI.BeginChangeCheck();
        _source = (SequenceSource)EditorGUILayout.EnumPopup("Source Type", _source, GUILayout.Width(250));
        EditorGUILayout.LabelField((_source == SequenceSource.Sequence ? "Source Sequence Directory" : "Source File") + _srcInfo);

        if(EditorGUI.EndChangeCheck() && _source == SequenceSource.Movie)
        {
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo();
                info.FileName = "ffmpeg";
                info.Arguments = "-version";
                info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

                System.Diagnostics.Process.Start(info);
                _existffmpeg = true;
            }
            catch(System.ComponentModel.Win32Exception)
            {
                _existffmpeg = false;
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if(_existffmpeg || _source == SequenceSource.Sequence)
            {
                _srcPath = EditorGUILayout.TextField(_srcPath);

                if(GUILayout.Button("Select", GUILayout.Width(80)))
                {
                    string startDir = _srcPath == string.Empty? Application.dataPath : Path.GetDirectoryName(_srcPath);
                    var prevSrc = _srcPath;

                    _srcPath = _source == SequenceSource.Sequence ?
                                            EditorUtility.OpenFolderPanel("Open", startDir, string.Empty) :
                                            EditorUtility.OpenFilePanel("Open", startDir, string.Empty);

                    if(_srcPath != prevSrc && _srcPath == string.Empty)
                        _srcPath = prevSrc;

                    CheckDirectoryPath();
                }
            }
            else
            {
                _srcPath = string.Empty;
                EditorGUILayout.LabelField("ffmpeg is Not Found", _boldLabel);
            }
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Save Directory");

        using (new EditorGUILayout.HorizontalScope())
        {
            _outDirectoryPath = EditorGUILayout.TextField(_outDirectoryPath);

            if(GUILayout.Button("Select", GUILayout.Width(80)))
            {
                _outDirectoryPath = EditorUtility.OpenFolderPanel("Open", Application.dataPath, string.Empty);
            }
        }

        EditorGUI.indentLevel--;
    }

    void DrawExportSettings()
    {
        EditorGUILayout.LabelField("Export Settings", _boldLabel);

        EditorGUI.indentLevel++;

        bool path1 = System.IO.File.Exists(DefaultNVTTPath);
        bool path2 = System.IO.File.Exists(DefaultNVTTPath2);

        if(!path1 && !path2)
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
            _nvttPath = path1? DefaultNVTTPath : DefaultNVTTPath2;
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
        using(new EditorGUI.DisabledGroupScope(!_existffmpeg && _source == SequenceSource.Movie))
        {
            if(GUILayout.Button("Proceed"))
            {

#if UNITY_EDITOR_WIN
                if(!System.IO.File.Exists(_nvttPath))
                {
                    Debug.LogError("[DDSSequencer Converter Window] Error : NVIDIA Texture Tools Exporter is Not Found");
                    return;
                }
#endif

                TextureConverter.Process(_srcPath, _outDirectoryPath, _nvttPath, new TextureConverter.ExportSetting
                {
                    format = _format,
                    quality = _quality,
                    useCuda = _useCuda,
                    mips = _mips,
                    yFlip = _yFlip,
                    deleteTmp = _deleteTmp,
                    fps = _fps
                }, _source);
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

            if(TextureConverter.IsffmpegProcess)
            {
                GUI.Label(rect, $"1/3 Proceeding Movie Convert.... {TextureConverter.CompleteCount} / {TextureConverter.Count}");
            }
            else if(TextureConverter.IsOptProcess)
            {
                if(_source == SequenceSource.Sequence)
                    GUI.Label(rect, $"2/2 Proceeding Asset Optimize.... {TextureConverter.CompleteCount} / {TextureConverter.Count}");
                else
                    GUI.Label(rect, $"3/3 Proceeding Asset Optimize.... {TextureConverter.CompleteCount} / {TextureConverter.Count}");
            }
            else if(TextureConverter.IsDdsProcess)
            {
                if(_source == SequenceSource.Sequence)
                    GUI.Label(rect, $"1/2 Proceeding dds Convert on NVIDIA Texture Tools Exporter.... {TextureConverter.CompleteCount} / {TextureConverter.Count}");
                else
                    GUI.Label(rect, $"2/3 Proceeding dds Convert on NVIDIA Texture Tools Exporter.... {TextureConverter.CompleteCount} / {TextureConverter.Count}");
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
        
        _srcPath = StoredSrcDirectory ?? _srcPath;
        _outDirectoryPath = StoredOutDirectory ?? _outDirectoryPath;

        CheckDirectoryPath();
    } 

    public void OnDisable()
    {
        StoredSrcDirectory = _srcPath;
        StoredOutDirectory = _outDirectoryPath;

        TextureConverter.StopProcess();
    }

    void CheckDirectoryPath()
    {
        if(_source == SequenceSource.Sequence)
        {
            if(!System.IO.Directory.Exists(_srcPath))
            {
                _srcInfo = "";
            }
            else
            {
                var files = System.IO.Directory.GetFiles(_srcPath)
                                                .Where(x => TextureConverter.IsImageFileExtension(x)).ToArray();

                _srcInfo = "      " + files.Length.ToString() + " Files";
            }
        }
        else
        {
            _srcInfo = "";
        }
    }
}

}