using MathSupport;
using OpenTK;
using Rendering;
using System;
using Utilities;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MartinVasina
{
  
  public class MathUtils
  {

    public static Vector3d CrossProduct (Vector3d u, Vector3d v)
    {
      return new Vector3d(
        u.Y * v.Z - u.Z * v.Y,
        u.Z * v.X - u.X * v.Z,
        u.X * v.Y - u.Y * v.X
      );
    }

    public static bool IsPointNearTriangleEdge (Vector3d p, Triangle t, double limit)
    {
      Vector3d q1 = t.a;
      Vector3d u1 = t.b - t.a;
      Vector3d pq1 = q1 - p;
      Vector3d numerator1 = MathUtils.CrossProduct(pq1, u1);
      double distance1 = numerator1.Length / u1.Length;

      Vector3d q2 = t.a;
      Vector3d u2 = t.c - t.a;
      Vector3d pq2 = q2 - p;
      Vector3d numerator2 = MathUtils.CrossProduct(pq2, u2);
      double distance2 = numerator2.Length / u2.Length;

      Vector3d q3 = t.b;
      Vector3d u3 = t.c - t.b;
      Vector3d pq3 = q3 - p;
      Vector3d numerator3 = MathUtils.CrossProduct(pq3, u3);
      double distance3 = numerator3.Length / u3.Length;

      if (distance1 < limit || distance2 < limit || distance3 < limit)
        return true;
      else
        return false;
    }

    public static Vector3d RotateVectorByRandomAngleY(Vector3d v, DeterministicRandom random)
    {
      double angle = random.NextDouble() * 2 * Math.PI;
      return new Vector3d(Math.Cos(angle) * v.X - Math.Sin(angle) * v.Z, v.Y, Math.Sin(angle) * v.X + Math.Cos(angle) * v.Z); 
    }

    public static void SetVectorLength(ref Vector3d v, double length)
    {
      if (v.Length > 0)
      {
        double ratio = length / v.Length;
        v *= ratio;
      }
    }

    // https://gamedev.stackexchange.com/questions/26713/calculate-random-points-pixel-within-a-circle-image
    public static Vector3d CalculateRandomPositionInCircle (double radius, DeterministicRandom random)
    {
      double angle = random.NextDouble() * Math.PI * 2;
      double r = random.NextDouble() * radius;
      double x = r * Math.Cos(angle);
      double y = r * Math.Sin(angle);
      return new Vector3d(x, 0, y);
    }
  }


  public class Triangle
  {
    public Vector3d a, b, c;
    public Vector3d normal;

    public Triangle (Vector3d a, Vector3d b, Vector3d c)
    {
      this.a = a;
      this.b = b;
      this.c = c;
      Vector3d u = b - a;
      Vector3d v = c - a;
      normal = new Vector3d(
        u.Y * v.Z - u.Z * v.Y,
        u.Z * v.X - u.X * v.Z,
        u.X * v.Y - u.Y * v.X
      );
    }

    public override string ToString ()
    {
      return "[" + a.X + "," + a.Y + "," + a.Z + "] [" + b.X + "," + b.Y + "," + b.Z + "] [" + c.X + "," + c.Y + "," + c.Z + "] ";
    }
  }


  public class DeterministicRandom
  {
    private static Random random = new Random();
    private static double[] randomValues = new double[10000];
    private int randomValuesIdx = 0;
    private static bool instanceCreated = false;

    public DeterministicRandom () {
      if (instanceCreated)
        return;
      for (int i = 0; i < randomValues.Length; ++i)
        randomValues[i] = random.NextDouble();
    }

    public double NextDouble()
    {
      return randomValues[randomValuesIdx++ % 1000]; 
    }

  }


  public class NoiseMaker
  {
    // adapted from http://cs.nyu.edu/~perlin/noise/
    // JAVA REFERENCE IMPLEMENTATION OF IMPROVED NOISE - COPYRIGHT 2002 KEN PERLIN.

    private static int[] p = new int[512];
    private static int[] permutation = { 151,160,137,91,90,15,
               131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
               190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
               88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
               77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
               102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
               135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
               5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
               223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
               129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
               251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
               49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
               138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
               };

    static NoiseMaker ()
    {
      CalculateP();
    }

    private static int _octaves;
    private static int _halfLength = 256;

    public static void SetOctaves (int octaves)
    {
      _octaves = octaves;

      var len = (int)Math.Pow(2, octaves);

      permutation = new int[len];

      Reseed();
    }

    private static void CalculateP ()
    {
      p = new int[permutation.Length * 2];
      _halfLength = permutation.Length;

      for (int i = 0; i < permutation.Length; i++)
        p[permutation.Length + i] = p[i] = permutation[i];
    }

    public static void Reseed ()
    {
      var random = new Random();
      var perm = Enumerable.Range(0, permutation.Length).ToArray();

      for (var i = 0; i < perm.Length; i++)
      {
        var swapIndex = random.Next(perm.Length);

        var t = perm[i];

        perm[i] = perm[swapIndex];

        perm[swapIndex] = t;
      }

      permutation = perm;

      CalculateP();

    }

    public static float Noise (float x, float y, float z, int octaves, ref float min, ref float max)
    {

      var perlin = 0f;
      var octave = 1;

      for (var i = 0; i < octaves; i++)
      {
        var noise = Noise(x * octave, y * octave, z * octave);

        perlin += noise / octave;

        octave *= 2;
      }

      perlin = Math.Abs((float)Math.Pow(perlin, 2));
      max = Math.Max(perlin, max);
      min = Math.Min(perlin, min);

      //perlin = 1f - 2 * perlin;

      return perlin;
    }

    public static float Noise (float x, float y, float z)
    {
      int X = (int)Math.Floor(x) % _halfLength;
      int Y = (int)Math.Floor(y) % _halfLength;
      int Z = (int)Math.Floor(z) % _halfLength;

      if (X < 0)
        X += _halfLength;

      if (Y < 0)
        Y += _halfLength;

      if (Z < 0)
        Z += _halfLength;

      x -= (int)Math.Floor(x);
      y -= (int)Math.Floor(y);
      z -= (int)Math.Floor(z);

      var u = Fade(x);
      var v = Fade(y);
      var w = Fade(z);

      int A = p[X] + Y, AA = p[A] + Z, AB = p[A + 1] + Z,      // HASH COORDINATES OF
          B = p[X + 1] + Y, BA = p[B] + Z, BB = p[B + 1] + Z;      // THE 8 CUBE CORNERS,


      return Lerp(
              Lerp(
                   Lerp(
                      Grad(p[AA], x, y, z) // AND ADD
                      ,
                      Grad(p[BA], x - 1, y, z) // BLENDED
                      ,
                      u
                      )
                  ,
                  Lerp(
                      Grad(p[AB], x, y - 1, z)  // RESULTS
                      ,
                      Grad(p[BB], x - 1, y - 1, z)
                      ,
                      u
                      )
                  ,
                  v
              )
              ,
              Lerp(
                  Lerp(
                      Grad(p[AA + 1], x, y, z - 1) // CORNERS
                      ,
                      Grad(p[BA + 1], x - 1, y, z - 1) // OF CUBE
                      ,
                      u
                      )
                  ,
                  Lerp(
                      Grad(p[AB + 1], x, y - 1, z - 1)
                      ,
                      Grad(p[BB + 1], x - 1, y - 1, z - 1)
                      ,
                      u
                      )
                  ,
                  v
              )
              ,
              w
          );

    }

    private static float Fade (float t) { return t * t * t * (t * (t * 6 - 15) + 10); }

    private static float Grad (int hash, float x, float y, float z)
    {
      int h = hash & 15;                      // CONVERT LO 4 BITS OF HASH CODE

      float u = h < 8 ? x : y,                 // INTO 12 GRADIENT DIRECTIONS.
             v = h < 4 ? y : h == 12 || h == 14 ? x : z;

      return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    private static float Lerp (float firstFloat, float secondFloat, float by)
    {
      return firstFloat * (1 - by) + secondFloat * by;
    }

  }
}

