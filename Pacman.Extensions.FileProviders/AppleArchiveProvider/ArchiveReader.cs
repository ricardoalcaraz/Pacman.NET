using System.Buffers;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pacman.Extensions.FileProviders.AppleArchiveProvider;

/// <summary>
///     Decode a tarball and create a directory map of all files that live inside
/// </summary>
public class ArchiveReader(Stream stream, bool leaveOpen = false) : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     Read the header information from the stream
    /// </summary>
    /// <param name="headerBuffer"></param>
    /// <returns></returns>
    public TarFileInfo ReadHeader(byte[] headerBuffer)
    {
        //technically header encoding is always ascii however we use UTF8 
        //as it's compatible and the rest of file is UTF8
        var fileName = Encoding.UTF8.GetString(headerBuffer[..100]);
        var fileMode = Encoding.UTF8.GetString(headerBuffer[100..108]);
        var groupId = Encoding.UTF8.GetString(headerBuffer[108..116]);
        var userId = Encoding.UTF8.GetString(headerBuffer[116..124]);
        //NOTE: this value is in octal encoded with ASCII
        var fieldSize = Encoding.UTF8.GetString(headerBuffer[124..136]);
        var lastModTime = Encoding.UTF8.GetString(headerBuffer[136..148]);
        var checksum = headerBuffer[148..156];
        var typeFlag = Encoding.UTF8.GetString(headerBuffer[156..157]);
        var linkedFileName = Encoding.UTF8.GetString(headerBuffer[157..257]);
        var ustar = Encoding.UTF8.GetString(headerBuffer[257..263]);
        var ustarVersion = Encoding.UTF8.GetString(headerBuffer[263..265]);
        var ownerUserName = Encoding.UTF8.GetString(headerBuffer[265..297]);
        var groupUsername = Encoding.UTF8.GetString(headerBuffer[297..354]);
        var fileNamePrefix = Encoding.UTF8.GetString(headerBuffer[345..500]);
        var restOfFile = Encoding.UTF8.GetString(headerBuffer[500..512]);
        if (typeFlag == "x")
        {
            var metadata = Encoding.UTF8.GetString(headerBuffer[512..1024].TakeWhile(c => c != 0).ToArray()).Trim();
        }

        return new TarFileInfo(); //typeFlag == "x" ? 1024 : 512;
    }


    /// <summary>
    ///     Read the entirety of a file based on the information contained in the <paramref name="header" />
    /// </summary>
    public Task ReadFile(TarFileInfo header)
    {
        return Task.CompletedTask;
    }


    public async Task ParseTarFile(Stream fileStream)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
// var db = Path.GetTempFileName();
// var tmpDb = File.Create(db);


// if (int.TryParse(fieldSize, out var size))
// {
//     var buffer = ArrayPool<byte>.Shared.Rent(size);
//     await gzipStream.ReadExactlyAsync(buffer, 0, size);
//     var cont = Encoding.UTF8.GetString(buffer);
//     ArrayPool<byte>.Shared.Return(buffer);
// }
//

//var size = header[124..136];
//IEnumerable<char> chars = size.Select(s => (char)s);
//var intFileSize = int.Parse(chars.ToArray());
//var headerString = Encoding.UTF8.GetString(header).Trim();
//await gzipStream.CopyToAsync(tmpDb);
//await tmpDb.DisposeAsync();
        var streamReader = new StreamReader(gzipStream, Encoding.UTF8, true);
        var packageList = new List<string>();

        while (!streamReader.EndOfStream)
        {
            //file is split in 512 large blocks
            //header info is laid below
            var headerBuffer = ArrayPool<byte>.Shared.Rent(3072);
            var maxBytes = await gzipStream.ReadAtLeastAsync(headerBuffer, 3072, false);
            var bytes = headerBuffer[..maxBytes];
            var headerInfo = ReadHeader(bytes);
            var numB = 0;
            var start = 512;
            var end = start + 512;
            //numB = ReadHeader(bytes[start..end]);
            start += numB;
            end += numB;
            var nextHeader2 = Encoding.UTF8.GetString(bytes[start..^512]);
            var endL = nextHeader2[^31..];
            start = 3072 + 512;
            ReadHeader(bytes[start..]);
            var nextHeader4 = Encoding.UTF8.GetString(headerBuffer[^512..]);
            ArrayPool<byte>.Shared.Return(headerBuffer);
            
            var stringBuilder2 = new StringBuilder();


            //read header
            //var headerBuffer = ArrayPool<byte>.Shared.Rent(3072);
            var sbuffer = ArrayPool<byte>.Shared.Rent(512);

            var bytesRead = 0;
            do
            {
                bytesRead = await gzipStream.ReadAsync(sbuffer, 0, 512);
                var cont = Encoding.UTF8.GetString(sbuffer[..bytesRead]);
                stringBuilder2.Append(cont);
            } while (sbuffer[..bytesRead] is not [.., 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);

            var finalString = stringBuilder2.ToString().Trim();
            ArrayPool<byte>.Shared.Return(sbuffer);
            //
            // var line = await streamReader.ReadLineAsync() ?? string.Empty;
            // if (line is ['%', ..var name, '%'])
            // {
            //     //following lines are the values
            //     var stringBuilder = new StringBuilder();
            //     while (!string.IsNullOrWhiteSpace(line = await streamReader.ReadLineAsync()))
            //     {
            //         stringBuilder.AppendLine(line);
            //     }
            //
            //     app._logger.LogTrace("{Name} contains following values:\n{Val}", name, stringBuilder);
            // }
            // else if (line.StartsWith("PaxHeader"))
            // {
            //     var fileName = line;
            //     while (!string.IsNullOrWhiteSpace(line))
            //     {
            //         fileName = line;
            //         line = await streamReader.ReadLineAsync();
            //     }
            //
            //     if (string.IsNullOrWhiteSpace(fileName))
            //     {
            //     }
            //     else
            //     {
            //         app._logger.LogDebug("Processing {File}", fileName);
            //         packageList.Add(fileName);
            //     }
            // }
        }

        stopWatch.Stop();
    }


    public Task ReadCompressedTar(string path)
    {
        return Task.CompletedTask;
    }


    /// <summary>
    ///     Check if the file starts with a magic number indicating Gzip compression
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Task<bool> IsCompressed(string path)
    {
        return Task.FromResult(true);
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }

    public async ValueTask DisposeAsync()
    {
        await stream.DisposeAsync();
    }
}

public record TarFileInfo
{
}