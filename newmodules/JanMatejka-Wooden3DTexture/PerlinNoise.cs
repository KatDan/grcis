using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using MathSupport;
using OpenTK;
using Utilities;

namespace Rendering
{
  namespace JanMatejka
  {
    /// <summary>
    /// Perlin noise class able to generate noise in 3D space
    /// </summary>
    public class PerlinNoise
    {
      public PerlinNoise (int seed = 1337)
      {
        rand = new Random(seed);
        ShufflePermutation();
      }

      public double Noise (double x, double y, double z)
      {
        // Determine which unit cube the input coords are in
        int unitCubeX0 = Floor(x) & 255;  // & 255 <=> % 256
        int unitCubeY0 = Floor(y) & 255;
        int unitCubeZ0 = Floor(z) & 255;

        int unitCubeX1 = (unitCubeX0 + 1) & 255;
        int unitCubeY1 = (unitCubeY0 + 1) & 255;
        int unitCubeZ1 = (unitCubeZ0 + 1) & 255;

        // Choose gradient vectors according to hash function
        Vector3d gradient1 = Gradients[Hash(unitCubeX0, unitCubeY0, unitCubeZ0) & 15];  // & 15 <=> % 16
        Vector3d gradient2 = Gradients[Hash(unitCubeX1, unitCubeY0, unitCubeZ0) & 15];
        Vector3d gradient3 = Gradients[Hash(unitCubeX0, unitCubeY1, unitCubeZ0) & 15];
        Vector3d gradient4 = Gradients[Hash(unitCubeX1, unitCubeY1, unitCubeZ0) & 15];
        Vector3d gradient5 = Gradients[Hash(unitCubeX0, unitCubeY0, unitCubeZ1) & 15];
        Vector3d gradient6 = Gradients[Hash(unitCubeX1, unitCubeY0, unitCubeZ1) & 15];
        Vector3d gradient7 = Gradients[Hash(unitCubeX0, unitCubeY1, unitCubeZ1) & 15];
        Vector3d gradient8 = Gradients[Hash(unitCubeX1, unitCubeY1, unitCubeZ1) & 15];

        // Determine coordinates inside the unit cube
        x -= Floor(x);
        y -= Floor(y);
        z -= Floor(z);

        double x0 = x-1;
        double y0 = y-1;
        double z0 = z-1;

        // Dot product of gradients and vectors from our point to corresponding corner
        double dot1 = Vector3d.Dot(gradient1, new Vector3d(x,y,z));
        double dot2 = Vector3d.Dot(gradient2, new Vector3d(x0,y,z));
        double dot3 = Vector3d.Dot(gradient3, new Vector3d(x,y0,z));
        double dot4 = Vector3d.Dot(gradient4, new Vector3d(x0,y0,z));
        double dot5 = Vector3d.Dot(gradient5, new Vector3d(x,y,z0));
        double dot6 = Vector3d.Dot(gradient6, new Vector3d(x0,y,z0));
        double dot7 = Vector3d.Dot(gradient7, new Vector3d(x,y0,z0));
        double dot8 = Vector3d.Dot(gradient8, new Vector3d(x0,y0,z0));

        // Interpolation
        double xEased = EaseCurve(x);
        double yEased = EaseCurve(y);
        double zEased = EaseCurve(z);

        double x1 = Interpolate(dot1, dot2, xEased);
        double x2 = Interpolate(dot3, dot4, xEased);
        double y1 = Interpolate(x1, x2, yEased);

        x1 = Interpolate(dot5, dot6, xEased);
        x2 = Interpolate(dot7, dot8, xEased);
        double y2 = Interpolate(x1, x2, yEased);

        return Interpolate(y1, y2, zEased);
      }

      // Quick floor function
      private static int Floor (double x)
      {
        return x < (int)x ? (int)x - 1 : (int)x;
      }

      // Hash lookup
      private byte Hash (int x, int y, int z)
      {
        return Permutation[Permutation[Permutation[x] + y] + z];
      }

      // Improved ease curve according to Perlin
      private static double EaseCurve (double t)
      {
        return t * t * t * (t * (t * 6 - 15) + 10);
      }

      private static double Interpolate (double a, double b, double x)
      {
        return a + x * (b - a);
      }

      private void ShufflePermutation ()
      {
        int n = Permutation.Count;
        while (n > 1)
        {
          n--;
          int k = rand.Next(n + 1);
          var value = Permutation[k];
          Permutation[k] = Permutation[n];
          Permutation[n] = value;
        }
      }

      // Permutation of numbers from 0 to 255 (the array is doubled to avoid using division (%) when indexing in the Hash function)
      // Serves as a hash lookup table
      private List<byte> Permutation = new List<byte>
      {
            9,109,227,5,234,212,243,177,221,209,204,201,79,136,81,64,120,45,43,223,255,67,110,
            140,218,156,61,6,219,84,38,121,30,170,200,107,222,13,143,20,60,232,238,138,66,21,154,
            241,149,23,159,39,87,1,135,250,3,29,182,33,2,22,169,62,178,55,90,50,123,104,168,254,
            16,122,31,181,147,213,99,53,86,116,25,68,125,108,167,32,253,46,161,119,105,155,82,113,
            70,94,146,42,176,65,216,174,47,4,128,51,229,242,15,150,133,190,72,152,111,189,231,188,
            85,233,142,249,224,252,115,186,148,205,24,12,71,228,76,18,17,180,131,187,97,183,83,73,
            166,92,56,194,0,102,132,49,185,197,101,8,145,124,203,164,95,96,93,63,165,48,163,245,57,
            196,230,244,157,91,202,44,211,98,112,151,137,75,172,236,26,247,139,158,193,198,141,37,
            173,207,127,206,129,106,88,191,78,144,217,28,195,226,40,11,27,134,52,210,103,35,171,41,
            239,77,34,54,208,160,74,117,235,199,19,59,153,237,246,240,162,69,118,10,214,192,215,130,
            100,80,36,248,114,14,175,184,89,225,126,7,179,251,220,58,

            9,109,227,5,234,212,243,177,221,209,204,201,79,136,81,64,120,45,43,223,255,67,110,
            140,218,156,61,6,219,84,38,121,30,170,200,107,222,13,143,20,60,232,238,138,66,21,154,
            241,149,23,159,39,87,1,135,250,3,29,182,33,2,22,169,62,178,55,90,50,123,104,168,254,
            16,122,31,181,147,213,99,53,86,116,25,68,125,108,167,32,253,46,161,119,105,155,82,113,
            70,94,146,42,176,65,216,174,47,4,128,51,229,242,15,150,133,190,72,152,111,189,231,188,
            85,233,142,249,224,252,115,186,148,205,24,12,71,228,76,18,17,180,131,187,97,183,83,73,
            166,92,56,194,0,102,132,49,185,197,101,8,145,124,203,164,95,96,93,63,165,48,163,245,57,
            196,230,244,157,91,202,44,211,98,112,151,137,75,172,236,26,247,139,158,193,198,141,37,
            173,207,127,206,129,106,88,191,78,144,217,28,195,226,40,11,27,134,52,210,103,35,171,41,
            239,77,34,54,208,160,74,117,235,199,19,59,153,237,246,240,162,69,118,10,214,192,215,130,
            100,80,36,248,114,14,175,184,89,225,126,7,179,251,220,58,
      };

      // Gradients suggested to be used by Perlin
      private static readonly List<Vector3d> Gradients = new List<Vector3d>
      {
            new Vector3d(1,1,0), new Vector3d(-1,1,0), new Vector3d(1,-1,0), new Vector3d(-1,-1,0),
            new Vector3d(1,0,1), new Vector3d(-1,0,1), new Vector3d(1,0,-1), new Vector3d(-1,0,-1),
            new Vector3d(0,1,1), new Vector3d(0,-1,1), new Vector3d(0,1,-1), new Vector3d(0,-1,-1),
            new Vector3d(1,1,0), new Vector3d(-1,1,0), new Vector3d(0,-1,1), new Vector3d(0,-1,-1)  // This line is extra to avoid division (%) by 12
      };

      private static Random rand;
    }
  }
}
