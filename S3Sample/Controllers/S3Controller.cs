using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace S3Sample.Controllers
{
    public class S3Controller : ApiController
    {
        private IAmazonS3 client;

        // Ideally all this configuration should be retrieved from the settings file
        // Enter your credentials here and specify the bucket name. 
        private readonly RegionEndpoint bucketRegion = RegionEndpoint.EUCentral1;
        private readonly string bucketName = "***";
        private readonly string accessKeyId = "***";
        private readonly string secretKeyId = "***";

        // Specify after how many minutes the presigned urls will expire
        private readonly int downloadPresignedUrlDuration = 5;
        private readonly int uploadPresignedUrlDuration = 5;

        public S3Controller()
        {
            client = new AmazonS3Client(new BasicAWSCredentials(accessKeyId, secretKeyId), bucketRegion);
        }

        // POST /api/s3/upload/server?filename=myfile.txt
        /// <summary>
        /// Upload local server file to the S3 bucket
        /// </summary>
        /// <param name="filename">The name of the local server file. In this sample project the files are kept in the Files folder</param>
        /// <returns>Status message</returns>
        [HttpPost]
        [Route("api/s3/upload/server")]
        public async Task<IHttpActionResult> UploadFromServerToS3(string filename)
        {
            // Get full path for the local server file that should be uploader to S3 and is located in the Files directory
            string filepath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", filename);

            if (!File.Exists(filepath))
            {
                return BadRequest(string.Format("Local file with name {0} doesn't exist", filename));
            }

            // Construct the request object
            PutObjectRequest putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = filename,
                FilePath = filepath
            };

            try
            {
                // Execute the upload request
                PutObjectResponse response = await client.PutObjectAsync(putRequest);
                return Ok("File successfully uploaded from server to S3");
            }
            catch (AmazonS3Exception e)
            {
                return BadRequest(string.Format("Error encountered ***. Message:'{0}' when writing an object", e.Message));
            }
            catch (Exception e)
            {
                return BadRequest(string.Format("Unknown encountered on server. Message:'{0}' when writing an object", e.Message));
            }
        }

        // POST /api/s3/download/server?filename=myfile.txt
        /// <summary>
        /// Download a file from S3 bucket to the server local file-system
        /// </summary>
        /// <param name="filename">The filename of the file in the S3 bucket that should be downloaded</param>
        /// <returns>Status message</returns>
        [HttpPost]
        [Route("api/s3/download/server")]
        public async Task<IHttpActionResult> DownloadFromS3ToServer(string filename)
        {
            // Get the full path to the destination in the server local file-system where the S3 object will be saved
            string filepath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", string.Format("{0}_{1}", new Random().Next(100000), filename));

            // Construct the request object
            GetObjectRequest getRequest = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = filename
            };

            try
            {
                // 1. Read the S3 object
                // 2. Get the stream of the S3 object
                // 3. Open output stream to the local file and write the S3 object in that local file
                using (GetObjectResponse response = await client.GetObjectAsync(getRequest))
                using (Stream responseStream = response.ResponseStream)
                using (FileStream outputStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                {
                    await responseStream.CopyToAsync(outputStream);
                }

                return Ok("File successfully downloaded from S3 to server");
            }
            catch (AmazonS3Exception e)
            {
                return BadRequest(string.Format("Error encountered ***. Message:'{0}' when getting an object", e.Message));
            }
            catch (Exception e)
            {
                return BadRequest(string.Format("Unknown encountered on server. Message:'{0}' when getting an object", e.Message));
            }
        }


        // GET /api/s3/upload/presignedurl?filename=myfile.txt
        /// <summary>
        /// Get a presigned url which will be used in the javascript client to upload a file to S3
        /// </summary>
        /// <param name="filename">The name of the file which is going to be uploaded by the client</param>
        /// <returns>The presigned url or error message if something failed</returns>
        [HttpGet]
        [Route("api/s3/upload/presignedurl")]
        public IHttpActionResult GetUploadPresignedUrl(string filename)
        {
            string key = filename;
            string contentDispositionHeader = string.Format("attachment; filename=\"{0}\"", filename);

            GetPreSignedUrlRequest preSignedUrlRequest = new GetPreSignedUrlRequest()
            {
                BucketName = bucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.Now.AddMinutes(uploadPresignedUrlDuration),
                ResponseHeaderOverrides = new ResponseHeaderOverrides
                {
                    ContentDisposition = contentDispositionHeader
                }
            };

            try
            {
                string url = client.GetPreSignedURL(preSignedUrlRequest);
                return Ok(url);
            }
            catch (AmazonS3Exception e)
            {
                return BadRequest(string.Format("Error encountered ***. Message:'{0}' when getting an object", e.Message));
            }
            catch (Exception e)
            {
                return BadRequest(string.Format("Unknown encountered on server. Message:'{0}' when getting an object", e.Message));
            }
        }

        // GET /api/s3/download/presignedurl?filename=myfile.txt
        /// <summary>
        /// Get a presigned url which will be used in the javascript client to download a file from S3
        /// </summary>
        /// <param name="filename">The name of the file which is going to be downloaded by the client</param>
        /// <returns>The presigned url or error message if something failed</returns>
        [HttpGet]
        [Route("api/s3/download/presignedurl")]
        public IHttpActionResult GetDownloadPresignedUrl(string filename)
        {
            string key = filename;
            string contentDispositionHeader = string.Format("attachment; filename=\"{0}\"", filename);

            GetPreSignedUrlRequest preSignedUrlRequest = new GetPreSignedUrlRequest()
            {
                BucketName = bucketName,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.Now.AddMinutes(downloadPresignedUrlDuration),
                ResponseHeaderOverrides = new ResponseHeaderOverrides
                {
                    ContentDisposition = contentDispositionHeader
                }
            };

            try
            {
                string url = client.GetPreSignedURL(preSignedUrlRequest);
                return Ok(url);
            }
            catch (AmazonS3Exception e)
            {
                return BadRequest(string.Format("Error encountered ***. Message:'{0}' when getting an object", e.Message));
            }
            catch (Exception e)
            {
                return BadRequest(string.Format("Unknown encountered on server. Message:'{0}' when getting an object", e.Message));
            }
        }
    }
}
