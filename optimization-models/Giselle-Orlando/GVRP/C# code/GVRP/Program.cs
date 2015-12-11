using Gurobi;
using System;
using System.IO;

namespace GVRP
{
    class Program
    {
        
        static void Main(string[] args)
        {

            try
            {
                string filename = args[0];
                var problem = GreenVRP.ParseToGRVP(filename);
                var mathModel = new GVRPMathModel(problem);
                mathModel.Model.Optimize();

                var result = mathModel.Model.Get(GRB.DoubleAttr.ObjVal);
                mathModel.Model.Dispose();
                var writer = new StreamWriter(filename+".out");
                writer.WriteLine("{0},{1}", filename, result);
                writer.Close();
            }
            catch (Exception e)
            {
            }

        }
    }
}
