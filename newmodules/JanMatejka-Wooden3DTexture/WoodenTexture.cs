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
    /// Simple 3D texture simulating wood using Perlin Noise in 3 dimensions.
    /// </summary>
    [Serializable]
    public class WoodenTexture : ITexture
    {
      public WoodenTexture (int seed = 1337)
      {
        perlinNoise = new PerlinNoise(seed);
      }

      public WoodenTexture (double lineFrequency, int octaves, double persistence, double distortion, int seed = 1337)
      {
        this.LineFrequency = lineFrequency;
        this.Octaves = octaves;
        this.Persistence = persistence;
        this.Distortion = distortion;
        perlinNoise = new PerlinNoise(seed);
      }

      public WoodenTexture (double[] firstColor, double[] secondColor, int seed = 1337)
      {
        Array.Copy(firstColor, this.FirstColor, this.FirstColor.Length);
        Array.Copy(secondColor, this.SecondColor, this.SecondColor.Length);
        perlinNoise = new PerlinNoise(seed);
      }

      public WoodenTexture (double[] firstColor, double[] secondColor, double lineFrequency, int octaves, double persistence, double distortion, int seed = 1337)
      {
        Array.Copy(firstColor, this.FirstColor, this.FirstColor.Length);
        Array.Copy(secondColor, this.SecondColor, this.SecondColor.Length);
        this.LineFrequency = lineFrequency;
        this.Octaves = octaves;
        this.Persistence = persistence;
        this.Distortion = distortion;
        perlinNoise = new PerlinNoise(seed);
      }

      /// <summary>
      /// Apply the relevant value-modulation in the given Intersection instance.
      /// Simple variant, w/o an integration support.
      /// </summary>
      /// <param name="inter">Data object to modify.</param>
      /// <returns>Hash value (texture signature) for adaptive subsampling.</returns>
      public virtual long Apply (Intersection inter)
      {
        var noise = perlinNoise.OctavesNoise(inter.CoordLocal.X, inter.CoordLocal.Y, inter.CoordLocal.Z, Octaves, Persistence);
        var distance = Math.Sqrt(inter.CoordLocal.X * inter.CoordLocal.X + inter.CoordLocal.Y * inter.CoordLocal.Y)
          + Math.Sin(noise)*Distortion;
        var result = distance / 0.60;
        var sine = Math.Sin(result*LineFrequency);
        FinalColor[0] = FirstColor[0] + sine * (SecondColor[0] - FirstColor[0]);
        FinalColor[1] = FirstColor[1] + sine * (SecondColor[1] - FirstColor[1]);
        FinalColor[2] = FirstColor[2] + sine * (SecondColor[2] - FirstColor[2]);

        Util.ColorCopy(FinalColor, inter.SurfaceColor);

        inter.textureApplied = true; // warning - this changes textureApplied bool even when only one texture was applied - not all of them

        return (long)RandomStatic.numericRecipes((ulong)(noise * 100000000000000000));
      }

      private double[] FirstColor = new double[] {0.5294, 0.2706, 0.1412 };
      private double[] SecondColor = new double[] {0.1922, 0.1176, 0.0549 };
      private double[] FinalColor = new double[3];
      private double LineFrequency = 50d;
      private int Octaves = 8;
      private double Persistence = 0.7d;
      private double Distortion = 0.1d;
      private PerlinNoise perlinNoise;
    }
  }
}
