using System.Collections.Generic;
using OpenTK;
using Rendering;

namespace JiriMuller
{
  public class MetaballsContainer : DefaultSceneNode, ISolid
  {
    /// <summary>
    /// List of all metaballs inside this container
    /// </summary>
    List<Metaball> Metaballs { get; } = new List<Metaball>();

    /// <summary>
    /// Rough step when searching for intersection
    /// </summary>
    public double InchStep { get; set; } = 0.01;

    /// <summary>
    /// How precise we want to pinpoint actual intersection point
    /// </summary>
    public double PrecisionInchStep { get; set; } = 0.0001;

    /// <summary>
    /// Metaballs potential threshold
    /// </summary>
    public double Threshold { get; }

    public MetaballsContainer (double threshold)
    {
      Threshold = threshold;
    }

    public void AddMetaball (Metaball metaball, Vector3d translation)
    {
      metaball.ToParentTranslation = translation;
      metaball.FromParentTranslation = -translation;
      Metaballs.Add(metaball);
    }

    public void GetBoundingBox (out Vector3d corner1, out Vector3d corner2)
    {
      if (Metaballs.Count == 0)
      {
        corner1 = Vector3d.Zero;
        corner2 = Vector3d.Zero;
        return;
      }
      corner1 = new Vector3d(double.PositiveInfinity);
      corner2 = new Vector3d(double.NegativeInfinity);

      foreach (Metaball m in Metaballs)
      {
        corner1 = Vector3d.ComponentMin(corner1, m.ToParentTranslation - new Vector3d(m.MaxRadius));
        corner2 = Vector3d.ComponentMax(corner2, m.ToParentTranslation + new Vector3d(m.MaxRadius));
      }
    }

    public override LinkedList<Intersection> Intersect (Vector3d p0, Vector3d p1)
    {
      // first obtain all metaball candidates for intersection
      List<IntersectionRecord> intersections = GetPossibleIntersections(p0, p1);
      if (intersections.Count == 0)
        return null;  // no intersections

      // sort all intersections
      intersections.Sort((a, b) => a.Intersection.T.CompareTo(b.Intersection.T));

      // Step through the ray to find real intersections
      LinkedList<Intersection> l = new LinkedList<Intersection>();
      BoundingSpheresCheckIntersections(fillList: l, intersections, p0, p1);

      return l;

    }

    /// <summary>
    /// Step along the ray and find intersections, keep a set of active "potential" metaballs,
    /// and only step through a space inside bounding spheres.
    /// When we step from through surface, we then pinpoint the intersection precisely using binary search
    /// </summary>
    /// <param name="fillList">Linked list to be filed with actual intersections</param>
    /// <param name="metaballsIntersections">List of found bounding spheres intersected by the ray, sorted from nearest to furthest</param>
    /// <param name="p0">Ray origin</param>
    /// <param name="p1">Ray direction</param>
    protected void BoundingSpheresCheckIntersections (LinkedList<Intersection> fillList, List<IntersectionRecord> metaballsIntersections, Vector3d p0, Vector3d p1)
    {
      HashSet<Metaball> activeMetaballs = new HashSet<Metaball>();
      double? previous = null;
      for (int ii = 0; ii < metaballsIntersections.Count - 1; ii++)
      {
        IntersectionRecord ir = metaballsIntersections[ii];
        if (!ir.Intersection.Enter)
        {
          activeMetaballs.Remove(ir.Metaball);
        }
        else
        {
          activeMetaballs.Add(ir.Metaball);
        }
        if (activeMetaballs.Count == 0)
          continue; // No need to check space between bounding boxes

        double t = System.Math.Max(ir.Intersection.T, InchStep); // In case ray starts on surface/inside metaball, only consider t > delta
        double nextT = metaballsIntersections[ii + 1].Intersection.T;
        if (t >= nextT)
          continue;

        while (t < nextT)
        {
          Vector3d pos = p0 + p1 * t;
          double value = ComputeTotalValueAtPosition(pos);
          if (value > Threshold && previous.HasValue && previous.Value < Threshold)
          {
            // we reached solid enter with rought inch step, now find precise surface position with binary search
            BinarySearchForIntersectionPoint(p0, p1, t, value, enter: true, out double tPrecise, out pos, out double newValue);

            Intersection intersection = GetIntersectionInfo(pos, tPrecise, enter: true, newValue, activeMetaballs);
            fillList.AddLast(intersection);
          }
          else if (value < Threshold && previous.HasValue && previous.Value > Threshold)
          {
            // same as with enter, using binary search
            BinarySearchForIntersectionPoint(p0, p1, t, value, enter: false, out double tPrecise, out pos, out double newValue);

            Intersection intersection = GetIntersectionInfo(pos, tPrecise, enter: false, newValue, activeMetaballs);
            fillList.AddLast(intersection);
          }

          t += InchStep;
          previous = value;
        }
      }
    }

    protected void BinarySearchForIntersectionPoint (Vector3d p0, Vector3d p1, double tRough, double originalValue, bool enter, out double tPrecise, out Vector3d position, out double value)
    {
      double precisionStep = InchStep / 2;
      double precisionCorrection = -precisionStep;

      tPrecise = tRough;
      position = p0 + p1 * tRough;
      value = originalValue;
      while (precisionStep >= PrecisionInchStep)
      {
        tPrecise = tRough + precisionCorrection;
        position = p0 + p1 * tPrecise;
        value = ComputeTotalValueAtPosition(position);

        precisionStep /= 2;
        if (!((value > Threshold) ^ enter))
        {
          precisionCorrection -= precisionStep;
        }
        else
        {
          precisionCorrection += precisionStep;
        }
      }
    }

    protected Intersection GetIntersectionInfo (Vector3d position, double t, bool enter, double totalValue, HashSet<Metaball> nearMetaballs)
    {
      Vector3d normal = Vector3d.Zero;
      foreach (var m in nearMetaballs)
      {
        /*
          Computation according to http://www.geisswerks.com/ryan/BLOBS/blobs.html
          This normal is INCORRECT, it might look good at first glance, but when more blobs interact, things get weird
       */
        //Vector3d v = position - m.ToParentTranslation;
        //normal += v.Normalized() * m.GetValueAtPosition(position + m.FromParentTranslation);

        /*
          Normal computed from gradient
        */
        normal += m.GetGradientAtPosition(position + m.FromParentTranslation);
      }

      return new Intersection(this) { T = t, Enter = enter, Front = enter, NormalLocal = normal, CoordLocal = position };
    }

    /// <summary>
    /// Gets a list of metaballs which we intersected their bounding spheres. 
    /// </summary>
    /// <param name="p0">Ray origin</param>
    /// <param name="p1">Ray direction</param>
    /// <returns>Enter and exit records for each intersected metaball's bounding sphere</returns>
    protected List<IntersectionRecord> GetPossibleIntersections (Vector3d p0, Vector3d p1)
    {
      List<IntersectionRecord> intersections = new List<IntersectionRecord>();

      LinkedList<Intersection> metaballsIntersections;
      foreach (Metaball m in Metaballs)
      {
        Vector3d pos = p0 + m.FromParentTranslation;
        Vector3d dir = p1;

        metaballsIntersections = m.IntersectBoundingSphere(pos, dir);
        if (metaballsIntersections != null)
        {
          foreach (Intersection i in metaballsIntersections)
          {
            intersections.Add(new IntersectionRecord() { Intersection = i, Metaball = m });
          }
        }
      }

      return intersections;
    }


    protected double ComputeTotalValueAtPosition (Vector3d position)
    {
      double sum = 0;
      foreach (Metaball m in Metaballs)
      {
        Vector3d pos = position + m.FromParentTranslation;
        sum += m.GetValueAtPosition(pos);
      }
      return sum;
    }

    public override void CompleteIntersection (Intersection inter)
    {
      inter.TextureCoord.X = inter.CoordLocal.X;
      inter.TextureCoord.Y = inter.CoordLocal.Z;
    }
  }


  public struct IntersectionRecord
  {
    public Intersection Intersection;
    public Metaball Metaball;
  }

  public class Metaball
  {
    public BlobFunction MetaballFunction { get; }
    public GradientFunction GradientFunction { get; }

    /// <summary>
    /// There exists class BoundingSphere, but that only has
    /// IBoundingVolume.Intersection returning information
    /// about first intersection, we want both intersections
    /// </summary>
    public Sphere BoundingSphere { get; }
    public double MaxRadius { get; }

    public Metaball (BlobFunction basisFunction, GradientFunction gradientFunction, double maxRadius)
    {
      MetaballFunction = basisFunction;
      GradientFunction = gradientFunction;
      MaxRadius = maxRadius;
      BoundingSphere = new Sphere();

      BoundingSphere.ToParent = Matrix4d.Scale(maxRadius);
      BoundingSphere.FromParent = BoundingSphere.ToParent.Inverted();
    }

    //public Matrix4d FromParent { get; set; }

    //public Matrix4d ToParent { get; set; }

    public Vector3d ToParentTranslation { get; set; }
    public Vector3d FromParentTranslation { get; set; }

    public LinkedList<Intersection> IntersectBoundingSphere (Vector3d p0, Vector3d p1)
    {
      p0 = Vector3d.TransformPosition(p0, BoundingSphere.FromParent);
      p1 = Vector3d.TransformVector(p1, BoundingSphere.FromParent);
      return BoundingSphere.Intersect(p0, p1);
    }

    public double GetValueAtPosition (Vector3d position)
    {
      return MetaballFunction(position);
    }
    public Vector3d GetGradientAtPosition (Vector3d position)
    {
      return GradientFunction(position);
    }
  }

  public delegate double BlobFunction (Vector3d coords);
  public delegate Vector3d GradientFunction (Vector3d coords);
}
