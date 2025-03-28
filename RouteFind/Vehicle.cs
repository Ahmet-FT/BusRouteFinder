using System;
using System.Data.Common;
namespace YolBUl;




public abstract class Vehicle
{
    public string Id;
    public string Name;
    public string Type;
    public double Lat;
    public double Lon;
    public bool SonDurak;
    public List<NextStop> NextStops = new List<NextStop>();
    public Transfer Transfer = new Transfer("", 0, 0);
    
    public List<Vehicle> NextStList = new List<Vehicle>();
    public List<Vehicle> TransferList = new List<Vehicle>();
    
    public Vehicle(string id, string name, string type,double lat, double lon, bool sonDurak)
    {
        Id = id;
        Name = name;
        Type = type;
        Lat = lat;
        Lon = lon;
        SonDurak = sonDurak;
        //NextStops = nextStop;
        //Transfer = transfer;
    }

    public static void appendNextStop(List<Vehicle> stopList)
    {
        foreach (var stop in stopList)
        {
            foreach (var nextst in stop.NextStops)
            {
                foreach (var bus in stopList)
                {
                    if (bus.Id == nextst.StopId)
                    {
                        stop.NextStList.Add(bus);
                    }
                }
            }
        }
    }

    public static void appendTransfer(List<Vehicle> stopList, List<Vehicle> transferList)
    {
        foreach (var stop in stopList)
        {
            if (stop.Transfer != null)
            {
                foreach (var st in transferList)
                {
                    if (st.Id == stop.Transfer.TransferStopId)
                    {
                        stop.TransferList.Add(st);
                        break;
                    }
                }
            }
        }
    }
    
    public static double rad2Deg(double rad) => rad * 180 / Math.PI;
    public static double deg2Rad(double deg) => deg * Math.PI / 180;

    public static double distance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371; // km
        var dLat = deg2Rad(lat2 - lat1);
        var dLon = deg2Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(deg2Rad(lat1)) * Math.Cos(deg2Rad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var d = R * c;
        return d;
    }
    
}

public class PathResult2
{
   public List<Vehicle> Path { get; set; }
   public double Cost { get; set; }
   public double Time { get; set; }
   public bool PathFound { get; set; }

   public string message = "";

    public PathResult2()
    {
        Path = new List<Vehicle>();
        Cost = 0;
        Time = 0;
        PathFound = false;
    }
}



public class Bus : Vehicle
{
    public Bus(string id, string name, string type, double lat, double lon, bool sonDurak) : base(id, name, type, lat, lon, sonDurak) { }

    

    public static PathResult2 FindPath(Vehicle start, Vehicle end, bool findShortestTime, bool allowTransfers, List<Vehicle> stopList, List<Vehicle> stationList)
    {
        // Dictionary to store the current best cost/time to reach each vehicle stop
        Dictionary<string, double> bestValues = new Dictionary<string, double>();
        
        // Dictionary to store the previous vehicle in the optimal path
        Dictionary<string, Vehicle> previous = new Dictionary<string, Vehicle>();
        
        // Dictionary to track accumulated cost and time
        Dictionary<string, double> accumulatedCost = new Dictionary<string, double>();
        Dictionary<string, double> accumulatedTime = new Dictionary<string, double>();
        
        // Dictionary to track whether a node in the path is a transfer
        Dictionary<string, bool> isTransfer = new Dictionary<string, bool>();
        
        // Set of unvisited vehicles
        HashSet<string> unvisited = new HashSet<string>();
        
        // Initialize for all potential vehicles in both lists
        InitializeDictionaries(stopList, stationList, bestValues, previous, accumulatedCost, accumulatedTime, isTransfer, unvisited);
        
        // Set starting values
        if (start != null)
        {
            bestValues[start.Id] = 0;
            accumulatedCost[start.Id] = 0;
            accumulatedTime[start.Id] = 0;
        }
        else
        {
            return new PathResult2(); // Return empty result if start is null
        }
        
        while (unvisited.Count > 0)
        {
            // Find the unvisited vehicle with the lowest best value
            string currentId = FindLowestValueId(unvisited, bestValues);
            
            // If we can't reach any more vehicles or we've reached the destination
            if (currentId == null || bestValues[currentId] == double.MaxValue)
                break;
                
            if (currentId == end.Id)
                break;
                
            // Remove current vehicle from unvisited
            unvisited.Remove(currentId);
            
            // Get the current vehicle
            Vehicle current = FindVehicleById(currentId, stopList, stationList);
            
            if (current == null || current.SonDurak)
                continue;
            
            // Process all adjacent stops
            ProcessAdjacentStops(current, unvisited, bestValues, previous, accumulatedCost, accumulatedTime, isTransfer, findShortestTime, end);
            
            // Check for transfer options if allowed
            if (allowTransfers && current.Transfer != null && !string.IsNullOrEmpty(current.Transfer.TransferStopId))
            {
                ProcessTransfer(current, unvisited, bestValues, previous, accumulatedCost, accumulatedTime, isTransfer, findShortestTime, stopList, stationList);
            }
        }
        
        // Construct the path
        return ConstructPath(start, end, previous, accumulatedCost, accumulatedTime);
    }
    
    private static void InitializeDictionaries(List<Vehicle> stopList, List<Vehicle> stationList, 
                                               Dictionary<string, double> bestValues,
                                               Dictionary<string, Vehicle> previous,
                                               Dictionary<string, double> accumulatedCost,
                                               Dictionary<string, double> accumulatedTime,
                                               Dictionary<string, bool> isTransfer,
                                               HashSet<string> unvisited)
    {
        // Initialize for bus stops
        if (stopList != null)
        {
            foreach (Vehicle vehicle in stopList)
            {
                string id = vehicle.Id;
                bestValues[id] = double.MaxValue;
                previous[id] = null;
                accumulatedCost[id] = double.MaxValue;
                accumulatedTime[id] = double.MaxValue;
                isTransfer[id] = false;
                unvisited.Add(id);
            }
        }
        
        // Initialize for tram stations
        if (stationList != null)
        {
            foreach (Vehicle vehicle in stationList)
            {
                string id = vehicle.Id;
                if (!bestValues.ContainsKey(id))
                {
                    bestValues[id] = double.MaxValue;
                    previous[id] = null;
                    accumulatedCost[id] = double.MaxValue;
                    accumulatedTime[id] = double.MaxValue;
                    isTransfer[id] = false;
                    unvisited.Add(id);
                }
            }
        }
    }
    
    private static string FindLowestValueId(HashSet<string> unvisited, Dictionary<string, double> bestValues)
    {
        string currentId = null;
        double lowestValue = double.MaxValue;
        
        foreach (string id in unvisited)
        {
            if (bestValues[id] < lowestValue)
            {
                lowestValue = bestValues[id];
                currentId = id;
            }
        }
        
        return currentId;
    }
    
    private static Vehicle FindVehicleById(string id, List<Vehicle> stopList, List<Vehicle> stationList)
    {
        // Check bus stops first
        if (stopList != null)
        {
            Vehicle busStop = stopList.FirstOrDefault(v => v.Id == id);
            if (busStop != null)
                return busStop;
        }
        
        // Check tram stations
        if (stationList != null)
        {
            Vehicle tramStation = stationList.FirstOrDefault(v => v.Id == id);
            if (tramStation != null)
                return tramStation;
        }
        
        return null;
    }
    
    private static void ProcessAdjacentStops(Vehicle current, HashSet<string> unvisited,
                                             Dictionary<string, double> bestValues,
                                             Dictionary<string, Vehicle> previous,
                                             Dictionary<string, double> accumulatedCost,
                                             Dictionary<string, double> accumulatedTime,
                                             Dictionary<string, bool> isTransfer,
                                             bool findShortestTime, Vehicle end)
    {
        if (current.NextStList != null)
        {
            for (int i = 0; i < current.NextStList.Count; i++)
            {
                Vehicle nextVehicle = current.NextStList[i];
                
                if (!unvisited.Contains(nextVehicle.Id))
                    continue;
                
                // Skip if it's the final stop and not our destination
                if (nextVehicle.SonDurak && nextVehicle.Id != end.Id)
                    continue;
                
                // Find the corresponding NextStop object to get cost and time
                if (current.NextStops != null && i < current.NextStops.Count)
                {
                    NextStop nextStopInfo = current.NextStops[i];
                    
                    // Calculate new values
                    double newTime = accumulatedTime[current.Id] + nextStopInfo.Sure;
                    double newCost = accumulatedCost[current.Id] + nextStopInfo.Ucret;
                    
                    // Determine value based on optimization criteria
                    double newValue = findShortestTime ? newTime : newCost;
                    
                    if (newValue < bestValues[nextVehicle.Id])
                    {
                        bestValues[nextVehicle.Id] = newValue;
                        previous[nextVehicle.Id] = current;
                        accumulatedCost[nextVehicle.Id] = newCost;
                        accumulatedTime[nextVehicle.Id] = newTime;
                        isTransfer[nextVehicle.Id] = false;
                    }
                }
            }
        }
    }
    
    private static void ProcessTransfer(Vehicle current, HashSet<string> unvisited,
                                        Dictionary<string, double> bestValues,
                                        Dictionary<string, Vehicle> previous,
                                        Dictionary<string, double> accumulatedCost,
                                        Dictionary<string, double> accumulatedTime,
                                        Dictionary<string, bool> isTransfer,
                                        bool findShortestTime,
                                        List<Vehicle> stopList, List<Vehicle> stationList)
    {
        // Find the transfer destination
        Vehicle transferDestination = FindVehicleById(current.Transfer.TransferStopId, stopList, stationList);
        
        if (transferDestination != null && unvisited.Contains(transferDestination.Id))
        {
            // Calculate new values including transfer time and cost
            double newTime = accumulatedTime[current.Id] + current.Transfer.TransferSure;
            double newCost = accumulatedCost[current.Id] + current.Transfer.TransferUcret;
            
            double newValue = findShortestTime ? newTime : newCost;
            
            if (newValue < bestValues[transferDestination.Id])
            {
                bestValues[transferDestination.Id] = newValue;
                previous[transferDestination.Id] = current;
                accumulatedCost[transferDestination.Id] = newCost;
                accumulatedTime[transferDestination.Id] = newTime;
                isTransfer[transferDestination.Id] = true;
            }
        }
    }
    
    private static PathResult2 ConstructPath(Vehicle start, Vehicle end,
                                             Dictionary<string, Vehicle> previous,
                                             Dictionary<string, double> accumulatedCost,
                                             Dictionary<string, double> accumulatedTime)
    {
        PathResult2 result = new PathResult2();
        
        if (end == null || !previous.ContainsKey(end.Id) || previous[end.Id] == null)
        {
            // No path found
            result.Path = null;
            return result;
        }
        
        // Start from the end and work backwards
        List<Vehicle> reversePath = new List<Vehicle>();
        Vehicle currentVehicle = end;
        
        while (currentVehicle != null)
        {
            reversePath.Add(currentVehicle);
            
            if (currentVehicle.Id == start.Id)
                break;
                
            currentVehicle = previous[currentVehicle.Id];
        }
        
        // Reverse the path to get start-to-end
        reversePath.Reverse();
        
        result.Path = reversePath;
        result.Cost = accumulatedCost[end.Id];
        result.Time = accumulatedTime[end.Id];
        result.PathFound = true;
        
        return result;
    }

    
}

public class RailWay : Vehicle
{
    public RailWay(string id, string name, string type, double lat, double lon, bool sonDurak) : base(id, name, type, lat, lon, sonDurak) { }
    

    public static PathResult2 findPath(Vehicle start, Vehicle end)
    {   
        PathResult2 result = new PathResult2();
        result.Cost= 0;
        result.Time = 0;

        List<Vehicle> path = new List<Vehicle>();
        //path.Add(start);

        if (start == null || end == null)
        {
            return result;
        }

        if(start.SonDurak)
        {
            return result;
        }

        path.Add(start);

        if (start.NextStList != null)
        {
            foreach (var stop in start.NextStList)
            {
                if (stop.Id == end.Id)
                {
                    path.Add(stop);
                    return result;
                }
                else if(stop.SonDurak && stop.Id != end.Id)
                {   
                    result.Path = null;
                    result.Cost = 0;
                    result.Time = 0;
                    return result;
                }
                else{
                    path.Add(stop);

                    if(stop.NextStops != null)
                    {
                        result.Cost += Convert.ToDouble(stop.NextStops[0].Ucret);
                        result.Time += Convert.ToDouble(stop.NextStops[0].Sure); 
                    }
                    else
                    {
                        result.Path = null;
                        result.Cost = 0;
                        result.Time = 0;
                        return result;
                    }
                }

            }
        }
        return result;
        
    }
}

public class Taxi(double openingFee, double costPerKm)
{
    public double OpeningFee { get; set; } = openingFee;
    public double CostPerKm { get; set; } = costPerKm;
}