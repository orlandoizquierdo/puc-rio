using Gurobi;
using System;
using System.Linq;

namespace GVRP
{


    public class GVRPMathModel
    {
        /// <summary>
        /// Distance: Spherical Earth projected to a plane
        /// </summary>
        /// <param name="a">Lcation A</param>
        /// <param name="b">Location B</param>
        /// <returns>Distance in miles</returns>
        public static double Distance(Location a, Location b)
        {
            var R = 3958.761;
            var med = (a.Latitude + b.Latitude) / 2;
            var d1 = (a.Latitude - b.Latitude) * Math.PI / 180;
            var d2 = (a.Longitude - b.Longitude) * Math.PI / 180;
            return R * Math.Sqrt(d1 * d1 + (Math.Cos(med) * d2) * (Math.Cos(med) * d2));

        }

        private GRBEnv env;
        public GVRPMathModel( GreenVRP problem)
        {
            env = new GRBEnv();
            Model = new GRBModel(env);

            //define variables

            GRBVar[,] X = new GRBVar[problem.N, problem.N];
            var list = problem.GetV().ToArray();

            GRBLinExpr objetive = new GRBLinExpr();

            double[,] D = new double[problem.N, problem.N];
            double[,] T = new double[problem.N, problem.N];
            double[] P = new double[problem.N];

            for (int i = 0; i < problem.N; i++)
            {
                for (int j = 0; j <=i; j++)
                {
                    D[i, j] = Distance(list[i], list[j]);
                    T[i, j] = (D[i, j] * 60) / problem.AV;
                    D[j, i] = D[i, j];
                    T[j, i] = T[j, i];
                }

                P[i] = (i < problem.FacilitiesCount + 1) ? 15 : 30;
                    
            }
            
            for (int i = 0; i < problem.N; i++)
            {
                for (int j = 0; j < problem.N; j++)
                {
                    X[i, j] = Model.AddVar(0.0, 1.0, 0.0 ,GRB.BINARY,"x"+i+"_"+"j");
                    if (i != j)
                        objetive.AddTerm(D[i,j] , X[i, j]);
                }

            }

            GRBVar[] t = new GRBVar[problem.N];
            for (int j = 1; j < problem.N; j++)
            {
                t[j] = Model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, "t" + j);
            }
            t[0] = Model.AddVar(0, problem.TL, 0.0, GRB.CONTINUOUS, "t0");

            var y = new GRBVar[problem.N];
            for (int i = 0; i < problem.N; i++)
            {
                y[i] = Model.AddVar(0.0, problem.Q, 0.0, GRB.CONTINUOUS, "y" + i);
            }
            Model.Update();


            //objetive function
            Model.SetObjective(objetive, GRB.MINIMIZE);

            //Constraints

            //Constraint 1 ...............................................................
            GRBLinExpr[] customersContraint = new GRBLinExpr[problem.CustomersCount];
            for (int i = problem.FacilitiesCount + 1; i < problem.N; i++)
            {
                var index = i - problem.FacilitiesCount - 1;
                customersContraint[index] = new GRBLinExpr();
                for (int j = 0; j < problem.N; j++)
                {
                    if (i != j)
                        customersContraint[index].AddTerm(1, X[i, j]);
                }
                
                Model.AddConstr(customersContraint[index],GRB.EQUAL,1,"C1_"+index);
            }

            //Constraint 2 ................................................................

            GRBLinExpr[] facilitiesConstraints = new GRBLinExpr[1 + problem.FacilitiesCount];
            for (int i = 0; i < 1 + problem.FacilitiesCount; i++)
            {
                var index = i;
                facilitiesConstraints[index] = new GRBLinExpr();
                for (int j = 0; j < problem.N; j++)
                {
                    if (i != j)
                        facilitiesConstraints[index].AddTerm(1, X[i, j]);
                }

                Model.AddConstr(facilitiesConstraints[index] <= 1, "C2_" + index + "_1");
            }

            //Constraint 3 ...................................................................
            GRBLinExpr[] In = new GRBLinExpr[problem.N];
            GRBLinExpr[] Out = new GRBLinExpr[problem.N];
            for (int i = 0; i < problem.N; i++)
            {
                In[i] = new GRBLinExpr();
                Out[i] = new GRBLinExpr();
                for (int j = 0; j < problem.N; j++)
                {
                    if (i!=j)
                    {
                        In[i].AddTerm(1,X[j, i]);
                        Out[i].AddTerm(1,X[i, j]);
                    }
                }

                Model.AddConstr(In[i] - Out[i], GRB.EQUAL,0, "C3_" + i);
            }


            //Constraint 4 ......................................................................

            for (int i = 1; i < problem.N; i++)
            {
                Model.AddConstr(X[0, i] <= problem.M, "C4_" + i);
            }

            // Constraint5 ......................................................................

            for (int i = 1; i < problem.N; i++)
            {
                Model.AddConstr(X[i, 0] <= problem.M, "C5_" + i);
            }

            //Constraint 6 ......................................................................

            for (int j = 1; j < problem.N; j++)
            {
                for (int i = 0; i < problem.N; i++)
                {
                    if (i!=j)
                    {
                        Model.AddConstr(t[j], GRB.GREATER_EQUAL, t[i] + (T[i, j] - P[j]) * X[i, j] - problem.TL*(1 - X[i,j]), "C6_"+j+"_"+i );
                    }
                }
            }

            //Constraint 7 ......................................................................


            Model.AddConstr(0 <= t[0], "C7_1");
            Model.AddConstr(t[0] <= problem.TL, "C7_2");

            //Constraint 8 ......................................................................

            for (int j = 1; j < problem.N; j++)
            {
                Model.AddConstr(T[0, j] <= t[j],"C8_"+j+"_0");
                Model.AddConstr(t[j] <= problem.TL - ( T[j,0] + P[j] ), "C8_" + j+"_max");
            }


            //Constraint 9 ......................................................................


            for (int j = problem.FacilitiesCount+1; j < problem.N; j++)
            {
                for (int i = 0; i < problem.N; i++)
                {
                    if (i != j)
                    {
                        Model.AddConstr(y[j] <= y[i] - problem.R * D[i, j] * X[i, j] + problem.Q * (1 - X[i, j]),"C9_"+j+"_"+i);
                    }
                }
            }

            //Constraint 10 .....................................................................

            for (int j = 0; j < problem.FacilitiesCount+1; j++)
            {
                Model.AddConstr(y[j] ,GRB.EQUAL, problem.Q, "C10_" + j);
            }

            //Constraint 11 .....................................................................

            for (int j = problem.FacilitiesCount+1; j < problem.N; j++)
            {
                for (int l = 0; l < problem.FacilitiesCount+1; l++)
                {
                    var min = Math.Min(problem.R * D[j, 0], problem.R * (D[j, l] + D[l, 0]));
                    Model.AddConstr(y[j] >= min, "C11_" + j + "_" + l);
                }
            }

            Model.Update();

        }

        public GRBModel Model { get; private set; }

    }
}
