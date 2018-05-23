
namespace EchoprintManager
{
    public interface ResulInfo
    {
        uint Id { get; set; }
      
        uint TrackId { get; set; }
       
        string Frequency { get; set; }
        
        double Coincidences { get; set; }
       
        EchonestResult Status { get; set; }

        string TimeStamp { get; set; }
    }
}