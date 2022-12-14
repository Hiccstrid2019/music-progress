using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Minio;
using MusicProgress.Services.Interfaces;

namespace MusicProgress.Services;

public class MinIoService : IFileAppService
{
    private readonly MinioClient _client;
    private readonly MinioConfig _config;

    public MinIoService(MinioClient client, IOptions<MinioConfig> options)
    {
        _client = client;
        _config = options.Value;
    }

    public async Task UploadObjectAsync(string name, Stream data)
    {
        data.Seek(0, SeekOrigin.Begin);
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_config.BucketName)
            .WithObject(name)
            .WithObjectSize(data.Length)
            .WithStreamData(data)
            .WithContentType(_config.ContentType);
        await _client.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
    }

    public async Task<Stream> GetObjectAsync(string name)
    {
        using var memoryStream = new MemoryStream();
        var getObjectArgs = new GetObjectArgs()
            .WithBucket(_config.BucketName)
            .WithObject(name)
            .WithCallbackStream((stream) =>
            {
                stream.CopyToAsync(memoryStream);
            });
        await _client.GetObjectAsync(getObjectArgs);

        return memoryStream;
    }

    public async Task<string> GetUrlObjectAsync(string name)
    {
        var getObjectArgs = new PresignedGetObjectArgs()
            .WithBucket(_config.BucketName)
            .WithObject(name)
            .WithExpiry(60 * 60 * 24);
        var url = await _client.PresignedGetObjectAsync(getObjectArgs);
        return url;
    }
}