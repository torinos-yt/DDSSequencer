using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

#if HAS_TIMELINE
using UnityEngine.Timeline;
#endif

namespace DDSSequencer.Runtime
{

[ExecuteInEditMode, AddComponentMenu("DDS Sequencer/Sequence Player")]
#if HAS_TIMELINE
public sealed class SequencePlayer : MonoBehaviour, ITimeControl, IPropertyPreview
#else
public sealed class SequencePlayer : MonoBehaviour
#endif
{
    #region Play Mode
    public enum SequncePlayMode
    {
        Time,
        Frame
    }
    #endregion // Play Mode

    #region Serialize field
    [SerializeField] string _sequencePath = "";

    [SerializeField] MeshRenderer _targetRenderer;
    [SerializeField] string _targetProperty = "";

    [SerializeField] RenderTexture _targetTexture;

    [SerializeField] SequncePlayMode _playMode;
    [SerializeField] float _speed = 1.0f;
    [SerializeField] bool _playOnAwake = false;
    [SerializeField] bool _loop = false;

    [SerializeField] float _time = 0f;

    /// <summary>
    /// This parameter can be changed at any time,
    /// but the actual playback is reflected
    /// in the timing of Start() or OpenSequenceFromDirectory().
    /// </summary>
    [SerializeField] bool _allFrameCache = false;
    #endregion

    #region Private field
    int _numFrames = int.MaxValue;
    float _duration = 0;
    int _fps = 0;

    int _indexTime => Mathf.FloorToInt(_time * _fps);
    int _prevIndexTime = 0;
    bool _timeline;
    bool _cachingFrame;

    Texture2D _texture;
    MaterialPropertyBlock _prop;

    string[] _frames;
    byte[][] _cachedFrames;
    bool _finishCaching = false;

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

    public SequncePlayMode PlayMode
    {
        get => _playMode;
        set { _playMode = value; }
    }

    /// <summary>
    /// This parameter can be changed at any time,
    /// but the actual playback is reflected
    /// in the timing of Start() or OpenSequenceFromDirectory().
    /// </summary>
    public bool AllFrameCache
    {
        get => _allFrameCache;
        set { _allFrameCache = value; }
    }

    public bool IsPlaying { get; private set; } = false;
    public bool IsValid { get; private set; } = false;
    public bool IsCacheFinished => _finishCaching;
    public float Duration => _duration;
    public int FrameCount => _numFrames;
    public int CurrentFrame => _indexTime;
    public Texture2D Texture => _texture;

    public (Vector2Int size, int fps, TextureFormat format, bool mips) FrameInfo => _frameInfo;
    #endregion // Punlic accesible property

    #region MonoBehabiour method implemetation
    void Start()
    {
        _cachingFrame = _allFrameCache;
        _finishCaching = !_allFrameCache;
        InitSequence(_cachingFrame);

        if(_playOnAwake) IsPlaying = true;
    }

    void LateUpdate()
    {
        if(IsValid && IsPlaying && Application.isPlaying && !_timeline)
        {
            if(_playMode == SequncePlayMode.Time)
                CurrentTime += Time.deltaTime * Speed;
            else
                CurrentTime = (_indexTime + 1) / (float)_fps + 1e-4f;
        }
    }

    void OnDestroy()
    {
        if(_cachedFrames != null) for(int i = 0; i < _cachedFrames.Length; i++) _cachedFrames[i] = null;

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

    #region Private method
    async void LoadCurrentFrame()
    {
        IntPtr ptr = IntPtr.Zero;
        int size = 0;

        if(!_cachingFrame || !Application.isPlaying)
        {
            byte[] bytes = await Task.Run(() =>
            {
                return File.ReadAllBytes(_frames[_indexTime]);
            });

            if(_texture == null) return;

            ptr = Snappy.DecodeToPtr(bytes, out size);
        }
        else if(_finishCaching)
        {
            if(_texture == null) return;

            ptr = Snappy.DecodeToPtr(_cachedFrames[_indexTime], out size);
        }

        _texture.LoadRawTextureData(ptr, size);
        _texture.Apply();

        Marshal.FreeCoTaskMem(ptr);

        UpdateTarget();
    }

    void UpdateTarget()
    {
        if(_targetRenderer != null)
        {
            _prop ??= new MaterialPropertyBlock();

            _targetRenderer.GetPropertyBlock(_prop);
            _prop.SetTexture(_targetProperty, _texture);
            _targetRenderer.SetPropertyBlock(_prop);
        }

        if(_targetTexture != null)
            Graphics.Blit(_texture, _targetTexture);
    }

    async void InitSequence(bool cacheFrame = false)
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

        if(cacheFrame && Application.isPlaying)
        {
            _cachingFrame = true;
            _finishCaching = false;

            _cachedFrames = new byte[_numFrames][];

            await Task.Run(() =>
            {
                Enumerable.Range(0, _numFrames)
                    .AsParallel()
                    .WithDegreeOfParallelism(4)
                    .ForAll(n =>
                {
                    _cachedFrames[n] = File.ReadAllBytes(_frames[n]);
                });
            });

            _finishCaching = true;
        }
        else
        {
            _cachingFrame = false;
            _finishCaching = false;
        }

        _texture = new Texture2D(_frameInfo.size.x, _frameInfo.size.y, _frameInfo.format, _frameInfo.mips);
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

    public void OpenSequenceFromDirectory(string seqauencePath, bool cacheFrame = false)
    {
        _sequencePath = seqauencePath;
        InitSequence(cacheFrame);
    }
    #endregion // Public API Method
}

}