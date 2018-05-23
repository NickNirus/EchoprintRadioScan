// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Code.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the Code type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
namespace EchoprintManager.Data
{
    using SharedType;
    using System.Collections.Generic;

    /*
     "MetaData": {
          "Id": 20015,
          "artist_id": "20012",
          "artist": "Ю�€ий Ан�‚онов",
          "release": "",
          "title": "�ž�‚ �Ÿе�‡али �”о Радос�‚и (zvukoff.ru)",
          "genre": "genre",
          "bitrate": 192,
          "sample_rate": 44100,
          "duration": 187,
          "filename": "D:\\ClientReposotory\\superpuper\\69c25c8b-e480-4286-aab5-814ddb7f6656.mp3",
          "samples_decoded": 330902,
          "given_duration": 30,
          "start_offset": 0,
          "version": "4.12",
          "codegen_time": 0.243,
          "decode_time": 1.077,
          "codes": null,
          "times": null,
          "Frequency": null,
          "Coincidences": 0,
          "Status": "Success",
          "TrackId": 20015,
          "TrackGuid": "69c25c8b-e480-4286-aab5-814ddb7f6656",
          "Histogram": null,
          "Size": null
        },
     
     */

    //public class Code
    //{
    //    public Code()
    //    {
    //        metadata = new Metadata();
    //    }
    //    public Metadata metadata { get; set; }
    //    public int code_count { get; set; }
    //    public string code { get; set; }
    //    public int tag { get; set; }
    //    public string error { get; set; }
    //}

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
        public string version { get; set; }
        public double codegen_time { get; set; }
        public double decode_time { get; set; }
        public int[] codes { get; set; }
        public int[] times { get; set; }
        public string Frequency { get; set; }
        public double Coincidences { get; set; }
        public string Status { get; set; }
        public uint TrackId { get; set; }
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

