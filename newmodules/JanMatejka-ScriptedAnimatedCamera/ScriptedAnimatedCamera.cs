using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;

namespace Rendering
{
  namespace JanMatejka
  {
    public class ScriptedAnimatedCamera : StaticCamera, ITimeDependent
    {
      public ScriptedAnimatedCamera (Vector3d lookat, Vector3d cen, double ang)
        : base(cen, lookat - cen, ang)
      {
        // Defaults:
        lookAt = lookat;
        center0 = cen;
        Start = 0.0;
        End = 10.0;
        time = 0.0;

        // Catmull-Rom interpolation loading
        pointsPerSegment = 75;
        timeDiff = 0d;

        // Interpolate
        interpolated = CatmullRomInterpolation(new List<Vector3d>
        {
          new Vector3d(3.0f, 0.0f, 3.0f), new Vector3d(3.0f, 2.0f, 5.0f), new Vector3d(1.0f, 5.0f, 6.0f), new Vector3d(0.0f, 3.0f, 10.0f),
          new Vector3d(-2.0f, -3.0f, 8.0f), new Vector3d(-3.0f, -1.0f, 2.0f)
        },
          pointsPerSegment);

        timeDiff = (End - Start) / interpolated.Count;
      }

      /// <summary>
      /// Clone all the time-dependent components, share the others.
      /// </summary>
      /// <returns></returns>
      public virtual object Clone ()
      {
        ScriptedAnimatedCamera c = new ScriptedAnimatedCamera( lookAt, center0, MathHelper.RadiansToDegrees( (float)hAngle ) );
        c.Start = Start;
        c.End = End;
        c.Time = Time;
        c.timeDiff = (c.End - c.Start) / c.interpolated.Count;
        return c;
      }

      /// <summary>
      /// Starting (minimal) time in seconds.
      /// </summary>
      public double Start
      {
        get;
        set;
      }

      /// <summary>
      /// Ending (maximal) time in seconds.
      /// </summary>
      public double End
      {
        get;
        set;
      }

      /// <summary>
      /// Current time in seconds.
      /// </summary>
      public double Time
      {
        get
        {
          return time;
        }
        set
        {
          setTime(value);
        }
      }

      protected double time;

      protected virtual void setTime (double newTime)
      {
        Debug.Assert(Start != End);

        time = newTime;    // Here Start & End define a periodicity, not bounds!

        // change the camera position:
        center = interpolated[GetInterpolatedIndex()];
        direction = lookAt - center;
        direction.Normalize();
        prepare();
      }

      /// <summary>
      /// Central point (to look at).
      /// </summary>
      protected Vector3d lookAt;

      /// <summary>
      /// Center for time == Start;
      /// </summary>
      protected Vector3d center0;

      /// <summary>
      /// Number of points to be generated for each segment (in Catmull-Rom interpolation)
      /// </summary>
      protected int pointsPerSegment;

      /// <summary>
      /// List of interpolated points
      /// </summary>
      protected List<Vector3d> interpolated;

      /// <summary>
      /// Time in seconds after which should the camera jump to new point
      /// </summary>
      protected double timeDiff;

      /// <summary>
      /// Returns an index to the interpolated points list according to the Time.
      /// </summary>
      protected int GetInterpolatedIndex ()
      {
        int ret = (int)Math.Round(Time/timeDiff);
        if (ret >= interpolated.Count)
        {
          ret = 0;
          Time = Start;
        }
        return ret;
      }

      /// <summary>
      /// Computes a list of points using a Catmull-Rom interpolation.
      /// </summary>
      /// <param name="points">List of key points.</param>
      /// <param name="pointsPerSegment">Number of points to be generated for each segment (between two key points).</param>
      /// <returns>List of interpolated points.</returns>
      private List<Vector3d> CatmullRomInterpolation (List<Vector3d> points, int pointsPerSegment)
      {
        if (points.Count < 4)
          throw new ArgumentException("Atleast 4 points are necessary!");

        List<Vector3d> ret = new List<Vector3d>();

        float distJumps = 1.0f / pointsPerSegment;  // Determines how many times does the second for cycle loop

        for (int i = 1; i < points.Count - 2; i++)
        {
          ret.Add(points[i]); // Starting point

          for (float dist = distJumps; dist < 1.0f; dist += distJumps)
          {
            // Interpolated point between start and end points
            ret.Add(GetInterpolatedPoint(new List<Vector3d> { points[i - 1], points[i], points[i + 1], points[i + 2] }, dist));
          }

          ret.Add(points[i + 1]); // End point
        }

        return ret;
      }

      protected static Matrix4 CatmullRomMatrix = new Matrix4(new Vector4(0f, 2f, 0f, 0f),
                                                            new Vector4(-1f, 0f, 1f, 0f),
                                                            new Vector4(2f, -5f, 4f, -1f),
                                                            new Vector4(-1f, 3f, -3f, 1f));

      /// <summary>
      /// Computes one interpolated point.
      /// </summary>
      /// <param name="points">List of 4 points defining a segment.</param>
      /// <param name="dist">Normalized distance from 0.0 to 1.0.</param>
      /// <returns>An interpolated point.</returns>
      private Vector3d GetInterpolatedPoint (List<Vector3d> points, float dist)
      {
        if (points.Count < 4)
          throw new ArgumentException("Atleast 4 points are necessary!");

        Vector3d newPoint = new Vector3d();

        newPoint.X = (CatmullRomMatrix.M11 * points[0].X + CatmullRomMatrix.M12 * points[1].X + CatmullRomMatrix.M13 * points[2].X + CatmullRomMatrix.M14 * points[3].X) +
                    ((CatmullRomMatrix.M21 * points[0].X + CatmullRomMatrix.M22 * points[1].X + CatmullRomMatrix.M23 * points[2].X + CatmullRomMatrix.M24 * points[3].X) * dist) +
                    ((CatmullRomMatrix.M31 * points[0].X + CatmullRomMatrix.M32 * points[1].X + CatmullRomMatrix.M33 * points[2].X + CatmullRomMatrix.M34 * points[3].X) * dist * dist) +
                    ((CatmullRomMatrix.M41 * points[0].X + CatmullRomMatrix.M42 * points[1].X + CatmullRomMatrix.M43 * points[2].X + CatmullRomMatrix.M44 * points[3].X) * dist * dist * dist);

        newPoint.Y = (CatmullRomMatrix.M11 * points[0].Y + CatmullRomMatrix.M12 * points[1].Y + CatmullRomMatrix.M13 * points[2].Y + CatmullRomMatrix.M14 * points[3].Y) +
                    ((CatmullRomMatrix.M21 * points[0].Y + CatmullRomMatrix.M22 * points[1].Y + CatmullRomMatrix.M23 * points[2].Y + CatmullRomMatrix.M24 * points[3].Y) * dist) +
                    ((CatmullRomMatrix.M31 * points[0].Y + CatmullRomMatrix.M32 * points[1].Y + CatmullRomMatrix.M33 * points[2].Y + CatmullRomMatrix.M34 * points[3].Y) * dist * dist) +
                    ((CatmullRomMatrix.M41 * points[0].Y + CatmullRomMatrix.M42 * points[1].Y + CatmullRomMatrix.M43 * points[2].Y + CatmullRomMatrix.M44 * points[3].Y) * dist * dist * dist);

        newPoint.Z = (CatmullRomMatrix.M11 * points[0].Z + CatmullRomMatrix.M12 * points[1].Z + CatmullRomMatrix.M13 * points[2].Z + CatmullRomMatrix.M14 * points[3].Z) +
                    ((CatmullRomMatrix.M21 * points[0].Z + CatmullRomMatrix.M22 * points[1].Z + CatmullRomMatrix.M23 * points[2].Z + CatmullRomMatrix.M24 * points[3].Z) * dist) +
                    ((CatmullRomMatrix.M31 * points[0].Z + CatmullRomMatrix.M32 * points[1].Z + CatmullRomMatrix.M33 * points[2].Z + CatmullRomMatrix.M34 * points[3].Z) * dist * dist) +
                    ((CatmullRomMatrix.M41 * points[0].Z + CatmullRomMatrix.M42 * points[1].Z + CatmullRomMatrix.M43 * points[2].Z + CatmullRomMatrix.M44 * points[3].Z) * dist * dist * dist);

        newPoint.X *= 0.5f;
        newPoint.Y *= 0.5f;
        newPoint.Z *= 0.5f;

        return newPoint;
      }

#if DEBUG
      /// <summary>
      /// Debugging - tracking of object instances/clones.
      /// </summary>
      public int getSerial ()
      {
        return 0;
      }
#endif
    }
  }
}
