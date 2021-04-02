using System;
using System.Collections.Generic;
using System.Text;

namespace ImageRekognition
{
    class RcImage
    {
        public string ID { get; set; } //auto generated guid for primary key

        public string bucketName { get; set; }

        public string objectName { get; set; }

        public string label { get; set; }

        public string etag { get; set; }
        public long size { get; set; }

        public DateTime created { get; set; }
        public DateTime updated { get; set; }
    }
}
