using System.IO;
using UnityEngine;

public static class WavUtility
{
    const int HEADER_SIZE = 44;

    /// <summary>
    /// Save the AudioClip data to a WAV file at persistentDataPath/filename.
    /// </summary>
    public static void SaveWav(string filename, AudioClip clip)
    {
        string filepath = Path.Combine(Application.persistentDataPath, filename);
        using (var fileStream = CreateEmpty(filepath))
        {
            ConvertAndWrite(fileStream, clip);
            WriteHeader(fileStream, clip);
        }
        Debug.Log($"WAV saved to: {filepath}");
    }

    static FileStream CreateEmpty(string filepath)
    {
        var fs = new FileStream(filepath, FileMode.Create);
        byte[] header = new byte[HEADER_SIZE];
        fs.Write(header, 0, HEADER_SIZE);
        return fs;
    }

    static void ConvertAndWrite(FileStream fs, AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // Convert to 16-bit PCM
        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];
        const float rescaleFactor = 32767f; 

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            byte[] byteArr = System.BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fs.Write(bytesData, 0, bytesData.Length);
    }

    static void WriteHeader(FileStream fs, AudioClip clip)
    {
        int hz = clip.frequency;
        int channels = clip.channels;
        int samples = clip.samples;

        fs.Seek(0, SeekOrigin.Begin);
        using (var writer = new BinaryWriter(fs))
        {
            // RIFF
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(HEADER_SIZE + samples * channels * 2 - 8);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            // fmt 
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);                // Subchunk1Size for PCM
            writer.Write((short)1);          // AudioFormat = PCM
            writer.Write((short)channels);
            writer.Write(hz);
            writer.Write(hz * channels * 2); // ByteRate
            writer.Write((short)(channels * 2)); // BlockAlign
            writer.Write((short)16);         // BitsPerSample
            // data
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(samples * channels * 2);
        }
    }
}