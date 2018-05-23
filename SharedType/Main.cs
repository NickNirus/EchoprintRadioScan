using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedType
{
    public class ModelsType
    {
    }

    public class EchoResponse
    {

        public string total_time { get; set; }
        public string score { get; set; }
        public bool ok { get; set; }
        public EchoTrackInfo track_info { get; set; }
        public string message { get; set; }


    }

    public class EchoTrackInfo
    {

        public string artist { get; set; }
        public string track { get; set; }
        public string track_id { get; set; }
        public string codever { get; set; }
        public string length { get; set; }

        public string score { get; set; }
        public string source { get; set; }
        public string release { get; set; }
        public string genre { get; set; }
    }


    public class _Code
    {
        public _Code()
        {
            metadata = new _Metadata();
        }
        public _Metadata metadata { get; set; }
        public int code_count { get; set; }
        public string code { get; set; }
        public int tag { get; set; }
    }

    public class _Metadata
    {

        public float codegen_time { get; set; }
        public string title { get; set; }
        public float decode_time { get; set; }

        public string artist { get; set; }

        public int start_offset { get; set; }
        public string filename { get; set; }
        public int given_duration { get; set; }
        public float version { get; set; }

        public int sample_rate { get; set; }

        public int samples_decoded { get; set; }
        public int duration { get; set; }
        public string release { get; set; }
        public string genre { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string track_id { get; set; }
        public int bitrate { get; set; }
    }


    public class Code
    {
        public Code()
        {
            metadata = new Metadata();
        }
        public Metadata metadata { get; set; }
        public int code_count { get; set; }
        public string code { get; set; }
        public int tag { get; set; }
        public string error { get; set; }
    }

    public class Metadata
    {
        public uint Id { get; set; }
        public string artist_id { get; set; }
        public string artist { get; set; }
        public string release { get; set; }
        public string title { get; set; }
        public string genre { get; set; }
        public int bitrate { get; set; }
        public int sample_rate { get; set; }
        public int duration { get; set; }
        public string filename { get; set; }
        public int samples_decoded { get; set; }
        public int given_duration { get; set; }
        public int start_offset { get; set; }
        public float version { get; set; }
        public double codegen_time { get; set; }
        public double decode_time { get; set; }
        public int[] codes { get; set; }
        public int[] times { get; set; }
        public string Frequency { get; set; }
        public double Coincidences { get; set; }
        public string Status { get; set; }
        public uint TrackId { get; set; }

        public string Track_Id { get; set; }
        public Guid TrackGuid { get; set; }
        public string Histogram { get; set; }
        public string Size { get; set; }
        //public DateTime DataCreated  { get; set; }
        //public DateTime LastModifiedOn { get; set; }

    }


    public class RequiredMeta
    {
        public string artist { get; set; }
        public string title { get; set; }
        public string genre { get; set; }
    }


    public class Response
    {
        public Response(Metadata metadata, string error)
        {
            Metadata = metadata;
            Error = error;
        }

        public Response(List<Metadata> metadataList, string error)
        {
            MetadataList = metadataList;
            Error = error;
        }

        public Response(string message)
        {
            Responce = message;
        }

        public string Responce { get; set; }
        public Metadata Metadata { get; set; }

        public List<Metadata> MetadataList { get; set; }

        public string Error { get; set; }
    }

    public class ResponseFull
    {
        public ResponseFull(Code code, string error)
        {
            Code = code;
            Error = error;
        }

        public ResponseFull(string message)
        {
            Responce = message;
        }

        public string Responce { get; set; }
        public Code Code { get; set; }

        public string Error { get; set; }
    }
}
