using System;
using System.Collections.Generic;

using YolBul;
namespace YolBUl;
using System.Text.Json;
using Microsoft.AspNetCore.Rewrite;

public class FindWay
{
  
    public List<PathResult2> FindRoutes(double start_lat, double start_lon, double end_lat, double end_lon, string passanger, string payment)
    {   
        string jsonString = System.IO.File.ReadAllText("VeriSeti.json"); // JSON dosyanı oku
        JsonDocument jsonDoc = JsonDocument.Parse(jsonString);
        JsonElement jsonData = jsonDoc.RootElement;



        // Create Taxi object from JSON data
        Taxi taxi = new Taxi(
        jsonData.GetProperty("taxi").GetProperty("openingFee").GetDouble(),
        jsonData.GetProperty("taxi").GetProperty("costPerKm").GetDouble()
        );
        // Create lists for buses and trams
        List<Vehicle> stopList = new List<Vehicle>();
        List<Vehicle> stationList = new List<Vehicle>();
 
        foreach (JsonElement station in jsonData.GetProperty("duraklar").EnumerateArray())
        {   
            if (station.GetProperty("type").GetString() == "bus")
            {
                stopList.Add(new Bus(
                    station.GetProperty("id").GetString(),
                    station.GetProperty("name").GetString(),
                    station.GetProperty("type").GetString(),
                    station.GetProperty("lat").GetDouble(),
                    station.GetProperty("lon").GetDouble(),
                    station.GetProperty("sonDurak").GetBoolean()));
                foreach (JsonElement nextStop in station.GetProperty("nextStops").EnumerateArray())
                {
                    stopList[stopList.Count - 1].NextStops.Add(new NextStop(
                        nextStop.GetProperty("stopId").GetString(),
                        nextStop.GetProperty("mesafe").GetDouble(),
                        nextStop.GetProperty("sure").GetDouble(),
                        nextStop.GetProperty("ucret").GetDouble()));
                }
                // Corrected transfer handling
                if (station.TryGetProperty("transfer", out JsonElement transferElement) && transferElement.ValueKind != JsonValueKind.Null)
                {
                    // Carefully check if all required properties exist before accessing
                    string transferStopId = transferElement.TryGetProperty("transferStopId", out JsonElement stopIdElement) ? stopIdElement.GetString() : null;

                    double transferSure = transferElement.TryGetProperty("transferSure", out JsonElement sureElement) 
                        ? sureElement.GetDouble() 
                        : 0.0;

                    double transferUcret = transferElement.TryGetProperty("transferUcret", out JsonElement ucretElement) 
                        ? ucretElement.GetDouble() 
                        : 0.0;

                    // Only create Transfer if transferStopId is not null
                    if (!string.IsNullOrEmpty(transferStopId))
                    {
                        stopList[stopList.Count - 1].Transfer = new Transfer(
                            transferStopId,
                            transferSure,
                            transferUcret
                        );
                    }
                    else
                    {
                        stopList[stopList.Count - 1].Transfer = null;
                    }
                }
                else
                {
                    stopList[stopList.Count - 1].Transfer = null;
                }
            }
            else if (station.GetProperty("type").GetString() == "tram")
            {
                stationList.Add(new RailWay(
                    station.GetProperty("id").GetString(),
                    station.GetProperty("name").GetString(),
                    station.GetProperty("type").GetString(),
                    station.GetProperty("lat").GetDouble(),
                    station.GetProperty("lon").GetDouble(),
                    false));

                foreach (JsonElement nextStop in station.GetProperty("nextStops").EnumerateArray())
                {
                    stationList[stationList.Count - 1].NextStops.Add(new NextStop(
                        nextStop.GetProperty("stopId").GetString(),
                        nextStop.GetProperty("mesafe").GetDouble(),
                        nextStop.GetProperty("sure").GetDouble(),
                        nextStop.GetProperty("ucret").GetDouble()
                    ));
                }
        
                // Corrected transfer handling
                if (station.TryGetProperty("transfer", out JsonElement tramTransferElement))
                {
                    stationList[stationList.Count - 1].Transfer = new Transfer(
                        tramTransferElement.GetProperty("transferStopId").GetString(),
                        tramTransferElement.GetProperty("transferSure").GetDouble(),
                        tramTransferElement.GetProperty("transferUcret").GetDouble()
                    );
                }
                else
                {
                    stationList[stationList.Count - 1].Transfer = null;
                }
            }
        }
        
    

        Vehicle.appendNextStop(stopList);
        Vehicle.appendTransfer(stopList, stationList);

        Vehicle.appendNextStop(stationList);
        Vehicle.appendTransfer(stationList, stopList);

        double distance = Vehicle.distance(start_lat, start_lon, end_lat, end_lon);

        (Vehicle startStop, double startStopDistance) = NextStop.findNearestStop(start_lat, start_lon, stopList);
        (Vehicle endStop, double endStopDistance) = NextStop.findNearestStop(end_lat, end_lon, stopList);

        (Vehicle startStation, double startStationDistance) = NextStop.findNearestStop(start_lat, start_lon, stationList);
        (Vehicle endStation, double endStationDistance) = NextStop.findNearestStop(end_lat, end_lon, stationList);



        bool FindShortestTime = true;
        bool AllowTransfers = true;





        //ana 4 yol en ucuz

        var onlyTramPath = Bus.FindPath(startStation, endStation, !FindShortestTime, !AllowTransfers, stopList, stationList);

        var onlyBusPath = Bus.FindPath(startStop, endStop, !FindShortestTime, !AllowTransfers, stopList, stationList);
        var bus2TramPath = Bus.FindPath(startStop, endStation, !FindShortestTime, AllowTransfers, stopList, stationList);
        var tram2BusPath = Bus.FindPath(startStation, endStop, !FindShortestTime, AllowTransfers, stopList, stationList);

        //belki yollar en kısa süre
        var bus2TramPathShortestTime = Bus.FindPath(startStop, endStation, FindShortestTime, AllowTransfers, stopList, stationList);
        var tram2BusPathShortestTime = Bus.FindPath(startStation, endStop, FindShortestTime, AllowTransfers, stopList, stationList);

        
        //mesafeleri hesapla
        double tramStartDistance = Vehicle.distance(start_lat, start_lon, startStation.Lat, startStation.Lon);
        double tramEndDistance = Vehicle.distance(start_lat, start_lon, endStation.Lat, endStation.Lon);

        double stopStartDistance = Vehicle.distance(start_lat, start_lon, startStop.Lat, startStop.Lon);
        double stopEndDistance = Vehicle.distance(start_lat, start_lon, endStop.Lat, endStop.Lon);

        double maxWalkDistance = 3.0;



        onlyTramPath.message += "Daha komforlu bir ulaşım için sadece tramvay kullanarak konumunuza ulaşabilirsiniz.\n";
        onlyBusPath.message += "Sadece otobüs kullanarak konumunuza ulaşabilirsiniz.\n";
        bus2TramPath.message += "Otobüsten tramvaya aktarma kullanmak isterseniz en ucuz yolu kullanabilirsiniz.\n";
        tram2BusPath.message += "Tramvaydan otobüse aktarma kullanmak isterseniz en ucuz yolu kullanabilirsiniz.\n";
        bus2TramPathShortestTime.message += "Otobüsten tramvaya aktarma kullanmak isterseniz en kısa zaman yolunu kullanabilirsiniz.\n";
        tram2BusPathShortestTime.message += "Otobüsten tramvaya aktarma kullanmak isterseniz en kısa zaman yolunu kullanabilirsiniz.\n";

        List<PathResult2> paths = new List<PathResult2>();

        if(onlyTramPath.PathFound)
        {
            paths.Add(onlyTramPath);
        }
        if(onlyBusPath.PathFound)
        {
            paths.Add(onlyBusPath);
        }
        if(bus2TramPath.PathFound)
        {
            paths.Add(bus2TramPath);
        }
        if(tram2BusPath.PathFound)
        {
            paths.Add(tram2BusPath);
        }
        if(bus2TramPathShortestTime.PathFound)
        {
            paths.Add(bus2TramPathShortestTime);
        }
        if(tram2BusPathShortestTime.PathFound)
        {
            paths.Add(tram2BusPathShortestTime);
        }
    


        if (maxWalkDistance <= tramStartDistance)
        {
            if(onlyTramPath.PathFound)
            {
                onlyTramPath.message += $"başlangıç durağı {maxWalkDistance:0.00}km den fazla olduğundan taxi kullanmalısınız\n{taxi.OpeningFee} + {tramStartDistance} * {taxi.CostPerKm} = { taxi.OpeningFee + (tramStartDistance * taxi.CostPerKm):0.00}\n";
            }
            if(tram2BusPath.PathFound)
            {
                tram2BusPath.message += $"başlangıç durağı {maxWalkDistance:0.00}km den fazla olduğundan taxi kullanmalısınız\n{taxi.OpeningFee} + {tramStartDistance} * {taxi.CostPerKm} = { taxi.OpeningFee + (tramStartDistance * taxi.CostPerKm):0.00}\n";
                
            }
            if(tram2BusPathShortestTime.PathFound)
            {
                tram2BusPathShortestTime.message += $"başlangıç durağı {maxWalkDistance:0.00}km den fazla olduğundan taxi kullanmalısınız\n{taxi.OpeningFee} + {tramStartDistance} * {taxi.CostPerKm} = { taxi.OpeningFee + (tramStartDistance * taxi.CostPerKm):0.00}\n";
                
            }

        }
        if (maxWalkDistance <= tramEndDistance  && onlyTramPath.PathFound)
        {
            if(onlyTramPath.PathFound)
            {
                onlyTramPath.message += $"bitiş durağı {maxWalkDistance:0.00}km den fazla olduğundan taxi kullanmalısınız\n{taxi.OpeningFee} + {tramEndDistance} * {taxi.CostPerKm} = { taxi.OpeningFee + (tramEndDistance * taxi.CostPerKm):0.00}\n";

            }
            if(bus2TramPath.PathFound)
            {
                bus2TramPath.message += $"bitiş durağı {maxWalkDistance:0.00}km den fazla olduğundan taxi kullanmalısınız\n{taxi.OpeningFee} + {tramEndDistance} * {taxi.CostPerKm} = { taxi.OpeningFee + (tramEndDistance * taxi.CostPerKm):0.00}\n";
                
            }
            if(bus2TramPathShortestTime.PathFound)
            {
                bus2TramPathShortestTime.message += $"bitiş durağı {maxWalkDistance:0.00}km den fazla olduğundan taxi kullanmalısınız\n{taxi.OpeningFee} + {tramEndDistance} * {taxi.CostPerKm} = { taxi.OpeningFee + (tramEndDistance * taxi.CostPerKm):0.00}\n"; 
            }
        }
        if(maxWalkDistance <= stopStartDistance && onlyTramPath.PathFound)
        {
            if(onlyBusPath.PathFound)
            {
                onlyBusPath.message += $"başlangıç durağı {maxWalkDistance:0.00}km den fazla olduğundan taxi kullanmalısınız\n{taxi.OpeningFee} + {stopStartDistance} * {taxi.CostPerKm} = { taxi.OpeningFee + (stopStartDistance * taxi.CostPerKm):0.00}\n";

            }
            if(bus2TramPath.PathFound)
            {
                bus2TramPath.message += $"başlangıç durağı {maxWalkDistance:0.00}km den fazla olduğundan taxi kullanmalısınız\n{taxi.OpeningFee} + {stopStartDistance} * {taxi.CostPerKm} = { taxi.OpeningFee + (stopStartDistance * taxi.CostPerKm):0.00}\n";
                
            }
            if(bus2TramPathShortestTime.PathFound)
            {
                bus2TramPathShortestTime.message += $"başlangıç durağı {maxWalkDistance:0.00}km den fazla olduğundan taxi kullanmalısınız\n{taxi.OpeningFee} + {stopStartDistance} * {taxi.CostPerKm} = { taxi.OpeningFee + (stopStartDistance * taxi.CostPerKm):0.00}\n";
                
            }
        }
        if(maxWalkDistance <= stopEndDistance  && onlyTramPath.PathFound)
        {   
            if(onlyBusPath.PathFound)
            {
                onlyBusPath.message += $"bitiş durağı {maxWalkDistance:0.00}km den fazla olduğundan taxi kullanmalısınız\n{taxi.OpeningFee:0.00} + {stopEndDistance:0.00} * {taxi.CostPerKm:0.00} = { taxi.OpeningFee + (stopEndDistance * taxi.CostPerKm):0.00}\n";
                
            }
            if(tram2BusPath.PathFound)
            {
                tram2BusPath.message += $"bitiş durağı {maxWalkDistance:0.00}km den fazla olduğundan taxi kullanmalısınız\n{taxi.OpeningFee:0.00} + {stopEndDistance:0.00} * {taxi.CostPerKm:0.00} = { taxi.OpeningFee + (stopEndDistance * taxi.CostPerKm):0.00}\n";
                
            }
            if(tram2BusPathShortestTime.PathFound)
            {
                tram2BusPathShortestTime.message += $"bitiş durağı {maxWalkDistance:0.00}km den fazla olduğundan taxi kullanmalısınız\n{taxi.OpeningFee:0.00} + {stopEndDistance:0.00} * {taxi.CostPerKm:0.00} = { taxi.OpeningFee + (stopEndDistance * taxi.CostPerKm):0.00}\n";
                
            }
        }


        if(onlyTramPath.PathFound)
        {
            onlyTramPath.message += $"{tramStartDistance:0.00}km -> ";
            foreach (var station in onlyTramPath.Path)
            {
                onlyTramPath.message += $"{station.Id} -> ";

            }
            onlyTramPath.message += $"{tramEndDistance:0.00}km \n";
        }

        if(onlyBusPath.PathFound)
        {
            onlyBusPath.message += $"{stopStartDistance:0.00}km -> ";
            foreach (var stop in onlyBusPath.Path)
            {
                onlyBusPath.message += $"{stop.Id} -> ";

            }
            onlyBusPath.message += $"{stopEndDistance:0.00}km \n";
        }

        if(bus2TramPath.PathFound)
        {
            bus2TramPath.message += $"{stopStartDistance:0.00}km -> ";
            foreach (var stop in bus2TramPath.Path)
            {
                bus2TramPath.message += $"{stop.Id} -> ";

            }
            bus2TramPath.message += $"{tramEndDistance:0.00}km \n";
        }

        if(tram2BusPath.PathFound)
        {
            tram2BusPath.message += $"{tramStartDistance:0.00}km -> ";
            foreach (var stop in tram2BusPath.Path)
            {
                tram2BusPath.message += $"{stop.Id} -> ";

            }
            tram2BusPath.message += $"{stopEndDistance:0.00}km \n";
        }

        if(bus2TramPathShortestTime.PathFound)
        {
            bus2TramPathShortestTime.message += $"{stopStartDistance:0.00}km -> ";
            foreach (var stop in bus2TramPathShortestTime.Path)
            {
                bus2TramPathShortestTime.message += $"{stop.Id} -> ";

            }
            bus2TramPathShortestTime.message += $"{tramEndDistance:0.00}km \n";
        }

        if(tram2BusPathShortestTime.PathFound)
        {
            tram2BusPathShortestTime.message += $"{tramStartDistance:0.00}km -> ";
            foreach (var stop in tram2BusPathShortestTime.Path)
            {
                tram2BusPathShortestTime.message += $"{stop.Id} -> ";

            }
            tram2BusPathShortestTime.message += $"{stopEndDistance:0.00}km \n";
        }


            


        


        Message message = new Message("", 0.0);

        if(passanger == Student.name)
        {
            foreach (var path in paths)
            {   
                if(path.PathFound){
                    message = Student.scanCard(path.Cost, payment);
                    path.message += message.Log;
                    path.Cost = message.price;
                }
            }
        }
        else if(passanger == Elderly.name)
        {
            foreach (var path in paths)
            {
                if(path.PathFound){
                    message = Elderly.scanCard(path.Cost, payment);
                    path.message += message.Log;
                    path.Cost = message.price;
                }
            }
        }
        else
        {
            foreach (var path in paths)
            {   
                if(path.PathFound){
                    message = Standart.scanCard(path.Cost, payment);
                    path.message += message.Log;
                    path.Cost = message.price;
                }
            }
        }
        
        string logMessage = "";

        double totalDistance = Vehicle.distance(start_lat, start_lon, end_lat, end_lon);

        PathResult2 taxi2 = new PathResult2();
        taxi2.message += $"En konforlu ulaşım için sadece taxi ile seyahat.\n({taxi.OpeningFee:0.00} + {totalDistance:0.00} * {taxi.CostPerKm:0.00} = {taxi.OpeningFee + (totalDistance * taxi.CostPerKm):0.00})\n\n";

        //logMessage += $"En konforlu ulaşım için sadece taxi ile seyahat.\n({taxi.OpeningFee:0.00} + {totalDistance:0.00} * {taxi.CostPerKm:0.00} = {taxi.OpeningFee + (totalDistance * taxi.CostPerKm):0.00})\n\n";


        double avarageSpeed = 40.00 / 60.00;    
        taxi2.Time = totalDistance / avarageSpeed;
        taxi2.Cost = taxi.OpeningFee + (totalDistance * taxi.CostPerKm);
        paths.Add(taxi2);

        foreach (var path in paths)
        {
            if(path.PathFound){
                path.message += $"\ntoplam Süre: ({path.Time:0.00} dk)\n";
                logMessage += path.message;
                logMessage += "\n\n";
            }
        }

        //System.Console.WriteLine(logMessage);

        string logFilePath = "log.txt";

        System.IO.File.WriteAllText(logFilePath, $"{logMessage}\n");

        
        if(paths.Count == 0)
        {
            return null;
        }
        else
        {
            return paths;
        }

        //int sayi;
        

    }
}
