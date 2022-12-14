using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MusicProgress.Models;
using MusicProgress.Services.Interfaces;

namespace MusicProgress.Controllers
{
    [ApiController]
    [Route("api/audio")]
    public class AudioController : ControllerBase
    {
        private readonly IAudioService _audioService;

        public AudioController(IAudioService audioService)
        {
            _audioService = audioService;
        }
        [HttpPost("")]
        public async Task<IActionResult> UploadAudio([FromForm] AudioModel model)
        {
            if (model.AudioFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await model.AudioFile.CopyToAsync(memoryStream);
                var id = await _audioService.AddAudioForLessonAsync(memoryStream, model.LessonId);
                var url = await _audioService.GetUrlAudioAsync(id);
                return Ok(new {UploadAudioId = id, Url = url});
            }

            return Ok();
        }

        [HttpGet("{fileName}")]
        public async Task<IActionResult> DownloadAudio(string fileName)
        {
            var stream = await _audioService.GetAudioAsync(fileName);
            return File(stream, "application/octet-stream");
        }
    }
}