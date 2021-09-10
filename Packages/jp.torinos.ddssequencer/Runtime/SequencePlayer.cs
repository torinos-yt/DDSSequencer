using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace DDSSequencer.Runtime
{

[ExecuteInEditMode, AddComponentMenu("DDS Sequencer/Sequence Player")]
#if HAS_TIMELINE
public sealed class SequencePlayer : MonoBehaviour, ITimeControl, IPropertyPreview
#else
public sealed class SequencePlayer : MonoBehaviour
#endif
{
    #region Serialize field
    [SerializeField] string _sequencePath = "";

    [SerializeField] MeshRenderer _targetRenderer;
    [SerializeField] string _targetProperty = "";

    [SerializeField] RenderTexture _targetTexture;

    [SerializeField] float _speed = 1.0f;
    [SerializeField] bool _playOnAwake = false;
    [SerializeField] bool _loop = false;

    [SerializeField] float _time = 0f;
    #endregion

    #region Private field
    int _numFrames = int.MaxValue;
    float _duration = 0;
    int _fps = 0;

    int _indexTime => Mathf.FloorToInt(_time * _fps);
    int _prevIndexTime = 0;
    bool _timeline;

    Texture2D _texture;
    MaterialPropertyBlock _prop;

    string[] _frames;
    (Vector2Int size, int fps, TextureFormat format, bool mips) _frameInfo;
    #endregion // Private field

    #region Public accessable property
    public float CurrentTime
    {
        get => _time;
        set
        {
            _prevIndexTime = _indexTime;
            _time = value;

            if(_indexTime > _numFrames - 1)
            {
                if(_loop)
                    _time = 0;
                else
                {
                    _time = Mathf.Min(value, _duration - 1e-4f);
                    Pause();
                }
            }
            else if(_indexTime < 0)
            {
                if(_loop)
                    _time = _duration - 1e-4f;
                else
                {
                    _time = 0;
                    Pause();
                }
            }

            if(IsValid && _prevIndexTime != _indexTime)
                LoadCurrentFrame();
        }
    }

    public float Speed
    {
        get => _speed;
        set{ _speed = value; }
    }

    public bool PlayOnAwake
    {
        get => _playOnAwake;
        set { _playOnAwake = value; }
    }

    public bool Loop
    {
        get => _loop;
        set { _loop = value; }
    }

    public bool IsPlaying { get; private set; } = false;
    public bool IsValid { get; private set; } = false;
    public float Duration => _duration;
    public int FrameCount => _numFrames;
    public int CurrentFrame => _indexTime;

    public (Vector2Int size, int fps, TextureFormat format, bool mips) FrameInfo => _frameInfo;
    #endregion // Punlic accesible property

    #region MonoBehabiour method implemetation
    void Start()
    {
        InitSequence();
        if(_playOnAwake) IsPlaying = true;
    }

    void LateUpdate()
    {
        if(IsValid && IsPlaying && Application.isPlaying && !_timeline)
        {
            CurrentTime += Time.deltaTime * Speed;
        } 
    }

    void OnDestroy()
    {
        if(_texture == null) return;
        
        if(Application.isPlaying)
            Destroy(_texture);
        else
            DestroyImmediate(_texture);

        _texture = null;
    }
    #endregion // MonoBehabiour method implemetation

    #region ITimeControl implementation
    #if HAS_TIMELINE
    public void OnControlTimeStart()
    {
        _timeline = true;
        _speed = 1.0f;
    }

    public void OnControlTimeStop()
    {
        _timeline = false;
    }

    public void SetTime(double time)
    {
        CurrentTime = Mathf.Max(Mathf.Min((float)time, _duration - 1e-4f), 0);
        _speed = 1.0f;
    }
    #endif
    #endregion // ITimeControl implementation

    #region IPropertyPreview implementation
    #if HAS_TIMELINE
    public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
    {
        driver.AddFromName<SequencePlayer>(gameObject, "_time");
    }
    #endif
    #endregion // IPropertyPreview implementation

    #region  Private method
    async void LoadCurrentFrame()
    {
        byte[] bytes = await Task.Run(() =>
        {
            return File.ReadAllBytes(_frames[_indexTime]);
        });

        if(_texture == null) return;

        _texture.LoadRawTextureData(bytes);
        _texture.Apply();

        UpdateTarget();
    }

    void UpdateTarget()
    {
        if(_targetRenderer != null)
        {
            _prop ??= new MaterialPropertyBlock();
            _prop.SetTexture(_targetProperty, _texture);
            _targetRenderer.SetPropertyBlock(_prop);
        }

        if(_targetTexture != null)
            Graphics.Blit(_texture, _targetTexture);
    }

    void InitSequence()
    {        
        var path = _sequencePath;
        if(path.Contains("StreamingAssets"))
            path = Application.streamingAssetsPath + _sequencePath.Substring(_sequencePath.IndexOf("StreamingAssets") + "StreamingAssets".Length);

        string[] files;
        string header;

        try
        {
            files = Directory.GetFiles(path);
        }
        catch(Exception)
        {
            if(Application.isPlaying) Debug.LogError("Missing Directory");
            IsValid = false;
            return;
        }

        try
        {
            header = files.Where(x => Path.GetExtension(x) == ".ddsmeta").First();
        }
        catch(Exception)
        {
            if(Application.isPlaying) Debug.LogError("Missing Header File in Source Directory");
            IsValid = false;
            return;
        }

        _frameInfo = Utility.ReadHeaderInfo(header);
        _frames = files.Where(x => Path.GetExtension(x) == ".ddssc").ToArray();

        _numFrames = _frames.Length;
        _fps = _frameInfo.fps;
        _duration = _numFrames / (float)_fps;

        OnDestroy();
        _texture = new Texture2D(_frameInfo.size.x, _frameInfo.size.y, _frameInfo.format, false);
        _texture.wrapMode = TextureWrapMode.Clamp;
        _texture.hideFlags = HideFlags.DontSave;

        CurrentTime = 0;
        LoadCurrentFrame();

        IsValid = true;
    }
    #endregion // Private method

    #region Public API method 
    public void Play()
    {
        if(_indexTime >= _numFrames - 1)
            CurrentTime = 0;

        IsPlaying = true;
    }

    public void Stop()
    {
        IsPlaying = false;
        CurrentTime = 0;
    }

    public void Pause()
    {
        IsPlaying = false;
    }

    public void Resume()
    {
        CurrentTime = 0;
    }

    public void OpenSequenceFromDirectory(string seqauencePath)
    {
        _sequencePath = seqauencePath;
        InitSequence();
    }
    #endregion // Public API Method
}

}