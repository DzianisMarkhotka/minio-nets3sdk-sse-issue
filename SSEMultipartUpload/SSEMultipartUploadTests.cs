using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using NUnit.Framework;

namespace SSEMultipartUpload
{
    public class SSEMultipartUploadTests
    {
        private const string AccessKey = "minioadmin";
        private const string SecretKey = "minioadmin";
        private const string Endpoint = "https://localhost:9000";
        private const string TestBucket = "test-bucket";

        private AmazonS3Client _s3Client;

        [SetUp]
        public async Task Setup()
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1,
                ServiceURL = Endpoint,
                ForcePathStyle = true
            };
            _s3Client = new AmazonS3Client(AccessKey, SecretKey, config);
            var bucketsResponse = await _s3Client.ListBucketsAsync();
            if (!bucketsResponse.Buckets.Exists(b => b.BucketName.Equals(TestBucket)))
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = TestBucket,
                    UseClientRegion = true
                };
                await _s3Client.PutBucketAsync(putBucketRequest);
            }
        }

        [Test]
        public void SSEMultipartUploadTest()
        {
            var testKey = "test.tst";
            var size = 1 * 1024 * 1024;
            var data = new byte[size];
            new Random().NextBytes(data);
            
            Aes aesEncryption = Aes.Create();
            aesEncryption.KeySize = 256;
            aesEncryption.GenerateKey();
            var encKey = Convert.ToBase64String(aesEncryption.Key);
            
            
            Assert.DoesNotThrowAsync(async () =>
            {
                var initiateRequest = new InitiateMultipartUploadRequest
                {
                    BucketName = TestBucket,
                    Key = testKey,
                    ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256,
                    ServerSideEncryptionCustomerProvidedKey = encKey
                };
                var initResponse = await _s3Client.InitiateMultipartUploadAsync(initiateRequest);
                // Assert.IsNotNull(initResponse.ServerSideEncryptionCustomerMethod);
                // Assert.IsNotNull(initResponse.ServerSideEncryptionCustomerProvidedKeyMD5);
                try
                {
                    UploadPartResponse uploadResponse;
                    await using (var stream = new MemoryStream(data))
                    {
                        UploadPartRequest uploadRequest = new UploadPartRequest
                        {
                            BucketName = TestBucket,
                            Key = testKey,
                            UploadId = initResponse.UploadId,
                            PartNumber = 1,
                            PartSize = size,
                            FilePosition = 0,
                            InputStream = stream,
                            ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256,
                            ServerSideEncryptionCustomerProvidedKey = encKey,
                        };
                        
                        uploadResponse = await _s3Client.UploadPartAsync(uploadRequest);
                    }

                    CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest
                    {
                        BucketName = TestBucket,
                        Key = testKey,
                        UploadId = initResponse.UploadId
                    };
                    completeRequest.AddPartETags(uploadResponse);

                    var completeUploadResponse = await _s3Client.CompleteMultipartUploadAsync(completeRequest);
                }
                catch
                {
                    var abortMPURequest = new AbortMultipartUploadRequest
                    {
                        BucketName = TestBucket,
                        Key = testKey,
                        UploadId = initResponse.UploadId
                    };
                    await _s3Client.AbortMultipartUploadAsync(abortMPURequest);
                    throw;
                }
            });
        }
    }
}