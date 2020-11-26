using System.IO;
using Nancy;

namespace Lidarr.Http
{
    public class ByteArrayResponse : Response
    {
        public ByteArrayResponse(byte[] body, string contentType)
        {
            ContentType = contentType;

            Contents = stream =>
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(body);
                }
            };
        }
    }
}