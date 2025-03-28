using System;
namespace YolBUl;





public class NextStop
{
    public string StopId { get; set; }
    public double Mesafe { get; set; }
    public double Sure { get; set; }
    public double Ucret { get; set; }

    public NextStop(string stopId, double mesafe, double sure, double ucret)
    {
        StopId = stopId;
        Mesafe = mesafe;
        Sure = sure;
        Ucret = ucret;
    }

    public static (Vehicle, double) findNearestStop(double lat, double lon, List<Vehicle> stops)
    {
        Vehicle? nearest_stop = null; //null hatasını önlemek için
        double min_distance = 0;
        foreach (var stop in stops)
        {
            double distance = Vehicle.distance(lat, lon, stop.Lat, stop.Lon);
            if (nearest_stop == null || distance < min_distance)
            {
                nearest_stop = stop;
                min_distance = distance;
            }
        }
        
        return (nearest_stop, min_distance);

    }
    
}

public class Transfer
{
    public string TransferStopId { get; set; }
    public double TransferSure { get; set; }
    public double TransferUcret { get; set; }

    public Transfer(string transferStopId, double transferSure, double transferUcret)
    {
        TransferStopId = transferStopId;
        TransferSure = transferSure;
        TransferUcret = transferUcret;
    }
}