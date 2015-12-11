using System.Collections.Generic;
using System.IO;

namespace GVRP
{
    public class GreenVRP
    {
        public GreenVRP()
        {
            Customers = new List<Location>();
            Facilities = new List<Location>();
        }

        /// <summary>
        /// Depot Location
        /// </summary>
        public Location Depot { get; private set; }

        /// <summary>
        /// List of clients locations
        /// </summary>
        public List<Location> Customers { get; private set; }

        /// <summary>
        /// Clients Count
        /// </summary>
        public int CustomersCount
        {
            get
            {
                return Customers.Count;
            }
        }

        /// <summary>
        /// F = Recharge Stations Locations U Dummy vertices Locations
        /// </summary>
        public List<Location> Facilities { get; private set; }

        /// <summary>
        /// Carinality of F = Recharges Station U Dummy vertices
        /// </summary>
        public int FacilitiesCount
        {
            get
            {
                return Facilities.Count;
            }
        }

        /// <summary>
        /// Vehicle Fuel Tank Capacity
        /// </summary>
        public int Q { get; private set; }

        /// <summary>
        /// Fuel Consumption Rate
        /// </summary>
        public double R { get; private set; }

        /// <summary>
        /// Tour Length (in minutes)
        /// </summary>
        public int TL { get; private set; }

        /// <summary>
        /// Number of vehicles
        /// </summary>
        public int M { get; set; }

        /// <summary>
        /// Average Velocity (miles / hour)
        /// </summary>
        public int AV { get; private set; }

        /// <summary>
        /// Function to parse the input
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>GVRP class object</returns>
        public static GreenVRP ParseToGRVP( string fileName )
        {
            var reader = new StreamReader(fileName);
            string line = reader.ReadLine();
            string[] split;

            var result = new GreenVRP();
            int id = 0;
            while ( !reader.EndOfStream )
            {
                line = reader.ReadLine();
                split = line.Split('\t');

                if (line.Length == 0)
                    break;

                switch (split[1])
                {
                    case "d":
                        result.Depot = new Location { Name = split[0], Longitude = double.Parse(split[2]), Latitude = double.Parse(split[3]) };
                        break;
                    case "f":
                        for (int i = 0; i < 5; i++)
                        {
                            result.Facilities.Add(new Location { Name = split[0], Longitude = double.Parse(split[2]), Latitude = double.Parse(split[3]) });
                        }
                        break;
                    case "c":
                        result.Customers.Add(new Location { Name = split[0], Longitude = double.Parse(split[2]), Latitude = double.Parse(split[3]) });
                        break;
                }
                id++;

            }

            for (int i = 0; i < 5; i++)
            {
                line = reader.ReadLine();
                split = line.Split('/');

                switch (i)
                {
                    case 0:
                        result.Q = int.Parse(split[1]);
                        break;
                    case 1:
                        result.R = double.Parse(split[1]);
                        break;
                    case 2:
                        result.TL = int.Parse(split[1]) * 60; //time in minutes
                        break;
                    case 3:
                        result.AV = int.Parse(split[1]);
                        break;
                    case 4:
                        result.M = int.Parse(split[1]);
                        break;
                }
            }
            reader.Close();
            return result;
        }
        
        /// <summary>
        /// cardinality of V' = Depot U RechargeStatitions U DummyVertices U Customers
        /// </summary>
        public int N
        {
            get
            {
                return FacilitiesCount + CustomersCount + 1;
            }
        }

        /// <summary>
        /// All vertices in the set V' =  Depot U RechargeStatitions U DummyVertices U Customers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Location> GetV()
        {
            yield return Depot;
            foreach (var item in Facilities)
            {
                yield return item;
            }
            foreach (var item in Customers)
            {
                yield return item;
            }
        }
    }
}
