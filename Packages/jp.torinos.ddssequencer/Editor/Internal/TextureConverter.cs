using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using DDSSequencer.Runtime;

namespace DDSSequencer.Editor.Pipeline
{

using SourceType = SeqnenceConverterWindow.SequenceSource;

internal static class TextureConverter
{
    public static int Count { get; private set; }
    public static int CompleteCount { get; private set; }

    public static bool IsffmpegProcess { get; private set; }
    public static bool IsDdsProcess { get; private set; }
    public static bool IsOptProcess { get; private set; }
    public static bool IsProceeding { get; private set; }

    static SeqnenceConverterWindow _window;

    static EditorCoroutine PipelineCoroutine = null;
    static EditorCoroutine ffmpegtCoroutine = null;
    static EditorCoroutine ConvertCoroutine = null;
    static EditorCoroutine OptimizeCoroutine = null;

    static System.Diagnostics.Process _process = null;
    static string _batchPath = null;

    public enum CompressFormat
    {
        BC7 = 23,
        DXT5 = 18,
        DXT1 = 15,
        BC5 = 21
    }

    public enum DDSCompressQuality
    {
        Fastest = 0,
        Normal,
        Production,
        Highest
    }

    public struct ExportSetting
    {
        public CompressFormat format;
        public DDSCompressQuality quality;
        public bool yFlip;
        public bool useCuda;
        public bool mips;
        public bool deleteTmp;
        public int fps;
    }

    // Entry Point
    public static void Process(string path, string outPath, string nvttPath, ExportSetting setting, SourceType sourceType)
    {
        if(PipelineCoroutine != null && IsProceeding)
        {
            Debug.LogError("[DDSSequencer Converter Window] Error : Proceeding Previous Process");
            return;
        }

        if(sourceType == SourceType.Sequence)
        {
            if(!Directory.Exists(path))
            {
                Debug.LogError("[DDSSequencer Converter Window] Error : Directory Not Found");
                return;
            }
        }
        else
        {
            if(!File.Exists(path))
            {
                Debug.LogError("[DDSSequencer Converter Window] Error : File Not Found");
                return;
            }
        }

        _window = SeqnenceConverterWindow.GetWindow<SeqnenceConverterWindow>();

        PipelineCoroutine =
            _window.StartCoroutine(Pipeline(path, outPath, nvttPath, setting, sourceType));
    }

    static IEnumerator Pipeline(string path, string outPath, string nvttPath, ExportSetting setting, SourceType sourceType)
    {
        IsProceeding = true;

        if(sourceType == SourceType.Movie)
        {
            IsffmpegProcess = true;
            _window.Repaint();

            ffmpegtCoroutine = _window.StartCoroutine(ConvertMovie(path, outPath, setting));
            yield return ffmpegtCoroutine;

            IsffmpegProcess = false;
        }
        
        Directory.CreateDirectory(outPath + "/dds");

        string[] files = Directory.GetFiles(sourceType == SourceType.Sequence ? path : outPath + "/imagesc")
                                .Where(x => IsImageFileExtension(x)).ToArray();

        Count = files.Length;

        IsDdsProcess = true;
        _window.Repaint();

        ConvertCoroutine = _window.StartCoroutine(ConvertTextureSequence(files, outPath, nvttPath, setting));
        yield return ConvertCoroutine;

        IsDdsProcess = false;
        IsOptProcess = true;

        OptimizeCoroutine = _window.StartCoroutine(OptimizeDdsSequence(outPath, setting));
        yield return OptimizeCoroutine;

        IsOptProcess = false;

        // Delete tmp files
        if(setting.deleteTmp)
        {
            if(sourceType == SourceType.Movie)
                Directory.Delete(outPath + "/imagesc", true);

            Directory.Delete(outPath + "/dds", true);

            // Try delete metadata file, if output path is in Unity Asset folder.
            try
            {
                if(sourceType == SourceType.Movie)
                    File.Delete(outPath + "/imagesc.meta");

                File.Delete(outPath + "/dds.meta");
            }
            finally
            {
                
            }
        }

        IsProceeding = false;

        AssetDatabase.Refresh();
        _window.Repaint();

        Debug.Log("[DDSSequencer Converter Window] Convert Process Complete");
    }

    static IEnumerator ConvertMovie(string src, string outPath, ExportSetting setting)
    {
        Directory.CreateDirectory(outPath + "/imagesc");

        var info = new System.Diagnostics.ProcessStartInfo();
        info.FileName = "ffprobe";
        info.UseShellExecute = false;
        info.Arguments = @"""" + src + @"""" + " -hide_banner -show_entries format=duration";
        info.RedirectStandardOutput = true;
        info.CreateNoWindow = true;

        _process = System.Diagnostics.Process.Start(info);

        while(true)
        {
            if(_process.HasExited) break;
            yield return null;
        }

        float.TryParse(_process.StandardOutput.ReadToEnd().Split("\n")[1].Split("=")[1], out var duration);
        Count = (int)((int)duration * setting.fps);

        info.FileName = "ffmpeg";
        info.Arguments = "-i " + @"""" + src + @"""" + $" -r {setting.fps} -vcodec png "+ @"""" + $"{outPath}/imagesc/{Path.GetFileNameWithoutExtension(src)}_%06d.png" + @"""";
        info.RedirectStandardOutput = false;
        info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

        _process = System.Diagnostics.Process.Start(info);

        while(true)
        {
            if(_process.HasExited) break;

            CompleteCount = Directory.GetFiles(outPath + "/imagesc").Where(x => Path.GetExtension(x) == ".png").Count();
            _window.Repaint();
            yield return new WaitForSecondsRealtime(.5f);
        }

        CompleteCount = Count = 0;
    }

    static IEnumerator ConvertTextureSequence(string[] files, string outPath, string nvttPath, ExportSetting setting)
    {
        var info = new System.Diagnostics.ProcessStartInfo();
        info.FileName = nvttPath;
        info.UseShellExecute = true;

        string batchArgs = "";

        foreach(var f in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(f);

            batchArgs += @"""" + f + @"""" + $" /f {(int)(setting.format)} /q {(int)(setting.quality)} /dx10";
            if(!setting.useCuda) batchArgs += " /no-cuda";
            if(!setting.mips) batchArgs += " /no-mips";
            else batchArgs += " /mips";
            if(setting.yFlip) batchArgs += " /save-flip-y";
            batchArgs += @" /o """ + outPath + "/dds/" + fileName + @".dds""";

            batchArgs += "\n";
        }

        // Create simple text file include batch command arguments
        // https://forums.developer.nvidia.com/t/texture-tools-exporter-standalone-batch-scripting-command-line/145541
        _batchPath = outPath + "/batch.nvdds";
        File.WriteAllText(_batchPath, batchArgs);

        info.Arguments = "/b " + _batchPath;
        info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        _process = System.Diagnostics.Process.Start(info);

        Count = files.Length;

        while(true)
        {
            if(_process.HasExited) break;

            CompleteCount = Directory.GetFiles(outPath + "/dds").Where(x => Path.GetExtension(x) == ".dds").Count();
            _window.Repaint();
            yield return new WaitForSecondsRealtime(.5f);
        }

        // Avoid IOException
        yield return new EditorWaitForSeconds(.5f);

        try
        {
            File.Delete(_batchPath);
            File.Delete(_batchPath + ".meta");
        }
        finally
        {
            // Finalize
            _batchPath = null;
            _process = null;
        }

        CompleteCount = Count = 0;
    }

    static IEnumerator OptimizeDdsSequence(string outPath, ExportSetting setting)
    {
        string[] DdsFiles = Directory.GetFiles(outPath + "/dds");

        // Filtering .dds file
        DdsFiles = DdsFiles.Where(x => Path.GetExtension(x) == ".dds").ToArray();

        Count = DdsFiles.Length;

        const int DDS_HEADER_SIZE = 148;

        byte[] headerByte = File.ReadAllBytes(DdsFiles[0]);
        var header = new byte[7];

        // height
        header[0] = headerByte[12];
        header[1] = headerByte[13];

        // width
        header[2] = headerByte[16];
        header[3] = headerByte[17];
        
        // mips
        header[4] = Convert.ToByte(setting.mips ? 1 : 0);

        // format
        header[5] = Convert.ToByte((int)setting.format);

        // frame rate
        header[6] = Convert.ToByte(setting.fps);

        File.WriteAllBytes(outPath + "/header.ddsmeta", header);

        for(int i = 0; i < DdsFiles.Length; i++)
        {
            byte[] bytes = File.ReadAllBytes(DdsFiles[i]);
    
            byte[] bcBytes = new byte[bytes.Length - DDS_HEADER_SIZE];
            System.Buffer.BlockCopy(bytes, DDS_HEADER_SIZE, bcBytes, 0, bytes.Length - DDS_HEADER_SIZE);

            byte[] snapBytes = Snappy.Encode(bcBytes);
    
            string fileName = Path.GetFileNameWithoutExtension(DdsFiles[i]) + ".ddssc";
            File.WriteAllBytes(outPath + "/" + fileName, snapBytes);

            CompleteCount++;
            _window.Repaint();

            if(i % 10 == 0) yield return null;
        }

        _window.Repaint();
        yield return null;

        CompleteCount = Count = 0;
    }

    public static void StopProcess()
    {
        if(PipelineCoroutine != null)
            EditorCoroutineUtility.StopCoroutine(PipelineCoroutine);

        if(ffmpegtCoroutine != null)
            EditorCoroutineUtility.StopCoroutine(ffmpegtCoroutine);

        if(ConvertCoroutine != null)
            EditorCoroutineUtility.StopCoroutine(ConvertCoroutine);

        if(OptimizeCoroutine != null)
            EditorCoroutineUtility.StopCoroutine(OptimizeCoroutine);

        PipelineCoroutine = ffmpegtCoroutine = ConvertCoroutine = OptimizeCoroutine = null;

        if(_process != null && !_process.HasExited) _process.Kill();

        _process = null;

        // Delete batch arg file
        Task.Run(async () =>
        {
            // Avoid IOException
            await Task.Delay(500);

            try
            {
                if(_batchPath != null)
                {
                    File.Delete(_batchPath);
                    File.Delete(_batchPath + ".meta");
                }
            }
            finally
            {
                _batchPath = null;
            }
        });

        if(IsProceeding || IsffmpegProcess || IsDdsProcess || IsOptProcess)
            Debug.Log("[DDSSequencer Converter Window] Stop Convert Process");

        IsProceeding = false;
        IsffmpegProcess = false;
        IsDdsProcess = false;
        IsOptProcess = false;

        Count = 0;
        CompleteCount = 0;

        _window?.Repaint();
    }

    public static bool IsImageFileExtension(string file)
    {
        var extension = Path.GetExtension(file);

        return  extension == ".png" ||
                extension == ".jpg" ||
                extension == ".tiff"||
                extension == ".exr" ||
                extension == ".hdr" ||
                extension == ".dds";
    }
}

}
