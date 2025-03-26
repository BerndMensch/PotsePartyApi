using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.Exceptions;
using NanoidDotNet;

namespace PotsePartyApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FilesController : Controller
    {
        private readonly ILogger<FilesController> _logger;
        string endpoint = "storage.potseparty.de";
        string accessKey = "kJBpR4CPhoM2doSgPOvw";
        string secretKey = "HtC0tAJEEW4WXQgOWDXKKmO9NXoagDIVY6aVJYfb";

        readonly MinioClient _minio;

        public FilesController(ILogger<FilesController> logger)
        {
            _logger = logger;

            try
            {
                _minio = new MinioClient()
                    .WithEndpoint(endpoint)
                    .WithCredentials(accessKey,
                        secretKey).WithSSL(false)
                    .Build();

                _minio.SetAppInfo("PotseParty API", "1.0");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        
        [HttpPost("UploadFile", Name = "UploadFile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<string>> UploadFile(
            IFormFile file, string bucket, string? folder,
            CancellationToken cancellationToken)
        {
            try
            {
                bool found = await _minio.BucketExistsAsync(bucket);
                if (!found)
                {
                    await _minio.MakeBucketAsync(bucket);
                    string policyJson =
                        string.Format(
                            "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Action\":[\"s3:GetObject\"],\"Effect\":\"Allow\",\"Principal\":{\"AWS\":[\"*\"]},\"Resource\":[\"arn:aws:s3:::{0}/*\"],\"Sid\":\"\"}]}",
                            bucket);
                    SetPolicyArgs args = new SetPolicyArgs()
                        .WithBucket(bucket)
                        .WithPolicy(policyJson);
                    await _minio.SetPolicyAsync(args);
                }

                var stream = file.OpenReadStream();
                var id = Nanoid.Generate();
                var ext = Path.GetExtension(file.FileName);
                var filename = id + ext;
                if (!string.IsNullOrEmpty(folder))
                {
                    filename = folder + "/" + id + ext;
                }

                await _minio.PutObjectAsync(bucket, filename, stream, stream.Length);
                return $"https://storage.potseparty.de/{bucket}/{filename}";
            }
            catch (MinioException e)
            {
                return BadRequest(new {message = e.Message});
            }
        }

        [Authorize]
        [HttpDelete("DeleteFile", Name = "DeleteFile")]
        public async Task<ActionResult<bool>> DeleteFile(string id, string token)
        {
            return false;
        }
    }
}