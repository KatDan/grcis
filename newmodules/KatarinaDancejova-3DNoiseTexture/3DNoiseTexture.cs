using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rendering;
using OpenTK;
using MathSupport;

namespace KatarinaDancejova
{
  public class PerlinNoise : ITexture
  {

    /// <summary>
    /// Primary color used in texture.
    /// </summary>
    public double[] Color1 = new double[] { 0.9, 0.9, 0.9 };

    /// <summary>
    /// Secondary color used in texture.
    /// </summary>
    public double[] Color2 = new double[] {0.1, 0.1, 0.11 };

    /// <summary>
    /// Frequency of the noise.
    /// </summary>
    public double frequency = 30;

    /// <summary>
    /// Permutation used to generate noise.
    /// </summary>
    static int[] permutation = { 151,160,137,91,90,15,
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

    /// <summary>
    /// Array used for calculation of the hash from the permutation.
    /// </summary>
    public static int[] p;

    public PerlinNoise () { }

    public PerlinNoise (double freq, double[] col1, double[] col2)
    {
      Color1 = col1;
      Color2 = col2;
      frequency = freq;
    }

    public long Apply (Intersection inter)
    {
      double q = MakeSomeNoise(inter.CoordWorld.X * frequency, inter.CoordWorld.Y * frequency, inter.CoordWorld.Z * frequency);

      for (int i = 0; i < inter.SurfaceColor.Length; i++)
      {
        if (q < 0)
          q = 1;
        inter.SurfaceColor[i] = Color1[i] * (1 - q) + q * Color2[i];
      }

      return (long)(q - (int)q);
    }

    public static double MakeSomeNoise (double x, double y, double z)
    {
      p = new int[512];
      for (int i = 0; i < 256; i++)
        p[256 + i] = p[i] = permutation[i];

      int X = (int)Math.Floor(x) & 255,                  // FIND UNIT CUBE THAT
          Y = (int)Math.Floor(y) & 255,                  // CONTAINS POINT.
          Z = (int)Math.Floor(z) & 255;
      x -= Math.Floor(x);                                // FIND RELATIVE X,Y,Z
      y -= Math.Floor(y);                                // OF POINT IN CUBE.
      z -= Math.Floor(z);
      double u = fade(x),                                // COMPUTE FADE CURVES
             v = fade(y),                                // FOR EACH OF X,Y,Z.
             w = fade(z);
      int A = p[X  ]+Y, AA = p[A]+Z, AB = p[A+1]+Z,      // HASH COORDINATES OF
          B = p[X+1]+Y, BA = p[B]+Z, BB = p[B+1]+Z;      // THE 8 CUBE CORNERS,

      return lerp(w, lerp(v, lerp(u, grad(p[AA], x, y, z),  // AND ADD
                                     grad(p[BA], x - 1, y, z)), // BLENDED
                             lerp(u, grad(p[AB], x, y - 1, z),  // RESULTS
                                     grad(p[BB], x - 1, y - 1, z))),// FROM  8
                     lerp(v, lerp(u, grad(p[AA + 1], x, y, z - 1),  // CORNERS
                                     grad(p[BA + 1], x - 1, y, z - 1)), // OF CUBE
                             lerp(u, grad(p[AB + 1], x, y - 1, z - 1),
                                     grad(p[BB + 1], x - 1, y - 1, z - 1))));
    }

    static double fade (double t) { return t * t * t * (t * (t * 6 - 15) + 10); }

    static double lerp (double t, double a, double b) { return a + t * (b - a); }

    static double grad (int hash, double x, double y, double z)
    {
      int h = hash & 15;                      // CONVERT LO 4 BITS OF HASH CODE
      double u = h<8 ? x : y,                 // INTO 12 GRADIENT DIRECTIONS.
             v = h<4 ? y : h==12||h==14 ? x : z;
      return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

  }

  public class Wood : ITexture
  {
    /// <summary>
    /// Primary color used in texture.
    /// </summary>
    double[] Color1 = new double[] { 240 / 255f, 155 / 255f, 76 / 255f };

    /// <summary>
    /// Secondary color used in texture.
    /// </summary>
    double[] Color2 = new double[] { 140 / 255f, 47 / 255f, 14 / 255f };

    /// <summary>
    /// Frequency of the rings.
    /// </summary>
    double frequency = 40;

    /// <summary>
    /// Ring deformation in the u-direction.
    /// </summary>
    double uDef = 0.25;

    /// <summary>
    /// Ring deformation in the v-direction.
    /// </summary>
    double vDef = 0.3;

    /// <summary>
    /// Ring deformation in the w-direction.
    /// </summary>
    double wDef = 0.1;

    /// <summary>
    /// Maximum depth of fractaling.
    /// </summary>
    int maxDepth = 512;

    public Wood () { }

    public Wood (double freq, double cNew, double dNew, double eNew, int dep, double[] col1, double[] col2)
    {
      Color1 = col1;
      Color2 = col2;
      frequency = freq;
      uDef = cNew;
      vDef = dNew;
      wDef = eNew;
      maxDepth = dep;
    }


    public long Apply (Intersection inter)
    {
      double u = inter.CoordWorld.X;
      double v = inter.CoordWorld.Y;
      double w = inter.CoordWorld.Z;

      double value = 0;
      double depth = maxDepth;

      while (depth >= 1)
      {
        value += PerlinNoise.MakeSomeNoise(u * uDef, v * vDef, w * wDef) * (1 / depth);
        depth /= 2.0;
      }

      double pom = Math.Sqrt(Math.Pow(u,2) + Math.Pow(v,2) + Math.Pow(w,2)) + value;

      double sine = Math.Sin(frequency * pom);
      if (sine < 0)
        sine = -sine;

      for (int i = 0; i < inter.SurfaceColor.Length; i++)
      {
        inter.SurfaceColor[i] = sine * Color1[i] + (1 - sine) * Color2[i];
      }

      Random r = new Random();
      return (long)(r.NextDouble());
    }
  }

  public class Marble : ITexture
  {

    /// <summary>
    /// Primary color used in texture.
    /// </summary>
    public double[] Color1 = new double[] {0.3,0.3, 0.3};

    /// <summary>
    /// Secondary color used in texture.
    /// </summary>
    public double[] Color2 = new double[] { 0.7, 1, 1 };

    /// <summary>
    /// Frequency in the u-direction.
    /// </summary>
    public double Fu = 0.8;

    /// <summary>
    /// Frequency in the v-direction.
    /// </summary>
    public double Fv = 0.9;

    /// <summary>
    /// Frequency in the w-direction.
    /// </summary>
    public double Fw = 1.2;

    /// <summary>
    /// Maximum depth of fractaling.
    /// </summary>
    int maxDepth = 2048;

    /// <summary>
    /// Diffusion of the veins. It also affects frequency.
    /// </summary>
    double veinDiffusion = 5;

    public Marble () { }

    public Marble (double fu, double fv, double fw, double soak, int dep, double[] col1, double[] col2)
    {
      Fu = fu;
      Fv = fv;
      Fw = fw;
      veinDiffusion = soak;
      maxDepth = dep;
      Color1 = col1;
      Color2 = col2;
    }


    public long Apply (Intersection inter)
    {
      double u = inter.CoordObject.X * Fu;
      double v = inter.CoordObject.Y * Fv;
      double w = inter.CoordObject.Z * Fw;

      double quotient = 0;
      int depth = maxDepth;
      int i = 0;
      while (depth >= 1)
      {
        quotient += (1.0 / depth) * Math.Pow(-1, i) * (PerlinNoise.MakeSomeNoise(u * depth, v * depth, w * depth));
        depth /= 2;
        i++;
      }
      quotient = Math.Sin(veinDiffusion * quotient);

      if (quotient < 0)
        quotient = -quotient;


      for (i = 0; i < inter.SurfaceColor.Length; i++)
      {
        inter.SurfaceColor[i] = Color1[i] * (1 - quotient) + quotient * Color2[i];
      }

      Random r = new Random();
      return (long)(r.NextDouble());
    }
  }

  public class Porcelain : ITexture
  {

    /// <summary>
    /// Primary color of texture.
    /// </summary>
    public double[] Color1 = new double[] { 0.7, 0.7, 0.7 };

    /// <summary>
    /// Secondary color of texture.
    /// </summary>
    public double[] Color2 = new double[] {0.051, 0.051, 0.81 };

    /// <summary>
    /// Maximum depth of fractaling.
    /// </summary>
    int maxDepth = 2048;

    /// <summary>
    /// Diffusion of the veins. It also affects frequency.
    /// </summary>
    double veinDiffusion = 35;

    /// <summary>
    /// Vein frequency.
    /// </summary>
    double frequency = 3;

    public Porcelain () { }

    public Porcelain (double freq, double soak, int dep, double[] col1, double[] col2)
    {
      frequency = freq;
      veinDiffusion = soak;
      maxDepth = dep;
      Color1 = col1;
      Color2 = col2;
    }


    public long Apply (Intersection inter)
    {

      double u = inter.CoordObject.X;
      double v = inter.CoordObject.Y;
      double w = inter.CoordObject.Z;

      double quotient = 0;
      int depth = maxDepth;


      int i = 0;
      while (depth >= 1)
      {
        quotient += (1.0 / depth) * Math.Pow(-1, i) * (PerlinNoise.MakeSomeNoise(u * frequency, v * frequency, w * frequency));
        depth /= 2;
        i++;
      }
      quotient = Math.Sin(veinDiffusion * quotient);

      if (quotient < 0)
        quotient = -quotient;


      for (i = 0; i < inter.SurfaceColor.Length; i++)
      {
        inter.SurfaceColor[i] = Color1[i] * quotient + (1 - quotient) * Color2[i];
      }

      return (long)(quotient);
    }
  }

  public class Flakes : ITexture
  {

    /// <summary>
    /// Primary color of texture.
    /// </summary>
    double[] Color1 = new double[] {0.6, 0, 1 };

    /// <summary>
    /// Secondary color of texture.
    /// </summary>
    double[] Color2 = new double[] { 0.9, 0.9, 0.9 };

    /// <summary>
    /// It affects whether there are any inner cascades in the flakes and their amount.
    /// </summary>
    int cascadity = 300;

    /// <summary>
    /// Fading of the flakes on the inside.
    /// </summary>
    double flakeFade = 100;

    //frekvencia
    double frequency = 5;

    public Flakes () { }

    public Flakes (double freq, double fade, int casc, double[] col1, double[] col2)
    {
      frequency = freq;
      flakeFade = fade;
      cascadity = casc;
      Color1 = col1;
      Color2 = col2;
    }


    public long Apply (Intersection inter)
    {
      double q = PerlinNoise.MakeSomeNoise((inter.CoordWorld.X)*frequency, (inter.CoordWorld.Y)*frequency,(inter.CoordWorld.Z)*frequency );
      int mod = ((int)(flakeFade * q))%cascadity;
      q = (double)mod / cascadity;

      for (int i = 0; i < inter.SurfaceColor.Length; i++)
      {
        if (q < 0)
          q = 1;
        inter.SurfaceColor[i] = Color1[i] * (1 - q) + q * Color2[i];
      }

      return (long)(q - (int)q);
    }
  }

  public class Opal : ITexture
  {

    /// <summary>
    /// Primary color of texture. There might be another colors in the result.
    /// </summary>
    double[] Color2 = new double[] { 0.6, 0.2, 0.8 };

    /// <summary>
    /// Secondary color of texture. There might be another colors in the result.
    /// </summary>
    double[] Color1 = new double[] { 0.3, 0.8, 0.4 };

    /// <summary>
    /// Frequency of the "opal eyes".
    /// </summary>
    double frequency = 1.1;

    /// <summary>
    /// The amount of the "eyes'" diffusion to the surroundings.
    /// </summary>
    double diffusion = 0.015;

    public Opal () { }

    public Opal (double freq, double seg, double[] col1, double[] col2)
    {
      frequency = freq;
      diffusion = seg;
      Color1 = col1;
      Color2 = col2;
    }

    public long Apply (Intersection inter)
    {
      double u = inter.CoordWorld.X;
      double v = inter.CoordWorld.Y;
      double w = inter.CoordWorld.Z;

      double q = PerlinNoise.MakeSomeNoise(u*frequency,v*frequency,w*frequency) + u * v * w * diffusion;

      for (int i = 0; i < inter.SurfaceColor.Length; i++)
      {
        if (q < 0)
          q = 1 / Math.Tan(q);
        else
          q = 1 - 1 / Math.Tan(q);

        inter.SurfaceColor[i] = Color1[i] * (1 - q) + q * Color2[i];
      }

      return (long)(q - (int)q);
    }
  }
}

