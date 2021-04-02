using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace Lab4LambdaS3
{
    public class Function
    {
        private readonly string[] _supportedImageTypes = new string[] { ".png", ".jpg", ".jpeg" };
        private readonly AmazonS3Client _s3Client;
        private readonly string thumbnailBucket = "lab4thumbnails";
        private readonly int TileSize = 64;

        public Function()
        {
            _s3Client = new AmazonS3Client();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>

        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
        /// to respond to S3 notifications.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(S3Event s3Event, ILambdaContext context)
        {
            foreach (var record in s3Event.Records)
            {
                if (!_supportedImageTypes.Contains(Path.GetExtension(record.S3.Object.Key).ToLower()))
                {
                    Console.WriteLine(
                        $"Object {record.S3.Bucket.Name}:{record.S3.Object.Key} is not a supported image type");
                    continue;
                }

                Console.WriteLine(
                    $"Determining whether image {record.S3.Bucket.Name}:{record.S3.Object.Key} has been compressed");

                // Get the existing tag set
                var taggingResponse = await _s3Client.GetObjectTaggingAsync(new GetObjectTaggingRequest
                {
                    BucketName = record.S3.Bucket.Name,
                    Key = record.S3.Object.Key
                });

                if (taggingResponse.Tagging.Any(tag => tag.Key == "Compressed" && tag.Value == "true"))
                {
                    Console.WriteLine(
                        $"Image {record.S3.Bucket.Name}:{record.S3.Object.Key} has already been compressed");
                    continue;
                }

                // Get the existing image
                using (var objectResponse = await _s3Client.GetObjectAsync(record.S3.Bucket.Name, record.S3.Object.Key))
                using (Stream responseStream = objectResponse.ResponseStream)
                {
                    Console.WriteLine($"Compressing image {record.S3.Bucket.Name}:{record.S3.Object.Key}");


                    // Upload the compressed image back to S3
                    using (Image image = Image.Load(responseStream))
                    using (var imageBuffer = new MemoryStream())
                    {
                        image.Mutate(x => x
                                      .Resize(this.TileSize, this.TileSize)
                                      .Grayscale());
                        image.SaveAsJpeg(imageBuffer);

                        string thumbnailFileName = "tn_"+ record.S3.Object.Key;
                        Console.WriteLine($"Uploading the thumbnail image {thumbnailBucket}:{thumbnailFileName}");
                        await _s3Client.PutObjectAsync(new PutObjectRequest
                        {
                            BucketName = thumbnailBucket,
                            Key = thumbnailFileName,
                            InputStream = imageBuffer,
                            TagSet = new List<Tag>
                            {
                                new Tag
                                {
                                    Key = "Compressed",
                                    Value = "true"
                                },
                                new Tag
                                {
                                    Key = "Thumbnail",
                                    Value = "true"
                                }
                            }
                        });
                    }
                }
            }
        }
    }
}
