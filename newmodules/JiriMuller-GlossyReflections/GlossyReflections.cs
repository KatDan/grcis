using MathSupport;
using OpenTK;
using Rendering;
using System;
using System.Collections.Generic;
using Utilities;

namespace JiriMuller
{
  public class GlossyRaytracing : RayTracing
  {
    public GlossyRaytracing () : base()
    {
    }

    RandomJames rj = new RandomJames();

    protected override long shade (int depth, double importance, ref Vector3d p0, ref Vector3d p1, double[] color)
    {
      Vector3d direction = p1;

      int bands = color.Length;
      LinkedList<Intersection> intersections = MT.scene.Intersectable.Intersect(p0, p1);

      // If the ray is primary, increment both counters
      Statistics.IncrementRaysCounters(1, depth == 0);

      Intersection i = Intersection.FirstIntersection(intersections, ref p1);

      if (i == null)
      {
        // No intersection -> background color
        rayRegisterer?.RegisterRay(AbstractRayRegisterer.RayType.rayVisualizerNormal, depth, p0, direction * 100000);

        return MT.scene.Background.GetColor(p1, color);
      }

      // There was at least one intersection
      i.Complete();

      rayRegisterer?.RegisterRay(AbstractRayRegisterer.RayType.unknown, depth, p0, i);

      // Hash code for adaptive supersampling
      long hash = i.Solid.GetHashCode();

      // Apply all the textures first
      if (i.Textures != null)
        foreach (ITexture tex in i.Textures)
          hash = hash * HASH_TEXTURE + tex.Apply(i);

      if (MT.pointCloudCheckBox && !MT.pointCloudSavingInProgress && !MT.singleRayTracing)
      {
        foreach (Intersection intersection in intersections)
        {
          if (!intersection.completed)
            intersection.Complete();

          if (intersection.Textures != null && !intersection.textureApplied)
            foreach (ITexture tex in intersection.Textures)
              tex.Apply(intersection);

          double[] vertexColor = new double[3];
          Util.ColorCopy(intersection.SurfaceColor, vertexColor);
          Master.singleton?.pointCloud?.AddToPointCloud(intersection.CoordWorld, vertexColor, intersection.Normal, MT.threadID);
        }
      }

      // Color accumulation.
      Array.Clear(color, 0, bands);
      double[] comp = new double[bands];

      // Optional override ray-processing (procedural).
      if (DoRecursion &&
          i.Solid?.GetAttribute(PropertyName.RECURSION) is RecursionFunction rf)
      {
        hash += HASH_RECURSION * rf(i, p1, importance, out RayRecursion rr);

        if (rr != null)
        {
          // Direct contribution.
          if (rr.DirectContribution != null &&
              rr.DirectContribution.Length > 0)
            if (rr.DirectContribution.Length == 1)
              Util.ColorAdd(rr.DirectContribution[0], color);
            else
              Util.ColorAdd(rr.DirectContribution, color);

          // Recursive rays.
          if (rr.Rays != null &&
              depth++ < MaxLevel)
            foreach (var ray in rr.Rays)
            {
              RayRecursion.RayContribution rc = ray;
              hash += HASH_REFLECT * shade(depth, rc.importance, ref rc.origin, ref rc.direction, comp);

              // Combine colors.
              if (ray.coefficient == null)
                Util.ColorAdd(comp, color);
              else
              if (ray.coefficient.Length == 1)
                Util.ColorAdd(comp, ray.coefficient[0], color);
              else
                Util.ColorAdd(comp, ray.coefficient, color);
            }

          return hash;
        }
      }

      // Default (Whitted) ray-tracing interaction (lights [+ reflection] [+ refraction]).
      p1 = -p1; // viewing vector
      p1.Normalize();

      if (MT.scene.Sources == null || MT.scene.Sources.Count < 1)
        // No light sources at all.
        Util.ColorAdd(i.SurfaceColor, color);
      else
      {
        // Apply the reflectance model for each source.
        i.Material = (IMaterial)i.Material.Clone();
        i.Material.Color = i.SurfaceColor;

        foreach (ILightSource source in MT.scene.Sources)
        {
          double[] intensity = source.GetIntensity(i, out Vector3d dir);

          if (MT.singleRayTracing && source.position != null)
            // Register shadow ray for RayVisualizer.
            rayRegisterer?.RegisterRay(AbstractRayRegisterer.RayType.rayVisualizerShadow, i.CoordWorld, (Vector3d)source.position);

          if (intensity != null)
          {
            if (DoShadows && dir != Vector3d.Zero)
            {
              intersections = MT.scene.Intersectable.Intersect(i.CoordWorld, dir);
              Statistics.allRaysCount++;
              Intersection si = Intersection.FirstRealIntersection(intersections, ref dir);
              // Better shadow testing: intersection between 0.0 and 1.0 kills the lighting.
              if (si != null && !si.Far(1.0, ref dir))
                continue;
            }

            double[] reflection = i.ReflectanceModel.ColorReflection(i, dir, p1, ReflectionComponent.ALL);
            if (reflection != null)
            {
              Util.ColorAdd(intensity, reflection, color);
              hash = hash * HASH_LIGHT + source.GetHashCode();
            }
          }
        }
      }

      // Check the recursion depth.
      if (depth++ >= MaxLevel || !DoReflections && !DoRefractions)
        // No further recursion.
        return hash;

      Vector3d r;
      double maxK;
      double newImportance;

      if (DoReflections)
      {
        // Shooting a reflected ray.
        Geometry.SpecularReflection(ref i.Normal, ref p1, out r);

        // This is all what we changed in this class:
        // only apply on objects using Phong material, otherwise skip
        if  (i.ReflectanceModel is PhongModel && i.Material is PhongMaterial m)
        {
          RandomizeReflectionVector(ref r, m.H);
        }
        // end of change

        double[] ks = i.ReflectanceModel.ColorReflection(i, p1, r, ReflectionComponent.SPECULAR_REFLECTION);
        if (ks != null)
        {
          maxK = ks[0];
          for (int b = 1; b < bands; b++)
            if (ks[b] > maxK)
              maxK = ks[b];

          newImportance = importance * maxK;
          if (newImportance >= MinImportance)
          {
            // Do compute the reflected ray.
            hash += HASH_REFLECT * shade(depth, newImportance, ref i.CoordWorld, ref r, comp);
            Util.ColorAdd(comp, ks, color);
          }
        }
      }

      if (DoRefractions)
      {
        // Shooting a refracted ray.
        maxK = i.Material.Kt;   // simple solution - no influence of reflectance model yet
        newImportance = importance * maxK;
        if (newImportance < MinImportance)
          return hash;

        // Refracted ray.
        if ((r = Geometry.SpecularRefraction(i.Normal, i.Material.n, p1)) == Vector3d.Zero)
          return hash;

        hash += HASH_REFRACT * shade(depth, newImportance, ref i.CoordWorld, ref r, comp);
        Util.ColorAdd(comp, maxK, color);
      }

      return hash;
    }

    /// <summary>
    /// Changes input vector to be slightly rotated proportionally to "cosine lobe".
    /// </summary>
    /// <param name="reflectionVector">Original vector to be adjusted</param>
    /// <param name="Hfactor">Specular factor</param>
    protected void RandomizeReflectionVector(ref Vector3d reflectionVector, double Hfactor)
    {
      // generate new coordinate matrix to transform a vector so "up" direction matces reflectionVector
      Vector3d v = Vector3d.UnitX;
      Vector3d rn = reflectionVector.Normalized();
      if (Vector3d.Dot(v, rn) > 0.9)
        v = Vector3d.UnitY;
      Vector3d fwd = Vector3d.Cross(reflectionVector, v);
      Vector3d right = Vector3d.Cross(fwd, reflectionVector);

      Matrix4d m = new Matrix4d(new Vector4d(right), new Vector4d(reflectionVector), new Vector4d(fwd), new Vector4d(0, 0, 0, 1));

      // create new random vector pointing to upper hemisphere
      double r1, r2;
      lock (rj)
      {
        r1 = rj.UniformNumber();
        r2 = rj.UniformNumber();
      }

      // according to https://people.cs.kuleuven.be/~philip.dutre/GI/TotalCompendium.pdf
      double x = Math.Cos(MathHelper.TwoPi * r1) * Math.Sqrt(1 - Math.Pow(r2, 2 / (Hfactor + 1)));
      double y = Math.Sin(MathHelper.TwoPi * r1) * Math.Sqrt(1 - Math.Pow(r2, 2 / (Hfactor + 1)));
      double z = Math.Pow(r2, 1 / (Hfactor + 1));

      Vector3d randomVector = new Vector3d(x, z, y);

      // rotate random vector so the normal point in the direction of original vector
      reflectionVector = Vector3d.TransformVector(randomVector, m);
    }
  }
}
